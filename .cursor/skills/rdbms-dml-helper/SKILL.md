---
name: rdbms-dml-helper
description: Guidance for DatabaseDMLHelper to generate insert, update, delete, select, bulk, parameterized, and advanced SQL queries across RDBMS providers.
---

# RDBMS DML Helper

Use this skill when generating DML SQL centrally instead of composing query strings manually.

## Core API Surface
- Basic: `GenerateInsertQuery`, `GenerateUpdateQuery`, `GenerateDeleteQuery`
- Advanced: `GenerateSelectQuery`, `GenerateJoinQuery`, `GenerateAggregationQuery`, `GenerateWindowFunctionQuery`
- Bulk: `GenerateBulkInsertQuery`, `GenerateUpsertQuery`, `GenerateBulkDeleteQuery`
- Parameterized: `GenerateParameterizedInsertQuery`, `GenerateParameterizedUpdateQuery`, `GenerateParameterizedDeleteQuery`
- Utility: `GetPagingSyntax`, `GetRecordCountQuery`, `SafeQuote`

## Usage Pattern
1. Build dictionaries/column lists from validated entities.
2. Generate SQL through helper.
3. Bind parameters through datasource provider if needed.

## Validation and Safety
- Prefer parameterized generator methods for external input.
- Use `SafeQuote` only for unavoidable literal composition.
- Keep `DataSourceType` explicit for provider-specific SQL.

## Pitfalls
- Do not hand-build paging clauses where helper supports them.
- Do not use unescaped identifiers from user input.

## Integration Points
- [universal-rdbms-helper](../universal-rdbms-helper/SKILL.md)
- [rdbms-query-repository-helper](../rdbms-query-repository-helper/SKILL.md)
