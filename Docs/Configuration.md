# Configuration Management Guide

## Overview

`ConfigEditor` is the persisted configuration manager with specialized sub-managers for different responsibilities. It serves as the facade for all configuration operations.

## Architecture

```
ConfigEditor (Facade)
├── ConfigPathManager              # Config root and folder structure
├── DataConnectionManager          # DataConnections CRUD and persistence
├── QueryManager                   # QueryList operations
├── EntityMappingManager           # Entity metadata and mappings
├── ComponentConfigManager         # Drivers, workflows, reports, projects
└── MigrationHistoryManager        # Per-datasource migration history
```

## File Locations

- `DataManagementEngineStandard/ConfigUtil/ConfigEditor.cs`
- `DataManagementEngineStandard/ConfigUtil/Managers/DataConnectionManager.cs`
- `DataManagementEngineStandard/ConfigUtil/Managers/QueryManager.cs`
- `DataManagementEngineStandard/ConfigUtil/Managers/EntityMappingManager.cs`
- `DataManagementEngineStandard/ConfigUtil/Managers/ComponentConfigManager.cs`
- `DataManagementEngineStandard/ConfigUtil/Managers/MigrationHistoryManager.cs`

## Core Managers

### ConfigPathManager

Manages configuration folder structure:

```csharp
// Paths are automatically determined based on platform
// Windows: %APPDATA%\BeepDM\{ContainerName}
// Linux: ~/.config/BeepDM/{ContainerName}
// macOS: ~/Library/Application Support/BeepDM/{ContainerName}

var configPath = editor.ConfigEditor.ConfigPath;
var exePath = editor.ConfigEditor.ExePath;
```

### DataConnectionManager

Manages data connections:

```csharp
// Load connections
var connections = editor.ConfigEditor.LoadDataConnectionsValues();

// Add connection
var props = new ConnectionProperties
{
    ConnectionName = "MyDatabase",
    DatabaseType = DataSourceType.SqlLite,
    Category = DatasourceCategory.RDBMS,
    ConnectionString = "Data Source=./Beep/dbfiles/app.db",
    DriverName = "SQLite"
};
editor.ConfigEditor.AddDataConnection(props);

// Save changes
editor.ConfigEditor.SaveDataconnectionsValues();

// Get connection
var conn = editor.ConfigEditor.DataConnections
    .FirstOrDefault(c => c.ConnectionName == "MyDatabase");

// Remove connection
editor.ConfigEditor.RemoveDataConnection("MyDatabase");
```

### QueryManager

Manages query configurations:

```csharp
// Initialize default queries
var queries = editor.ConfigEditor.InitQueryDefaultValues();
editor.ConfigEditor.QueryList = queries;
editor.ConfigEditor.SaveQueryFile();

// Add custom query
editor.ConfigEditor.QueryList.Add(new QueryConfig
{
    QueryName = "GetActiveCustomers",
    QueryType = QueryType.Select,
    Sql = "SELECT * FROM Customers WHERE IsActive = 1"
});
editor.ConfigEditor.SaveQueryFile();
```

### EntityMappingManager

Manages entity mappings:

```csharp
// Save mapping
var mapping = new EntityDataMap
{
    EntityName = "Customer",
    DataSourceName = "MyDatabase",
    Fields = new List<MappingField>
    {
        new MappingField { SourceField = "CustName", TargetField = "CustomerName" }
    }
};
editor.ConfigEditor.SaveMappingValues("Customer", "MyDatabase", mapping);

// Load mapping
var loadedMapping = editor.ConfigEditor.LoadMappingValues("Customer", "MyDatabase");
```

### ComponentConfigManager

Manages drivers, workflows, reports, and projects:

```csharp
// Add driver configuration
var driver = new ConnectionDriversConfig
{
    GuidID = "sqlite-driver",
    PackageName = "SQLite",
    DriverClass = "SQLite",
    version = "1.0.0",
    DbConnectionType = "SQLiteConnection",
    ConnectionString = "Data Source={Database};",
    classHandler = "SQLiteDataSource",
    DatasourceCategory = DatasourceCategory.RDBMS,
    DatasourceType = DataSourceType.SqlLite,
    ADOType = false
};
editor.ConfigEditor.DataDriversClasses.Add(driver);
editor.ConfigEditor.SaveConfigValues();
```

### MigrationHistoryManager

Tracks migration history per datasource:

```csharp
// Record migration
editor.ConfigEditor.RecordMigration(
    dataSourceName: "MyDatabase",
    migrationName: "InitialSchema",
    version: "1.0.0");

// Get migration history
var history = editor.ConfigEditor.GetMigrationHistory("MyDatabase");
foreach (var migration in history)
{
    Console.WriteLine($"{migration.MigrationName} - {migration.AppliedDate}");
}

// Check if migration applied
bool applied = editor.ConfigEditor.IsMigrationApplied("MyDatabase", "InitialSchema");
```

## Configuration Files

### DataConnections.json

```json
[
  {
    "ConnectionName": "MyDB",
    "ConnectionString": "Data Source=./app.db",
    "DatabaseType": "SqlLite",
    "DriverName": "SQLite",
    "Category": "RDBMS"
  }
]
```

### ConnectionConfig.json

```json
[
  {
    "PackageName": "SQLite",
    "DatasourceType": "SqlLite",
    "ClassHandler": "SQLiteDataSource",
    "version": "1.0.0",
    "ConnectionString": "Data Source={Database};"
  }
]
```

### DataTypeMapping.json

Maps data types between different datasources.

### QueryList.json

Contains query templates for different datasource types.

## Typical Workflow

1. Access `editor.ConfigEditor`; avoid creating ad-hoc config stores once the editor exists.
2. Load the relevant collection such as `LoadDataConnectionsValues()`.
3. Update through facade methods like `AddDataConnection`, `SaveDataconnectionsValues`, `SaveQueryFile`, or `SaveMappingValues`.
4. Refresh in-memory collections when code depends on newly persisted state.
5. Let downstream systems consume config through `IDMEEditor`, not duplicate file parsing.

## Validation and Safety

- After adding or changing a connection, call `SaveDataconnectionsValues()`.
- Use `LoadDataConnectionsValues()` when you need a refreshed in-memory snapshot.
- Initialize or refresh `QueryList` before operations that depend on generated SQL metadata.
- Keep config paths stable unless you are intentionally migrating application storage.

## Pitfalls

- Bypassing facade methods can desync in-memory collections from persisted files.
- Renaming config files or changing folder layout breaks existing tools and app assumptions.
- Replacing `Config` or re-initializing paths without updating dependent managers causes stale references.
- Creating a second `ConfigEditor` for the same app context fragments state and makes debugging harder.

## Example: Complete Setup

```csharp
var editor = new DMEEditor();

// Initialize configuration
editor.ConfigEditor.Init();

// Load existing connections
var connections = editor.ConfigEditor.LoadDataConnectionsValues();

// Add new connection
var props = new ConnectionProperties
{
    ConnectionName = "MyDb",
    DatabaseType = DataSourceType.SqlLite,
    Category = DatasourceCategory.RDBMS,
    ConnectionString = "Data Source=./Beep/dbfiles/app.db"
};
editor.ConfigEditor.AddDataConnection(props);
editor.ConfigEditor.SaveDataconnectionsValues();

// Initialize query defaults
var queries = editor.ConfigEditor.InitQueryDefaultValues();
editor.ConfigEditor.QueryList = queries;
editor.ConfigEditor.SaveQueryFile();

// Save driver configurations
editor.ConfigEditor.SaveConfigValues();
```

## Related Documentation

- [Core Architecture](CoreArchitecture.md) - IDMEEditor overview
- [Service Registration](ServiceRegistration.md) - DI setup
- [Assembly Handler](AssemblyHandler.md) - Plugin loading
