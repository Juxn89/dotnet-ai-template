namespace App.Infrastructure.Ai;

using System.ComponentModel.DataAnnotations;

/// <summary>The model, its prices and its limits, bound from the <c>Ai</c> section and validated at startup.</summary>
public sealed class AiOptions
{
	/// <summary>The configuration section.</summary>
	public const string SectionName = "Ai";

	/// <summary>Claude Haiku 5.5 for every call, as in P1-P4 (AGENTS.md).</summary>
	[Required]
	public string Model { get; set; } = "claude-haiku-5-5";

	/// <summary>A summary is a short JSON object plus Haiku 5.5's adaptive thinking, which counts toward this cap.</summary>
	[Range(256, 32_000)]
	public int MaxOutputTokens { get; set; } = 1024;

	/// <summary>Summarizing a note needs little reasoning, so <c>low</c>, as in P2-P4.</summary>
	[Required]
	public string Effort { get; set; } = "low";

	/// <summary>USD per million input tokens: Haiku 5.5 for prompts up to 100K tokens (models reference, 23 Oct 2026).</summary>
	[Range(0, 100)]
	public decimal InputUsdPerMillionTokens { get; set; } = 0.10m;

	/// <summary>USD per million output tokens, thinking included, same source and date.</summary>
	[Range(0, 100)]
	public decimal OutputUsdPerMillionTokens { get; set; } = 0.50m;

	/// <summary>What all calls together may spend per UTC day; past it, calls are refused before they are made.</summary>
	[Range(0.0001, 1000)]
	public decimal DailyBudgetUsd { get; set; } = 1.00m;

	/// <summary>A call should take seconds; past this something is stuck.</summary>
	[Range(typeof(TimeSpan), "00:00:05", "00:05:00")]
	public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

	/// <summary>The SDK's retries of connection errors, 429s and 5xx. The paid evals set 0: a retry can be billed twice.</summary>
	[Range(0, 10)]
	public int MaxRetries { get; set; } = 2;

	/// <summary>
	/// A JSON file of scripted replies instead of the model: the end-to-end tests, the evals' oracle and the demo's
	/// rehearsal. Development and Testing only; the API refuses to start with it anywhere else.
	/// </summary>
	public string? ScriptFile { get; set; }

	private const decimal TokensPerMillion = 1_000_000m;

	/// <summary>What a call cost.</summary>
	/// <param name="inputTokens">Input tokens.</param>
	/// <param name="outputTokens">Output tokens.</param>
	/// <returns>USD.</returns>
	public decimal Cost(long inputTokens, long outputTokens) =>
		((inputTokens * InputUsdPerMillionTokens) + (outputTokens * OutputUsdPerMillionTokens)) / TokensPerMillion;
}

/// <summary>The Anthropic key, bound from the <c>Anthropic</c> section. It comes from the environment, never the repo.</summary>
public sealed class AnthropicOptions
{
	/// <summary>The configuration section.</summary>
	public const string SectionName = "Anthropic";

	/// <summary>The API key. Without one the AI endpoints answer 503 and the rest of the app works.</summary>
	public string? ApiKey { get; set; }
}
