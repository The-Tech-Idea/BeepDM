---
name: rdbms-entity-helper-sql
description: Guidance for DatabaseEntityHelper and DatabaseEntitySqlGenerator to create insert/update/delete SQL from EntityStructure plus value dictionaries.
---

# RDBMS Entity Helper SQL

Use this skill when converting entity metadata + value maps into SQL operations.

## Responsibilities
- Use `DatabaseEntityHelper` facade for entity SQL generation.
- Delegate concrete SQL building to `DatabaseEntitySqlGenerator`.

## Core API Surface
- `GenerateDeleteEntityWithValues(EntityStructure, Dictionary<string, object>)`
- `GenerateInsertWithValues(EntityStructure, Dictionary<string, object>)`
- `GenerateUpdateEntityWithValues(EntityStructure, Dictionary<string, object>, Dictionary<string, object>)`
- `CreateBasicField(...)` for field construction helpers

## Usage Pattern
1. Ensure `EntityStructure.DatabaseType` and fields are valid.
2. Prepare value and condition dictionaries.
3. Generate SQL via helper and execute in datasource.

## Validation and Safety
- Validate entity shape before generation.
- Keep dictionary keys aligned with real field names.

## Pitfalls
- Do not call with empty value dictionaries.
- Do not pass entities with unknown `DatabaseType`.

## Integration Points
- [rdbms-entity-validation](../rdbms-entity-validation/SKILL.md)
- [rdbms-dml-helper](../rdbms-dml-helper/SKILL.md)
- [idatasource](../idatasource/SKILL.md)
