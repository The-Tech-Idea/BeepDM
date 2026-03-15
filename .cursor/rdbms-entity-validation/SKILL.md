---
name: rdbms-entity-validation
description: Guidance for DatabaseEntityValidator and related naming/keyword validators in BeepDM. Use when validating entity structure, identifiers, and reserved-keyword safety before SQL generation or migration.
---

# RDBMS Entity Validation

Use this skill when validating entity schemas before create, alter, copy, or SQL-generation operations.

## File Locations
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/EntityHelpers/DatabaseEntityValidator.cs`
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/EntityHelpers/DatabaseEntityNamingValidator.cs`
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/EntityHelpers/DatabaseEntityReservedKeywordChecker.cs`
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/EntityHelpers/DatabaseEntityTypeHelper.cs`

## Core APIs
- `ValidateEntityStructure(...)`
- `ValidateEntityFields(...)`
- naming validation helpers
- reserved-keyword checks

## Working Rules
1. Validate before DDL or SQL generation, not after.
2. Treat provider-specific reserved keywords as part of validation, not an optional lint pass.
3. Preserve invariant/case-insensitive comparisons where the helper relies on them.

## Related Skills
- [`rdbms-object-creation-helper`](../rdbms-object-creation-helper/SKILL.md)
- [`rdbms-entity-helper-sql`](../rdbms-entity-helper-sql/SKILL.md)
- [`migration`](../migration/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for validator responsibilities and common pitfalls.
