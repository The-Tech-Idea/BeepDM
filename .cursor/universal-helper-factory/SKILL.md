---
name: universal-helper-factory
description: Guidance for DataSourceHelperFactory and GeneralDataSourceHelper usage to resolve IDataSourceHelper implementations by DataSourceType. Use when routing helper selection dynamically.
---

# Universal Helper Factory

Use this skill when you need to select the correct `IDataSourceHelper` for a datasource type at runtime.

## Responsibilities
- Resolve helper implementation using `DataSourceHelperFactory.CreateHelper(...)`.
- Verify coverage via `IsHelperAvailable(...)` and `GetSupportedDataSources()`.
- Register custom helpers with `RegisterHelper(...)`.

## Core API Surface
- `DataSourceHelperFactory(IDMEEditor dmeEditor)`
- `CreateHelper(DataSourceType datasourceType)`
- `IsHelperAvailable(DataSourceType datasourceType)`
- `RegisterHelper(DataSourceType datasourceType, Func<IDMEEditor, IDataSourceHelper> factory)`
- `GetSupportedDataSources()`

## Typical Usage Pattern
1. Create factory from `IDMEEditor`.
2. Resolve helper from `ConnectionProperties.DatabaseType`.
3. Use resolved helper for SQL generation and validation.
4. Register overrides for project-specific datasource types.

## Validation and Safety
- Always handle fallback helper behavior for unknown datasource types.
- Do not assume helper exists; check `IsHelperAvailable(...)` for strict flows.

## Integration Points
- [universal-general-helper](../universal-general-helper/SKILL.md)
- [universal-rdbms-helper](../universal-rdbms-helper/SKILL.md)
- [idatasource](../idatasource/SKILL.md)
