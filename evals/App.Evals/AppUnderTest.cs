namespace App.Evals;

using App.Application.Features.Notes;
using App.Infrastructure;
using App.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

/// <summary>What happened to one note.</summary>
/// <param name="Id">The note's id in the set.</param>
/// <param name="Summary">The summary, when there was one.</param>
/// <param name="Tags">Its tags.</param>
/// <param name="Error">The error's code, when there was no summary.</param>
/// <param name="CostUsd">What the call cost.</param>
/// <param name="InputTokens">Input tokens.</param>
/// <param name="OutputTokens">Output tokens.</param>
/// <param name="LatencyMs">How long the call took.</param>
internal sealed record NoteResult(string Id, string? Summary, IReadOnlyList<string> Tags, string? Error, decimal CostUsd, long InputTokens, long OutputTokens, long LatencyMs);

/// <summary>The real app (use cases, meter, Postgres) on a throwaway database, as the API would run it.</summary>
internal sealed class AppUnderTest : IAsyncDisposable
{
	private const string Image = "postgres:18.6-alpine";

	private readonly PostgreSqlContainer _postgres;
	private readonly ServiceProvider _services;

	private AppUnderTest(PostgreSqlContainer postgres, ServiceProvider services)
	{
		_postgres = postgres;
		_services = services;
	}

	public static async Task<AppUnderTest> StartAsync(Dictionary<string, string?> settings)
	{
		var postgres = new PostgreSqlBuilder(Image).Build();
		await postgres.StartAsync();
		settings[$"ConnectionStrings:{DependencyInjection.DatabaseConnectionName}"] = postgres.GetConnectionString();
		var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
		var services = new ServiceCollection().AddLogging().AddInfrastructure(configuration).BuildServiceProvider();
		await DatabaseMigrator.MigrateAsync(services);
		return new AppUnderTest(postgres, services);
	}

	/// <summary>Writes each note and summarizes it, one at a time.</summary>
	/// <param name="set">The notes.</param>
	/// <returns>What happened to each.</returns>
	public async Task<IReadOnlyList<NoteResult>> SummarizeAllAsync(EvalSet set)
	{
		var results = new List<NoteResult>();
		foreach (var item in set.Notes)
		{
			await using var scope = _services.CreateAsyncScope();
			var note = (await scope.ServiceProvider.GetRequiredService<CreateNote>().ExecuteAsync(item.Title, item.Body, CancellationToken.None)).Value;
			var summary = await scope.ServiceProvider.GetRequiredService<SummarizeNote>().ExecuteAsync(note.Id, CancellationToken.None);
			var call = scope.ServiceProvider.GetRequiredService<AppDbContext>().AiUsage.OrderByDescending(static u => u.OccurredAt).FirstOrDefault();
			results.Add(new NoteResult(
				item.Id,
				summary.IsSuccess ? summary.Value.Summary.Summary : null,
				summary.IsSuccess ? summary.Value.Summary.NoteTags : [],
				summary.Error?.Code,
				call?.CostUsd ?? 0m,
				call?.InputTokens ?? 0,
				call?.OutputTokens ?? 0,
				call?.LatencyMs ?? 0));
			Console.WriteLine($"{item.Id} {(summary.IsSuccess ? string.Join(',', summary.Value.Summary.NoteTags) + " | " + summary.Value.Summary.Summary : summary.Error!.Code)}");
		}

		return results;
	}

	public async ValueTask DisposeAsync()
	{
		await _services.DisposeAsync();
		await _postgres.DisposeAsync();
	}
}
