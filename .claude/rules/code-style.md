---
paths:
  - "src/**"
  - "tests/**"
  - "evals/**"
---

# Code style

- C# 14, file-scoped namespaces, `sealed` by default, nullable on. Tabs (`.editorconfig`; the hook formats).
- No magic strings or numbers: limits are named constants with a comment saying where the value comes from, or
  options validated at startup (`ValidateDataAnnotations().ValidateOnStart()`).
- Time is `IClock` and NodaTime types; never `DateTime.Now`.
- Expected failures are `Error` values with a stable dotted code in a `Result<T>`; every new code goes into
  `docs/errors.md`. Exceptions are for bugs and for provider failures that a use case turns into an `Error`.
- Comments say *why*. No commented-out code. XML docs on public members (the analyzers ask for them).
