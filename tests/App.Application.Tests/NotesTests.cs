namespace App.Application.Tests;

using App.Application.Features.Notes;
using App.Domain.Notes;
using NodaTime;
using NodaTime.Testing;

/// <summary>The use cases against an in-memory port: no database, no host.</summary>
public sealed class NotesTests
{
	private readonly FakeClock _clock = new(Instant.FromUtc(2026, 10, 27, 15, 0));
	private readonly InMemoryNotes _store = new();

	[Fact]
	public async Task A_valid_note_is_saved_with_the_clocks_instant()
	{
		var result = await new CreateNote(_store, _clock).ExecuteAsync("Pedido", "Llamar al proveedor.", CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal(_clock.GetCurrentInstant(), result.Value.CreatedAt);
		Assert.Single(_store.Notes);
	}

	[Fact]
	public async Task An_invalid_note_is_refused_with_its_code_and_nothing_is_saved()
	{
		var result = await new CreateNote(_store, _clock).ExecuteAsync("Pedido", " ", CancellationToken.None);

		Assert.Equal(CreateNote.InvalidBody, result.Error);
		Assert.Empty(_store.Notes);
	}

	[Fact]
	public async Task An_unknown_note_is_not_found()
	{
		var result = await new NoteQueries(_store).GetAsync(Guid.NewGuid(), CancellationToken.None);

		Assert.Equal(NoteQueries.NotFound, result.Error);
	}

	private sealed class InMemoryNotes : INoteStore
	{
		public List<Note> Notes { get; } = [];

		public Task AddAsync(Note note, CancellationToken cancellationToken)
		{
			Notes.Add(note);
			return Task.CompletedTask;
		}

		public Task<Note?> FindAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Notes.Find(n => n.Id == id));

		public Task<IReadOnlyList<Note>> LatestAsync(int limit, CancellationToken cancellationToken) =>
			Task.FromResult<IReadOnlyList<Note>>([.. Notes.OrderByDescending(static n => n.CreatedAt).Take(limit)]);
	}
}
