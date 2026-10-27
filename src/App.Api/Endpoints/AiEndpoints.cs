namespace App.Api.Endpoints;

using App.Infrastructure;

/// <summary>What the model has cost today: the page shows it next to every AI feature.</summary>
internal static class AiEndpoints
{
	public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
	{
		app.MapGet("/v1/ai/usage", static async (AiBudgetQuery budget, CancellationToken cancellationToken) =>
			Results.Ok(await budget.TodayAsync(cancellationToken))).WithTags("AI");
		return app;
	}
}
