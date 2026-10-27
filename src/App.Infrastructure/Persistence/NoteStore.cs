namespace App.Infrastructure.Persistence;

using App.Application.Features.Notes;
using App.Domain.Notes;
using Microsoft.EntityFrameworkCore;

/// <summary>Notes in Postgres.</summary>
/// <param name="db">The context.</param>
internal sealed class NoteStore(AppDbContext db) : INoteStore
{
	public async Task AddAsync(Note note, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(note);
		db.Notes.Add(new NoteRecord { Id = note.Id, Title = note.Title, Body = note.Body, CreatedAt = note.CreatedAt });
		await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public async Task<Note?> FindAsync(Guid id, CancellationToken cancellationToken) =>
		await db.Notes.AsNoTracking().Where(n => n.Id == id).Select(static n => ToNote(n)).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

	public async Task<IReadOnlyList<Note>> LatestAsync(int limit, CancellationToken cancellationToken) =>
		await db.Notes.AsNoTracking()
			.OrderByDescending(static n => n.CreatedAt)
			.ThenByDescending(static n => n.Id)
			.Take(limit)
			.Select(static n => ToNote(n))
			.ToListAsync(cancellationToken)
			.ConfigureAwait(false);

	private static Note ToNote(NoteRecord record) => Note.Restore(record.Id, record.Title, record.Body, record.CreatedAt);
}
