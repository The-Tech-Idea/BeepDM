---
name: universal-general-helper
description: Guidance for GeneralDataSourceHelper as an IDataSourceHelper delegator that forwards schema, DDL, DML, constraints, and utility calls to datasource-specific helpers.
---

# Universal General Helper

Use this skill when you want one consistent `IDataSourceHelper` interface while still honoring datasource-specific behavior.

## Responsibilities
- Wrap a datasource-specific helper selected by `DataSourceType`.
- Delegate full helper surface without duplicating dialect logic.

## Core API Surface
- `GeneralDataSourceHelper(DataSourceType dataSourceType, IDMEEditor dmeEditor)`
- Delegated operations:
  - Schema: `GetSchemaQuery`, `GetTableExistsQuery`, `GetColumnInfoQuery`
  - DDL: `GenerateCreateTableSql`, `GenerateDropTableSql`, `GenerateAddColumnSql`
  - DML: `GenerateInsertSql`, `GenerateUpdateSql`, `GenerateDeleteSql`, `GenerateSelectSql`
  - Utility: `QuoteIdentifier`, `ValidateEntity`, `SupportsCapability`

## Typical Usage Pattern
1. Instantiate with target `DataSourceType`.
2. Call helper methods through common interface.
3. Avoid direct type checks in caller code.

## Pitfalls
- Do not bypass this helper with inline SQL for operations already covered.
- Do not ignore returned `Success` and `ErrorMessage`.

## Integration Points
- [universal-helper-factory](../universal-helper-factory/SKILL.md)
- [universal-rdbms-helper](../universal-rdbms-helper/SKILL.md)
- [connection](../connection/SKILL.md)
