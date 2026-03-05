---
name: universal-rdbms-helper
description: Guidance for UniversalDataSourceHelpers.RdbmsHelper implementing IDataSourceHelper for RDBMS dialects with delegated schema, DDL, DML, typing, and capability checks.
---

# Universal RDBMS Helper

Use this skill when generating RDBMS-safe SQL and capability-aware operations through a unified helper.

## Responsibilities
- Provide one `IDataSourceHelper` for SQL Server, MySQL, PostgreSQL, Oracle, SQLite, DB2, FireBird, and related RDBMS.
- Delegate to specialized helpers: schema, DDL, DML, and entity validation.

## Core API Surface
- `SupportedType`, `Capabilities`, `Name`
- Schema: `GetSchemaQuery`, `GetTableExistsQuery`, `GetColumnInfoQuery`
- DDL: `GenerateCreateTableSql`, `GenerateDropTableSql`, `GenerateTruncateTableSql`, `GenerateAddColumnSql`
- DML: `GenerateInsertSql`, `GenerateUpdateSql`, `GenerateDeleteSql`, `GenerateSelectSql`
- Utilities: `QuoteIdentifier`, `MapClrTypeToDatasourceType`, `MapDatasourceTypeToClrType`, `ValidateEntity`

## Validation and Safety
- Call `ValidateEntity(...)` before create/alter DDL generation.
- Use `QuoteIdentifier(...)` when composing table/column names.
- Respect `SupportsCapability(...)` before relying on advanced SQL features.

## Pitfalls
- Do not hardcode type mappings across providers; use helper mapping methods.
- Avoid provider assumptions about pagination or identity syntax.

## Integration Points
- [rdbms-helper-facade](../rdbms-helper-facade/SKILL.md)
- [rdbms-dml-helper](../rdbms-dml-helper/SKILL.md)
- [rdbms-feature-helper](../rdbms-feature-helper/SKILL.md)
