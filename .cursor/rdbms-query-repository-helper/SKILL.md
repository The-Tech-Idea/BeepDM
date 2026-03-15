---
name: rdbms-query-repository-helper
description: Guidance for DatabaseQueryRepositoryHelper in BeepDM. Use when retrieving predefined SQL templates by DataSourceType and Sqlcommandtype, validating repository coverage, or deciding when to fall back to generated SQL.
---

# RDBMS Query Repository Helper

Use this skill when consuming or validating prebuilt query templates.

## File Locations
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/DatabaseQueryRepositoryHelper.cs`
- `DataManagementEngineStandard/ConfigUtil/Managers/QueryManager.cs`

## Core APIs
- `GetQuery(...)`
- `GetQueriesForDatabase(...)`
- `GetDatabasesForQueryType(...)`
- `QueryExists(...)`
- `IsSqlStatementValid(...)`
- `CreateQuerySqlRepos()`
- `GetQueryStatistics()`
- `ValidateAllQueries()`

## Working Rules
1. Prefer repository lookups for stable provider templates.
2. Fall back to generated SQL when a repository entry is absent or too generic.
3. Keep placeholder expectations explicit and avoid baking runtime values into shared templates.

## Related Skills
- [`rdbms-schema-query-helper`](../rdbms-schema-query-helper/SKILL.md)
- [`rdbms-dml-helper`](../rdbms-dml-helper/SKILL.md)
- [`configeditor`](../configeditor/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for query coverage checks and fallback guidance.
