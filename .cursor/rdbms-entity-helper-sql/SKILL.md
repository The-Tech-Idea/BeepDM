---
name: rdbms-entity-helper-sql
description: Guidance for DatabaseEntityHelper and DatabaseEntitySqlGenerator in BeepDM. Use when converting EntityStructure metadata plus value dictionaries into insert, update, or delete SQL for RDBMS providers.
---

# RDBMS Entity Helper SQL

Use this skill when converting entity metadata plus value maps into SQL operations.

## File Locations
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/EntityHelpers/DatabaseEntityHelper.cs`
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/EntityHelpers/DatabaseEntitySqlGenerator.cs`
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/EntityHelpers/DatabaseEntityAnalyzer.cs`

## Core APIs
- `GenerateDeleteEntityWithValues(...)`
- `GenerateInsertWithValues(...)`
- `GenerateUpdateEntityWithValues(...)`
- `CreateBasicField(...)`

## Working Rules
1. Ensure `EntityStructure.DatabaseType` and fields are valid before generation.
2. Keep value/condition dictionary keys aligned with actual field names.
3. Prefer this helper when entity metadata is already available instead of re-deriving column lists manually.

## Related Skills
- [`rdbms-entity-validation`](../rdbms-entity-validation/SKILL.md)
- [`rdbms-dml-helper`](../rdbms-dml-helper/SKILL.md)
- [`idatasource`](../idatasource/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for entity-SQL generation patterns and pitfalls.
