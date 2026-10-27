namespace App.Application.Features.Notes;

using App.Application.Common;
using App.Domain.Notes;
using NodaTime;

/// <summary>Where notes are kept.</summary>
public interface INoteStore
{
	/// <summary>Saves a new note.</summary>
	/// <param name="note">The note.</param>
	/// <param name="cancellationToken">Cancellation.</param>
	/// <returns>A task.</returns>
	Task AddAsync(Note note, CancellationToken cancellationToken);

	/// <summary>A note by id.</summary>
	/// <param name="id">The id.</param>
	/// <param name="cancellationToken">Cancellation.</param>
	/// <returns>The note, or null.</returns>
	Task<Note?> FindAsync(Guid id, CancellationToken cancellationToken);

	/// <summary>The newest notes first.</summary>
	/// <param name="limit">How many.</param>
	/// <param name="cancellationToken">Cancellation.</param>
	/// <returns>The notes.</returns>
	Task<IReadOnlyList<Note>> LatestAsync(int limit, CancellationToken cancellationToken);
}

/// <summary>Writes a note. One use case, one class: no mediator (ADR 0002).</summary>
/// <param name="store">The notes.</param>
/// <param name="clock">The clock.</param>
public sealed class CreateNote(INoteStore store, IClock clock)
{
	/// <summary>The title is empty or too long.</summary>
	public static readonly Error InvalidTitle = Error.Validation("note.invalid_title", $"A title has 1 to {Note.MaxTitleLength} characters.");

	/// <summary>The text is empty or too long.</summary>
	public static readonly Error InvalidBody = Error.Validation("note.invalid_body", $"A note has 1 to {Note.MaxBodyLength} characters.");

	/// <summary>Writes it.</summary>
	/// <param name="title">The title.</param>
	/// <param name="body">The text.</param>
	/// <param name="cancellationToken">Cancellation.</param>
	/// <returns>The note, or why it was refused.</returns>
	public async Task<Result<Note>> ExecuteAsync(string? title, string? body, CancellationToken cancellationToken)
	{
		var created = Note.Create(title, body, clock.GetCurrentInstant());
		if (created.Note is not { } note)
		{
			return created.Broken == NoteRule.Title ? InvalidTitle : InvalidBody;
		}

		await store.AddAsync(note, cancellationToken).ConfigureAwait(false);
		return note;
	}
}

/// <summary>Reads notes.</summary>
/// <param name="store">The notes.</param>
public sealed class NoteQueries(INoteStore store)
{
	/// <summary>The most notes a list returns.</summary>
	public const int ListLimit = 100;

	/// <summary>No such note.</summary>
	public static readonly Error NotFound = Error.NotFound("note.not_found", "There is no such note.");

	/// <summary>The newest notes.</summary>
	/// <param name="cancellationToken">Cancellation.</param>
	/// <returns>The notes.</returns>
	public Task<IReadOnlyList<Note>> LatestAsync(CancellationToken cancellationToken) => store.LatestAsync(ListLimit, cancellationToken);

	/// <summary>A note.</summary>
	/// <param name="id">Its id.</param>
	/// <param name="cancellationToken">Cancellation.</param>
	/// <returns>The note, or <see cref="NotFound"/>.</returns>
	public async Task<Result<Note>> GetAsync(Guid id, CancellationToken cancellationToken) =>
		await store.FindAsync(id, cancellationToken).ConfigureAwait(false) is { } note ? note : NotFound;
}
