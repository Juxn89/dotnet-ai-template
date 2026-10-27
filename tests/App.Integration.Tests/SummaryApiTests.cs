namespace App.Integration.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using App.Infrastructure;
using App.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The summary endpoint with the scripted model, on a real Postgres: every call is recorded in <c>ai_usage</c>, and
/// once the day's budget is spent the next call is refused before it reaches the model.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class SummaryApiTests(PostgresFixture postgres) : IAsyncLifetime
{
	private const string Marker = "proveedor de café";

	private readonly string _script = Path.Combine(Path.GetTempPath(), $"summary-script-{Guid.NewGuid():N}.json");

	public Task InitializeAsync() => File.WriteAllTextAsync(_script, $$"""
		[{ "match": "{{Marker}}", "reply": { "language": "Spanish", "summary": "Llamar al proveedor de café el lunes.", "tags": ["work", "shopping"] } },
		 { "match": "etiqueta inventada", "reply": { "language": "Spanish", "summary": "Una nota.", "tags": ["urgent"] } }]
		""");

	public Task DisposeAsync()
	{
		File.Delete(_script);
		return Task.CompletedTask;
	}

	[Fact]
	public async Task A_summary_is_saved_shown_with_the_note_and_recorded_as_a_call()
	{
		using var api = Api();
		var client = api.CreateClient();
		var id = await NewNote(client, $"Llamar al {Marker} el lunes.");

		var response = await client.PostAsync($"/v1/notes/{id}/summary", null);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var note = await client.GetFromJsonAsync<JsonElement>($"/v1/notes/{id}");
		Assert.Equal(["work", "shopping"], note.GetProperty("summary").GetProperty("tags").EnumerateArray().Select(static t => t.GetString()));
		var usage = await client.GetFromJsonAsync<JsonElement>("/v1/ai/usage");
		Assert.True(usage.GetProperty("callsToday").GetInt32() >= 1);
	}

	[Fact]
	public async Task A_reply_the_rules_reject_is_a_503_and_no_summary_is_saved()
	{
		using var api = Api();
		var client = api.CreateClient();
		var id = await NewNote(client, "Esta nota trae una etiqueta inventada.");

		var response = await client.PostAsync($"/v1/notes/{id}/summary", null);

		Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
		Assert.Equal("ai.failed", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
		Assert.Equal(JsonValueKind.Null, (await client.GetFromJsonAsync<JsonElement>($"/v1/notes/{id}")).GetProperty("summary").ValueKind);
	}

	[Fact]
	public async Task Once_the_days_budget_is_spent_the_next_call_is_refused_before_the_model()
	{
		// A budget of a hundredth of a cent, already spent by an earlier call today; small enough not to spend the other
		// tests' default budget on the same database.
		using var api = Api(("Ai:DailyBudgetUsd", "0.0001"));
		var client = api.CreateClient();
		await using (var scope = api.Services.CreateAsyncScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			db.AiUsage.Add(new AiUsageRecord { Id = Guid.NewGuid(), OccurredAt = NodaTime.SystemClock.Instance.GetCurrentInstant(), Operation = "test", Model = "test", CostUsd = 0.001m });
			await db.SaveChangesAsync();
		}

		var id = await NewNote(client, $"Llamar al {Marker} el martes.");
		var calls = await Calls(api);
		var response = await client.PostAsync($"/v1/notes/{id}/summary", null);

		Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
		Assert.Equal("ai.daily_budget", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
		Assert.Equal(calls, await Calls(api));
	}

	private WebApplicationFactory<Program> Api(params (string Key, string Value)[] settings) =>
		postgres.Api.WithWebHostBuilder(host =>
		{
			host.UseSetting("Ai:ScriptFile", _script);
			foreach (var (key, value) in settings)
			{
				host.UseSetting(key, value);
			}
		});

	private static async Task<Guid> NewNote(HttpClient client, string body)
	{
		var created = await client.PostAsJsonAsync("/v1/notes", new { title = "Nota de prueba", body });
		return (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
	}

	private static async Task<int> Calls(WebApplicationFactory<Program> api)
	{
		await using var scope = api.Services.CreateAsyncScope();
		return scope.ServiceProvider.GetRequiredService<AppDbContext>().AiUsage.Count();
	}
}
