// Spanish is the reference dictionary: its shape is the type every other language must match.
export const es = {
	app: {
		name: "Notas con IA",
		title: "Tus notas",
		tagline: "Escribe una nota y pide un resumen: la IA propone una frase y etiquetas de una lista fija, y cada llamada queda medida, con su costo y un tope diario.",
	},
	nav: { language: "Idioma" },
	form: { title: "Nueva nota", noteTitle: "Título", noteBody: "Texto", save: "Guardar nota" },
	notes: { title: "Notas", loading: "Cargando…", empty: "Todavía no hay notas. Escribe la primera." },
	summary: {
		label: "Resumen",
		tags: "Etiquetas",
		action: "Resumir con IA",
		again: "Volver a resumir",
		working: "Resumiendo…",
		cost: (usd: string) => `Costo de IA: ${usd}`,
	},
	spend: {
		title: "Gasto de IA de hoy",
		of: (spent: string, budget: string, calls: number) => `${spent} de ${budget} · ${calls} ${calls === 1 ? "llamada" : "llamadas"}`,
		hint: "Al llegar al tope, las llamadas se rechazan antes de hacerse, hasta mañana.",
	},
	tags: {
		work: "trabajo",
		personal: "personal",
		shopping: "compras",
		money: "dinero",
		health: "salud",
		ideas: "ideas",
	} as Record<string, string>,
	errors: {
		"note.invalid_title": "El título debe tener entre 1 y 120 caracteres.",
		"note.invalid_body": "La nota debe tener entre 1 y 4,000 caracteres.",
		"note.not_found": "La nota ya no existe.",
		"ai.not_configured": "El resumen necesita la IA y no hay una clave configurada.",
		"ai.daily_budget": "Se alcanzó el presupuesto de IA de hoy. Vuelve a intentarlo mañana.",
		"ai.failed": "La IA no pudo resumir la nota. Inténtalo de nuevo.",
		unknown: "Algo salió mal. Inténtalo de nuevo.",
	} as Record<string, string>,
};

export type Messages = typeof es;
