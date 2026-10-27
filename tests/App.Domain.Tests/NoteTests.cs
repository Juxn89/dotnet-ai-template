namespace App.Domain.Tests;

using App.Domain.Notes;
using NodaTime;

public sealed class NoteTests
{
	private static readonly Instant Now = Instant.FromUtc(2026, 10, 27, 15, 0);

	[Fact]
	public void A_note_keeps_its_text_trimmed_and_the_instant_it_was_written()
	{
		var note = Note.Create("  Pedido de café  ", "\nLlamar al proveedor el lunes.\n", Now).Note!;

		Assert.Equal("Pedido de café", note.Title);
		Assert.Equal("Llamar al proveedor el lunes.", note.Body);
		Assert.Equal(Now, note.CreatedAt);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void A_note_needs_a_title(string? title) =>
		Assert.Equal(NoteRule.Title, Note.Create(title, "Texto", Now).Broken);

	[Fact]
	public void A_title_or_a_text_past_its_limit_is_refused()
	{
		Assert.Equal(NoteRule.Title, Note.Create(new string('a', Note.MaxTitleLength + 1), "Texto", Now).Broken);
		Assert.Equal(NoteRule.Body, Note.Create("Título", new string('a', Note.MaxBodyLength + 1), Now).Broken);
		Assert.NotNull(Note.Create(new string('a', Note.MaxTitleLength), new string('a', Note.MaxBodyLength), Now).Note);
	}

	[Fact]
	public void Ids_follow_the_order_notes_are_written_in()
	{
		var first = Note.Create("Uno", "Texto", Now).Note!;
		var second = Note.Create("Dos", "Texto", Now.Plus(Duration.FromSeconds(1))).Note!;

		Assert.True(first.Id.CompareTo(second.Id) < 0);
	}
}
