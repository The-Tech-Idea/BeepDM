---
name: connectionproperties
description: Guidance for ConnectionProperties usage in BeepDM, including provider selection, endpoint/file settings, credentials, flags, and safe persistence. Use when building or updating datasource definitions before saving them through ConfigEditor or opening them with IDMEEditor.
---

# ConnectionProperties Guide

Use this skill when creating or modifying `ConnectionProperties` objects that will be stored in `ConfigEditor.DataConnections` and later used by `IDMEEditor`.

## Use this skill when
- Creating a new datasource definition before calling `AddDataConnection`
- Translating UI input or app settings into a BeepDM connection model
- Debugging why driver selection, file resolution, or datasource categorization is wrong
- Filtering saved connections by capability or deployment style

## Do not use this skill when
- The main problem is driver lookup, connection-string validation, or masking. Use [`connection`](../connection/SKILL.md).
- The main problem is persistence of config files. Use [`configeditor`](../configeditor/SKILL.md).
- The main problem is opening or using the datasource after save. Use [`beepdm`](../beepdm/SKILL.md) or [`idatasource`](../idatasource/SKILL.md).

## Responsibilities
- Populate the correct provider identity: `DatabaseType`, `Category`, `DriverName`, `DriverVersion`.
- Populate the correct transport details: host/database/schema for server systems, file path/name for file systems, URL and API settings for remote systems.
- Set behavior flags consistently so UI grouping, lifecycle helpers, and filtering code behave correctly.
- Keep secrets and persistence concerns separate from usage concerns.

## Core Property Groups
- Identity:
  - `ConnectionName`, `GuidID`, `CompositeLayerName`
- Provider and category:
  - `DatabaseType`, `Category`, `DriverName`, `DriverVersion`
- Endpoint and storage:
  - `Host`, `Port`, `Database`, `SchemaName`, `OracleSIDorService`
  - `FilePath`, `FileName`, `Ext`, `Url`, `Delimiter`
- Credentials and request settings:
  - `UserID`, `Password`, `ConnectionString`, `Parameters`, `ParameterList`, `ApiKey`, `KeyToken`, `HttpMethod`, `Timeout`
- Metadata and defaults:
  - `Entities`, `DatasourceDefaults`, `Databases`
- Behavioral flags:
  - `IsLocal`, `IsRemote`, `IsWebApi`, `IsFile`, `IsDatabase`, `IsCloud`, `IsInMemory`, `IsComposite`, `ReadOnly`, `Favourite`

## Typical Usage Pattern
1. Start with `ConnectionName`, `DatabaseType`, and `Category`.
2. Fill the location fields that match the provider type.
3. Build or normalize `ConnectionString` using the driver template and helper methods.
4. Resolve `DriverName` and `DriverVersion` dynamically, not by guesswork.
5. Set classification flags so later filtering code and UI behavior are accurate.
6. Save through `ConfigEditor.AddDataConnection` and `SaveDataconnectionsValues`.

## Validation and Safety
- Ensure `ConnectionName` is unique in `ConfigEditor.DataConnections`.
- Prefer `ConnectionHelper.GetBestMatchingDriver` instead of hardcoded driver names.
- Normalize file paths for local/file datasources before save.
- Do not log `Password`, `ApiKey`, `KeyToken`, or raw secure connection strings.
- Keep `Category` aligned with `DatabaseType`; mismatches can break discovery and UI categorization.

## Pitfalls
- Setting `ConnectionString` but leaving location fields inconsistent makes maintenance harder and breaks template-based regeneration.
- Forgetting `IsInMemory`, `IsFile`, or `IsWebApi` causes downstream filtering and UX issues.
- Treating all providers like RDBMS connections hides API/file-specific settings that the runtime depends on.
- Saving secrets in plain text without masking or policy consideration creates avoidable exposure.

## File Locations
- `DataManagementModelsStandard/ConfigUtil/ConnectionProperties.cs`
- `DataManagementModelsStandard/ConfigUtil/IConnectionProperties.cs`
- `DataManagementEngineStandard/Helpers/ConnectionHelpers/ConnectionHelper.cs`

## Example
```csharp
var conn = new ConnectionProperties
{
    ConnectionName = "LocalDb",
    DatabaseType = DataSourceType.SqlLite,
    Category = DatasourceCategory.RDBMS,
    IsLocal = true,
    IsFile = true,
    IsDatabase = true,
    FilePath = Path.Combine(AppContext.BaseDirectory, "Databases"),
    FileName = "app.db",
    ConnectionString = "Data Source=./Databases/app.db;Version=3;"
};
```

## Task-Specific Examples

### Filter Local File Databases
```csharp
var localFileDbs = editor.ConfigEditor.DataConnections
    .Where(c => c.IsLocal && c.IsFile && c.IsDatabase)
    .ToList();
```

### Resolve Driver Metadata Before Save
```csharp
var driver = ConnectionHelper.GetBestMatchingDriver(conn, editor.ConfigEditor);
if (driver != null)
{
    conn.DriverName = driver.PackageName;
    conn.DriverVersion = driver.version;
}
```

## Related Skills
- [`connection`](../connection/SKILL.md)
- [`configeditor`](../configeditor/SKILL.md)
- [`beepdm`](../beepdm/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for quick property examples and common filtering patterns.
