namespace App.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

/// <summary>The app's tables. Rows are records of their own, mapped to and from the domain by the stores.</summary>
/// <param name="options">The options.</param>
internal sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
	public DbSet<NoteRecord> Notes => Set<NoteRecord>();

	public DbSet<NoteSummaryRecord> NoteSummaries => Set<NoteSummaryRecord>();

	public DbSet<AiUsageRecord> AiUsage => Set<AiUsageRecord>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<NoteRecord>(note =>
		{
			note.ToTable("notes");
			note.HasKey(static n => n.Id);
			note.Property(static n => n.Title).HasMaxLength(Domain.Notes.Note.MaxTitleLength).IsRequired();
			note.Property(static n => n.Body).HasMaxLength(Domain.Notes.Note.MaxBodyLength).IsRequired();
			note.HasIndex(static n => n.CreatedAt);
		});

		modelBuilder.Entity<NoteSummaryRecord>(summary =>
		{
			summary.ToTable("note_summaries");
			summary.HasKey(static s => s.NoteId);
			summary.Property(static s => s.Summary).HasMaxLength(Domain.Notes.NoteSummary.MaxSummaryLength).IsRequired();
			summary.Property(static s => s.Tags).HasColumnType("text[]");
			summary.HasOne<NoteRecord>().WithOne().HasForeignKey<NoteSummaryRecord>(static s => s.NoteId).OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<AiUsageRecord>(usage =>
		{
			usage.ToTable("ai_usage");
			usage.HasKey(static u => u.Id);
			usage.Property(static u => u.Operation).IsRequired();
			usage.Property(static u => u.Model).IsRequired();
			usage.Property(static u => u.CostUsd).HasPrecision(12, 6);
			usage.HasIndex(static u => u.OccurredAt);
		});
	}
}

/// <summary>A row of <c>notes</c>.</summary>
internal sealed class NoteRecord
{
	public Guid Id { get; set; }

	public required string Title { get; set; }

	public required string Body { get; set; }

	public Instant CreatedAt { get; set; }
}

/// <summary>A row of <c>note_summaries</c>: one per note, the latest.</summary>
internal sealed class NoteSummaryRecord
{
	public Guid NoteId { get; set; }

	public required string Summary { get; set; }

	public List<string> Tags { get; set; } = [];

	public Instant CreatedAt { get; set; }
}

/// <summary>A row of <c>ai_usage</c>: one model call.</summary>
internal sealed class AiUsageRecord
{
	public Guid Id { get; set; }

	public Instant OccurredAt { get; set; }

	public required string Operation { get; set; }

	public required string Model { get; set; }

	public long InputTokens { get; set; }

	public long OutputTokens { get; set; }

	public decimal CostUsd { get; set; }

	public long LatencyMs { get; set; }

	public string? PromptHash { get; set; }
}

internal static class PersistenceSetup
{
	public static void Configure(DbContextOptionsBuilder options, string connectionString) =>
		options
			.UseNpgsql(connectionString, static npgsql => npgsql.UseNodaTime())
			.UseSnakeCaseNamingConvention();
}

/// <summary>Applies the migrations; run at startup in Development and by the tests.</summary>
public static class DatabaseMigrator
{
	/// <summary>Migrates the database to the latest schema.</summary>
	/// <param name="services">The app's services.</param>
	/// <param name="cancellationToken">Cancellation.</param>
	/// <returns>A task.</returns>
	public static async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
	{
		await using var scope = services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
	}
}

/// <summary>For <c>dotnet ef</c>: a context without the app's configuration.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
	private const string PlaceholderConnectionString = "Host=localhost;Database=app_design";

	public AppDbContext CreateDbContext(string[] args)
	{
		var options = new DbContextOptionsBuilder<AppDbContext>();
		PersistenceSetup.Configure(options, PlaceholderConnectionString);
		return new AppDbContext(options.Options);
	}
}
