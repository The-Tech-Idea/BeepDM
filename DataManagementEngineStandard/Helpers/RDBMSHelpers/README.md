# RDBMSHelpers

SQL and schema helper stack for relational database operations.

## Entry Facade
- `RDBMSHelper` is the public static facade consumed by query/config/editor flows.

## Internal Organization
- `DatabaseFeatureHelper`: feature capability and transaction syntax.
- `DatabaseSchemaQueryHelper`: schema/database/table metadata queries.
- `DatabaseObjectCreationHelper`: create/drop/alter DDL generation.
- `DMLHelpers/*`: insert/update/delete/select, bulk, parameterized, advanced queries.
- `EntityHelpers/*`: entity-based SQL generation and structure validation.
- `DatabaseQueryRepositoryHelper`: default query repository generation.

## Key Capabilities
- Generate provider-specific SQL for major CRUD + DDL operations.
- Validate schema queries and entity structures.
- Provide paging, transaction, and feature support abstractions.
- Support query repository bootstrapping (`CreateQuerySqlRepos`).

## Usage Guidance
- Use `RDBMSHelper` facade from caller code.
- Avoid hardcoded SQL in higher layers when helper generation exists.
- Pair generated SQL with datasource capability checks for portability.
