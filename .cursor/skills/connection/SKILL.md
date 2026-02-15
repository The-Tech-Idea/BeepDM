---
name: connection
description: Guidance for managing datasource connections in BeepDM, including driver linking, connection string processing, validation, and security.
---

# Connection Management Guide

Use this skill when creating or updating datasource connections, resolving drivers, or validating connection strings.

## Core Helpers
- ConnectionHelper: main facade
- ConnectionDriverLinkingHelper: driver resolution
- ConnectionStringProcessingHelper: placeholder replacement and path normalization
- ConnectionStringValidationHelper: format and datasource validation
- ConnectionStringSecurityHelper: masking and sensitive parameter detection

## Workflow
1. Build `ConnectionProperties` for the target datasource.
2. Resolve driver with `GetBestMatchingDriver`.
3. Replace placeholders via `ReplaceValueFromConnectionString` when using templates.
4. Validate the connection string.
5. Save to `ConfigEditor` and open datasource.

## Validation
- `ValidateConnectionStringStructure` for format checks.
- `IsConnectionStringValid(connectionString, dataSourceType)` for datasource checks.
- Ensure driver resolution returns a non-null driver.

## Pitfalls
- Hardcoding driver names bypasses configuration changes.
- Logging raw connection strings can leak secrets.
- Skipping path normalization breaks file-based providers.

## File Locations
- DataManagementEngineStandard/Helpers/ConnectionHelpers/ConnectionHelper.cs
- DataManagementEngineStandard/Helpers/ConnectionHelpers/ConnectionDriverLinkingHelper.cs
- DataManagementEngineStandard/Helpers/ConnectionHelpers/ConnectionStringProcessingHelper.cs
- DataManagementEngineStandard/Helpers/ConnectionHelpers/ConnectionStringValidationHelper.cs
- DataManagementEngineStandard/Helpers/ConnectionHelpers/ConnectionStringSecurityHelper.cs

## Example
```csharp
var props = new ConnectionProperties
{
    ConnectionName = "MyDb",
    DatabaseType = DataSourceType.SqlServer,
    Category = DatasourceCategory.RDBMS,
    ConnectionString = "Server=localhost;Database=MyDb;Integrated Security=True;TrustServerCertificate=True;"
};

var driver = ConnectionHelper.GetBestMatchingDriver(props, editor.ConfigEditor);
if (driver != null)
{
    props.DriverName = driver.PackageName;
    props.DriverVersion = driver.version;
}

if (!ConnectionHelper.IsConnectionStringValid(props.ConnectionString, props.DatabaseType))
{
    throw new InvalidOperationException("Invalid connection string");
}

editor.ConfigEditor.AddDataConnection(props);
```

## Task-Specific Examples

### Mask Connection String Before Logging
```csharp
var secured = ConnectionHelper.SecureConnectionString(props.ConnectionString);
logger.WriteLog($"Connecting: {secured}");
```

### Normalize File Path For Local DB
```csharp
ConnectionHelper.NormalizeFilePath(props, AppContext.BaseDirectory);
```