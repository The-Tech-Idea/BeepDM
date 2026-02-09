# ConfigUtil Managers

Technical reference for manager classes used by `ConfigEditor`.

## Manager Responsibilities
- `ConfigPathManager`
  - Resolves `ExePath`, `ContainerName`, and typed folder locations.
  - Creates required directories for application/data connector modes.

- `DataConnectionManager`
  - Maintains in-memory `DataConnections` list.
  - Supports add/update/remove operations by name, id, and guid.
  - Persists to `DataConnections.json`.

- `QueryManager`
  - Provides SQL generation and query lookup APIs.
  - Bridges to `RDBMSHelper` for database-specific syntax.
  - Persists to `QueryList.json`.

- `EntityMappingManager`
  - Saves/loads entity structures and datasource entity lists.
  - Persists `EntityDataMap` under `Mapping/{datasource}`.
  - Handles mapping schema and datatype mapping files.

- `ComponentConfigManager`
  - Manages driver definitions (`ConnectionConfig.json`).
  - Handles workflows, reports, project roots, and category folders.

- `MigrationHistoryManager`
  - Tracks migration records per datasource.
  - Appends migration steps and persists history snapshots.

## Design Rules
- Managers are persistence-focused and intentionally thin.
- `ConfigEditor` owns orchestration and cross-manager coordination.
- New manager functionality should expose simple methods with explicit file targets.
