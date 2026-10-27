namespace App.Evals;

using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>One note to summarize, and what a right answer looks like.</summary>
/// <param name="Id">Its id.</param>
/// <param name="Language">The language it is written in, and the summary must be.</param>
/// <param name="Title">Its title; unique, so the oracle's script can match on it.</param>
/// <param name="Body">Its text.</param>
/// <param name="AcceptableTags">The tags that are right for it.</param>
/// <param name="RequiredTag">The one tag it must have.</param>
/// <param name="Mentions">Words the summary should contain at least one of.</param>
/// <param name="Reference">A right answer, which the oracle replays.</param>
internal sealed record EvalNote(string Id, string Language, string Title, string Body, IReadOnlyList<string> AcceptableTags, string RequiredTag, IReadOnlyList<string> Mentions, ReferenceAnswer Reference);

/// <summary>A right answer for a note.</summary>
/// <param name="Summary">The sentence.</param>
/// <param name="Tags">The tags.</param>
internal sealed record ReferenceAnswer(string Summary, IReadOnlyList<string> Tags);

/// <summary>The pass marks, in <c>evals/notes.json</c> next to the notes they apply to.</summary>
/// <param name="Tags">Share of notes with right tags.</param>
/// <param name="Mentions">Share of summaries that mention what the note is about.</param>
/// <param name="Language">Share of summaries in the note's language.</param>
internal sealed record Thresholds(decimal Tags, decimal Mentions, decimal Language);

/// <summary>The golden set.</summary>
/// <param name="Threshold">The pass marks.</param>
/// <param name="Notes">The notes.</param>
internal sealed record EvalSet(Thresholds Threshold, IReadOnlyList<EvalNote> Notes)
{
	private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

	public static EvalSet Load(string path) => JsonSerializer.Deserialize<EvalSet>(File.ReadAllText(path), Json)!;

	/// <summary>A script for the scripted model: each note's reference answer, matched by its title.</summary>
	/// <returns>The script's JSON.</returns>
	public string OracleScript() =>
		new JsonArray([.. Notes.Select(static n => new JsonObject
		{
			["match"] = n.Title,
			["reply"] = new JsonObject
			{
				["language"] = n.Language,
				["summary"] = n.Reference.Summary,
				["tags"] = new JsonArray([.. n.Reference.Tags.Select(static t => (JsonNode)t)]),
			},
		})]).ToJsonString();
}
