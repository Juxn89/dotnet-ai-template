namespace App.Api.Endpoints;

using App.Application.Features.Notes;
using App.Domain.Notes;
using NodaTime;

/// <summary>The notes' endpoints: the template's example slice, deleted in a project made from it.</summary>
internal static class NoteEndpoints
{
	public static IEndpointRouteBuilder MapNoteEndpoints(this IEndpointRouteBuilder app)
	{
		var notes = app.MapGroup("/v1/notes").WithTags("Notes");
		notes.MapPost("/", CreateAsync);
		notes.MapGet("/", ListAsync);
		notes.MapGet("/{id:guid}", GetAsync);
		return app;
	}

	private static async Task<IResult> CreateAsync(NewNote request, CreateNote create, CancellationToken cancellationToken) =>
		(await create.ExecuteAsync(request.Title, request.Body, cancellationToken)).ToHttp(static note => Results.Created($"/v1/notes/{note.Id}", NoteView.Of(note)));

	private static async Task<IResult> ListAsync(NoteQueries queries, CancellationToken cancellationToken) =>
		Results.Ok((await queries.LatestAsync(cancellationToken)).Select(NoteView.Of));

	private static async Task<IResult> GetAsync(Guid id, NoteQueries queries, CancellationToken cancellationToken) =>
		(await queries.GetAsync(id, cancellationToken)).ToHttp(static note => Results.Ok(NoteView.Of(note)));
}

/// <summary>A note to write.</summary>
/// <param name="Title">Its title.</param>
/// <param name="Body">Its text.</param>
internal sealed record NewNote(string? Title, string? Body);

/// <summary>A note as the API returns it.</summary>
/// <param name="Id">Its id.</param>
/// <param name="Title">Its title.</param>
/// <param name="Body">Its text.</param>
/// <param name="CreatedAt">When it was written.</param>
internal sealed record NoteView(Guid Id, string Title, string Body, Instant CreatedAt)
{
	public static NoteView Of(Note note) => new(note.Id, note.Title, note.Body, note.CreatedAt);
}
