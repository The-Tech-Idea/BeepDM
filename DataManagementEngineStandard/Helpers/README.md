# Helpers

Cross-cutting helper libraries used by datasources, editors, and tools.

## High-Value Areas
- `ConnectionHelpers/`
  - Driver linking, connection string building/validation/masking, test utilities.

- `DataTypesHelpers/`
  - Datasource-specific type mapping, cache-aware type resolution.

- `RDBMSHelpers/`
  - Database-agnostic SQL generation facade and specialized helper layers.

- `UniversalDataSourceHelpers/`
  - `IDataSourceHelper` implementations for many datasource families.
  - Factory-based helper selection (`DataSourceHelperFactory`).

- `FileandFolderHelpers/`, `ProjectandLibraryHelpers/`
  - File/project lifecycle support for tool and runtime workflows.

## How to Use
- Prefer these helpers from editor/services layers rather than duplicating logic.
- Keep datasource-specific branching inside helper modules, not callers.
- Reuse shared validation/generation helpers for consistency.

## Related
- `ConnectionHelpers/README.md`
- `DataTypesHelpers/README.md`
- `RDBMSHelpers/README.md`
- `UniversalDataSourceHelpers/README.md`
