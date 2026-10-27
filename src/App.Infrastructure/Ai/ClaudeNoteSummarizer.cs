namespace App.Infrastructure.Ai;

using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic.Models.Messages;
using App.Application.Abstractions.Ai;
using App.Application.Common;
using App.Application.Features.Notes;
using App.Domain.Notes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using NodaTime;

/// <summary>
/// Summarizes a note with the model through structured outputs: the answer is constrained to a JSON Schema whose
/// tags are the domain's list, then checked again by the domain. Every call goes through <see cref="MeteredChatClient"/>.
/// </summary>
/// <param name="model">The bare model.</param>
/// <param name="log">Where calls are recorded.</param>
/// <param name="options">Model, prices and the cap.</param>
/// <param name="clock">The clock.</param>
internal sealed class ClaudeNoteSummarizer(IChatClient model, IAiUsageLog log, IOptions<AiOptions> options, IClock clock) : INoteSummarizer
{
	/// <summary>The operation's name in <c>ai_usage</c>.</summary>
	public const string Operation = "note.summary";

	private static readonly (string Text, string Hash) Prompt = PromptLibrary.Read("summarize-note.md");

	private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

	private static readonly Dictionary<string, JsonElement> Schema = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
		JsonSerializer.Serialize(new
		{
			type = "object",
			additionalProperties = false,
			required = new[] { "language", "summary", "tags" },
			properties = new
			{
				language = new { type = "string", @enum = new[] { "Spanish", "English" }, description = "The language the note is written in." },
				summary = new { type = "string", description = "One sentence in the note's language." },
				tags = new { type = "array", items = new { type = "string", @enum = NoteSummary.Tags }, description = "Up to three, most relevant first." },
			},
		}))!;

	public bool IsConfigured => ChatModel.IsConfigured(model);

	public async Task<Result<SummaryDraft>> SummarizeAsync(Note note, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(note);
		var settings = options.Value;
		using var metered = new MeteredChatClient(model, log, settings, clock, Operation, Prompt.Hash);
		var request = new ChatOptions
		{
			MaxOutputTokens = settings.MaxOutputTokens,
			RawRepresentationFactory = _ => new MessageCreateParams
			{
				Model = settings.Model,
				MaxTokens = settings.MaxOutputTokens,
				Messages = [],
				OutputConfig = new OutputConfig { Effort = settings.Effort, Format = new JsonOutputFormat { Schema = Schema } },
			},
		};

		ChatResponse response;
		try
		{
			response = await metered.GetResponseAsync(
				[
					new ChatMessage(ChatRole.System, Prompt.Text),
					new ChatMessage(ChatRole.User, $"<note>\n<title>{note.Title}</title>\n<text>{note.Body}</text>\n</note>"),
				],
				request,
				cancellationToken).ConfigureAwait(false);
		}
		catch (AiBudgetExceededException)
		{
			return AiErrors.DailyBudgetSpent;
		}
		catch (AiUnavailableException)
		{
			return AiErrors.Failed;
		}

		var cost = response.AdditionalProperties?[MeteredChatClient.CostProperty] is decimal value ? value : 0m;
		try
		{
			return JsonSerializer.Deserialize<ModelSummary>(response.Text, Json) is { Summary: { } summary } answer
				? new SummaryDraft(summary, answer.Tags ?? [], cost)
				: AiErrors.Failed;
		}
		catch (JsonException)
		{
			return AiErrors.Failed;
		}
	}

	private sealed record ModelSummary(
		[property: JsonPropertyName("language")] string? Language,
		[property: JsonPropertyName("summary")] string? Summary,
		[property: JsonPropertyName("tags")] IReadOnlyList<string>? Tags);
}
