---
name: connectionproperties
description: Comprehensive guide for ConnectionProperties class in BeepDM. Use when working with connection configuration, filtering connections by type (local, remote, in-memory, etc.), or managing datasource connections.
---

# ConnectionProperties Guide

Expert guidance for working with `ConnectionProperties`, the comprehensive configuration class for all datasource connections in BeepDM. This skill covers properties, feature flags, filtering patterns, and best practices.

## Overview

`ConnectionProperties` is the central configuration class that stores all connection information for datasources in BeepDM. It implements `IConnectionProperties` and inherits from `Entity`, providing INotifyPropertyChanged support.

**Location**: `DataManagementModelsStandard/ConfigUtil/ConnectionProperties.cs`

## Core Structure

### Identifiers & Naming
```csharp
int ID { get; set; }                          // Unique integer identifier
string GuidID { get; set; }                   // Unique GUID identifier
string ConnectionName { get; set; }           // User-friendly connection name
string CompositeLayerName { get; set; }       // For composite datasources
```

### Provider & Categorization
```csharp
DataSourceType DatabaseType { get; set; }      // Type (SqlServer, MySQL, MongoDB, etc.)
DatasourceCategory Category { get; set; }      // Category (RDBMS, NoSQL, File, etc.)
string DriverName { get; set; }                // Driver package name
string DriverVersion { get; set; }            // Driver version
```

### Endpoints, Files & Data Targets
```csharp
string Host { get; set; }                      // Host name or IP address
int Port { get; set; }                        // Port number
string Database { get; set; }                  // Database name
string SchemaName { get; set; }                // Schema name
string OracleSIDorService { get; set; }       // Oracle SID or Service
char Delimiter { get; set; }                   // Delimiter for file-based sources
string Ext { get; set; }                       // File extension
string FilePath { get; set; }                  // File path for file-based sources
string FileName { get; set; }                  // File name
string Url { get; set; }                       // URL for web-based services
List<string> Databases { get; set; }            // List of available databases
List<EntityStructure> Entities { get; set; }  // Entity structures
List<DefaultValue> DatasourceDefaults { get; set; }  // Default values
```

### Credentials & Connection String
```csharp
string UserID { get; set; }                    // User ID
string Password { get; set; }                  // Password
string ConnectionString { get; set; }         // Full connection string
string Parameters { get; set; }                // Additional parameters
Dictionary<string, string> ParameterList { get; set; }  // Parameter dictionary
string ApiKey { get; set; }                    // API key
string KeyToken { get; set; }                  // Key token
```

## Feature Flags

`ConnectionProperties` includes comprehensive feature flags for filtering and categorization:

### Type Flags
```csharp
bool IsLocal { get; set; }                     // Local datasource (file-based, embedded)
bool IsRemote { get; set; }                    // Remote datasource (network-based)
bool IsWebApi { get; set; }                    // Web API datasource
bool IsFile { get; set; }                      // File-based datasource
bool IsDatabase { get; set; }                 // Database datasource
bool IsComposite { get; set; }                 // Composite datasource
bool IsCloud { get; set; }                     // Cloud-based datasource
bool IsInMemory { get; set; }                  // In-memory datasource
```

### Behavior Flags
```csharp
bool Favourite { get; set; }                   // Marked as favorite
bool IsFavourite { get; set; }                 // Same as Favourite
bool IsDefault { get; set; }                   // Default connection
bool Drawn { get; set; }                       // Shown on design surface
bool ReadOnly { get; set; }                    // Read-only connection
```

## Filtering Patterns

### Filter by IsLocal Flag

```csharp
// Get all local datasources
var localConnections = editor.ConfigEditor.DataConnections
    .Where(c => c.IsLocal)
    .ToList();

// Get local SQLite connections
var localSqliteConnections = editor.ConfigEditor.DataConnections
    .Where(c => c.IsLocal && c.DatabaseType == DataSourceType.SqlLite)
    .ToList();
```

### Filter by Multiple Flags

```csharp
// Get local file-based databases
var localFileDatabases = editor.ConfigEditor.DataConnections
    .Where(c => c.IsLocal && c.IsFile && c.IsDatabase)
    .ToList();

// Get in-memory local databases
var inMemoryLocal = editor.ConfigEditor.DataConnections
    .Where(c => c.IsLocal && c.IsInMemory)
    .ToList();
```

### Filter by Category and Type

```csharp
// Get all RDBMS local connections
var localRDBMS = editor.ConfigEditor.DataConnections
    .Where(c => c.IsLocal && c.Category == DatasourceCategory.RDBMS)
    .ToList();

// Get remote database connections
var remoteDatabases = editor.ConfigEditor.DataConnections
    .Where(c => c.IsRemote && c.IsDatabase)
    .ToList();
```

## Common Usage Patterns

### Creating Local Database Connection

```csharp
var localConnection = new ConnectionProperties
{
    ConnectionName = "LocalAppDb",
    DatabaseType = DataSourceType.SqlLite,
    Category = DatasourceCategory.RDBMS,
    IsLocal = true,                    // Mark as local
    IsFile = true,                     // File-based
    IsDatabase = true,                 // Database type
    FilePath = Path.Combine(AppContext.BaseDirectory, "Databases"),
    FileName = "app.db",
    ConnectionString = $"Data Source={dbPath};Version=3;"
};

// Get driver information
var driver = ConnectionHelper.GetBestMatchingDriver(localConnection, editor.ConfigEditor);
if (driver != null)
{
    localConnection.DriverName = driver.PackageName;
    localConnection.DriverVersion = driver.version;
}

// Add to ConfigEditor
editor.ConfigEditor.AddDataConnection(localConnection);
```

### Creating In-Memory Connection

```csharp
var inMemoryConnection = new ConnectionProperties
{
    ConnectionName = "InMemoryDb",
    DatabaseType = DataSourceType.SqlLite,
    Category = DatasourceCategory.RDBMS,
    IsLocal = true,                    // Local
    IsInMemory = true,                 // In-memory
    IsDatabase = true,                 // Database
    ConnectionString = "Data Source=:memory:;Version=3;New=True;"
};
```

### Creating Remote Database Connection

```csharp
var remoteConnection = new ConnectionProperties
{
    ConnectionName = "RemoteSqlServer",
    DatabaseType = DataSourceType.SqlServer,
    Category = DatasourceCategory.RDBMS,
    IsRemote = true,                   // Remote
    IsDatabase = true,                 // Database
    Host = "server.example.com",
    Port = 1433,
    Database = "MyDatabase",
    UserID = "username",
    Password = "password",
    IntegratedSecurity = false
};
```

### Creating Web API Connection

```csharp
var webApiConnection = new ConnectionProperties
{
    ConnectionName = "MyWebAPI",
    Category = DatasourceCategory.WebAPI,
    IsWebApi = true,                   // Web API
    IsRemote = true,                   // Remote
    Url = "https://api.example.com",
    UseApiKey = true,
    ApiKey = "your-api-key",
    Headers = new List<WebApiHeader>
    {
        new WebApiHeader { Headername = "Content-Type", Headervalue = "application/json" }
    }
};
```

## Authentication Properties

### Windows Authentication
```csharp
connection.IntegratedSecurity = true;
connection.UseWindowsAuthentication = true;
connection.TrustedConnection = true;
```

### SQL Authentication
```csharp
connection.UserID = "username";
connection.Password = "password";
connection.UseUserAndPassword = true;
connection.SavePassword = false;  // Security best practice
```

### OAuth Authentication
```csharp
connection.UseOAuth = true;
connection.Authority = "https://login.microsoftonline.com/{tenant-id}";
connection.TenantId = "tenant-id";
connection.ApplicationId = "app-id";
connection.ClientSecret = "client-secret";
connection.Resource = "https://graph.microsoft.com";
```

### Certificate Authentication
```csharp
connection.UseCertificate = true;
connection.CertificatePath = "path/to/certificate.pfx";
connection.ClientCertificateThumbprint = "thumbprint";
connection.ClientCertificateStoreLocation = "CurrentUser";
connection.ClientCertificateStoreName = "My";
```

## SSL/TLS Configuration

```csharp
connection.EncryptConnection = true;
connection.TrustServerCertificate = false;
connection.UseSSL = true;
connection.RequireSSL = true;
connection.SSLMode = "Required";
connection.SSLTimeout = 30;
```

## Connection String Management

### Building Connection String

```csharp
// Use ConnectionHelper to build connection string from properties
var connectionString = ConnectionHelper.ReplaceValueFromConnectionString(
    driver,
    connectionProperties,
    editor
);
```

### Processing Connection String

```csharp
// Replace placeholders in connection string template
var processedString = ConnectionHelper.ReplaceValueFromConnectionString(
    driver,
    connectionProperties,
    editor
);
```

## Best Practices

### 1. Always Set Feature Flags
```csharp
// When creating a connection, set appropriate flags
connection.IsLocal = true;      // For local databases
connection.IsFile = true;       // For file-based sources
connection.IsDatabase = true;   // For database sources
```

### 2. Use IsLocal for Filtering
```csharp
// Filter local connections efficiently
var localConnections = connections.Where(c => c.IsLocal).ToList();
```

### 3. Link to Drivers
```csharp
// Always link connection to driver for proper configuration
var driver = ConnectionHelper.GetBestMatchingDriver(connection, editor.ConfigEditor);
if (driver != null)
{
    connection.DriverName = driver.PackageName;
    connection.DriverVersion = driver.version;
}
```

### 4. Secure Sensitive Information
```csharp
// Don't save passwords unless necessary
connection.SavePassword = false;

// Use secure connection strings
connection.EncryptConnection = true;
```

### 5. Validate Before Use
```csharp
// Validate connection properties before adding
if (string.IsNullOrEmpty(connection.ConnectionName))
{
    throw new ArgumentException("ConnectionName is required");
}

if (connection.IsLocal && string.IsNullOrEmpty(connection.FilePath))
{
    throw new ArgumentException("FilePath is required for local connections");
}
```

## Filtering Examples

### Find ILocalDB Compatible Connections
```csharp
var localDBConnections = editor.ConfigEditor.DataConnections
    .Where(c => c.IsLocal && 
                c.IsDatabase && 
                (c.DatabaseType == DataSourceType.SqlLite || 
                 c.DatabaseType == DataSourceType.Access))
    .ToList();
```

### Find IInMemoryDB Compatible Connections
```csharp
var inMemoryConnections = editor.ConfigEditor.DataConnections
    .Where(c => c.IsLocal && c.IsInMemory && c.IsDatabase)
    .ToList();
```

### Find Remote Database Connections
```csharp
var remoteDatabases = editor.ConfigEditor.DataConnections
    .Where(c => c.IsRemote && c.IsDatabase && !c.IsCloud)
    .ToList();
```

### Find Cloud Connections
```csharp
var cloudConnections = editor.ConfigEditor.DataConnections
    .Where(c => c.IsCloud)
    .ToList();
```

## Related Interfaces

- **IConnectionProperties**: Interface definition
- **Entity**: Base class with INotifyPropertyChanged
- **ConnectionHelper**: Helper for connection management (see **@connection** skill)

## Related Skills

- **@connection** - Connection management, driver linking, connection string processing
- **@localdb** - Guide for implementing ILocalDB
- **@inmemorydb** - Guide for implementing IInMemoryDB
- **@beepdm** - Main BeepDM skill

## File Locations

- **ConnectionProperties**: `DataManagementModelsStandard/ConfigUtil/ConnectionProperties.cs`
- **IConnectionProperties**: `DataManagementModelsStandard/ConfigUtil/IConnectionProperties.cs`


## Repo Documentation Anchors

- DataManagementModelsStandard/ConfigUtil/ConnectionProperties.cs
- DataManagementEngineStandard/ConfigUtil/README.md
- DataManagementEngineStandard/Docs/registerbeep.html

