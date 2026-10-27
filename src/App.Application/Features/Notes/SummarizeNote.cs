namespace App.Application.Features.Notes;

using App.Application.Abstractions.Ai;
using App.Application.Common;
using App.Domain.Notes;
using NodaTime;

/// <summary>What the model proposed for a note, before the domain accepts it.</summary>
/// <param name="Summary">One sentence.</param>
/// <param name="Tags">Tags from <see cref="NoteSummary.Tags"/>.</param>
/// <param name="CostUsd">What the call cost.</param>
public sealed record SummaryDraft(string Summary, IReadOnlyList<string> Tags, decimal CostUsd);

/// <summary>Summarizes a note with a model. The port is the capability, not the provider: ADR 0003.</summary>
public interface INoteSummarizer
{
	/// <summary>Whether it can summarize at all (a model needs a key).</summary>
	bool IsConfigured { get; }

	/// <summary>Proposes a summary and tags.</summary>
	/// <param name="note">The note.</param>
	/// <param name="cancellationToken">Cancellation.</param>
	/// <returns>The proposal, or why there is none (<see cref="AiErrors"/>).</returns>
	Task<Result<SummaryDraft>> SummarizeAsync(Note note, CancellationToken cancellationToken);
}

/// <summary>A note's summary and what it cost.</summary>
/// <param name="Summary">The summary.</param>
/// <param name="CostUsd">What the model cost.</param>
public sealed record SummarizedNote(NoteSummary Summary, decimal CostUsd);

/// <summary>
/// Summarizes a note: the model proposes a sentence and tags, the domain checks them, and the summary is saved.
/// A proposal the rules reject is a failure of the model, not of the person, so it is <c>ai.failed</c>.
/// </summary>
/// <param name="store">The notes.</param>
/// <param name="summarizer">The model.</param>
/// <param name="clock">The clock.</param>
public sealed class SummarizeNote(INoteStore store, INoteSummarizer summarizer, IClock clock)
{
	/// <summary>Summarizes it.</summary>
	/// <param name="noteId">The note.</param>
	/// <param name="cancellationToken">Cancellation.</param>
	/// <returns>The summary, or why there is none.</returns>
	public async Task<Result<SummarizedNote>> ExecuteAsync(Guid noteId, CancellationToken cancellationToken)
	{
		if (await store.FindAsync(noteId, cancellationToken).ConfigureAwait(false) is not { } entry)
		{
			return NoteQueries.NotFound;
		}

		if (!summarizer.IsConfigured)
		{
			return AiErrors.NotConfigured;
		}

		var draft = await summarizer.SummarizeAsync(entry.Note, cancellationToken).ConfigureAwait(false);
		if (!draft.IsSuccess)
		{
			return draft.Error!;
		}

		if (NoteSummary.TryCreate(noteId, draft.Value.Summary, draft.Value.Tags, clock.GetCurrentInstant()) is not { } summary)
		{
			return AiErrors.Failed;
		}

		await store.SaveSummaryAsync(summary, cancellationToken).ConfigureAwait(false);
		return new SummarizedNote(summary, draft.Value.CostUsd);
	}
}

/// <summary>What the model has cost today, against the budget: for the page that shows it.</summary>
/// <param name="SpentTodayUsd">USD since 00:00 UTC.</param>
/// <param name="CallsToday">Calls since 00:00 UTC.</param>
/// <param name="DailyBudgetUsd">The cap.</param>
public sealed record AiBudget(decimal SpentTodayUsd, int CallsToday, decimal DailyBudgetUsd);
