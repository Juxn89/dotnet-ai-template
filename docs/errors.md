# Errors

Every expected failure is an RFC 9457 problem document with a stable `code`. The `type` links here. A project made
from this template replaces the notes' rows with its own.

| Code | Status | When |
|---|---|---|
| <a id="note-invalid_title"></a>`note.invalid_title` | 400 | An empty title, or one longer than 120 characters |
| <a id="note-invalid_body"></a>`note.invalid_body` | 400 | An empty note, or one longer than 4,000 characters |
| <a id="note-not_found"></a>`note.not_found` | 404 | No note with that id |
