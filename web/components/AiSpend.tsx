"use client";

import type { AiUsage } from "@/lib/api";
import { formatUsd } from "@/lib/format";
import { useI18n } from "@/lib/i18n";

/** Today's spend on the model against the cap: every call is metered, and past the cap calls are refused. */
export function AiSpend({ usage }: { usage: AiUsage }) {
	const { locale, t } = useI18n();
	const share = usage.dailyBudgetUsd > 0 ? Math.min(1, usage.spentTodayUsd / usage.dailyBudgetUsd) : 0;

	return (
		<section aria-labelledby="ai-spend" className="space-y-2 rounded-xl border border-line bg-card p-4">
			<div className="flex flex-wrap items-baseline justify-between gap-2">
				<h2 id="ai-spend" className="text-sm font-semibold">
					{t.spend.title}
				</h2>
				<p className="tabular text-sm text-muted">
					{t.spend.of(formatUsd(usage.spentTodayUsd, locale), formatUsd(usage.dailyBudgetUsd, locale), usage.callsToday)}
				</p>
			</div>
			<div
				role="meter"
				aria-label={t.spend.title}
				aria-valuemin={0}
				aria-valuemax={usage.dailyBudgetUsd}
				aria-valuenow={usage.spentTodayUsd}
				className="h-2 overflow-hidden rounded-full bg-paper"
			>
				<div className="h-full rounded-full bg-brand" style={{ width: `${Math.max(share * 100, share > 0 ? 1 : 0)}%` }} />
			</div>
			<p className="text-xs text-muted">{t.spend.hint}</p>
		</section>
	);
}
