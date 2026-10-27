# dotnet-ai-template

A GitHub template for small AI products on .NET 10: layered vertical slices, a metered and capped model port,
evals with an oracle in CI, Aspire, Testcontainers, and a Next.js front end.

Work in progress (day 1 of 3). The full README, with how to start a project from it, comes with the last day.

```bash
dotnet run --project src/App.AppHost   # Postgres and the API, through .NET Aspire
dotnet test                            # needs Docker (Testcontainers)
```

MIT licence.
