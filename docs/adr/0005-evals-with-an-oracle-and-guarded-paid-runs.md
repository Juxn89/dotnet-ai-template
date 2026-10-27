# 0005 · Evals: an oracle in CI, and paid runs by hand behind the ledger

- Status: accepted
- Date: 27 Oct 2026

## Context

A prompt change can make an AI feature worse without any test failing. Running the model in every CI build costs
money and needs a secret; not running it leaves the harness itself untested.

## Decision

`evals/App.Evals` runs a golden set through the real app. `oracle` replays each item's reference answer through the
scripted client, so CI checks the harness and the scoring for free. `run --allow-paid` asks the real model by hand:
it reads the shared ledger first, stops before the session's cap, records what it spent and writes the results,
failures included, to `evals/results/`. Scoring is deterministic; pass marks live next to the golden set.

## Consequences

Every prompt change is followed by a paid run of a fraction of a cent. The results files are the history of the
feature's quality, and the README quotes them.
