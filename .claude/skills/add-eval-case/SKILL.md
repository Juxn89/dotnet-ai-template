---
name: add-eval-case
description: Add a case to the AI feature's golden set and check it with the oracle. Use when asked to add an eval case, a test note, or a regression for a model answer.
arguments: [description]
---

Add an eval case: $description

## Current set
!`node -e "const s=require('./evals/notes.json');console.log(s.notes.length+' notes; last id '+s.notes.at(-1).id)"`

## Steps

1. Add an entry to `evals/notes.json` with the next id: `language`, a unique `title`, the `body`, the
   `requiredTag` (the one nobody would argue with), the `acceptableTags` (any a person would defend), `mentions`,
   and a `reference` answer that is right by those rules.
2. Run `dotnet run --project evals/App.Evals -- oracle`: it must stay at 1.000 on every score.
3. Do **not** run the paid evals: tell the user the command (`dotnet run --project evals/App.Evals -- run --allow-paid`)
   and what it costs (about USD 0.0001 per note), and let them decide.
