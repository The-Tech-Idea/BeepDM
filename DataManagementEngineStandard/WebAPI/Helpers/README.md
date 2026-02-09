# WebAPI Helpers

Helper stack used by `WebAPIDataSource`.

## Helper Responsibilities
- `WebAPIConfigurationHelper`
  - Reads config from `IConnectionProperties` and validates runtime settings.
  - Resolves endpoint and pagination configurations.

- `WebAPIAuthenticationHelper`
  - Applies auth headers.
  - Manages bearer/oauth token refresh flows.

- `WebAPIRequestHelper`
  - Centralized `HttpClient` request execution with retry and concurrency control.

- `WebAPIDataHelper`
  - Endpoint URL construction, response parsing, id extraction, cache-key helpers.

- `WebAPISchemaHelper`
  - Schema inference from JSON samples and schema validation/update.

- `WebAPICacheHelper`
  - In-memory response caching.

- `WebAPIRateLimitHelper`
  - Request throttling and rate state helpers.

- `WebAPIErrorHelper`
  - Normalized error handling and reporting.

## Integration Pattern
`WebAPIDataSource` composes these helpers during initialization and routes all concern-specific logic through them.

## Extension Guidance
- Add new behavior in the appropriate helper class first.
- Keep `WebAPIDataSource` methods focused on orchestration and contract compliance.
