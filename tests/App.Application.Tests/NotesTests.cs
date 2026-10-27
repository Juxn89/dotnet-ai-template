namespace App.Application.Tests;

using App.Application.Abstractions.Ai;
using App.Application.Common;
using App.Application.Features.Notes;
using App.Domain.Notes;
using NodaTime;
using NodaTime.Testing;

/// <summary>The use cases against in-memory ports: no database, no host, no model.</summary>
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

	[Fact]
	public async Task A_summary_the_rules_accept_is_saved_with_its_cost()
	{
		var note = await Note("Café", "Llamar al proveedor el lunes.");
		var summarizer = new FakeSummarizer(new SummaryDraft("Llamar al proveedor de café.", ["work"], 0.0003m));

		var result = await new SummarizeNote(_store, summarizer, _clock).ExecuteAsync(note.Id, CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal(0.0003m, result.Value.CostUsd);
		Assert.Equal(["work"], _store.Summaries[note.Id].NoteTags);
	}

	[Fact]
	public async Task A_proposal_the_rules_reject_is_a_model_failure_and_nothing_is_saved()
	{
		var note = await Note("Café", "Llamar al proveedor el lunes.");
		var summarizer = new FakeSummarizer(new SummaryDraft("Llamar.", ["urgent"], 0.0003m));

		var result = await new SummarizeNote(_store, summarizer, _clock).ExecuteAsync(note.Id, CancellationToken.None);

		Assert.Equal(AiErrors.Failed, result.Error);
		Assert.Empty(_store.Summaries);
	}

	[Fact]
	public async Task Without_a_model_the_answer_says_so_and_no_call_is_made()
	{
		var note = await Note("Café", "Llamar al proveedor el lunes.");
		var summarizer = new FakeSummarizer(null);

		var result = await new SummarizeNote(_store, summarizer, _clock).ExecuteAsync(note.Id, CancellationToken.None);

		Assert.Equal(AiErrors.NotConfigured, result.Error);
		Assert.Equal(0, summarizer.Calls);
	}

	private async Task<Note> Note(string title, string body) =>
		(await new CreateNote(_store, _clock).ExecuteAsync(title, body, CancellationToken.None)).Value;

	private sealed class FakeSummarizer(SummaryDraft? draft) : INoteSummarizer
	{
		public int Calls { get; private set; }

		public bool IsConfigured => draft is not null;

		public Task<Result<SummaryDraft>> SummarizeAsync(Note note, CancellationToken cancellationToken)
		{
			Calls++;
			return Task.FromResult<Result<SummaryDraft>>(draft!);
		}
	}

	private sealed class InMemoryNotes : INoteStore
	{
		public List<Note> Notes { get; } = [];

		public Dictionary<Guid, NoteSummary> Summaries { get; } = [];

		public Task AddAsync(Note note, CancellationToken cancellationToken)
		{
			Notes.Add(note);
			return Task.CompletedTask;
		}

		public Task<NoteEntry?> FindAsync(Guid id, CancellationToken cancellationToken) =>
			Task.FromResult(Notes.Find(n => n.Id == id) is { } note ? new NoteEntry(note, Summaries.GetValueOrDefault(id)) : null);

		public Task<IReadOnlyList<NoteEntry>> LatestAsync(int limit, CancellationToken cancellationToken) =>
			Task.FromResult<IReadOnlyList<NoteEntry>>([.. Notes.OrderByDescending(static n => n.CreatedAt).Take(limit).Select(n => new NoteEntry(n, Summaries.GetValueOrDefault(n.Id)))]);

		public Task SaveSummaryAsync(NoteSummary summary, CancellationToken cancellationToken)
		{
			Summaries[summary.NoteId] = summary;
			return Task.CompletedTask;
		}
	}
}
