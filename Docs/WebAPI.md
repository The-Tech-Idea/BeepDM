# WebAPI DataSource Guide

## Overview

WebAPI DataSource provides REST API connectivity for BeepDM, treating HTTP endpoints as queryable data sources with full CRUD support.

## Architecture

Core object graph in `WebAPIDataSource`:
- `WebAPIConfigurationHelper` - Configuration management
- `WebAPIAuthenticationHelper` - Auth handling
- `WebAPIRequestHelper` - HTTP request execution
- `WebAPIDataHelper` - Data transformation
- `WebAPICacheHelper` - Response caching
- `WebAPIRateLimitHelper` - Rate limiting
- `WebAPISchemaHelper` - Schema inference
- `WebAPIErrorHelper` - Error handling

## Functional Areas

- **Connection lifecycle**: open/close and transaction-style stubs
- **HTTP execution**: GET/POST/PUT/PATCH/DELETE with retry and auth
- **Data operations**: `GetEntity`, `InsertEntity`, `UpdateEntity`, `DeleteEntity`, bulk update
- **Structure/schema**: entity discovery and inferred `EntityStructure` handling
- **Query/scripting**: query wrappers and script-oriented methods

## Configuration

```csharp
var props = new WebAPIConnectionProperties
{
    ConnectionName = "MyAPI",
    DatabaseType = DataSourceType.WebService,
    BaseUrl = "https://api.example.com/v1",
    AuthMode = WebAPIAuthMode.Bearer,
    AuthToken = "your-token",
    Timeout = TimeSpan.FromSeconds(30),
    RetryCount = 3,
    EnableCaching = true,
    CacheExpiration = TimeSpan.FromMinutes(5),
    RateLimitRequestsPerSecond = 10
};

editor.ConfigEditor.AddDataConnection(props);
```

## Authentication Modes

- None
- ApiKey (header or query parameter)
- Basic (username/password)
- Bearer (token)
- OAuth2 (client credentials flow)

## Usage

```csharp
var ds = editor.GetDataSource("MyAPI");
ds.Openconnection();

// GET request
var customers = ds.GetEntity("Customers", new List<AppFilter>{
    new AppFilter { FieldName = "Status", Operator = "=", FilterValue = "Active" }
});

// POST request
var newCustomer = new Dictionary<string, object>
{
    ["Name"] = "New Customer",
    ["Email"] = "customer@example.com"
};
ds.InsertEntity("Customers", newCustomer);
```

## File Locations

- `DataManagementEngineStandard/WebAPI/WebAPIDataSource.*.cs`
- `DataManagementEngineStandard/WebAPI/Helpers/`
- `DataManagementModelsStandard/WEPAPI/IWebAPIDataSource.cs`

## Related Documentation

- [Core Architecture](CoreArchitecture.md)
- [Data Source Implementation](HowToCreateNewDataSource.md)
