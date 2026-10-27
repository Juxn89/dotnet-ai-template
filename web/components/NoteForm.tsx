"use client";

import { Plus } from "lucide-react";
import { useState, type FormEvent } from "react";
import { api, ApiProblem } from "@/lib/api";
import { useI18n } from "@/lib/i18n";

const input = "w-full rounded-md border border-line bg-card px-3 py-2 text-sm focus:border-brand focus:outline-none";

/** A new note: a title and its text. The API's limits are the domain's (120 and 4,000 characters). */
export function NoteForm({ onCreated }: { onCreated: () => void }) {
	const { t } = useI18n();
	const [title, setTitle] = useState("");
	const [body, setBody] = useState("");
	const [error, setError] = useState<string | null>(null);
	const [saving, setSaving] = useState(false);

	const submit = async (event: FormEvent) => {
		event.preventDefault();
		setSaving(true);
		setError(null);
		try {
			await api.create(title, body);
			setTitle("");
			setBody("");
			onCreated();
		} catch (problem) {
			setError(t.errors[problem instanceof ApiProblem ? problem.code : "unknown"] ?? t.errors.unknown!);
		} finally {
			setSaving(false);
		}
	};

	return (
		<form onSubmit={submit} aria-labelledby="new-note" className="h-fit space-y-3 rounded-xl border border-line bg-card p-5">
			<h2 id="new-note" className="text-lg font-semibold">
				{t.form.title}
			</h2>
			<label className="block space-y-1">
				<span className="text-sm font-medium">{t.form.noteTitle}</span>
				<input className={input} value={title} onChange={(e) => setTitle(e.target.value)} maxLength={120} />
			</label>
			<label className="block space-y-1">
				<span className="text-sm font-medium">{t.form.noteBody}</span>
				<textarea className={`${input} min-h-32`} value={body} onChange={(e) => setBody(e.target.value)} maxLength={4000} />
			</label>
			{error ? (
				<p role="alert" className="rounded-md bg-danger-soft px-3 py-2 text-sm text-danger">
					{error}
				</p>
			) : null}
			<button
				type="submit"
				disabled={saving || !title.trim() || !body.trim()}
				className="inline-flex items-center gap-2 rounded-md bg-brand px-4 py-2 text-sm font-semibold text-white hover:bg-brand-dark disabled:opacity-50"
			>
				<Plus className="size-4" aria-hidden />
				{t.form.save}
			</button>
		</form>
	);
}
