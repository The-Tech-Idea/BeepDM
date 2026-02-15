---
name: configeditor
description: Guidance for ConfigEditor usage, manager responsibilities, and persisted configuration files in BeepDM.
---

# ConfigEditor Guide

Use this skill when working with BeepDM configuration persistence, connections, queries, mappings, or component metadata.

## Core Managers
- ConfigPathManager: config root and folder structure
- DataConnectionManager: DataConnections CRUD
- QueryManager: QueryList operations
- EntityMappingManager: entity structures and mappings
- ComponentConfigManager: drivers, workflows, reports, projects
- MigrationHistoryManager: per-datasource migration history

## Workflow
1. Access `editor.ConfigEditor` (do not create your own).
2. Load or update configuration collections (connections, drivers, query list).
3. Save via facade methods on ConfigEditor.
4. Use mapping and entity structure methods for metadata persistence.

## Validation
- After adding a connection, call `SaveDataconnectionsValues()`.
- Ensure `QueryList` is populated before schema operations.
- Use `LoadDataConnectionsValues()` to refresh in-memory state.

## Pitfalls
- Bypassing manager facade methods can desync in-memory and persisted state.
- Changing file names or paths breaks existing tools and docs.
- Forgetting to refresh managers after config re-init can drop references.

## File Locations
- DataManagementEngineStandard/ConfigUtil/ConfigEditor.cs
- DataManagementEngineStandard/ConfigUtil/README.md
- DataManagementEngineStandard/ConfigUtil/Managers/

## Example
```csharp
var config = editor.ConfigEditor;

// Load existing connections
var connections = config.LoadDataConnectionsValues();

// Add a new connection
var props = new ConnectionProperties
{
    ConnectionName = "MyDb",
    DatabaseType = DataSourceType.SqlLite,
    Category = DatasourceCategory.RDBMS,
    ConnectionString = "Data Source=./Beep/dbfiles/app.db"
};
config.AddDataConnection(props);
config.SaveDataconnectionsValues();
```

## Task-Specific Examples

### Initialize Query List Defaults
```csharp
var queries = config.InitQueryDefaultValues();
config.QueryList = queries;
config.SaveQueryFile();
```

### Persist Mapping For Entity
```csharp
config.SaveMappingValues("Customers", "MyDb", mapping);
```
