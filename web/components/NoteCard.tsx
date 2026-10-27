"use client";

import { Sparkles } from "lucide-react";
import { useState } from "react";
import { api, ApiProblem, type Note } from "@/lib/api";
import { formatUsd } from "@/lib/format";
import { useI18n } from "@/lib/i18n";

/** A note, and its summary by the model: on demand, with its tags and what it cost. */
export function NoteCard({ note, onSummarized }: { note: Note; onSummarized: () => void }) {
	const { locale, t } = useI18n();
	const [busy, setBusy] = useState(false);
	const [cost, setCost] = useState<number | null>(null);
	const [error, setError] = useState<string | null>(null);

	const summarize = async () => {
		setBusy(true);
		setError(null);
		try {
			const summary = await api.summarize(note.id);
			setCost(summary.costUsd ?? 0);
			onSummarized();
		} catch (problem) {
			setError(t.errors[problem instanceof ApiProblem ? problem.code : "unknown"] ?? t.errors.unknown!);
		} finally {
			setBusy(false);
		}
	};

	return (
		<article aria-label={note.title} className="space-y-3 rounded-xl border border-line bg-card p-4">
			<div className="space-y-1">
				<h3 className="font-semibold">{note.title}</h3>
				<p className="whitespace-pre-line text-sm text-muted">{note.body}</p>
			</div>

			{note.summary ? (
				<div className="space-y-2 rounded-lg bg-brand-soft/60 px-3 py-2.5">
					<p className="text-sm">
						<span className="font-semibold">{t.summary.label}: </span>
						{note.summary.summary}
					</p>
					<ul aria-label={t.summary.tags} className="flex flex-wrap gap-1.5">
						{note.summary.tags.map((tag) => (
							<li key={tag} className="rounded-full bg-slate px-2.5 py-0.5 text-xs font-semibold text-white">
								{t.tags[tag] ?? tag}
							</li>
						))}
					</ul>
					{cost !== null ? <p className="tabular text-xs text-muted">{t.summary.cost(formatUsd(cost, locale))}</p> : null}
				</div>
			) : null}

			{error ? (
				<p role="alert" className="rounded-md bg-danger-soft px-3 py-2 text-sm text-danger">
					{error}
				</p>
			) : null}

			<button
				type="button"
				onClick={() => void summarize()}
				disabled={busy}
				className="inline-flex items-center gap-1.5 rounded-md border border-line px-3 py-1.5 text-sm font-medium hover:border-brand hover:text-brand disabled:opacity-50"
			>
				<Sparkles className="size-4" aria-hidden />
				{busy ? t.summary.working : note.summary ? t.summary.again : t.summary.action}
			</button>
		</article>
	);
}
