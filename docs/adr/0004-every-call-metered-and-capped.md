# 0004 · Every model call is metered, and the daily cap is checked before calling

- Status: accepted
- Date: 27 Oct 2026

## Context

A public demo or a busy day must not produce a surprise bill, and a change in a prompt should show up as a change
in cost. P3 and P4 summed their spend from their own tables; the original plan had Valkey for it.

## Decision

`MeteredChatClient` wraps the model for every request. Before the call it sums today's `ai_usage` (UTC) and refuses
with `ai.daily_budget` once `Ai:DailyBudgetUsd` is spent: the call is not made. After it, the tokens are priced from
configuration and a row is written with the operation, model, latency and prompt hash. `GET /v1/ai/usage` shows
today's spend against the cap.

## Consequences

One table and one index instead of a cache server. A concurrent burst can pass the cap by the calls in flight; for
a template, that is the right trade-off. A hard ceiling still belongs in the provider's console.
