namespace App.Infrastructure.Persistence;

using App.Application.Abstractions.Ai;
using Microsoft.EntityFrameworkCore;
using NodaTime;

/// <summary>Model calls in <c>ai_usage</c>; the daily budget is their sum since midnight UTC (ADR 0004).</summary>
/// <param name="db">The context.</param>
internal sealed class AiUsageLog(AppDbContext db) : IAiUsageLog
{
	public async Task AddAsync(AiUsage usage, Instant at, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(usage);
		db.AiUsage.Add(new AiUsageRecord
		{
			Id = Guid.CreateVersion7(at.ToDateTimeOffset()),
			OccurredAt = at,
			Operation = usage.Operation,
			Model = usage.Model,
			InputTokens = usage.InputTokens,
			OutputTokens = usage.OutputTokens,
			CostUsd = usage.CostUsd,
			LatencyMs = usage.LatencyMs,
			PromptHash = usage.PromptHash,
		});
		await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public async Task<AiSpend> SpentSinceAsync(Instant since, CancellationToken cancellationToken)
	{
		var rows = db.AiUsage.AsNoTracking().Where(u => u.OccurredAt >= since);
		return new AiSpend(
			await rows.SumAsync(static u => u.CostUsd, cancellationToken).ConfigureAwait(false),
			await rows.CountAsync(cancellationToken).ConfigureAwait(false));
	}
}
