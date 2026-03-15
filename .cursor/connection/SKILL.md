---
name: connection
description: Guidance for managing datasource connections in BeepDM, including driver resolution, connection-string processing, validation, normalization, and masking. Use when wiring ConnectionProperties to actual driver metadata or validating a connection before ConfigEditor persistence and IDMEEditor usage.
---

# Connection Management Guide

Use this skill when converting `ConnectionProperties` into a usable, validated connection definition.

## Use this skill when
- Resolving which driver configuration should back a connection
- Replacing driver-template placeholders with actual values
- Normalizing relative file paths before save or open
- Validating or masking connection strings
- Debugging why a connection opens in config but fails at runtime

## Do not use this skill when
- The main task is deciding which fields belong on `ConnectionProperties`. Use [`connectionproperties`](../connectionproperties/SKILL.md).
- The main task is saving or loading configuration files. Use [`configeditor`](../configeditor/SKILL.md).
- The main task is using the opened datasource for CRUD or schema operations. Use [`idatasource`](../idatasource/SKILL.md).

## Core Helpers
- `ConnectionHelper`: facade over all connection helper operations
- `ConnectionDriverLinkingHelper`: dynamic driver resolution
- `ConnectionStringProcessingHelper`: placeholder replacement and path normalization
- `ConnectionStringValidationHelper`: datasource-specific and structural validation
- `ConnectionStringSecurityHelper`: masking and sensitive parameter detection

## Typical Workflow
1. Start from a populated `ConnectionProperties` instance.
2. Resolve the driver with `GetBestMatchingDriver` or `LinkConnection2Drivers`.
3. If a template-based connection string is used, call `ReplaceValueFromConnectionString`.
4. Normalize relative file paths for file-based providers.
5. Validate structure and datasource-specific requirements.
6. Mask secrets before logging, then save through `ConfigEditor`.

## Validation and Safety
- Use `ValidateConnectionStringStructure` for generic format checks.
- Use `IsConnectionStringValid(connectionString, dataSourceType)` for provider-specific checks.
- Use `ValidateRequiredPlaceholders` before replacing template values.
- Use `SecureConnectionString` or `SelectiveMask` in logs and UI.
- Ensure driver resolution returns a non-null driver before persisting `DriverName` and `DriverVersion`.

## Pitfalls
- Hardcoding driver names bypasses configuration-driven updates.
- Logging raw connection strings leaks credentials and tokens.
- Skipping path normalization breaks local/file datasources when the base path changes.
- Validating only the string format but not the datasource type misses provider-specific issues.

## File Locations
- `DataManagementEngineStandard/Helpers/ConnectionHelpers/ConnectionHelper.cs`
- `DataManagementEngineStandard/Helpers/ConnectionHelpers/ConnectionDriverLinkingHelper.cs`
- `DataManagementEngineStandard/Helpers/ConnectionHelpers/ConnectionStringProcessingHelper.cs`
- `DataManagementEngineStandard/Helpers/ConnectionHelpers/ConnectionStringValidationHelper.cs`
- `DataManagementEngineStandard/Helpers/ConnectionHelpers/ConnectionStringSecurityHelper.cs`

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

## Related Skills
- [`connectionproperties`](../connectionproperties/SKILL.md)
- [`configeditor`](../configeditor/SKILL.md)
- [`beepdm`](../beepdm/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for the common helper methods, validation calls, and masking patterns.
