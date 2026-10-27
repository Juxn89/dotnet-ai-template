// The API's contract, as the .NET side serialises it (camelCase, enums as names, decimals as numbers, dates ISO).

export type Summary = { summary: string; tags: string[]; createdAt: string; costUsd?: number | null };

export type Note = { id: string; title: string; body: string; createdAt: string; summary: Summary | null };

export type AiUsage = { spentTodayUsd: number; callsToday: number; dailyBudgetUsd: number };

/** A request the API refused, with its stable code (docs/errors.md). */
export class ApiProblem extends Error {
	constructor(
		readonly status: number,
		readonly code: string,
		message: string,
	) {
		super(message);
	}
}

const base = "/api/v1";

async function send<T>(path: string, init?: RequestInit): Promise<T> {
	const response = await fetch(`${base}${path}`, { cache: "no-store", ...init });
	if (!response.ok) {
		const problem = (await response.json().catch(() => null)) as { code?: string; title?: string } | null;
		throw new ApiProblem(response.status, problem?.code ?? "unknown", problem?.title ?? response.statusText);
	}

	return (await response.json()) as T;
}

export const api = {
	notes: () => send<Note[]>("/notes"),
	create: (title: string, body: string) =>
		send<Note>("/notes", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ title, body }) }),
	summarize: (id: string) => send<Summary>(`/notes/${id}/summary`, { method: "POST" }),
	usage: () => send<AiUsage>("/ai/usage"),
};
