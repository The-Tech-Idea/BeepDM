# WebAPI

Web API datasource implementation for BeepDM. Provides a comprehensive HTTP-based data source adapter supporting RESTful API integration with authentication, caching, rate limiting, pagination, and schema inference.

## Main Runtime Type

- `WebAPIDataSource` (partial class spanning ~20 files for connection, HTTP, data, structure, query, scripting, bulk operations, and paging)

## Architecture

### Partial Class Breakdown

| File | Responsibility |
|------|---------------|
| `WebAPIDataSource.cs` | Core state (properties/fields), constructor, dispose pattern |
| `WebAPIDataSource.CoreInterface.cs` | `IDataSource` interface implementation, event handling |
| `WebAPIDataSource.CoreInterface.Fixed.cs` | Additional/corrected interface members |
| `WebAPIDataSource.Interface.cs` | Supplementary interface contracts |
| `WebAPIDataSource.Connection.cs` | Connection lifecycle: open/close, auth integration |
| `WebAPIDataSource.Http.cs` | Centralized HTTP execution layer |
| `WebAPIDataSource.Data.cs` | Core data operations (`GetEntity`, `InsertEntity`, `UpdateEntity`, `DeleteEntity`, `UpdateEntities`) |
| `WebAPIDataSource.DataOperations.cs` | Extended data retrieval and manipulation |
| `WebAPIDataSource.EntityMethods.cs` | Entity discovery and CRUD orchestration |
| `WebAPIDataSource.Structure.cs` | Schema/entity structure discovery and management |
| `WebAPIDataSource.Query.cs` | Query execution and scalar retrieval |
| `WebAPIDataSource.QueryMethods.cs` | Extended query processing |
| `WebAPIDataSource.PagingAsync.cs` | Pagination and async data retrieval |
| `WebAPIDataSource.Bulk.cs` | Basic bulk operation stubs |
| `WebAPIDataSource.BulkOperations.cs` | Enhanced batch processing |
| `WebAPIDataSource.Scripting.cs` | Script execution and entity creation |
| `WebAPIDataSource.ScriptTransactions.cs` | Transaction lifecycle stubs |
| `WebAPIDataSource.WebAPI.cs` | `IWebAPIDataSource`-specific `ReadData` method |
| `WebAPIDataSource.Complete.cs` | Final assembly/completion members |
| `WebAPIDataSource.Main.cs` | Additional orchestration entry points |

### Helper Stack

- `WebAPIConfigurationHelper` — Configuration validation, endpoint resolution, pagination settings
- `WebAPIAuthenticationHelper` — Auth header injection, OAuth2/bearer token refresh
- `WebAPIRequestHelper` — Centralized `HttpClient` execution with retry and concurrency
- `WebAPIDataHelper` — URL construction, response parsing, id extraction, cache-key helpers
- `WebAPICacheHelper` — In-memory response caching with TTL
- `WebAPIRateLimitHelper` — Request throttling and rate state tracking
- `WebAPISchemaHelper` — Schema inference from JSON samples and entity validation
- `WebAPIErrorHelper` — Normalized error handling and reporting

### Connection & Configuration Model

- `WebAPIDataConnection` — `IDataConnection` implementation with URL validation, auth validation, connection string build/parse
- `WebAPIConnectionProperties` — Primary settings contract extending `IConnectionProperties`

#### Supported Auth Modes
- `None` — No authentication
- `ApiKey` — Header or query-parameter-based API key
- `Basic` — Username/password via `Authorization: Basic`
- `Bearer` — Static or refreshable bearer/JWT token
- `OAuth2` — Client credentials flow with token refresh

#### Configuration Options
- Timeout, max retries, retry delay, cache duration, max concurrent requests
- Rate limiting: requests-per-minute toggle
- Pagination: page number/parameter names, default/max page size
- Response handling: format (json/xml/csv), data path, total count path
- SSL: ignore errors, validate certificate, client certificate support
- Proxy: URL, port, credentials, bypass-on-local

### Functional Areas

- **Connection lifecycle**: Open/close with auth validation and URL reachability test
- **HTTP execution**: GET/POST/PUT/PATCH/DELETE with retry (exponential backoff) and auth
- **Data operations**: Full CRUD (`GetEntity`, `InsertEntity`, `UpdateEntity`, `DeleteEntity`), bulk update path
- **Schema**: Entity discovery, dynamic schema inference from JSON responses, `EntityStructure` management
- **Query/scripting**: SQL-like query translation to API parameters, script-oriented methods
- **Pagination**: Page-based, offset-based, and cursor-based pagination support
- **Caching**: In-memory response cache with configurable TTL and key generation

## Implementation Status

All core features are implemented and functional. See `Plan.md` for detailed implementation status and future enhancement roadmap.

## Related

- `WebAPI/Helpers/README.md`
- `WebAPI/Plan.md`
- `DataManagementEngineStandard/Docs/creating-custom-datasources.html`
