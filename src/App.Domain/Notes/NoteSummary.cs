namespace App.Domain.Notes;

using NodaTime;

/// <summary>
/// A note's summary and tags, as the model proposed them and the domain accepted them. The model's answer is
/// checked again here even though a JSON Schema constrained it: the schema is the provider's promise, these rules
/// are the product's.
/// </summary>
public sealed record NoteSummary
{
	/// <summary>The longest summary: one sentence.</summary>
	public const int MaxSummaryLength = 300;

	/// <summary>The most tags on a note.</summary>
	public const int MaxTags = 3;

	/// <summary>The tags a note can have; the model may only pick from these.</summary>
	public static readonly IReadOnlyList<string> Tags = ["work", "personal", "shopping", "money", "health", "ideas"];

	private NoteSummary(Guid noteId, string summary, IReadOnlyList<string> tags, Instant createdAt)
	{
		NoteId = noteId;
		Summary = summary;
		NoteTags = tags;
		CreatedAt = createdAt;
	}

	/// <summary>The note.</summary>
	public Guid NoteId { get; }

	/// <summary>One sentence about it.</summary>
	public string Summary { get; }

	/// <summary>Its tags, from <see cref="Tags"/>, without repeats.</summary>
	public IReadOnlyList<string> NoteTags { get; }

	/// <summary>When it was written.</summary>
	public Instant CreatedAt { get; }

	/// <summary>A summary, or null when the proposal breaks a rule.</summary>
	/// <param name="noteId">The note.</param>
	/// <param name="summary">The sentence.</param>
	/// <param name="tags">The tags.</param>
	/// <param name="now">The current instant.</param>
	/// <returns>The summary, or null.</returns>
	public static NoteSummary? TryCreate(Guid noteId, string? summary, IReadOnlyList<string>? tags, Instant now)
	{
		var text = summary?.Trim() ?? string.Empty;
		var picked = tags ?? [];
		var valid = text.Length is > 0 and <= MaxSummaryLength
			&& picked.Count <= MaxTags
			&& picked.All(Tags.Contains)
			&& picked.Distinct(StringComparer.Ordinal).Count() == picked.Count;
		return valid ? new NoteSummary(noteId, text, [.. picked], now) : null;
	}

	/// <summary>A summary read back from storage, already validated when it was written.</summary>
	/// <param name="noteId">The note.</param>
	/// <param name="summary">The sentence.</param>
	/// <param name="tags">The tags.</param>
	/// <param name="createdAt">When it was written.</param>
	/// <returns>The summary.</returns>
	public static NoteSummary Restore(Guid noteId, string summary, IReadOnlyList<string> tags, Instant createdAt) => new(noteId, summary, tags, createdAt);
}
