---
paths:
  - "src/App.Api/**"
---

# API conventions

- Minimal APIs, one `<Feature>Endpoints.cs` per feature, mapping only: no logic in endpoints.
- Routes are versioned (`/v1/...`) and plural nouns; actions on a resource are sub-routes (`POST /v1/notes/{id}/summary`).
- Failures go through `ResultMapping.ToHttp`, which writes RFC 9457 problem details with the stable `code` and a
  `type` linking to `docs/errors.md`. Never pick a status code in an endpoint.
- Responses are view records (`NoteView`), never domain or EF types.
- Anything that calls the model returns its cost, and its errors are the `ai.*` ones.
