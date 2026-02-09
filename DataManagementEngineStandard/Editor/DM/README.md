# DM

Core editor orchestration centered on `DMEEditor`.

## Key Types
- `DMEEditor`
- `DMEEditor.UniversalDataSourceHelpers` (partial extension)

## Responsibilities
- Datasource lookup/open/close/create by name or guid.
- Lazy datasource creation through lifecycle helpers.
- Entity structure and data retrieval wrappers.
- Central eventing and logging (`PassEvent`, `AddLogMessage`).
- Bridge to configuration (`ConfigEditor`), ETL, type helpers, and assembly handlers.

## Important Behavior
- If datasource is missing, `DMEEditor` attempts to create it from configuration.
- Entity metadata can be loaded from persisted config when available.
- Errors are routed through `ErrorObject` and logger channels.

## Integration Points
- `ConfigEditor` for persisted connection/mapping/entity metadata.
- `DataSourceLifecycleHelper` for datasource construction/opening.
- Editor submodules (`Importing`, `ETL`, `UOW`, `Mapping`) consume `IDMEEditor`.
