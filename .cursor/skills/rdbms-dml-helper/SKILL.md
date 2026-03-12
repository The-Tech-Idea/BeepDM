---
name: rdbms-dml-helper
description: Guidance for DatabaseDMLHelper in BeepDM. Use when generating insert, update, delete, select, bulk, parameterized, or advanced SQL queries across RDBMS providers instead of composing SQL manually.
---

# RDBMS DML Helper

Use this skill when generating DML centrally instead of hand-building query strings.

## File Locations
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/DMLHelpers/DatabaseDMLHelper.cs`
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/DMLHelpers/DatabaseDMLBasicOperations.cs`
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/DMLHelpers/DatabaseDMLAdvancedQueryGenerator.cs`
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/DMLHelpers/DatabaseDMLParameterizedQueries.cs`
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/DMLHelpers/DatabaseDMLBulkOperations.cs`
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/DMLHelpers/DatabaseDMLUtilities.cs`

## Core APIs
- basic operations such as `GenerateInsertQuery`, `GenerateUpdateQuery`, `GenerateDeleteQuery`
- advanced operations such as `GenerateSelectQuery`, joins, aggregations, and window functions
- bulk and upsert helpers
- parameterized query generators
- utilities such as `GetPagingSyntax`, `GetRecordCountQuery`, and `SafeQuote`

## Working Rules
1. Prefer parameterized generators for external input.
2. Keep `DataSourceType` explicit for provider-specific syntax.
3. Use helper paging/count utilities instead of manual clauses.

## Related Skills
- [`rdbms-helper-facade`](../rdbms-helper-facade/SKILL.md)
- [`rdbms-entity-helper-sql`](../rdbms-entity-helper-sql/SKILL.md)
- [`idatasource`](../idatasource/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for the DML API surface and safety notes.
