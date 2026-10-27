namespace App.Domain.Notes;

using NodaTime;

/// <summary>
/// A note: the template's example entity, deleted in a project made from it. It shows the pattern: rules in the
/// domain, limits as named constants, construction that cannot produce an invalid value.
/// </summary>
public sealed record Note
{
	/// <summary>The longest title.</summary>
	public const int MaxTitleLength = 120;

	/// <summary>The longest body: a note, not a document.</summary>
	public const int MaxBodyLength = 4000;

	private Note(Guid id, string title, string body, Instant createdAt)
	{
		Id = id;
		Title = title;
		Body = body;
		CreatedAt = createdAt;
	}

	/// <summary>Its id.</summary>
	public Guid Id { get; }

	/// <summary>Its title, trimmed.</summary>
	public string Title { get; }

	/// <summary>Its text, trimmed.</summary>
	public string Body { get; }

	/// <summary>When it was written.</summary>
	public Instant CreatedAt { get; }

	/// <summary>A new note, or why it cannot be one.</summary>
	/// <param name="title">The title.</param>
	/// <param name="body">The text.</param>
	/// <param name="now">The current instant.</param>
	/// <returns>The note, or the rule it breaks.</returns>
	public static NoteResult Create(string? title, string? body, Instant now)
	{
		var cleanTitle = title?.Trim() ?? string.Empty;
		var cleanBody = body?.Trim() ?? string.Empty;
		if (cleanTitle.Length is 0 or > MaxTitleLength)
		{
			return NoteResult.Invalid(NoteRule.Title);
		}

		if (cleanBody.Length is 0 or > MaxBodyLength)
		{
			return NoteResult.Invalid(NoteRule.Body);
		}

		return NoteResult.Valid(new Note(Guid.CreateVersion7(now.ToDateTimeOffset()), cleanTitle, cleanBody, now));
	}

	/// <summary>A note read back from storage, already validated when it was written.</summary>
	/// <param name="id">Its id.</param>
	/// <param name="title">Its title.</param>
	/// <param name="body">Its text.</param>
	/// <param name="createdAt">When it was written.</param>
	/// <returns>The note.</returns>
	public static Note Restore(Guid id, string title, string body, Instant createdAt) => new(id, title, body, createdAt);
}

/// <summary>Which rule a note breaks.</summary>
public enum NoteRule
{
	/// <summary>The title is empty or too long.</summary>
	Title,

	/// <summary>The text is empty or too long.</summary>
	Body,
}

/// <summary>A note, or the rule that kept it from being one.</summary>
/// <param name="Note">The note, when valid.</param>
/// <param name="Broken">The rule, when not.</param>
public sealed record NoteResult(Note? Note, NoteRule? Broken)
{
	/// <summary>A valid note.</summary>
	/// <param name="note">The note.</param>
	/// <returns>The result.</returns>
	public static NoteResult Valid(Note note) => new(note, null);

	/// <summary>An invalid one.</summary>
	/// <param name="rule">The rule it breaks.</param>
	/// <returns>The result.</returns>
	public static NoteResult Invalid(NoteRule rule) => new(null, rule);
}
