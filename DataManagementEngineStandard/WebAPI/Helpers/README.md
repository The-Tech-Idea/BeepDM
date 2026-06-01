# WebAPI Helpers

Helper stack used by `WebAPIDataSource`. All helpers live in the `TheTechIdea.Beep.WebAPI.Helpers` namespace and implement `IDisposable`.

## Helper Responsibilities

- **`WebAPIConfigurationHelper`**
  - Reads config from `IConnectionProperties` / `WebAPIConnectionProperties`
  - Exposes strongly-typed properties: `BaseUrl`, `ApiVersion`, `AuthenticationType`, `TimeoutMs`, `MaxRetries`, `RetryDelayMs`, `CacheEnabled`, `CacheDurationMinutes`, `MaxConcurrentRequests`, `RateLimitRequestsPerMinute`, `EnableRateLimit`, `EnableCompression`, `UserAgent`
  - Endpoint configuration dictionary and pagination settings (`PageNumberParameter`, `PageSizeParameter`, `DefaultPageSize`, `MaxPageSize`)
  - Response handling: `ResponseFormat`, `DataPath`, `TotalCountPath`
  - Proxy, SSL, and certificate configuration properties
  - `ValidateConfiguration()` and `GetValidationErrors()` methods

- **`WebAPIAuthenticationHelper`**
  - `EnsureAuthenticatedAsync()` — validates/refreshes auth state before requests
  - `AddAuthenticationHeaders(HttpRequestMessage)` — injects appropriate auth headers
  - Supports: None, ApiKey (header/query param), Basic, Bearer (static), OAuth2 (client credentials)
  - Token caching with `_tokenExpiry` tracking and thread-safe refresh via `_tokenLock`
  - Compatible with Instagram Basic Display API (`AccessToken`, `AppId`, `AppSecret` aliases)

- **`WebAPIRequestHelper`**
  - `SendWithRetryAsync(HttpRequestMessage, operationName)` — centralized HTTP execution
  - Concurrency control via `SemaphoreSlim` (configurable max concurrent requests)
  - Request cloning for safe retries
  - Delegates retry/backoff logic to `WebAPIErrorHelper.ExecuteWithRetryAsync`

- **`WebAPIDataHelper`**
  - `BuildEndpointUrl(baseUrl, endpoint, filters, pageNumber, pageSize)` — URL construction with query params
  - `ProcessApiResponse<T>(HttpResponseMessage)` — response deserialization
  - `ExtractIdFromResponse<T>(T response, string idField)` — ID extraction
  - `GenerateDataCacheKey(entityName, filters)` — cache-key generation
  - `ParseJsonArray`, `ParseJsonObject`, `TransformToJsonString` — JSON utilities
  - `ConvertToEntityStructure` — dynamic type creation from JSON samples

- **`WebAPISchemaHelper`**
  - `InferSchemaFromJsonAsync(HttpClient, entityName, endpoint)` — schema inference from JSON response samples
  - `DiscoverEntitiesAsync(HttpClient, properties)` — entity discovery from API
  - `InferEntityStructureAsync(entityName, HttpClient, properties)` — per-entity schema inference
  - `GetEntityType(entityName)`, `ValidateEntityStructure(EntityStructure)`, `EntityExistsAsync(entityName)`
  - Schema caching with configurable expiration (`SchemaCacheExpirationMinutes`)
  - Configurable `MaxNestingDepth` and `MinSampleSize` for schema analysis

- **`WebAPICacheHelper`**
  - Concurrent in-memory cache backed by `ConcurrentDictionary<string, CacheEntry>`
  - `Get<T>(cacheKey)`, `Set(cacheKey, value, ttlMinutes)`, `Remove(cacheKey)`, `Clear()`, `Contains(cacheKey)`
  - `GenerateCacheKey(operation, parameters)` — structured key format: `{datasource}:{operation}:{params}`
  - Automatic expired-entry cleanup via background timer (every 5 minutes)
  - Cache statistics tracking (hit/miss counts)

- **`WebAPIRateLimitHelper`**
  - Token bucket algorithm for smooth rate limiting
  - `WaitForCapacityAsync(entityName)` — blocks until capacity is available
  - `AcquireAsync(entityName, tokens)` — acquire specific number of tokens
  - Per-endpoint buckets via `ConcurrentDictionary<string, TokenBucket>`
  - Configurable `DefaultRequestsPerSecond`, `DefaultBurstCapacity`, `CleanupIntervalMinutes`
  - Automatic stale bucket cleanup via background timer

- **`WebAPIErrorHelper`**
  - `HandleErrorResponseAsync(HttpResponseMessage)` — normalized error extraction from API responses
  - `ExecuteWithRetryAsync<T>(Func<Task<T>> operation, ...)` — retry with exponential backoff
  - Circuit breaker pattern: configurable `FailureThreshold`, `RecoveryTimeoutSeconds`
  - `IsTransientError(HttpResponseMessage)` — classifies HTTP status codes as transient vs permanent
  - `CategorizeError(Exception)` — error categorization for diagnostic logging
  - `DefaultMaxRetries = 3`, `DefaultBaseDelayMs = 1000`

- **`WebAPIInterfaces`**
  - `IWebAPISchemaHelper` — contract for schema discovery
  - `IWebAPIDataHelper` — contract for data transformation
  - `DataValidationResult` — validation result model

## Integration Pattern

`WebAPIDataSource` composes all helpers in its constructor:

```csharp
_configHelper  = new WebAPIConfigurationHelper(...)
_authHelper    = new WebAPIAuthenticationHelper(...)
_errorHelper   = new WebAPIErrorHelper(...)
_requestHelper = new WebAPIRequestHelper(...)
_cacheHelper   = new WebAPICacheHelper(...)
_dataHelper    = new WebAPIDataHelper(...)
_rateLimitHelper = new WebAPIRateLimitHelper(...)
_schemaHelper  = new WebAPISchemaHelper(...)
```

The `HttpClient` is shared across `_authHelper` and `_requestHelper`. All helpers that allocate managed resources (timers, semaphores, etc.) are disposed in `WebAPIDataSource.Dispose()`.

## Extension Guidance

- Add new behavior in the appropriate helper class first
- Keep `WebAPIDataSource` partial methods focused on orchestration and contract compliance
- Implement new interfaces in `WebAPIInterfaces.cs` for testability
- Use `WebAPIConfigurationHelper` to expose new config properties rather than reading `IConnectionProperties` directly
