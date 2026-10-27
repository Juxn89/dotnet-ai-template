namespace App.Infrastructure.Ai;

using System.Text.Json;
using Microsoft.Extensions.AI;

/// <summary>
/// Answers from a script instead of the model, so the end-to-end tests and the evals' oracle run in CI without a key
/// and at no cost (lesson from P3 and P4). Each entry is <c>{ "match": "...", "reply": { ... } }</c>; the first entry
/// whose <c>match</c> occurs in the last user message wins, and its reply is returned as the model's text. Without a
/// match the reply is an empty object, which the feature's rules reject. Development and Testing only.
/// </summary>
/// <param name="path">The script.</param>
internal sealed class ScriptedChatClient(string path) : IChatClient
{
	private readonly Lazy<IReadOnlyList<(string Match, string Reply)>> _script = new(() => Load(path));

	public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
	{
		var last = messages.LastOrDefault(static m => m.Role == ChatRole.User)?.Text ?? string.Empty;
		var reply = _script.Value.FirstOrDefault(entry => last.Contains(entry.Match, StringComparison.Ordinal)).Reply ?? "{}";
		return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)) { ModelId = "scripted" });
	}

	public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
		throw new NotSupportedException("The script answers whole replies.");

	public object? GetService(Type serviceType, object? serviceKey = null) => null;

	public void Dispose()
	{
	}

	private static List<(string Match, string Reply)> Load(string path)
	{
		using var document = JsonDocument.Parse(File.ReadAllText(path));
		return [.. document.RootElement.EnumerateArray().Select(static entry => (entry.GetProperty("match").GetString()!, entry.GetProperty("reply").GetRawText()))];
	}
}
