namespace App.Infrastructure.Persistence;

using App.Application.Features.Notes;
using App.Domain.Notes;
using Microsoft.EntityFrameworkCore;

/// <summary>Notes and their summaries in Postgres.</summary>
/// <param name="db">The context.</param>
internal sealed class NoteStore(AppDbContext db) : INoteStore
{
	public async Task AddAsync(Note note, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(note);
		db.Notes.Add(new NoteRecord { Id = note.Id, Title = note.Title, Body = note.Body, CreatedAt = note.CreatedAt });
		await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public async Task<NoteEntry?> FindAsync(Guid id, CancellationToken cancellationToken) =>
		(await WithSummariesAsync(db.Notes.AsNoTracking().Where(n => n.Id == id), cancellationToken).ConfigureAwait(false)).SingleOrDefault();

	public async Task<IReadOnlyList<NoteEntry>> LatestAsync(int limit, CancellationToken cancellationToken)
	{
		var latest = db.Notes.AsNoTracking().OrderByDescending(static n => n.CreatedAt).ThenByDescending(static n => n.Id).Take(limit);
		var entries = await WithSummariesAsync(latest, cancellationToken).ConfigureAwait(false);

		// The join does not keep the subquery's order, so it is applied again in memory, on at most `limit` rows.
		return [.. entries.OrderByDescending(static e => e.Note.CreatedAt).ThenByDescending(static e => e.Note.Id)];
	}

	public async Task SaveSummaryAsync(NoteSummary summary, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(summary);
		var row = await db.NoteSummaries.FindAsync([summary.NoteId], cancellationToken).ConfigureAwait(false);
		if (row is null)
		{
			db.NoteSummaries.Add(new NoteSummaryRecord { NoteId = summary.NoteId, Summary = summary.Summary, Tags = [.. summary.NoteTags], CreatedAt = summary.CreatedAt });
		}
		else
		{
			row.Summary = summary.Summary;
			row.Tags = [.. summary.NoteTags];
			row.CreatedAt = summary.CreatedAt;
		}

		await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	// Filtered and ordered on the notes first: EF Core cannot translate a filter on a record built in the projection.
	private async Task<List<NoteEntry>> WithSummariesAsync(IQueryable<NoteRecord> notes, CancellationToken cancellationToken)
	{
		var rows = await (
			from note in notes
			join summary in db.NoteSummaries.AsNoTracking() on note.Id equals summary.NoteId into summaries
			from summary in summaries.DefaultIfEmpty()
			select new { note, summary })
			.ToListAsync(cancellationToken)
			.ConfigureAwait(false);
		return [.. rows.Select(static row => ToEntry(row.note, row.summary))];
	}

	private static NoteEntry ToEntry(NoteRecord note, NoteSummaryRecord? summary) => new(
		Note.Restore(note.Id, note.Title, note.Body, note.CreatedAt),
		summary is null ? null : NoteSummary.Restore(summary.NoteId, summary.Summary, summary.Tags, summary.CreatedAt));
}
