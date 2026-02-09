# WebAPI

Web API datasource implementation for BeepDM.

## Main Runtime Type
- `WebAPIDataSource` (partial class across connection, HTTP, data, structure, query, and scripting files)

## Architecture
Core object graph in `WebAPIDataSource`:
- `WebAPIConfigurationHelper`
- `WebAPIAuthenticationHelper`
- `WebAPIRequestHelper`
- `WebAPIDataHelper`
- `WebAPICacheHelper`
- `WebAPIRateLimitHelper`
- `WebAPISchemaHelper`
- `WebAPIErrorHelper`

## Functional Areas
- Connection lifecycle: open/close and transaction-style stubs.
- HTTP execution: GET/POST/PUT/PATCH/DELETE with retry and auth.
- Data operations: `GetEntity`, `InsertEntity`, `UpdateEntity`, `DeleteEntity`, bulk update path.
- Structure/schema: entity discovery and inferred `EntityStructure` handling.
- Query/scripting: query wrappers and script-oriented methods.

## Configuration Model
- `WebAPIConnectionProperties` is the primary settings contract.
- Supports auth modes: none, api key, basic, bearer, oauth2.
- Supports retry, timeout, caching, rate limit, and pagination tuning.

## Implementation Notes
- Several partial files are placeholders for future expansion.
- Prefer helper services for all cross-cutting concerns instead of inline logic.
- Validate configuration before calling high-volume operations.

## Related
- `WebAPI/Helpers/README.md`
- `DataManagementEngineStandard/Docs/creating-custom-datasources.html`
