import type { Locale } from "@/lib/i18n";

const tags: Record<Locale, string> = { es: "es-NI", en: "en-US" };

/** A cost in dollars with the precision a model call needs: a tenth of a cent is common, so up to six decimals. */
export function formatUsd(value: number, locale: Locale): string {
	return `USD ${new Intl.NumberFormat(tags[locale], { minimumFractionDigits: 2, maximumFractionDigits: 6 }).format(value)}`;
}
