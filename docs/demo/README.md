# Demo

`demo.mp4` is a short walkthrough at 1920×1080, recorded on 27 Oct 2026 against Claude Haiku 5.5, as is: the
summaries and tags are the model's.

| File | Shows |
|---|---|
| `demo.mp4` | Two notes (one in Spanish, one in English) summarized with their tags and cost, today's spend against the cap, and the page in English |
| `poster.jpg` | The video's poster, taken from it: the first summary with its tags and cost |
| `01-resumen.png` | A Spanish note with its summary, tags and the call's cost (USD 0.000099) |
| `02-gasto.png` | Today's AI spend against the daily budget, after two calls |
| `03-ingles.png` | The same page in English |

## Recording it again

```bash
cd web
pnpm e2e:deps                     # Postgres (5434) for the run
DEMO_SCRIPTED=1 pnpm demo         # rehearsal with a script: free
pnpm demo                         # the real take: ANTHROPIC_API_KEY from the environment or .env, ~USD 0.0002
```
