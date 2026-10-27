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
