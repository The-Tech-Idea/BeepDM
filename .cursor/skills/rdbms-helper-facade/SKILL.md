---
name: rdbms-helper-facade
description: Guidance for the static RDBMSHelper facade in BeepDM. Use when callers need a single entrypoint for schema queries, DDL, DML, feature checks, query repository lookups, or entity helper operations across RDBMS providers.
---

# RDBMS Helper Facade

Use this skill when you need the static `RDBMSHelper` entrypoint rather than the universal instance-based `RdbmsHelper`.

## Use this skill when
- Consuming existing static helper calls from query/config code
- Deciding which specialized RDBMS helper owns a new static facade method
- Keeping callers stable while helper internals evolve

## Do not use this skill when
- The task belongs in the instance-based universal helper implementing `IDataSourceHelper`. Use [`universal-rdbms-helper`](../universal-rdbms-helper/SKILL.md).
- The task is isolated to one helper area like DML, schema queries, or validation. Use the narrower `rdbms-*` skill for that subsystem.

## File Locations
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/RDBMSHelper.cs`
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/DatabaseSchemaQueryHelper.cs`
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/DatabaseObjectCreationHelper.cs`
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/DatabaseFeatureHelper.cs`
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/DatabaseQueryRepositoryHelper.cs`

## Responsibilities
- Keep the caller-facing static API stable.
- Delegate actual work to specialized helpers.
- Avoid duplicating logic that already exists in the specialized helpers.

## Related Skills
- [`rdbms-schema-query-helper`](../rdbms-schema-query-helper/SKILL.md)
- [`rdbms-object-creation-helper`](../rdbms-object-creation-helper/SKILL.md)
- [`rdbms-dml-helper`](../rdbms-dml-helper/SKILL.md)
- [`rdbms-feature-helper`](../rdbms-feature-helper/SKILL.md)
- [`rdbms-query-repository-helper`](../rdbms-query-repository-helper/SKILL.md)
- [`rdbms-entity-validation`](../rdbms-entity-validation/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for the façade API surface and delegation map.
