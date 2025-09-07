# WebAPIDataSource

A comprehensive REST API data source implementation for the BeepDM framework that provides database-like operations for web APIs.

## 🚀 Quick Start

```csharp
// Initialize the data source
var webApiDS = new WebAPIDataSource("MyAPI", logger, dmeEditor, DataSourceType.WebApi, errorObject);

// Configure connection
ConnectionProp.ConnectionString = "https://api.example.com/v1";
ConnectionProp.ExtendedProperties["AuthenticationType"] = "Bearer";
ConnectionProp.ExtendedProperties["Token"] = "your-api-token";

// Open connection and authenticate
var result = webApiDS.Openconnection();

// Use like a database!
var users = webApiDS.GetEntity("Users", null);
webApiDS.InsertEntity("Users", newUser);
```

## 📚 Documentation

For complete documentation, examples, and advanced usage, see:
- **[WebAPIDataSource Documentation](../Docs/webapidatasource.html)** - Complete guide with examples
- **[API Reference](../Docs/api-reference.html)** - Technical API documentation

## 🏗️ Architecture

The WebAPIDataSource uses a partial class architecture with specialized helpers:

- **WebAPIDataSource.cs** - Main class with properties and constructor
- **WebAPIDataSource.Connection.cs** - Connection management and authentication
- **WebAPIDataSource.Data.cs** - CRUD operations
- **WebAPIDataSource.Query.cs** - Query execution
- **WebAPIDataSource.Structure.cs** - Entity discovery and schema
- **WebAPIDataSource.Scripting.cs** - Script execution
- **WebAPIDataSource.WebAPI.cs** - IWebAPIDataSource specific methods

### Helper Classes

- **WebAPIConfigurationHelper** - Configuration management
- **WebAPIAuthenticationHelper** - OAuth2, Bearer, API Key authentication
- **WebAPIRequestHelper** - HTTP requests with retry and circuit breaker
- **WebAPICacheHelper** - Response caching with TTL
- **WebAPIDataHelper** - Data transformation and URL building
- **WebAPIRateLimitHelper** - Rate limiting and throttling
- **WebAPISchemaHelper** - Schema inference from API responses
- **WebAPIErrorHelper** - Comprehensive error handling

## 🔧 Configuration

### Basic Setup
```csharp
ConnectionProp.ConnectionString = "https://api.example.com/v1";
ConnectionProp.ExtendedProperties["AuthenticationType"] = "Bearer";
ConnectionProp.ExtendedProperties["Token"] = "your-token";
```

### Advanced Configuration
```csharp
ConnectionProp.ExtendedProperties["TimeoutMs"] = "30000";
ConnectionProp.ExtendedProperties["MaxRetries"] = "3";
ConnectionProp.ExtendedProperties["CacheDurationMinutes"] = "10";
ConnectionProp.ExtendedProperties["MaxConcurrentRequests"] = "10";
```

## 🌟 Key Features

- ✅ **Database-like Operations** - Use familiar CRUD operations
- ✅ **Multiple Authentication** - OAuth2, Bearer, API Key, Basic Auth
- ✅ **Circuit Breaker** - Automatic failure recovery
- ✅ **Intelligent Caching** - Response caching with TTL
- ✅ **Rate Limiting** - Built-in throttling
- ✅ **Schema Inference** - Automatic API schema discovery
- ✅ **Async Support** - Full async/await operations
- ✅ **Error Recovery** - Comprehensive error handling

## 📖 Examples

### GitHub API
```csharp
ConnectionProp.ConnectionString = "https://api.github.com";
ConnectionProp.ExtendedProperties["AuthenticationType"] = "Bearer";
ConnectionProp.ExtendedProperties["Token"] = "your-github-token";

var repos = webApiDS.GetEntity("Repos", null);
```

### Company Internal API
```csharp
ConnectionProp.ConnectionString = "https://api.company.internal/v2";
ConnectionProp.ExtendedProperties["AuthenticationType"] = "OAuth2";
ConnectionProp.ExtendedProperties["TokenEndpoint"] = "https://auth.company.internal/oauth/token";
ConnectionProp.ExtendedProperties["ClientId"] = "your-client-id";

var employees = webApiDS.GetEntity("Employees", null);
```

## 🔍 Troubleshooting

- Check the **[WebAPIDataSource Documentation](../Docs/webapidatasource.html)** for detailed troubleshooting
- Verify authentication configuration
- Check network connectivity and API endpoints
- Review error logs for detailed error information

## 📋 Requirements

- .NET 6.0 or higher
- BeepDM Framework
- Valid API credentials and endpoints

## 📞 Support

For issues and questions:
1. Check the **[documentation](../Docs/webapidatasource.html)**
2. Review the **[examples](../Docs/examples.html)**
3. Check the **[API reference](../Docs/api-reference.html)**

---

*For complete documentation and examples, visit the [WebAPIDataSource Documentation](../Docs/webapidatasource.html)*</content>
<parameter name="filePath">C:\Users\f_ald\source\repos\The-Tech-Idea\BeepDM\DataManagementEngineStandard\WebAPI\README.md
