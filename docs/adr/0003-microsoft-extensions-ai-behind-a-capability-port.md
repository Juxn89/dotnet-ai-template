# 0003 · Microsoft.Extensions.AI in Infrastructure, behind a port named after the capability

- Status: accepted
- Date: 27 Oct 2026

## Context

The model's provider will change (price, quality, a client's own contract), and tests must not call it. A use case
also should not know about chat messages, schemas or tokens.

## Decision

The Application port is the capability: `INoteSummarizer` proposes a summary. In Infrastructure, every model is an
`IChatClient` from Microsoft.Extensions.AI: Claude through the official `Anthropic` SDK (MIT), a scripted client for
tests, the evals' oracle and rehearsals, or a stand-in without a key. Structured output goes through the SDK's raw
request (`OutputConfig.Format` with a JSON Schema built from the domain's list), and the answer is checked again
by the domain. Prompts are Markdown files embedded in the assembly, versioned by their hash.

## Consequences

Changing provider is a new `IChatClient`; a use case and its tests do not move. The scripted client lets CI run
the whole path, end to end, with no key and no cost. Semantic Kernel was not needed: one call per feature.
