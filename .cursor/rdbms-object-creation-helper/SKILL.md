---
name: rdbms-object-creation-helper
description: Guidance for DatabaseObjectCreationHelper in BeepDM. Use when generating create, drop, truncate, primary-key, or index DDL from EntityStructure metadata across RDBMS providers.
---

# RDBMS Object Creation Helper

Use this skill when creating or altering database objects from `EntityStructure`.

## File Locations
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/DatabaseObjectCreationHelper.cs`
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/DatabaseFeatureHelper.cs`

## Core APIs
- `GenerateCreateTableSQL(EntityStructure entity)`
- `GeneratePrimaryKeyQuery(...)`
- `GeneratePrimaryKeyFromEntity(...)`
- `GenerateCreateIndexQuery(...)`
- `GenerateUniqueIndexFromEntity(...)`
- `GetDropEntity(...)`
- `GetTruncateTableQuery(...)`

## Working Rules
1. Validate entity shape before DDL generation.
2. Keep provider-specific identity and index semantics delegated through helper logic.
3. Preserve tuple-style success/error contracts where exposed.

## Related Skills
- [`rdbms-entity-validation`](../rdbms-entity-validation/SKILL.md)
- [`rdbms-feature-helper`](../rdbms-feature-helper/SKILL.md)
- [`migration`](../migration/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for DDL helpers, pitfalls, and example flows.
