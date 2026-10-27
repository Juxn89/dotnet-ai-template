---
paths:
  - "tests/**"
  - "web/e2e/**"
---

# Testing

- Pick the cheapest layer that can prove the behaviour: Domain (pure) → Application (in-memory ports) →
  Integration (Testcontainers + WebApplicationFactory) → E2E (Playwright, one flow per feature).
- Tests read as sentences (`A_spent_day_refuses_the_call_without_calling_the_model`).
- Integration tests share one Postgres per collection and must not depend on each other's data, budgets
  included: a test that spends the day's budget sets its own cap.
- Never call the real model from tests or the E2E: use the scripted client (`Ai:ScriptFile`). Real calls belong
  to the paid evals and the demo, by hand.
- A rule-like test (architecture, the spending cap, a guardrail) must be proven able to fail once.
- Nothing is done until `dotnet build`, `dotnet test`, `dotnet format whitespace --verify-no-changes`, the evals'
  oracle and, for the web, `pnpm lint` and `pnpm typecheck` ran green.
