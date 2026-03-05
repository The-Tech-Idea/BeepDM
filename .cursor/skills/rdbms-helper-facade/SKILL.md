---
name: rdbms-helper-facade
description: Guidance for RDBMSHelper static facade usage and delegation boundaries across schema, object creation, DML, feature, query repository, and entity helpers.
---

# RDBMS Helper Facade

Use this skill when you need a single static entrypoint for RDBMS query generation and entity helper operations.

## Responsibilities
- Route calls to specialized helper classes.
- Keep callers stable while internals evolve.

## Core API Surface
- Schema: `GetSchemasorDatabases`, `GetTableExistsQuery`, `GetColumnInfoQuery`
- Object creation: `GenerateCreateTableSQL`, `GeneratePrimaryKeyQuery`, `GenerateCreateIndexQuery`
- DML: `GenerateInsertQuery`, `GenerateUpdateQuery`, `GenerateDeleteQuery`, `GetPagingSyntax`
- Features: `GenerateFetchLastIdentityQuery`, `GetTransactionStatement`, `SupportsFeature`
- Query repository: `GetQuery`, `IsSqlStatementValid`
- Entity: `ValidateEntityStructure`, `GenerateInsertWithValues`, `GenerateUpdateEntityWithValues`

## Typical Usage Pattern
1. Resolve target `DataSourceType`.
2. Generate SQL via facade method.
3. Execute via datasource abstraction and inspect errors.

## Pitfalls
- Do not mix facade-generated SQL with conflicting manual syntax.
- Avoid skipping validation calls for dynamic entities.

## Integration Points
- [rdbms-schema-query-helper](../rdbms-schema-query-helper/SKILL.md)
- [rdbms-object-creation-helper](../rdbms-object-creation-helper/SKILL.md)
- [rdbms-dml-helper](../rdbms-dml-helper/SKILL.md)
