namespace App.Domain.Tests;

using App.Domain.Notes;
using NodaTime;

public sealed class NoteSummaryTests
{
	private static readonly Instant Now = Instant.FromUtc(2026, 10, 27, 15, 0);

	[Fact]
	public void A_summary_with_tags_from_the_list_is_accepted()
	{
		var summary = NoteSummary.TryCreate(Guid.NewGuid(), " Llamar al proveedor de café el lunes. ", ["work", "shopping"], Now);

		Assert.NotNull(summary);
		Assert.Equal("Llamar al proveedor de café el lunes.", summary.Summary);
		Assert.Equal(["work", "shopping"], summary.NoteTags);
	}

	[Fact]
	public void No_tags_is_a_valid_answer() =>
		Assert.NotNull(NoteSummary.TryCreate(Guid.NewGuid(), "Una idea suelta.", [], Now));

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void A_summary_needs_a_sentence(string text) =>
		Assert.Null(NoteSummary.TryCreate(Guid.NewGuid(), text, ["work"], Now));

	[Fact]
	public void Tags_off_the_list_repeated_or_too_many_are_refused()
	{
		Assert.Null(NoteSummary.TryCreate(Guid.NewGuid(), "Texto.", ["urgent"], Now));
		Assert.Null(NoteSummary.TryCreate(Guid.NewGuid(), "Texto.", ["work", "work"], Now));
		Assert.Null(NoteSummary.TryCreate(Guid.NewGuid(), "Texto.", ["work", "money", "ideas", "health"], Now));
		Assert.Null(NoteSummary.TryCreate(Guid.NewGuid(), new string('a', NoteSummary.MaxSummaryLength + 1), ["work"], Now));
	}
}
