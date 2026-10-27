import type { Messages } from "@/messages/es";

export const en: Messages = {
	app: {
		name: "Notes with AI",
		title: "Your notes",
		tagline: "Write a note and ask for a summary: the AI proposes a sentence and tags from a fixed list, and every call is metered, with its cost and a daily cap.",
	},
	nav: { language: "Language" },
	form: { title: "New note", noteTitle: "Title", noteBody: "Text", save: "Save note" },
	notes: { title: "Notes", loading: "Loading…", empty: "No notes yet. Write the first one." },
	summary: {
		label: "Summary",
		tags: "Tags",
		action: "Summarize with AI",
		again: "Summarize again",
		working: "Summarizing…",
		cost: (usd: string) => `AI cost: ${usd}`,
	},
	spend: {
		title: "Today's AI spend",
		of: (spent: string, budget: string, calls: number) => `${spent} of ${budget} · ${calls} ${calls === 1 ? "call" : "calls"}`,
		hint: "Once the cap is reached, calls are refused before they are made, until tomorrow.",
	},
	tags: {
		work: "work",
		personal: "personal",
		shopping: "shopping",
		money: "money",
		health: "health",
		ideas: "ideas",
	},
	errors: {
		"note.invalid_title": "A title has 1 to 120 characters.",
		"note.invalid_body": "A note has 1 to 4,000 characters.",
		"note.not_found": "The note no longer exists.",
		"ai.not_configured": "Summaries need the AI, and no key is configured.",
		"ai.daily_budget": "Today's AI budget is spent. Try again tomorrow.",
		"ai.failed": "The AI could not summarize the note. Try again.",
		unknown: "Something went wrong. Try again.",
	},
};
