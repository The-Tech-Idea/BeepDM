# REST API Helper

## Purpose
This folder contains REST-specific helper logic for treating HTTP APIs as BeepDM data sources.

## Key Files
- `RestApiHelper.cs`: Helper for request/response mapping, query translation, and capability signaling.

## Runtime Flow
1. Translate BeepDM filters and operations into HTTP request semantics.
2. Execute CRUD-style API requests through configured transport.
3. Map payloads back into entity-oriented results.

## Extension Guidelines
- Keep query parameter encoding deterministic.
- Normalize API error payloads into `IErrorsInfo`.
- Support pagination and filtering contracts consistently with other helpers.
