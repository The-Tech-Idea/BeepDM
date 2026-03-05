---
name: rdbms-schema-query-helper
description: Guidance for DatabaseSchemaQueryHelper to generate and validate schema, table-existence, and column metadata queries across datasource types.
---

# RDBMS Schema Query Helper

Use this skill when building metadata queries and validating schema-discovery SQL/commands.

## Core API Surface
- `GetSchemasorDatabases(DataSourceType rdbms, string userName)`
- `GetSchemasorDatabasesSafe(...)`
- `ValidateSchemaQuery(...)`
- `GetTableExistsQuery(DataSourceType, string tableName, string schemaName = null)`
- `GetColumnInfoQuery(DataSourceType, string tableName, string schemaName = null)`

## Usage Pattern
1. Generate query based on provider and user/schema context.
2. Validate with `ValidateSchemaQuery(...)` for safety and diagnostics.
3. Execute through datasource query API.

## Validation and Safety
- Escape or sanitize user/schema input before query generation.
- Use safe variant for error-aware flows.
- Expect provider-specific non-SQL commands for some NoSQL types.

## Pitfalls
- Do not assume relational SQL for all datasource types.
- Do not ignore warnings from validation results.

## Integration Points
- [rdbms-helper-facade](../rdbms-helper-facade/SKILL.md)
- [rdbms-query-repository-helper](../rdbms-query-repository-helper/SKILL.md)
