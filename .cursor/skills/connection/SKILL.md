---
name: connection
description: Expert guidance for managing datasource connections in BeepDM, including connection properties, driver linking, connection string processing, validation, and security. Use when working with ConnectionProperties, ConnectionHelper, driver configurations, or connection string management.
---

# Connection Management Guide

Expert guidance for managing datasource connections in Beep Data Management Engine (BeepDM), including connection properties, driver linking, connection string processing, validation, and security.

## Core Components

### ConnectionHelper (Facade)
**Location**: `DataManagementEngineStandard/Helpers/ConnectionHelpers/ConnectionHelper.cs`

Main facade class that delegates to specialized helpers:
- **Driver Linking**: `ConnectionDriverLinkingHelper`
- **String Processing**: `ConnectionStringProcessingHelper`
- **Validation**: `ConnectionStringValidationHelper`
- **Security**: `ConnectionStringSecurityHelper`

### ConnectionProperties
**Interface**: `IConnectionProperties`
**Key Properties**:
- `ConnectionName` - Unique name for the connection
- `DatabaseType` - `DataSourceType` enum (SQL Server, MySQL, PostgreSQL, etc.)
- `Category` - `DatasourceCategory` enum (RDBMS, NoSQL, FILE, etc.)
- `ConnectionString` - The actual connection string
- `DriverName` - NuGet package name for the driver
- `DriverVersion` - Version of the driver package
- `FileName` - For file-based connections
- `Server` - Server/host name
- `Database` - Database name
- `UserId` / `Password` - Authentication credentials

## Driver Linking

### GetBestMatchingDriver
Finds the best matching driver configuration for connection properties:

```csharp
var driver = ConnectionHelper.GetBestMatchingDriver(connectionProps, editor.ConfigEditor);
if (driver != null)
{
    connectionProps.DriverName = driver.PackageName;
    connectionProps.DriverVersion = driver.version;
}
```

**Matching Priority**:
1. Exact package name + version match
2. Package name match only
3. DataSourceType match
4. File extension match (for file-based connections)

### LinkConnection2Drivers
Links a connection to its corresponding drivers:

```csharp
var driverConfig = ConnectionHelper.LinkConnection2Drivers(connectionProps, configEditor);
```

### GetDriversForDataSourceType
Gets all available drivers for a specific datasource type:

```csharp
var sqlServerDrivers = ConnectionHelper.GetDriversForDataSourceType(
    DataSourceType.SqlServer, 
    configEditor
);
```

## Connection String Processing

### ReplaceValueFromConnectionString
Replaces placeholders in connection string templates:

```csharp
var processedString = ConnectionHelper.ReplaceValueFromConnectionString(
    driverConfig, 
    connectionProps, 
    editor
);
```

**Common Placeholders**:
- `{Server}` - Server name
- `{Database}` - Database name
- `{UserId}` - User ID
- `{Password}` - Password
- `{Port}` - Port number
- `{FileName}` - File path (for file-based)

### NormalizePath
Converts relative paths to absolute paths:

```csharp
var absolutePath = ConnectionHelper.NormalizePath(
    relativePath, 
    AppContext.BaseDirectory
);
```

### NormalizeFilePath
Normalizes file paths in connection properties:

```csharp
ConnectionHelper.NormalizeFilePath(connectionProps, basePath);
```

### ValidateRequiredPlaceholders
Validates that all required placeholders have values:

```csharp
var missing = ConnectionHelper.ValidateRequiredPlaceholders(
    connectionStringTemplate, 
    connectionProps
);
if (missing.Any())
{
    throw new InvalidOperationException($"Missing placeholders: {string.Join(", ", missing)}");
}
```

## Connection String Validation

### IsConnectionStringValid
Validates a connection string for a specific datasource type:

```csharp
bool isValid = ConnectionHelper.IsConnectionStringValid(
    connectionString, 
    DataSourceType.SqlServer
);
```

### Datasource-Specific Validation
```csharp
// SQL Server
bool isValid = ConnectionHelper.ValidateSqlServerConnectionString(connectionString);

// MySQL
bool isValid = ConnectionHelper.ValidateMySqlConnectionString(connectionString);

// SQLite
bool isValid = ConnectionHelper.ValidateSQLiteConnectionString(connectionString);
```

### GetValidationRequirements
Gets validation requirements for a datasource type:

```csharp
string requirements = ConnectionHelper.GetValidationRequirements(DataSourceType.SqlServer);
```

### ValidateConnectionStringStructure
Validates connection string structure (format) without datasource-specific checks:

```csharp
bool isValid = ConnectionHelper.ValidateConnectionStringStructure(connectionString);
```

## Connection String Security

### SecureConnectionString
Masks sensitive information in connection strings:

```csharp
string secured = ConnectionHelper.SecureConnectionString(connectionString);
// Example: "Server=myserver;Database=mydb;User Id=sa;Password=****"
```

### ContainsSensitiveInformation
Checks if connection string contains sensitive data:

```csharp
bool hasSensitive = ConnectionHelper.ContainsSensitiveInformation(connectionString);
```

### SelectiveMask
Selectively masks sensitive parts while keeping structure visible:

```csharp
string masked = ConnectionHelper.SelectiveMask(
    connectionString, 
    maskChar: '*', 
    visibleChars: 2
);
// Example: "Password=Pa****rd"
```

### GetSensitiveParameterNames
Gets list of parameter names considered sensitive:

```csharp
string[] sensitiveParams = ConnectionHelper.GetSensitiveParameterNames();
// Returns: ["Password", "Pwd", "PWD", "User Id", "UserID", etc.]
```

### IsConnectionStringSecured
Validates that connection string appears to be secured:

```csharp
bool isSecured = ConnectionHelper.IsConnectionStringSecured(connectionString);
```

## Creating Connection Properties

### SQLite Connection
```csharp
var props = new ConnectionProperties
{
    ConnectionName = "MyDatabase",
    DatabaseType = DataSourceType.SqlLite,
    Category = DatasourceCategory.RDBMS,
    ConnectionString = $"Data Source={dbPath};Version=3;"
};

// Get driver dynamically
var driver = ConnectionHelper.GetBestMatchingDriver(props, editor.ConfigEditor);
if (driver != null)
{
    props.DriverName = driver.PackageName;
    props.DriverVersion = driver.version;
}
```

### SQL Server Connection
```csharp
var props = new ConnectionProperties
{
    ConnectionName = "MyDatabase",
    DatabaseType = DataSourceType.SqlServer,
    Category = DatasourceCategory.RDBMS,
    ConnectionString = integratedSecurity
        ? $"Server={server};Database={database};Integrated Security=True;TrustServerCertificate=True;"
        : $"Server={server};Database={database};User Id={userId};Password={password};TrustServerCertificate=True;"
};

// Get driver dynamically
var driver = ConnectionHelper.GetBestMatchingDriver(props, editor.ConfigEditor);
if (driver != null)
{
    props.DriverName = driver.PackageName;
    props.DriverVersion = driver.version;
}
```

### MySQL Connection
```csharp
var props = new ConnectionProperties
{
    ConnectionName = "MyDatabase",
    DatabaseType = DataSourceType.MySQL,
    Category = DatasourceCategory.RDBMS,
    ConnectionString = $"Server={server};Port={port};Database={database};Uid={userId};Pwd={password};"
};

var driver = ConnectionHelper.GetBestMatchingDriver(props, editor.ConfigEditor);
if (driver != null)
{
    props.DriverName = driver.PackageName;
    props.DriverVersion = driver.version;
}
```

### PostgreSQL Connection
```csharp
var props = new ConnectionProperties
{
    ConnectionName = "MyDatabase",
    DatabaseType = DataSourceType.PostgreSQL,
    Category = DatasourceCategory.RDBMS,
    ConnectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};"
};

var driver = ConnectionHelper.GetBestMatchingDriver(props, editor.ConfigEditor);
if (driver != null)
{
    props.DriverName = driver.PackageName;
    props.DriverVersion = driver.version;
}
```

## Best Practices

### 1. Always Use Dynamic Driver Resolution
**❌ Wrong**: Hardcoding driver names/versions
```csharp
props.DriverName = "Microsoft.Data.SqlClient";
props.DriverVersion = "5.1.0";
```

**✅ Correct**: Use `GetBestMatchingDriver`
```csharp
var driver = ConnectionHelper.GetBestMatchingDriver(props, editor.ConfigEditor);
if (driver != null)
{
    props.DriverName = driver.PackageName;
    props.DriverVersion = driver.version;
}
```

### 2. Validate Before Use
```csharp
// Validate structure
if (!ConnectionHelper.ValidateConnectionStringStructure(connectionString))
{
    throw new ArgumentException("Invalid connection string format");
}

// Validate for specific datasource
if (!ConnectionHelper.IsConnectionStringValid(connectionString, DataSourceType.SqlServer))
{
    throw new ArgumentException("Invalid SQL Server connection string");
}
```

### 3. Secure Sensitive Information
```csharp
// Always mask sensitive data in logs
_logger.LogInformation($"Connecting to: {ConnectionHelper.SecureConnectionString(connectionString)}");

// Check before logging
if (ConnectionHelper.ContainsSensitiveInformation(connectionString))
{
    var secured = ConnectionHelper.SecureConnectionString(connectionString);
    _logger.LogInformation($"Connection: {secured}");
}
```

### 4. Normalize File Paths
```csharp
// For file-based connections, always normalize paths
if (props.Category == DatasourceCategory.FILE)
{
    ConnectionHelper.NormalizeFilePath(props, AppContext.BaseDirectory);
}
```

### 5. Handle Missing Placeholders
```csharp
var missing = ConnectionHelper.ValidateRequiredPlaceholders(template, props);
if (missing.Any())
{
    throw new InvalidOperationException(
        $"Missing required connection properties: {string.Join(", ", missing)}"
    );
}
```

## Integration with IDMEEditor

### Creating DataSource from Connection Properties
```csharp
// Create connection properties
var props = new ConnectionProperties { /* ... */ };

// Get driver
var driver = ConnectionHelper.GetBestMatchingDriver(props, editor.ConfigEditor);
if (driver != null)
{
    props.DriverName = driver.PackageName;
    props.DriverVersion = driver.version;
}

// Create datasource via IDMEEditor
var dataSource = editor.CreateNewDataSourceConnection(props);
if (dataSource != null)
{
    editor.OpenDataSource(dataSource.DatasourceName);
}
```

## Common Patterns

### Pattern 1: Datasource-Agnostic Connection Creation
```csharp
public static ConnectionProperties CreateConnectionProps(
    IDMEEditor editor,
    DataSourceType dataSourceType,
    string connectionName,
    Dictionary<string, object> parameters)
{
    var props = new ConnectionProperties
    {
        ConnectionName = connectionName,
        DatabaseType = dataSourceType,
        Category = GetCategoryForType(dataSourceType)
    };

    // Build connection string based on datasource type
    props.ConnectionString = BuildConnectionString(dataSourceType, parameters);

    // Get driver dynamically
    var driver = ConnectionHelper.GetBestMatchingDriver(props, editor.ConfigEditor);
    if (driver != null)
    {
        props.DriverName = driver.PackageName;
        props.DriverVersion = driver.version;
    }

    return props;
}
```

### Pattern 2: Connection Validation Before Creation
```csharp
public static IErrorsInfo ValidateAndCreateConnection(
    IDMEEditor editor,
    ConnectionProperties props)
{
    // Validate structure
    if (!ConnectionHelper.ValidateConnectionStringStructure(props.ConnectionString))
    {
        return new ErrorsInfo 
        { 
            Flag = Errors.Failed, 
            Message = "Invalid connection string format" 
        };
    }

    // Validate for datasource type
    if (!ConnectionHelper.IsConnectionStringValid(props.ConnectionString, props.DatabaseType))
    {
        return new ErrorsInfo 
        { 
            Flag = Errors.Failed, 
            Message = $"Invalid connection string for {props.DatabaseType}" 
        };
    }

    // Get driver
    var driver = ConnectionHelper.GetBestMatchingDriver(props, editor.ConfigEditor);
    if (driver == null)
    {
        return new ErrorsInfo 
        { 
            Flag = Errors.Failed, 
            Message = $"No driver found for {props.DatabaseType}" 
        };
    }

    props.DriverName = driver.PackageName;
    props.DriverVersion = driver.version;

    // Create datasource
    var dataSource = editor.CreateNewDataSourceConnection(props);
    return dataSource?.ErrorObject ?? new ErrorsInfo { Flag = Errors.Ok };
}
```

## Related Skills

- **@beepdm** - Core BeepDM architecture and IDataSource usage
- **@unitofwork** - UnitOfWork pattern for CRUD operations

## Key Files

- `ConnectionHelper.cs` - Main facade
- `ConnectionDriverLinkingHelper.cs` - Driver matching logic
- `ConnectionStringProcessingHelper.cs` - String processing
- `ConnectionStringValidationHelper.cs` - Validation logic
- `ConnectionStringSecurityHelper.cs` - Security utilities
