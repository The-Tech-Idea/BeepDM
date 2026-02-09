# ConfigUtil

Configuration orchestration and persistence for BeepDM.

## Purpose
`ConfigUtil` provides the implementation behind `IConfigEditor` and centralizes loading, saving, and path initialization for Beep configuration artifacts.

## Core Architecture
- `ConfigEditor` is the facade used by `DMEEditor` and other runtime services.
- Work is delegated to specialized managers:
  - `ConfigPathManager`: folder/bootstrap path handling.
  - `DataConnectionManager`: data connection CRUD and persistence.
  - `QueryManager`: SQL/query repository generation and lookup.
  - `EntityMappingManager`: entity structures, mapping schema, datatype files.
  - `ComponentConfigManager`: drivers, workflows, reports, projects, categories.
  - `MigrationHistoryManager`: per-datasource migration history.

## Initialization Flow
1. `ConfigEditor` creates path and manager instances.
2. `InitConfig()` ensures directory structure and base `Config.json`.
3. Connection/query/component state is loaded from persisted JSON.
4. Manager `Config` references are refreshed after initialization.

## Persisted Files
Main config path (`ConfigPath`):
- `Config.json`
- `DataConnections.json`
- `ConnectionConfig.json`
- `QueryList.json`
- `CategoryFolders.json`
- `Reportslist.json`
- `reportsDefinition.json`
- `AIScripts.json`
- `Projects.json`

Other managed paths:
- `WorkFlow/DataWorkFlow.json`
- `Mapping/{datasource}/{entity}_Mapping.json`
- `Entities/{datasource}_entities.json`
- `Migrations/{datasource}_MigrationHistory.json`

## Key Integration Points
- `DMEEditor.ConfigEditor` is the primary entry point at runtime.
- `MappingManager` reads/writes mappings through `ConfigEditor.SaveMappingValues` and `LoadMappingValues`.
- Import/default workflows rely on stored datasource defaults and connection metadata.

## Implementation Notes
- Use `ConfigEditor` facade methods instead of calling managers directly.
- Keep filename expectations synchronized with docs under `DataManagementEngineStandard/Docs`.
- Avoid bypassing `ConfigPathManager` when adding new persisted assets.
