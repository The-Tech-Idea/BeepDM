# Web API Data Source (BeepDM)

A robust REST/Web API data source for BeepDM providing database-like operations, helpers, and utilities to call, authenticate, cache, transform, and validate API interactions.

## What’s new

- Unified configuration via WebAPIConnectionProperties (no separate auth DTOs)
- Validation with local ConfigurationValidationResult (IsValid, Errors, Warnings)
- New HTTP convenience methods in WebAPIDataSource.Http.cs:
  - GetAsync(string urlOrEndpoint, Dictionary<string,string> query = null, Dictionary<string,string> headers = null)
  - GetAsync<T>(...) deserializes JSON response
- Consistent usage of helpers across partials (auth, request retry, error handling, schema, cache, rate limit)

## Project layout (partials)

- WebAPIDataSource.cs: core state, ctor, DI of helpers
- WebAPIDataSource.Connection.cs: open/close, transactional stubs
- WebAPIDataSource.Data.cs: CRUD operations
- WebAPIDataSource.Query.cs: RunQuery, GetScalar
- WebAPIDataSource.Structure.cs: entities discovery and schema
- WebAPIDataSource.Http.cs: HTTP convenience methods (GetAsync)
- WebAPIDataSource.*.cs: other specialized behaviors (paging, scripting, entity ops)

## Helpers

- WebAPIConfigurationHelper
  - Uses WebAPIConnectionProperties directly
  - GetConfigValue<T>(), GetHeaders(), GetEndpointConfiguration(entity)
  - ValidateConfiguration() -> ConfigurationValidationResult (IsValid, Errors, Warnings)
- WebAPIAuthenticationHelper
  - Uses WebAPIConnectionProperties for all auth
  - Supports AuthType: None, ApiKey, Basic, Bearer, OAuth2
  - EnsureAuthenticatedAsync(), AddAuthenticationHeaders(HttpRequestMessage)
- WebAPIRequestHelper
  - Centralized SendWithRetryAsync, concurrency control
- WebAPICacheHelper
  - In-memory response cache with TTL
- WebAPIDataHelper
  - BuildEndpointUrl(baseUrl, endpoint, filters, pagination)
  - ProcessApiResponse()/ProcessJson* helpers, CreateRequest, AddCustomHeaders
- WebAPIRateLimitHelper
  - Simple per-minute throttling hooks
- WebAPISchemaHelper
  - Infer schema/EntityStructure from sample JSON
- WebAPIErrorHelper
  - Normalize/handle error responses

## Configuration (WebAPIConnectionProperties)

Core URL/auth
- Url: base URL (https://api.example.com)
- AuthType: None | ApiKey | Basic | Bearer | OAuth2
- ApiKey / ApiKeyHeader (default X-API-Key)
- UserID / Password (Basic)
- ClientId / ClientSecret / TokenUrl / AuthUrl / Scope / GrantType (OAuth2)

Behavior
- TimeoutMs, RetryCount, RetryDelayMs
- EnableCaching, CacheExpiryMinutes
- MaxConcurrentRequests
- EnableRateLimit, RateLimitRequestsPerMinute

Pagination and response
- PageNumberParameter (default page), PageSizeParameter (default limit)
- ResponseFormat, DataPath, TotalCountPath

Headers and parameters
- Headers: List<WebApiHeader>
- Parameters: string key=value;... (optional)

## Endpoints configuration

WebAPIConfigurationHelper.GetEndpointConfiguration(entityName) resolves:
- Endpoints.{entity}.Get / Post / Put / Delete / List
- Endpoints.{entity}.Method, RequiresAuth, CacheDuration, RateLimit
Falls back to "/{entity}" when not supplied.

## Data operations (WebAPIDataSource.Data.cs)

CRUD functions exposed by the data source:

- IBindingList GetEntity(string entityName, List<AppFilter> filter)
  - GET {BaseUrl}/{Endpoints.{entity}.Get} with optional filters appended as query string
  - Parses JSON to BindingList<object> (dictionary per row)

- PagedResult GetEntity(string entityName, List<AppFilter> filter, int pageNumber, int pageSize)
  - GET {BaseUrl}/{Endpoints.{entity}.List} with paging parameters
  - Returns data plus TotalRecords (from X-Total-Count header or JSON fields)

- Task<IBindingList> GetEntityAsync(string entityName, List<AppFilter> filter)
  - Async wrapper for GetEntity

- IErrorsInfo InsertEntity(string entityName, object data)
  - POST {BaseUrl}/{Endpoints.{entity}.Post} with JSON body

- IErrorsInfo UpdateEntity(string entityName, object data)
  - PUT {BaseUrl}/{Endpoints.{entity}.Put} with JSON body

- IErrorsInfo DeleteEntity(string entityName, object data)
  - DELETE {BaseUrl}/{Endpoints.{entity}.Delete}

- IErrorsInfo UpdateEntities(string entityName, object uploadData, IProgress<PassedArgs> progress)
  - Bulk update helper that iterates items and calls UpdateEntity per item

All calls:
- Use WebAPIConfigurationHelper for headers and endpoints
- Use WebAPIAuthenticationHelper to ensure and add auth headers
- Use WebAPIRequestHelper for resilient HTTP with retry
- Use WebAPIErrorHelper for non-success responses

### Quick CRUD examples

```csharp
// Read all
var orders = ds.GetEntity("Orders", null);

// Read paged
var page = ds.GetEntity("Orders", null, pageNumber: 1, pageSize: 50);

// Insert
var resIns = ds.InsertEntity("Orders", new { customerId = 1, total = 99.5 });

// Update
var resUpd = ds.UpdateEntity("Orders", new { id = 123, total = 120.0 });

// Delete
var resDel = ds.DeleteEntity("Orders", new { id = 123 });
