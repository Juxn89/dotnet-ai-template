namespace App.Application.Abstractions.Ai;

using App.Application.Common;
using NodaTime;

/// <summary>One call to a model, as it is recorded: what it was for, what it cost and how long it took.</summary>
/// <param name="Operation">What the call was for, e.g. <c>note.summary</c>.</param>
/// <param name="Model">The model.</param>
/// <param name="InputTokens">Input tokens.</param>
/// <param name="OutputTokens">Output tokens, thinking included.</param>
/// <param name="CostUsd">USD.</param>
/// <param name="LatencyMs">How long it took.</param>
/// <param name="PromptHash">The prompt's version, to tie a change in cost or quality to a change in the prompt.</param>
public sealed record AiUsage(string Operation, string Model, long InputTokens, long OutputTokens, decimal CostUsd, long LatencyMs, string? PromptHash);

/// <summary>Where model calls are recorded; their costs are what the daily budget adds up.</summary>
public interface IAiUsageLog
{
	/// <summary>Records a call.</summary>
	/// <param name="usage">The call.</param>
	/// <param name="at">When it was made.</param>
	/// <param name="cancellationToken">Cancellation.</param>
	/// <returns>A task.</returns>
	Task AddAsync(AiUsage usage, Instant at, CancellationToken cancellationToken);

	/// <summary>What the model has cost since an instant, and how many calls.</summary>
	/// <param name="since">The instant.</param>
	/// <param name="cancellationToken">Cancellation.</param>
	/// <returns>USD and calls.</returns>
	Task<AiSpend> SpentSinceAsync(Instant since, CancellationToken cancellationToken);
}

/// <summary>What the model has cost over a period.</summary>
/// <param name="CostUsd">USD.</param>
/// <param name="Calls">How many calls.</param>
public sealed record AiSpend(decimal CostUsd, int Calls);

/// <summary>Why a model's answer is not available.</summary>
public static class AiErrors
{
	/// <summary>No API key is configured.</summary>
	public static readonly Error NotConfigured = Error.Unavailable("ai.not_configured", "This needs the model, and no API key is configured.");

	/// <summary>The day's budget is spent.</summary>
	public static readonly Error DailyBudgetSpent = Error.Unavailable("ai.daily_budget", "Today's budget for the model is spent; try again tomorrow.");

	/// <summary>The model failed, declined, or answered with something the rules reject.</summary>
	public static readonly Error Failed = Error.Unavailable("ai.failed", "The model could not answer; try again.");
}
