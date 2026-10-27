namespace App.Infrastructure.Ai;

using System.Diagnostics;
using App.Application.Abstractions.Ai;
using Microsoft.Extensions.AI;
using NodaTime;

/// <summary>
/// Every model call, measured and capped (ADR 0004): before the call, the day's spend in <c>ai_usage</c> is checked
/// against <see cref="AiOptions.DailyBudgetUsd"/> and the call is refused if it is spent; after it, the tokens are
/// priced from configuration and recorded with the operation, the latency and the prompt's version.
/// </summary>
/// <param name="inner">The model.</param>
/// <param name="log">Where calls are recorded.</param>
/// <param name="options">Prices and the cap.</param>
/// <param name="clock">The clock.</param>
/// <param name="operation">What the calls are for, e.g. <c>note.summary</c>.</param>
/// <param name="promptHash">The prompt's version.</param>
internal sealed class MeteredChatClient(IChatClient inner, IAiUsageLog log, AiOptions options, IClock clock, string operation, string? promptHash)
	: DelegatingChatClient(inner)
{
	/// <summary>The key under which the call's cost travels on the response, for the caller to report.</summary>
	public const string CostProperty = "cost_usd";

	public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options1 = null, CancellationToken cancellationToken = default)
	{
		var now = clock.GetCurrentInstant();
		var spent = await log.SpentSinceAsync(now.InUtc().Date.AtStartOfDayInZone(DateTimeZone.Utc).ToInstant(), cancellationToken).ConfigureAwait(false);
		if (spent.CostUsd >= options.DailyBudgetUsd)
		{
			throw new AiBudgetExceededException($"Today's budget of USD {options.DailyBudgetUsd} is spent.");
		}

		var stopwatch = Stopwatch.StartNew();
		var response = await base.GetResponseAsync(messages, options1, cancellationToken).ConfigureAwait(false);
		var input = response.Usage?.InputTokenCount ?? 0;
		var output = response.Usage?.OutputTokenCount ?? 0;
		var cost = options.Cost(input, output);
		await log.AddAsync(new AiUsage(operation, response.ModelId ?? options.Model, input, output, cost, stopwatch.ElapsedMilliseconds, promptHash), now, cancellationToken).ConfigureAwait(false);

		(response.AdditionalProperties ??= [])[CostProperty] = cost;
		return response;
	}

	public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options1 = null, CancellationToken cancellationToken = default) =>
		throw new NotSupportedException("The template's features answer whole replies.");
}
