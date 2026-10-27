namespace App.Integration.Tests;

using App.Infrastructure;
using App.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

/// <summary>One real Postgres per test run, migrated once, plus the API on top of it. Same image as the AppHost.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
	public const string Image = "postgres:18.6-alpine";

	private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(Image).Build();

	private WebApplicationFactory<Program>? _api;

	public string ConnectionString => _container.GetConnectionString();

	public WebApplicationFactory<Program> Api => _api ?? throw new InvalidOperationException("Not initialised.");

	public async Task InitializeAsync()
	{
		await _container.StartAsync();
		_api = new WebApplicationFactory<Program>().WithWebHostBuilder(host => host
			.UseEnvironment("Testing")
			.UseSetting($"ConnectionStrings:{DependencyInjection.DatabaseConnectionName}", ConnectionString));
		await DatabaseMigrator.MigrateAsync(_api.Services);
	}

	public async Task DisposeAsync()
	{
		if (_api is not null)
		{
			await _api.DisposeAsync();
		}

		await _container.DisposeAsync();
	}
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
	public const string Name = "postgres";
}
