namespace App.Integration.Tests;

using App.Application.Abstractions.Ai;
using App.Infrastructure.Ai;
using Microsoft.Extensions.AI;
using NodaTime;
using NodaTime.Testing;

/// <summary>The meter on its own: prices from configuration, one record per call, the cap checked before calling.</summary>
public sealed class MeteredChatClientTests
{
	private static readonly AiOptions Options = new() { InputUsdPerMillionTokens = 0.10m, OutputUsdPerMillionTokens = 0.50m, DailyBudgetUsd = 0.01m };

	private readonly FakeClock _clock = new(Instant.FromUtc(2026, 10, 27, 15, 0));

	[Fact]
	public async Task A_call_is_priced_from_its_tokens_and_recorded_once()
	{
		var log = new Log();
		using var metered = new MeteredChatClient(new Model(1_000, 200), log, Options, _clock, "note.summary", "abc123");

		var response = await metered.GetResponseAsync([new ChatMessage(ChatRole.User, "hola")]);

		// 1,000 × 0.10 / 1M + 200 × 0.50 / 1M = USD 0.0002.
		Assert.Equal(0.0002m, response.AdditionalProperties![MeteredChatClient.CostProperty]);
		var recorded = Assert.Single(log.Calls);
		Assert.Equal(("note.summary", 1_000L, 200L, "abc123"), (recorded.Operation, recorded.InputTokens, recorded.OutputTokens, recorded.PromptHash));
	}

	[Fact]
	public async Task A_spent_day_refuses_the_call_without_calling_the_model()
	{
		var model = new Model(1_000, 200);
		var log = new Log { Spent = Options.DailyBudgetUsd };
		using var metered = new MeteredChatClient(model, log, Options, _clock, "note.summary", null);

		await Assert.ThrowsAsync<AiBudgetExceededException>(() => metered.GetResponseAsync([new ChatMessage(ChatRole.User, "hola")]));
		Assert.Equal(0, model.Calls);
		Assert.Empty(log.Calls);
	}

	private sealed class Model(long input, long output) : IChatClient
	{
		public int Calls { get; private set; }

		public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
		{
			Calls++;
			return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{}")) { Usage = new UsageDetails { InputTokenCount = input, OutputTokenCount = output } });
		}

		public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
			throw new NotSupportedException();

		public object? GetService(Type serviceType, object? serviceKey = null) => null;

		public void Dispose()
		{
		}
	}

	private sealed class Log : IAiUsageLog
	{
		public decimal Spent { get; init; }

		public List<AiUsage> Calls { get; } = [];

		public Task AddAsync(AiUsage usage, Instant at, CancellationToken cancellationToken)
		{
			Calls.Add(usage);
			return Task.CompletedTask;
		}

		public Task<AiSpend> SpentSinceAsync(Instant since, CancellationToken cancellationToken) => Task.FromResult(new AiSpend(Spent, 0));
	}
}
