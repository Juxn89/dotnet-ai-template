# Errors

Every expected failure is an RFC 9457 problem document with a stable `code`. The `type` links here. A project made
from this template replaces the notes' rows with its own and keeps the `ai.*` ones.

| Code | Status | When |
|---|---|---|
| <a id="note-invalid_title"></a>`note.invalid_title` | 400 | An empty title, or one longer than 120 characters |
| <a id="note-invalid_body"></a>`note.invalid_body` | 400 | An empty note, or one longer than 4,000 characters |
| <a id="note-not_found"></a>`note.not_found` | 404 | No note with that id |
| <a id="ai-not_configured"></a>`ai.not_configured` | 503 | No API key: the feature needs the model |
| <a id="ai-daily_budget"></a>`ai.daily_budget` | 503 | Today's budget for the model is spent (USD 1 by default, `Ai:DailyBudgetUsd`); the call was not made |
| <a id="ai-failed"></a>`ai.failed` | 503 | The model failed, declined, or answered with something the rules reject |
