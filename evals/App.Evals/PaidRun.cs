namespace App.Evals;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// A run against the real model, guarded like P3's and P4's: it reads the shared ledger first and stops before the
/// session's cap, appends one row with what it really spent, and writes the results, failures included, to
/// <c>evals/results/</c>.
/// </summary>
internal static class PaidRun
{
	/// <summary>The session in <c>~/.anthropic-budget/ledger.tsv</c>; a project made from the template sets its own.</summary>
	public const string Session = "workana-dotnet-ai-template";

	/// <summary>The session's cap (README of the ledger, confirmed by the user on 27 Oct 2026).</summary>
	private const decimal SessionCapUsd = 0.10m;

	/// <summary>What one note can cost at most: 2K tokens in and 1K out with Haiku 5.5, rounded up.</summary>
	private const decimal WorstNoteUsd = 0.001m;

	private const string KeyVariable = "ANTHROPIC_API_KEY";

	private const string Project = "dotnet-ai-template";

	private const string Experiment = "eval-run";

	public static string LedgerPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".anthropic-budget", "ledger.tsv");

	/// <summary>The key, from the environment or the repo's git-ignored .env; never printed.</summary>
	/// <returns>The key, or null.</returns>
	public static string? ApiKey() =>
		Environment.GetEnvironmentVariable(KeyVariable) is { Length: > 0 } key
			? key
			: File.Exists(".env")
				? File.ReadLines(".env")
					.Select(static line => line.Trim())
					.Where(static line => line.StartsWith(KeyVariable + "=", StringComparison.Ordinal))
					.Select(static line => line[(KeyVariable.Length + 1)..].Trim().Trim('"'))
					.LastOrDefault(static value => value.Length > 0)
				: null;

	/// <summary>What the session has spent so far, from the ledger.</summary>
	/// <returns>USD.</returns>
	public static decimal SessionSpent() =>
		File.Exists(LedgerPath)
			? File.ReadLines(LedgerPath).Skip(1)
				.Select(static line => line.Split('\t'))
				.Where(static cells => cells.Length > 10 && cells[1] == Session)
				.Sum(static cells => decimal.Parse(cells[10], CultureInfo.InvariantCulture))
			: 0m;

	/// <summary>Whether a whole run still fits under the cap, at the worst cost per note.</summary>
	/// <param name="notes">How many notes.</param>
	/// <param name="spentBefore">What the session had spent before this run.</param>
	/// <returns>Whether to go on.</returns>
	public static bool Affordable(int notes, decimal spentBefore) => spentBefore + (notes * WorstNoteUsd) <= SessionCapUsd;

	/// <summary>Appends this run's row to the ledger.</summary>
	/// <param name="model">The model.</param>
	/// <param name="results">Every note in the run.</param>
	/// <param name="note">A note.</param>
	public static void Record(string model, IReadOnlyList<NoteResult> results, string note)
	{
		var row = string.Join('\t',
		[
			DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
			Session,
			Project,
			Experiment,
			model,
			results.Count(static r => r.InputTokens > 0).ToString(CultureInfo.InvariantCulture),
			results.Sum(static r => r.InputTokens).ToString(CultureInfo.InvariantCulture),
			"0",
			"0",
			results.Sum(static r => r.OutputTokens).ToString(CultureInfo.InvariantCulture),
			results.Sum(static r => r.CostUsd).ToString("0.000000", CultureInfo.InvariantCulture),
			note,
		]);
		File.AppendAllText(LedgerPath, row + "\n");
	}

	/// <summary>Writes a results file.</summary>
	/// <param name="path">Where.</param>
	/// <param name="results">The results.</param>
	public static void Write(string path, JsonObject results)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, results.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }) + "\n");
	}
}
