---
name: rdbms-schema-query-helper
description: Guidance for DatabaseSchemaQueryHelper in BeepDM. Use when generating or validating schema, table-existence, and column metadata queries across RDBMS providers.
---

# RDBMS Schema Query Helper

Use this skill when building metadata queries and schema-discovery SQL.

## File Locations
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/DatabaseSchemaQueryHelper.cs`
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/DatabaseQueryRepositoryHelper.cs`

## Core APIs
- `GetSchemasorDatabases(...)`
- `GetSchemasorDatabasesSafe(...)`
- `ValidateSchemaQuery(...)`
- `GetTableExistsQuery(...)`
- `GetColumnInfoQuery(...)`

## Working Rules
1. Keep `DataSourceType` explicit for provider-specific syntax.
2. Use safe/validated variants where caller diagnostics matter.
3. Preserve placeholders and schema-name handling consistently across providers.

## Related Skills
- [`rdbms-helper-facade`](../rdbms-helper-facade/SKILL.md)
- [`rdbms-query-repository-helper`](../rdbms-query-repository-helper/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for method list, validation notes, and pitfalls.
