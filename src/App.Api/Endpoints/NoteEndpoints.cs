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
		notes.MapPost("/{id:guid}/summary", SummarizeAsync);
		return app;
	}

	private static async Task<IResult> CreateAsync(NewNote request, CreateNote create, CancellationToken cancellationToken) =>
		(await create.ExecuteAsync(request.Title, request.Body, cancellationToken))
			.ToHttp(static note => Results.Created($"/v1/notes/{note.Id}", NoteView.Of(new NoteEntry(note, null))));

	private static async Task<IResult> ListAsync(NoteQueries queries, CancellationToken cancellationToken) =>
		Results.Ok((await queries.LatestAsync(cancellationToken)).Select(NoteView.Of));

	private static async Task<IResult> GetAsync(Guid id, NoteQueries queries, CancellationToken cancellationToken) =>
		(await queries.GetAsync(id, cancellationToken)).ToHttp(static entry => Results.Ok(NoteView.Of(entry)));

	private static async Task<IResult> SummarizeAsync(Guid id, SummarizeNote summarize, CancellationToken cancellationToken) =>
		(await summarize.ExecuteAsync(id, cancellationToken))
			.ToHttp(static result => Results.Ok(new SummaryView(result.Summary.Summary, result.Summary.NoteTags, result.Summary.CreatedAt, result.CostUsd)));
}

/// <summary>A note to write.</summary>
/// <param name="Title">Its title.</param>
/// <param name="Body">Its text.</param>
internal sealed record NewNote(string? Title, string? Body);

/// <summary>A note's summary as the API returns it.</summary>
/// <param name="Summary">One sentence.</param>
/// <param name="Tags">Its tags.</param>
/// <param name="CreatedAt">When it was written.</param>
/// <param name="CostUsd">What the model cost, when it was just written.</param>
internal sealed record SummaryView(string Summary, IReadOnlyList<string> Tags, Instant CreatedAt, decimal? CostUsd = null);

/// <summary>A note as the API returns it.</summary>
/// <param name="Id">Its id.</param>
/// <param name="Title">Its title.</param>
/// <param name="Body">Its text.</param>
/// <param name="CreatedAt">When it was written.</param>
/// <param name="Summary">Its summary, when it has one.</param>
internal sealed record NoteView(Guid Id, string Title, string Body, Instant CreatedAt, SummaryView? Summary)
{
	public static NoteView Of(NoteEntry entry) => new(
		entry.Note.Id,
		entry.Note.Title,
		entry.Note.Body,
		entry.Note.CreatedAt,
		entry.Summary is { } s ? new SummaryView(s.Summary, s.NoteTags, s.CreatedAt) : null);
}
