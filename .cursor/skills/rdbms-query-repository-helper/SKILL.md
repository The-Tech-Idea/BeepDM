---
name: rdbms-query-repository-helper
description: Guidance for DatabaseQueryRepositoryHelper to retrieve predefined SQL templates by DataSourceType and Sqlcommandtype, validate query repository quality, and inspect coverage.
---

# RDBMS Query Repository Helper

Use this skill when consuming or validating prebuilt query templates.

## Core API Surface
- `GetQuery(DataSourceType, Sqlcommandtype)`
- `GetQueriesForDatabase(DataSourceType)`
- `GetDatabasesForQueryType(Sqlcommandtype)`
- `QueryExists(DataSourceType, Sqlcommandtype)`
- `IsSqlStatementValid(string sqlString)`
- `CreateQuerySqlRepos()`
- `GetQueryStatistics()`
- `ValidateAllQueries()`

## Usage Pattern
1. Resolve query by database + command type.
2. Validate query is present before execution.
3. Fall back to helper-generated SQL when repository entry is absent.

## Pitfalls
- Do not treat all repository commands as ANSI SQL (some are provider commands).
- Avoid hardcoding `"YourSchema"` placeholders; replace with runtime values.

## Integration Points
- [rdbms-schema-query-helper](../rdbms-schema-query-helper/SKILL.md)
- [rdbms-dml-helper](../rdbms-dml-helper/SKILL.md)
- [connection](../connection/SKILL.md)
