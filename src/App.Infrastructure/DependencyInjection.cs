namespace App.Infrastructure;

using App.Application.Abstractions.Ai;
using App.Application.Features.Notes;
using App.Infrastructure.Ai;
using App.Infrastructure.Persistence;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NodaTime;

/// <summary>Registers the adapters and the use cases. The API calls it once; the tests and the evals against their own Postgres.</summary>
public static class DependencyInjection
{
	/// <summary>The connection string's name, the same as the database's name in the AppHost.</summary>
	public const string DatabaseConnectionName = "appdb";

	/// <summary>Whether the model is replaced by a script (end-to-end tests, the evals' oracle, the demo's rehearsal).</summary>
	/// <param name="configuration">The configuration.</param>
	/// <returns><see langword="true"/> when a script is configured.</returns>
	public static bool UsesScriptedModel(IConfiguration configuration) =>
		!string.IsNullOrEmpty(configuration?[$"{AiOptions.SectionName}:{nameof(AiOptions.ScriptFile)}"]);

	/// <summary>Adds everything the API needs.</summary>
	/// <param name="services">The services.</param>
	/// <param name="configuration">The configuration.</param>
	/// <returns>The services.</returns>
	/// <exception cref="InvalidOperationException">The connection string is missing.</exception>
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(configuration);
		var connectionString = configuration.GetConnectionString(DatabaseConnectionName)
			?? throw new InvalidOperationException(
				$"Missing connection string '{DatabaseConnectionName}'. Run through the AppHost, or set ConnectionStrings__{DatabaseConnectionName}.");

		services.AddSingleton<IClock>(SystemClock.Instance);
		services.AddDbContext<AppDbContext>(options => PersistenceSetup.Configure(options, connectionString));

		services.AddOptions<AiOptions>().Bind(configuration.GetSection(AiOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
		services.AddOptions<AnthropicOptions>().Bind(configuration.GetSection(AnthropicOptions.SectionName));
		services.AddSingleton<IChatClient>(static provider =>
			ChatModel.Create(provider.GetRequiredService<IOptions<AiOptions>>().Value, provider.GetRequiredService<IOptions<AnthropicOptions>>().Value));
		services.AddScoped<IAiUsageLog, AiUsageLog>();

		services.AddScoped<INoteStore, NoteStore>();
		services.AddScoped<INoteSummarizer, ClaudeNoteSummarizer>();
		services.AddScoped<CreateNote>();
		services.AddScoped<NoteQueries>();
		services.AddScoped<SummarizeNote>();
		services.AddScoped<AiBudgetQuery>();
		return services;
	}
}

/// <summary>What the model has cost today against the budget.</summary>
/// <param name="log">The calls.</param>
/// <param name="options">The budget.</param>
/// <param name="clock">The clock.</param>
public sealed class AiBudgetQuery(IAiUsageLog log, IOptions<AiOptions> options, IClock clock)
{
	/// <summary>Today's spend.</summary>
	/// <param name="cancellationToken">Cancellation.</param>
	/// <returns>The spend and the cap.</returns>
	public async Task<AiBudget> TodayAsync(CancellationToken cancellationToken)
	{
		var midnight = clock.GetCurrentInstant().InUtc().Date.AtStartOfDayInZone(DateTimeZone.Utc).ToInstant();
		var spent = await (log ?? throw new InvalidOperationException()).SpentSinceAsync(midnight, cancellationToken).ConfigureAwait(false);
		return new AiBudget(spent.CostUsd, spent.Calls, options.Value.DailyBudgetUsd);
	}
}
