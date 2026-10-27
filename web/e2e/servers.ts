import type { PlaywrightTestConfig } from "@playwright/test";

// Ports of their own, so a run never lands on the dev servers (5140 and 3040).
const apiPort = 5199;
const webPort = 3041;

export const baseURL = `http://localhost:${webPort}`;

/**
 * The real API (notes, summaries, the meter) and a production build of the web app. Each run gets its own database.
 * @param model How notes are summarized: a script (the end-to-end test, the demo's rehearsal) or the real model.
 */
export function servers(model: Record<string, string>): PlaywrightTestConfig["webServer"] {
	const run = (process.env.E2E_RUN ??= `${Date.now()}`);
	const database = process.env.E2E_DATABASE ?? `Host=localhost;Port=5434;Username=postgres;Password=postgres;Database=app_e2e_${run}`;

	return [
		{
			command: "dotnet run --project ../src/App.Api --no-launch-profile",
			url: `http://localhost:${apiPort}/health`,
			env: {
				ASPNETCORE_ENVIRONMENT: "Development",
				ASPNETCORE_URLS: `http://localhost:${apiPort}`,
				ConnectionStrings__appdb: database,
				...model,
			},
			timeout: 180_000,
		},
		{
			// The rewrite to the API is fixed at build time, hence API_URL here.
			command: `pnpm build && pnpm exec next start --port ${webPort}`,
			url: baseURL,
			env: { API_URL: `http://localhost:${apiPort}`, NEXT_TELEMETRY_DISABLED: "1" },
			timeout: 180_000,
		},
	];
}
