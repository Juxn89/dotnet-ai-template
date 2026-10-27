---
name: add-endpoint
description: Add a Minimal API endpoint backed by an Application use case, with its error codes and an integration test. Use when asked to add or expose an endpoint, route or API operation.
arguments: [feature, action]
---

Add the `$action` endpoint for the `$feature` feature.

## Existing features
!`ls src/App.Application/Features 2>/dev/null || echo "(none yet)"`

## Steps

1. Rules first: anything about what is valid goes in the Domain, with named limits and a test in
   `tests/App.Domain.Tests`.
2. Use case in `src/App.Application/Features/$feature/`: a sealed class with
   `ExecuteAsync(..., CancellationToken)` returning `Result<T>`; its errors as `static readonly Error` fields with
   dotted codes, listed in `docs/errors.md`.
3. If it needs storage or a model, add a port in Application and its adapter in Infrastructure; register both in
   `DependencyInjection.cs`.
4. Endpoint in `src/App.Api/Endpoints/$featureEndpoints.cs`: mapping only, through `ResultMapping.ToHttp`.
5. Tests: the use case against in-memory ports (`tests/App.Application.Tests`), and an integration test for the
   happy path and one failure (`tests/App.Integration.Tests`).
6. Run `dotnet build`, `dotnet test` and `dotnet format whitespace --verify-no-changes`. Report the actual output.
