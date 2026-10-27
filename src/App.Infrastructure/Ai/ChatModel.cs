namespace App.Infrastructure.Ai;

using Anthropic;
using Microsoft.Extensions.AI;

/// <summary>A model call failed: no key, a provider error or a timeout. The callers turn it into <c>ai.failed</c>.</summary>
public sealed class AiUnavailableException : Exception
{
	/// <summary>Creates one.</summary>
	public AiUnavailableException()
	{
	}

	/// <summary>Creates one.</summary>
	/// <param name="message">Why.</param>
	public AiUnavailableException(string message)
		: base(message)
	{
	}

	/// <summary>Creates one.</summary>
	/// <param name="message">Why.</param>
	/// <param name="innerException">The provider's error.</param>
	public AiUnavailableException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}

/// <summary>The day's budget is spent: the call was refused before it was made.</summary>
public sealed class AiBudgetExceededException : Exception
{
	/// <summary>Creates one.</summary>
	public AiBudgetExceededException()
	{
	}

	/// <summary>Creates one.</summary>
	/// <param name="message">Why.</param>
	public AiBudgetExceededException(string message)
		: base(message)
	{
	}

	/// <summary>Creates one.</summary>
	/// <param name="message">Why.</param>
	/// <param name="innerException">The cause.</param>
	public AiBudgetExceededException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}

/// <summary>
/// The bare model behind every AI feature (ADR 0003): Claude through the official SDK as an <see cref="IChatClient"/>,
/// a script in tests, or a stand-in that answers "unavailable" without a key. Metering is added per request, on top.
/// </summary>
public static class ChatModel
{
	/// <summary>Activity source of the model calls, so they appear in the request's trace in the Aspire dashboard.</summary>
	public const string TelemetrySource = "App.Ai";

	/// <summary>The model.</summary>
	/// <param name="ai">Model, limits and an optional script.</param>
	/// <param name="anthropic">The key.</param>
	/// <returns>The model.</returns>
	public static IChatClient Create(AiOptions ai, AnthropicOptions anthropic)
	{
		ArgumentNullException.ThrowIfNull(ai);
		ArgumentNullException.ThrowIfNull(anthropic);
		if (ai.ScriptFile is { Length: > 0 } script)
		{
			return new ScriptedChatClient(script);
		}

		if (string.IsNullOrWhiteSpace(anthropic.ApiKey))
		{
			return new UnconfiguredChatClient();
		}

		IAnthropicClient client = new AnthropicClient { ApiKey = anthropic.ApiKey, Timeout = ai.Timeout };
		client = client.WithOptions(o => o with { MaxRetries = ai.MaxRetries });
		return new ChatClientBuilder(client.AsIChatClient(ai.Model))
			.UseOpenTelemetry(sourceName: TelemetrySource)
			.Use(static inner => new ProviderFailures(inner))
			.Build();
	}

	/// <summary>Whether the model can be called at all.</summary>
	/// <param name="model">The model.</param>
	/// <returns>False for the stand-in without a key.</returns>
	public static bool IsConfigured(IChatClient model) => model is not UnconfiguredChatClient;
}

/// <summary>Any provider failure (HTTP, 4xx/5xx, timeout) becomes <see cref="AiUnavailableException"/>: callers answer 503, not a stack trace.</summary>
/// <param name="inner">The SDK's client.</param>
internal sealed class ProviderFailures(IChatClient inner) : DelegatingChatClient(inner)
{
	public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
	{
		try
		{
			return await base.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
#pragma warning disable CA1031 // Whatever the provider throws, the person sees "try again".
		catch (Exception exception)
#pragma warning restore CA1031
		{
			throw new AiUnavailableException("The model call failed.", exception);
		}
	}

	public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
		throw new NotSupportedException("The template's features answer whole replies.");
}

/// <summary>Stands in when no API key is configured: the app starts, and the AI endpoints answer 503.</summary>
internal sealed class UnconfiguredChatClient : IChatClient
{
	private const string Message = "No Anthropic API key is configured (Anthropic:ApiKey).";

	public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
		throw new AiUnavailableException(Message);

	public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
		throw new AiUnavailableException(Message);

	public object? GetService(Type serviceType, object? serviceKey = null) => null;

	public void Dispose()
	{
	}
}
