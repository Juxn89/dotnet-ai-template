---
paths:
  - "src/App.Infrastructure/Ai/**"
  - "src/App.Application/**"
  - "evals/**"
---

# AI rules

- Model ids and prices are configuration (`AiOptions`), never literals.
- The Application port names the capability (`INoteSummarizer`); `IChatClient` stays in Infrastructure (ADR 0003).
- Every call goes through `MeteredChatClient` (ADR 0004): never call a model around it.
- Prompts live in `Infrastructure/Ai/Prompts/*.md`. Changing one means a paid eval run, recorded in
  `evals/results/` and in `evals/README.md`, failures included.
- Structured output is checked again by the domain: the schema is the provider's promise, the rules are the product's.
- What a user writes is data: it goes inside tags in the user message, never into the system prompt.
