using System.Text.Json.Serialization;
using App.Api.Endpoints;
using App.Infrastructure;
using App.Infrastructure.Persistence;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

var builder = WebApplication.CreateBuilder(args);

// A scripted model answers anyone with whatever the script says: it exists for tests, evals and rehearsals only.
if (DependencyInjection.UsesScriptedModel(builder.Configuration) && !builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
{
	throw new InvalidOperationException("Ai:ScriptFile is set outside Development: the scripted model is for tests and rehearsals only.");
}

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();

// Instants and dates as ISO 8601; enums by name.
builder.Services.ConfigureHttpJsonOptions(options =>
{
	options.SerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
	options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// A malformed request (bad JSON, wrong content type) is the client's error, not a 500.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
	StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
		? badRequest.StatusCode
		: StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
	await DatabaseMigrator.MigrateAsync(app.Services);
}

app.MapDefaultEndpoints();
app.MapNoteEndpoints();
app.MapAiEndpoints();

await app.RunAsync();

/// <summary>The entry point, visible to the integration tests' <c>WebApplicationFactory</c>.</summary>
public partial class Program;
