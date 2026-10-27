namespace App.Evals;

using System.Globalization;
using System.Text.Json.Nodes;

/// <summary>
/// How the summaries did. Deterministic, no judge model: the tags are right when the required one is there and none
/// is off the acceptable list; a summary mentions the note when it contains one of the note's words; its language
/// is told by a handful of common words, enough to tell Spanish from English.
/// </summary>
/// <param name="Tags">Share of notes with right tags.</param>
/// <param name="Mentions">Share of summaries that mention what the note is about.</param>
/// <param name="Language">Share of summaries in the note's language.</param>
/// <param name="AverageCostUsd">Average cost per note.</param>
/// <param name="Lines">One line per note.</param>
internal sealed record Score(decimal Tags, decimal Mentions, decimal Language, decimal AverageCostUsd, IReadOnlyList<(string Id, bool Tags, bool Mentions, bool Language)> Lines)
{
	private static readonly string[] SpanishWords = [" el ", " la ", " los ", " las ", " de ", " del ", " para ", " con ", " y ", " por "];
	private static readonly string[] EnglishWords = [" the ", " a ", " and ", " for ", " to ", " with ", " of ", " by "];

	public static Score Of(EvalSet set, IReadOnlyList<NoteResult> results)
	{
		var lines = set.Notes.Zip(results, static (note, result) =>
		(
			note.Id,
			Tags: result.Summary is not null && result.Tags.Contains(note.RequiredTag) && result.Tags.All(note.AcceptableTags.Contains),
			Mentions: result.Summary is { } s && note.Mentions.Any(m => s.Contains(m, StringComparison.OrdinalIgnoreCase)),
			Language: result.Summary is { } t && LanguageOf(t) == note.Language)).ToList();
		return new Score(
			Ratio(lines.Select(static l => l.Tags)),
			Ratio(lines.Select(static l => l.Mentions)),
			Ratio(lines.Select(static l => l.Language)),
			results.Count == 0 ? 0m : decimal.Round(results.Average(static r => r.CostUsd), 6),
			lines);
	}

	public bool Passes(EvalSet set) =>
		Tags >= set.Threshold.Tags && Mentions >= set.Threshold.Mentions && Language >= set.Threshold.Language;

	public string Report() => string.Create(
		CultureInfo.InvariantCulture,
		$"tags {Tags:0.000} · mentions {Mentions:0.000} · language {Language:0.000} · USD {AverageCostUsd:0.000000} per note");

	public JsonObject ToJson(string model, IReadOnlyList<NoteResult> results) => new()
	{
		["model"] = model,
		["date"] = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
		["tags"] = Tags,
		["mentions"] = Mentions,
		["language"] = Language,
		["averageCostUsd"] = AverageCostUsd,
		["totalCostUsd"] = results.Sum(static r => r.CostUsd),
		["items"] = new JsonArray([.. Lines.Zip(results, static (line, result) => (JsonNode)new JsonObject
		{
			["id"] = line.Id,
			["tagsRight"] = line.Tags,
			["mentions"] = line.Mentions,
			["languageRight"] = line.Language,
			["summary"] = result.Summary,
			["tags"] = new JsonArray([.. result.Tags.Select(static t => (JsonNode)t)]),
			["error"] = result.Error,
			["costUsd"] = result.CostUsd,
			["latencyMs"] = result.LatencyMs,
		})]),
	};

	private static string LanguageOf(string text)
	{
		var padded = $" {text.ToLowerInvariant()} ";
		return SpanishWords.Count(padded.Contains) >= EnglishWords.Count(padded.Contains) ? "Spanish" : "English";
	}

	private static decimal Ratio(IEnumerable<bool> items)
	{
		var all = items.ToArray();
		return all.Length == 0 ? 0m : decimal.Round(all.Count(static x => x) / (decimal)all.Length, 3);
	}
}
