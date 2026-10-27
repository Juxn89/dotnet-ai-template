"use client";

import { useCallback, useEffect, useState } from "react";
import { AiSpend } from "@/components/AiSpend";
import { NoteCard } from "@/components/NoteCard";
import { NoteForm } from "@/components/NoteForm";
import { api, type AiUsage, type Note } from "@/lib/api";
import { useI18n } from "@/lib/i18n";

/** The page: today's AI spend, a form for a new note, and the notes, newest first. */
export function Notes() {
	const { t } = useI18n();
	const [notes, setNotes] = useState<Note[] | null>(null);
	const [usage, setUsage] = useState<AiUsage | null>(null);

	const refresh = useCallback(async () => {
		const [list, spend] = await Promise.all([api.notes(), api.usage()]);
		setNotes(list);
		setUsage(spend);
	}, []);

	useEffect(() => {
		// Loading on mount is the effect's job; the state it sets comes from the server.
		// eslint-disable-next-line react-hooks/set-state-in-effect
		void refresh();
	}, [refresh]);

	return (
		<div className="space-y-8 pt-8">
			<section className="space-y-2">
				<h1 className="text-2xl font-bold tracking-tight">{t.app.title}</h1>
				<p className="max-w-2xl text-muted">{t.app.tagline}</p>
			</section>

			{usage ? <AiSpend usage={usage} /> : null}

			<div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.4fr)]">
				<NoteForm onCreated={() => void refresh()} />
				<section aria-labelledby="notes" className="space-y-3">
					<h2 id="notes" className="text-lg font-semibold">
						{t.notes.title}
					</h2>
					{notes === null ? (
						<p className="text-muted">{t.notes.loading}</p>
					) : notes.length === 0 ? (
						<p className="text-muted">{t.notes.empty}</p>
					) : (
						notes.map((note) => <NoteCard key={note.id} note={note} onSummarized={() => void refresh()} />)
					)}
				</section>
			</div>
		</div>
	);
}
