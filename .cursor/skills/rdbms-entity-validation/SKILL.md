---
name: rdbms-entity-validation
description: Guidance for DatabaseEntityValidator, DatabaseEntityNamingValidator, and DatabaseEntityReservedKeywordChecker to validate entity structure, naming, and reserved keywords before SQL generation.
---

# RDBMS Entity Validation

Use this skill when validating entity schemas before create/alter/copy operations.

## Responsibilities
- Structural validation through `DatabaseEntityValidator`.
- Naming checks through `DatabaseEntityNamingValidator`.
- Keyword checks through `DatabaseEntityReservedKeywordChecker`.

## Core API Surface
- `ValidateEntityStructure(EntityStructure entity)`
- `ValidateEntityFields(List<EntityField> fields)`
- `ValidateNamingConventions(EntityStructure entity)`
- `IsValidIdentifier(string identifier)`
- `IsReservedKeyword(string identifier, DataSourceType databaseType)`

## Usage Pattern
1. Validate structure and fields.
2. Validate naming conventions and reserved keywords.
3. Stop execution on any validation errors.

## Validation and Safety
- Require at least one primary key field.
- Prevent invalid identifiers and provider-reserved names.
- Enforce `DataSourceType` to evaluate provider-specific rules.

## Pitfalls
- Do not skip validation for generated entities.
- Do not rely on case-sensitive checks only; use invariant comparison rules.

## Integration Points
- [rdbms-object-creation-helper](../rdbms-object-creation-helper/SKILL.md)
- [rdbms-entity-helper-sql](../rdbms-entity-helper-sql/SKILL.md)
- [migration](../migration/SKILL.md)
