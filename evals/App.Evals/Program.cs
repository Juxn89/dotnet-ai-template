using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using App.Evals;

// The template's evals (plan §5): every note in evals/notes.json goes through the real app (use case, meter,
// Postgres) and its summary is scored. `oracle` answers with each note's reference summary, so CI checks the harness
// and the scoring without a key; `run --allow-paid` asks Claude, guarded by the ledger.
//
//   dotnet run --project evals/App.Evals -- oracle
//   dotnet run --project evals/App.Evals -- run --allow-paid
var mode = args.FirstOrDefault() ?? "oracle";
var set = EvalSet.Load(Path.Combine("evals", "notes.json"));

if (mode == "oracle")
{
	var script = Path.Combine(Path.GetTempPath(), $"oracle-{Guid.NewGuid():N}.json");
	await File.WriteAllTextAsync(script, set.OracleScript());
	try
	{
		await using var app = await AppUnderTest.StartAsync(new() { ["Ai:ScriptFile"] = script });
		var results = await app.SummarizeAllAsync(set);
		var score = Score.Of(set, results);
		Console.WriteLine(score.Report());
		return score.Passes(set) && score.Tags == 1m ? 0 : 1;
	}
	finally
	{
		File.Delete(script);
	}
}

if (mode == "run")
{
	if (!args.Contains("--allow-paid"))
	{
		Console.WriteLine("The run calls Claude and costs money: add --allow-paid.");
		return 2;
	}

	if (PaidRun.ApiKey() is not { } key)
	{
		Console.WriteLine("No ANTHROPIC_API_KEY in the environment or in .env.");
		return 2;
	}

	var spentBefore = PaidRun.SessionSpent();
	if (!PaidRun.Affordable(set.Notes.Count, spentBefore))
	{
		Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"The session has spent USD {spentBefore:0.0000}; this run could pass its cap."));
		return 2;
	}

	// No retries: a retried call can be billed twice.
	await using var app = await AppUnderTest.StartAsync(new() { ["Anthropic:ApiKey"] = key, ["Ai:MaxRetries"] = "0" });
	var results = await app.SummarizeAllAsync(set);
	var score = Score.Of(set, results);
	Console.WriteLine(score.Report());

	var model = new AiDefaults().Model;
	PaidRun.Record(model, results, $"evals through the app, {results.Count} notes");
	PaidRun.Write(Path.Combine("evals", "results", $"{model}.json"), score.ToJson(model, results));
	return score.Passes(set) ? 0 : 1;
}

Console.WriteLine("Usage: oracle | run --allow-paid");
return 2;

/// <summary>The model's id as the app configures it by default.</summary>
internal sealed class AiDefaults
{
	public string Model { get; } = new App.Infrastructure.Ai.AiOptions().Model;
}
