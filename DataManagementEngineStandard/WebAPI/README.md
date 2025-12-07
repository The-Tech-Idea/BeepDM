# Web API Data Source (BeepDM)

A robust REST/Web API data source for BeepDM providing database-like operations, helpers, and utilities to call, authenticate, cache, transform, and validate API interactions.

## What’s new

- Unified configuration via WebAPIConnectionProperties (no separate auth DTOs)
- Validation with local ConfigurationValidationResult (IsValid, Errors, Warnings)
- High-level CRUD operations for REST API interactions
- Consistent usage of helpers across partials (auth, request retry, error handling, schema, cache, rate limit)

## Project layout (partials)

- **WebAPIDataSource.cs**: core state, constructor, helper initialization
- **WebAPIDataSource.Connection.cs**: Openconnection/Closeconnection, transaction stubs (BeginTransaction, EndTransaction, Commit)
- **WebAPIDataSource.Http.cs**: low-level HTTP methods (GetAsync, PostAsync, PutAsync, PatchAsync, DeleteAsync with generic overloads)
- **WebAPIDataSource.Data.cs**: high-level CRUD operations (GetEntity, InsertEntity, UpdateEntity, DeleteEntity, UpdateEntities)
- **WebAPIDataSource.Query.cs**: RunQuery, GetScalar, ExecuteSql
- **WebAPIDataSource.Structure.cs**: entity discovery and schema (GetEntitesList, GetEntityStructure, CheckEntityExist, etc.)
- **WebAPIDataSource.Scripting.cs**: scripting and entity creation (RunScript, GetCreateEntityScript, CreateEntities)
- **WebAPIDataSource.WebAPI.cs**: IWebAPIDataSource specific methods (ReadData)

Note: Several partial files exist but are currently empty (Bulk.cs, BulkOperations.cs, Complete.cs, CoreInterface.cs, DataOperations.cs, EntityMethods.cs, Interface.cs, Main.cs, PagingAsync.cs, QueryMethods.cs, ScriptTransactions.cs). These are placeholders for future functionality.

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

## Connection Management (WebAPIDataConnection)

The `WebAPIDataConnection` class implements `IDataConnection` and manages Web API connections:

- **Connection Properties**: Uses `WebAPIConnectionProperties` (or any `IConnectionProperties`)
- **Connection State**: Tracks `ConnectionState` (Open, Closed, Broken)
- **Validation**: Validates URL format and authentication requirements before opening
- **Connection String**: Supports building/parsing connection strings from properties

Key methods:
- `OpenConnection()` / `OpenConnection(IConnectionProperties)` - Opens and validates connection
- `CloseConn()` - Closes the connection
- `ValidateConfiguration()` - Validates required properties based on AuthType

## Configuration (WebAPIConnectionProperties)

Core URL/auth
- `Url`: base URL (https://api.example.com)
- `AuthType`: `AuthTypeEnum` (None | ApiKey | Basic | Bearer | OAuth2)
- `ApiKey` / `ApiKeyHeader` (default "X-API-Key")
- `UserID` / `Password` (Basic authentication)
- `ClientId` / `ClientSecret` / `TokenUrl` / `AuthUrl` / `Scope` / `GrantType` (OAuth2)
- `KeyToken` / `BearerToken` (Bearer authentication)

Behavior
- `TimeoutMs` (default: 30000), `MaxRetries` / `RetryCount` (default: 3), `RetryIntervalMs` / `RetryDelayMs` (default: 1000)
- `EnableCaching` (default: true), `CacheExpiryMinutes` (default: 15)
- `MaxConcurrentRequests` (default: 10)
- `EnableRateLimit` (default: true), `RateLimitRequestsPerMinute` (default: 60)

Pagination and response
- `PageNumberParameter` (default: "page"), `PageSizeParameter` (default: "limit")
- `DefaultPageSize` (default: 100), `MaxPageSize` (default: 1000)
- `ResponseFormat` (default: "json"), `DataPath`, `TotalCountPath`

Headers and parameters
- `Headers`: `List<WebApiHeader>` (custom HTTP headers)
- `Parameters`: string in format "key=value;key2=value2" (optional connection parameters)
- `ParameterList`: `Dictionary<string, string>` (alternative parameter storage)

Validation
- `ValidateConfiguration()` - Validates required properties for current AuthType
- `GetValidationErrors()` - Returns descriptive error messages for missing/invalid configuration
- `UpdateAuthenticationRequirements()` - Automatically updates `RequiresAuthentication` and `RequiresTokenRefresh` based on AuthType

## Endpoints configuration

`WebAPIConfigurationHelper.GetEndpointConfiguration(entityName)` resolves endpoint configurations:
- `GetEndpoint` / `PostEndpoint` / `PutEndpoint` / `DeleteEndpoint` / `ListEndpoint`
- Additional properties: `Method`, `RequiresAuth`, `CacheDuration`, `RateLimit`
- Falls back to "/{entityName}" when specific endpoint configuration is not supplied

## Data operations (WebAPIDataSource.Data.cs)

CRUD functions exposed by the data source:

- `IEnumerable<object> GetEntity(string entityName, List<AppFilter> filter)`
  - GET {BaseUrl}/{Endpoints.{entity}.Get} with optional filters appended as query string
  - Parses JSON response to IEnumerable<object> (dictionary per row)

- `PagedResult GetEntity(string entityName, List<AppFilter> filter, int pageNumber, int pageSize)`
  - GET {BaseUrl}/{Endpoints.{entity}.List} with paging parameters
  - Returns data plus TotalRecords (from X-Total-Count header or JSON fields)

- `Task<IEnumerable<object>> GetEntityAsync(string entityName, List<AppFilter> filter)`
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

## Usage for Derived Classes

When creating custom data sources that inherit from WebAPIDataSource:

1. **Low-level HTTP methods are available** - WebAPIDataSource exposes public virtual methods: GetAsync, PostAsync, PutAsync, PatchAsync, DeleteAsync (with generic overloads). These can be used directly or overridden for custom behavior.
2. **Use high-level CRUD methods** - Override GetEntity, InsertEntity, UpdateEntity, DeleteEntity as needed for domain-specific logic
3. **Handle authentication via configuration** - Set up WebAPIConnectionProperties with appropriate auth settings (AuthTypeEnum: None, ApiKey, Basic, Bearer, OAuth2)
4. **Use connection management** - Override Openconnection/Closeconnection for custom connection logic
5. **Configure endpoints** - Define entity endpoints in your configuration or override endpoint resolution via WebAPIConfigurationHelper
6. **Access helper instances** - Use the private helper fields (_configHelper, _authHelper, _requestHelper, etc.) in your overrides

### Example Custom Data Source Pattern

```csharp
public class MyApiDataSource : WebAPIDataSource
{
    public MyApiDataSource(string datasourcename, IDMLogger logger, IDMEEditor editor, 
                          DataSourceType type, IErrorsInfo errors, MyConfig config)
        : base(datasourcename, logger, editor, type, errors)
    {
        // Set up connection properties
        if (Dataconnection?.ConnectionProp is WebAPIConnectionProperties props)
        {
            props.Url = config.BaseUrl;
            props.ApiKey = config.ApiKey;
            props.AuthType = AuthTypeEnum.ApiKey;
            // ... other properties
        }
    }

    public override IEnumerable<object> GetEntity(string entityName, List<AppFilter> filter)
    {
        // Option 1: Use high-level base implementation
        return base.GetEntity(entityName, filter);
        
        // Option 2: Use low-level HTTP methods
        // var response = await GetAsync<T>($"/api/{entityName}", queryParams);
        
        // Option 3: Custom implementation using helpers
        // var endpointConfig = _configHelper.GetEndpointConfiguration(entityName);
        // var url = _dataHelper.BuildEndpointUrl(_configHelper.BaseUrl, endpointConfig.GetEndpoint);
        // ...
    }
}
```

### Quick CRUD examples

```csharp
// Read all (returns IEnumerable<object>)
var orders = ds.GetEntity("Orders", null);

// Read paged (returns PagedResult)
var page = ds.GetEntity("Orders", null, pageNumber: 1, pageSize: 50);
var data = page.Data; // IEnumerable<object>
var total = page.TotalRecords;

// Insert (returns IErrorsInfo)
var resIns = ds.InsertEntity("Orders", new { customerId = 1, total = 99.5 });
if (resIns.Flag == Errors.Ok) { /* success */ }

// Update (returns IErrorsInfo)
var resUpd = ds.UpdateEntity("Orders", new { id = 123, total = 120.0 });

// Delete (returns IErrorsInfo)
var resDel = ds.DeleteEntity("Orders", new { id = 123 });

// Using low-level HTTP methods
var response = await ds.GetAsync<OrderResponse>("/api/orders/123");
var ordersList = await ds.GetAsync<List<Order>>("/api/orders", query: new Dictionary<string, string> { { "status", "active" } });
```

### Connection Management

```csharp
// Open connection (validates configuration and tests connectivity)
var state = ds.Openconnection();
if (state == ConnectionState.Open) { /* ready to use */ }

// Close connection
ds.Closeconnection();

// Transaction support (stubs for Web APIs)
ds.BeginTransaction(new PassedArgs());
// ... operations ...
ds.Commit(new PassedArgs());
ds.EndTransaction(new PassedArgs());
```
