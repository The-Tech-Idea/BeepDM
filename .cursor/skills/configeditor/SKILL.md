---
name: configeditor
description: Guidance for ConfigEditor usage, delegated managers, and persisted configuration files in BeepDM. Use when loading, updating, or saving data connections, query metadata, mappings, drivers, workflows, reports, projects, or migration history.
---

# ConfigEditor Guide

Use this skill when working with BeepDM configuration persistence and the manager façade exposed through `IConfigEditor`.

## Use this skill when
- Loading or saving `DataConnections`
- Working with query metadata, mappings, and entity structures
- Managing component metadata such as drivers, workflows, reports, and projects
- Understanding how config paths and specialized managers are composed

## Do not use this skill when
- The main problem is building a connection definition itself. Use [`connectionproperties`](../connectionproperties/SKILL.md).
- The main problem is validating or securing connection strings. Use [`connection`](../connection/SKILL.md).
- The main problem is opening and using the datasource. Use [`beepdm`](../beepdm/SKILL.md).

## Core Managers
- `ConfigPathManager`: config root and folder structure
- `DataConnectionManager`: `DataConnections` CRUD and persistence
- `QueryManager`: `QueryList` operations and default query initialization
- `EntityMappingManager`: entity metadata, mappings, and datasource entities
- `ComponentConfigManager`: drivers, workflows, reports, projects, add-ins, and component metadata
- `MigrationHistoryManager`: per-datasource migration history

## Responsibilities
- Treat `ConfigEditor` as the façade; prefer its public methods over directly manipulating underlying managers.
- Persist configuration to the configured app/container path.
- Keep in-memory collections and on-disk configuration synchronized.
- Provide the metadata needed by datasource creation, migrations, ETL, and UI tooling.

## Typical Workflow
1. Access `editor.ConfigEditor`; avoid creating ad-hoc config stores once the editor exists.
2. Load the relevant collection such as `LoadDataConnectionsValues()`.
3. Update through façade methods like `AddDataConnection`, `SaveDataconnectionsValues`, `SaveQueryFile`, or `SaveMappingValues`.
4. Refresh in-memory collections when code depends on newly persisted state.
5. Let downstream systems consume config through `IDMEEditor`, not duplicate file parsing.

## Validation and Safety
- After adding or changing a connection, call `SaveDataconnectionsValues()`.
- Use `LoadDataConnectionsValues()` when you need a refreshed in-memory snapshot.
- Initialize or refresh `QueryList` before operations that depend on generated SQL metadata.
- Keep config paths stable unless you are intentionally migrating application storage.

## Pitfalls
- Bypassing façade methods can desync in-memory collections from persisted files.
- Renaming config files or changing folder layout breaks existing tools and app assumptions.
- Replacing `Config` or re-initializing paths without updating dependent managers causes stale references.
- Creating a second `ConfigEditor` for the same app context fragments state and makes debugging harder.

## File Locations
- `DataManagementEngineStandard/ConfigUtil/ConfigEditor.cs`
- `DataManagementEngineStandard/ConfigUtil/Managers/DataConnectionManager.cs`
- `DataManagementEngineStandard/ConfigUtil/Managers/QueryManager.cs`
- `DataManagementEngineStandard/ConfigUtil/Managers/EntityMappingManager.cs`
- `DataManagementEngineStandard/ConfigUtil/Managers/ComponentConfigManager.cs`
- `DataManagementEngineStandard/ConfigUtil/Managers/MigrationHistoryManager.cs`

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

## Related Skills
- [`connectionproperties`](../connectionproperties/SKILL.md)
- [`connection`](../connection/SKILL.md)
- [`beepdm`](../beepdm/SKILL.md)
- [`migration`](../migration/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for quick save/load snippets and manager-specific examples.
