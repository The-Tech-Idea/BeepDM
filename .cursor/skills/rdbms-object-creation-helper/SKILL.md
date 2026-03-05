---
name: rdbms-object-creation-helper
description: Guidance for DatabaseObjectCreationHelper to generate create/drop/truncate table SQL and index/primary-key DDL from entity structures.
---

# RDBMS Object Creation Helper

Use this skill when creating or altering database objects from `EntityStructure` definitions.

## Core API Surface
- `GenerateCreateTableSQL(EntityStructure entity)`
- `GeneratePrimaryKeyQuery(DataSourceType, tableName, primaryKey, type)`
- `GeneratePrimaryKeyFromEntity(EntityStructure entity)`
- `GenerateCreateIndexQuery(DataSourceType, tableName, indexName, columns, options = null)`
- `GenerateUniqueIndexFromEntity(EntityStructure entity)`
- `GetDropEntity(DataSourceType, entityName)`
- `GetTruncateTableQuery(DataSourceType, tableName, schemaName = null)`

## Usage Pattern
1. Validate entity first.
2. Generate DDL via helper.
3. Execute DDL and handle provider-specific constraints.

## Validation and Safety
- Always check `Success` and `ErrorMessage` tuples.
- Use normalized entity and field names to avoid identifier issues.

## Pitfalls
- Avoid generating DDL for entities with missing fields.
- Do not assume all databases support identical identity syntax.

## Integration Points
- [rdbms-entity-validation](../rdbms-entity-validation/SKILL.md)
- [rdbms-feature-helper](../rdbms-feature-helper/SKILL.md)
