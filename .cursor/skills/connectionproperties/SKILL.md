---
name: connectionproperties
description: Guidance for ConnectionProperties usage, flags, filtering patterns, and safe configuration in BeepDM.
---

# ConnectionProperties Guide

Use this skill when creating or filtering datasource connections and when setting feature flags for local, remote, file, or in-memory sources.

## Core Properties
- ConnectionName, DatabaseType, Category, ConnectionString
- Host, Port, Database, UserID, Password
- FilePath, FileName, Ext (file based sources)

## Feature Flags
- IsLocal, IsRemote, IsWebApi
- IsFile, IsDatabase, IsCloud
- IsInMemory, IsComposite

## Filtering Patterns
```csharp
var localConnections = editor.ConfigEditor.DataConnections
    .Where(c => c.IsLocal)
    .ToList();

var inMemory = editor.ConfigEditor.DataConnections
    .Where(c => c.IsLocal && c.IsInMemory)
    .ToList();
```

## Validation
- Ensure `ConnectionName` is set and unique.
- For file sources, validate `FilePath` and `FileName`.
- Link drivers with `ConnectionHelper.GetBestMatchingDriver`.

## Pitfalls
- Missing flags can break filtering and UI grouping.
- Saving passwords by default can leak secrets.
- Forgetting driver linkage can cause runtime load failures.

## File Locations
- DataManagementModelsStandard/ConfigUtil/ConnectionProperties.cs
- DataManagementModelsStandard/ConfigUtil/IConnectionProperties.cs

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

### Create In-Memory Connection
```csharp
var inMemory = new ConnectionProperties
{
    ConnectionName = "InMemoryDb",
    DatabaseType = DataSourceType.SqlLite,
    Category = DatasourceCategory.RDBMS,
    IsLocal = true,
    IsInMemory = true,
    IsDatabase = true,
    ConnectionString = "Data Source=:memory:;Version=3;New=True;"
};
```