namespace App.Integration.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

/// <summary>The notes' endpoints on a real Postgres: what the web app and a project's own slices build on.</summary>
[Collection(PostgresCollection.Name)]
public sealed class NotesApiTests(PostgresFixture postgres)
{
	private readonly HttpClient _client = postgres.Api.CreateClient();

	[Fact]
	public async Task A_note_is_written_read_back_and_listed()
	{
		var created = await _client.PostAsJsonAsync("/v1/notes", new { title = "Pedido de café", body = "Llamar al proveedor el lunes." });
		Assert.Equal(HttpStatusCode.Created, created.StatusCode);
		var note = await created.Content.ReadFromJsonAsync<JsonElement>();
		var id = note.GetProperty("id").GetGuid();

		var read = await _client.GetFromJsonAsync<JsonElement>($"/v1/notes/{id}");
		Assert.Equal("Pedido de café", read.GetProperty("title").GetString());
		Assert.Equal(created.Headers.Location?.ToString(), $"/v1/notes/{id}");

		var list = await _client.GetFromJsonAsync<JsonElement>("/v1/notes");
		Assert.Contains(list.EnumerateArray(), n => n.GetProperty("id").GetGuid() == id);
	}

	[Fact]
	public async Task An_invalid_note_is_a_400_with_its_stable_code()
	{
		var response = await _client.PostAsJsonAsync("/v1/notes", new { title = "", body = "Texto" });

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
		Assert.Equal("note.invalid_title", problem.GetProperty("code").GetString());
	}

	[Fact]
	public async Task An_unknown_note_is_a_404()
	{
		var response = await _client.GetAsync($"/v1/notes/{Guid.NewGuid()}");

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.Equal("note.not_found", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
	}
}
