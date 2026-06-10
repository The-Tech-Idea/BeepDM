---
name: beepdm-configuration
description: Use when loading, updating, or saving persisted BeepDM configuration — connections, queries, mappings, drivers, workflows, reports, projects, or migration history — through the ConfigEditor facade. Hands off to Setup (first run), Migration (history reads), and ETL (mappings) skills.
---

# beepdm-configuration

`ConfigEditor` is the **persisted-configuration facade** for BeepDM. It owns the JSON config files under the app container path and exposes focused sub-managers so the rest of the engine reads/writes through a single, stable API.

## When to use this skill

- Adding / removing / updating a `ConnectionProperties` entry.
- Loading or saving `QueryList` (SQL templates for metadata discovery).
- Saving or reading `EntityDataMap` mappings between entity and datasource.
- Registering a new `ConnectionDriversConfig` (driver class metadata).
- Recording / querying migration history per datasource.
- Inspecting the config path on the current platform.

## Do NOT use this skill for

- Building a connection definition for the first time on a fresh machine → use **beepdm-setup**.
- Designing / applying schema migrations → use **beepdm-migration**.
- Bulk data movement → use **beepdm-etl**.

## File Locations

`DataManagementEngineStandard/ConfigUtil/`:

- `ConfigEditor.cs` — façade
- `Managers/ConfigPathManager.cs` — config root and folder structure
- `Managers/DataConnectionManager.cs` — connection CRUD
- `Managers/QueryManager.cs` — `QueryList` operations + default init
- `Managers/EntityMappingManager.cs` — entity metadata + mappings
- `Managers/ComponentConfigManager.cs` — drivers, workflows, reports, projects
- `Managers/MigrationHistoryManager.cs` — per-datasource migration history

There is also a separate **app-level** config layer at `DataManagementEngineStandard/Configuration/` (`AppSettings.cs`, `ISettingsProvider.cs`) — use that for environment-wide settings, not for per-datasource metadata. The two layers are deliberately separate.

## Architecture

```
ConfigEditor (Facade)
├── ConfigPathManager              # Config root and folder structure
├── DataConnectionManager          # Connection CRUD + persistence
├── QueryManager                   # QueryList operations
├── EntityMappingManager           # Entity metadata + mappings
├── ComponentConfigManager         # Drivers, workflows, reports, projects
└── MigrationHistoryManager        # Per-datasource migration history
```

## Sub-managers — when to use which

| Sub-manager | Use it for | Example API |
|---|---|---|
| `ConfigPathManager` | Resolving the container's config root on this OS. | `editor.ConfigEditor.ConfigPath` |
| `DataConnectionManager` | CRUD on `ConnectionProperties`. | `LoadDataConnectionsValues()`, `AddDataConnection(props)`, `SaveDataconnectionsValues()` |
| `QueryManager` | `QueryList` for metadata-discovery SQL. | `InitQueryDefaultValues()`, `SaveQueryFile()` |
| `EntityMappingManager` | Saving/loading `EntityDataMap` per (entity, datasource). | `SaveMappingValues(entity, ds, map)`, `LoadMappingValues(entity, ds)` |
| `ComponentConfigManager` | Driver, workflow, report, project metadata. | `DataDriversClasses`, `SaveConfigValues()` |
| `MigrationHistoryManager` | Recording which migrations ran against which datasource. | `RecordMigration(ds, name, version)`, `IsMigrationApplied(ds, name)`, `GetMigrationHistory(ds)` |

## Configuration Files

| File | Owner | Purpose |
|---|---|---|
| `DataConnections.json` | `DataConnectionManager` | Connection definitions |
| `ConnectionConfig.json` | `ComponentConfigManager` | Driver class metadata |
| `DataTypeMapping.json` | `DataTypesHelper` | Type translation between sources |
| `QueryList.json` | `QueryManager` | SQL templates for metadata discovery |
| `MigrationHistory/{ds}.json` | `MigrationHistoryManager` | Per-datasource applied-migration log |

## Typical Workflow

1. Access `editor.ConfigEditor`; **avoid creating ad-hoc config stores** once the editor exists.
2. Load the relevant collection (`LoadDataConnectionsValues()`, etc.).
3. Update through the façade methods (`AddDataConnection`, `SaveDataconnectionsValues`, `SaveQueryFile`, `SaveMappingValues`).
4. Refresh in-memory collections when downstream code depends on the new state.
5. Let other systems consume config through `IDMEEditor`, not duplicate file parsing.

## How this skill works with the rest of the data-management layer

| Handoff | Direction | What flows |
|---|---|---|
| **beepdm-setup** | ← Setup | `ConnectionConfigStep` writes connections through this façade. First-run connections land in `DataConnections.json`. |
| **beepdm-migration** | ↔ Migration | `MigrationManager` consults `IsMigrationApplied(ds, name)` before running and calls `RecordMigration(...)` after success. `MigrationHistoryManager` is the persisted source of truth. |
| **beepdm-etl** | ↔ ETL | ETL reads `EntityDataMap` from `EntityMappingManager` to know how source fields map to target fields. ETL does not maintain its own mapping store. |
| **beepdm-unitofwork** | ← UoW | UoW does not write to `ConfigEditor`; it operates against an already-configured datasource. |
| **beepdm-forms** | ← Forms | Forms read entity structure from config when present, but fall back to runtime discovery via `IDataSource.GetEntityStructure`. |

## Pitfalls

- **Bypass the façade** → in-memory collections desync from on-disk files. Always use the public methods.
- **Rename config files / move folders** → existing tools and app assumptions break. Treat the layout as a public contract.
- **Replace `Config` mid-run** → dependent managers hold stale references. Initialize once, reuse.
- **Two `ConfigEditor` instances for the same app** → state fragments. Use DI to share a single instance.
- **Forget `Save*` after a mutation** → the change is in memory only. Persist before expecting downstream readers to see it.

## Cross-references

- See **beepdm-setup** for the wizard that writes first-run config.
- See **beepdm-migration** for the history manager it shares with.
- See **beepdm-etl** for the mapping manager it shares with.
- See `.cursor/configeditor/SKILL.md` for the deep-dive implementation details.
