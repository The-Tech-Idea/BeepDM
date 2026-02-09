# Editor

High-level orchestration layer for data operations in BeepDM.

## What This Layer Owns
- Datasource lifecycle orchestration (`DMEEditor`).
- ETL, importing, mapping, defaults, and synchronization workflows.
- Unit-of-work abstractions for stateful CRUD and commit/rollback behavior.

## Main Modules
- `DM/`
  - `DMEEditor` core orchestration, datasource resolution, logging/events.

- `Defaults/`
  - Default value resolution engine with pluggable resolvers and validation.

- `ETL/`
  - Script generation, structure copy, and ETL operation flow.

- `Importing/`
  - `DataImportManager` with helper-based pipeline (validation, transform, batch, progress).

- `Mapping/`
  - Entity/field mapping creation and object-to-object transformation tools.

- `UOW/`
  - `UnitofWork<T>` and wrappers for change tracking and commit workflows.

- `BeepSync/`
  - Synchronization-specific manager and helper interfaces.

- `Migration/`
  - Migration-focused editor abstractions.

## Typical Workflow
1. Resolve source/destination datasources via `DMEEditor`.
2. Load structures and mappings (`ConfigEditor` + `MappingManager`).
3. Run import/ETL/sync with progress and validation.
4. Persist metadata/history via `ConfigUtil` managers.

## Error and Logging Pattern
- Use `IErrorsInfo` for operation status.
- Use `DMEEditor.AddLogMessage(...)` for structured runtime logs.
- Prefer continuing with explicit warnings when non-critical sub-operations fail.

## Extension Guidance
- Add new operations as focused managers/helpers rather than expanding `DMEEditor` monolithically.
- Keep backward-compatible overloads when introducing enhanced configuration APIs.
