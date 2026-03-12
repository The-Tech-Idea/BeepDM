---
name: migration
description: Guidance for MigrationManager usage, entity discovery, and schema migration workflows in BeepDM. Use when creating datasource-agnostic schema creation or upgrade flows based on Entity types, EntityStructure metadata, and IDataSource capabilities.
---

# Migration Guide

Use this skill when creating or applying schema migrations with `MigrationManager`.

## Use this skill when
- Creating databases from Entity/POCO types
- Adding missing tables or columns through datasource-agnostic migration flows
- Registering assemblies for entity discovery
- Troubleshooting why entity discovery or migration summary is incomplete

## Do not use this skill when
- The task is only about CRUD or transactional app logic. Use [`unitofwork`](../unitofwork/SKILL.md) or [`idatasource`](../idatasource/SKILL.md).
- The task is only about connection definition or config persistence. Use [`connection`](../connection/SKILL.md) and [`configeditor`](../configeditor/SKILL.md).

## Core Capabilities
- Discover entity types across assemblies
- Register assemblies explicitly for discovery
- Ensure a database/schema exists from Entity types or `EntityStructure`
- Apply migrations for missing entities or columns
- Track migration history and results

## Design Rules From Source
- Pass .NET type names through `EntityStructure`; do not pre-map to provider-native types.
- Prefer `IDataSource.CreateEntityAs(entity)` for datasource-agnostic entity creation.
- Use `IDataSourceHelper` for validation and column-level DDL when supported.
- Treat helper support and datasource capabilities as conditional, not guaranteed.

## Typical Workflow
1. Create `MigrationManager(editor, dataSource)` and ensure `MigrateDataSource` is set.
2. Register extra assemblies if entity types live outside normal discovery paths.
3. Discover entity types or pass explicit types.
4. Call `GetMigrationSummary` to inspect pending changes.
5. Call `EnsureDatabaseCreated`, `ApplyMigrations`, `EnsureEntity`, or explicit type overloads as appropriate.
6. Check each returned `IErrorsInfo.Flag` before continuing.

## Validation and Safety
- Ensure `MigrateDataSource` is not null before migration operations.
- Use `GetMigrationSummary` before applying changes when you need a preview.
- Keep `EntityStructure.Fieldtype` values as .NET type names; let the datasource map them.
- Expect column-level DDL to vary by provider and helper capability.

## Pitfalls
- Pre-mapping types breaks `CreateEntityAs` and corrupts datasource-agnostic behavior.
- Missing or unregistered assemblies cause discovery to return zero entities.
- File-based or schema-limited datasources may not support full DDL operations.
- Assuming every provider supports add/alter/drop column operations creates false confidence.

## File Locations
- `DataManagementEngineStandard/Editor/Migration/IMigrationManager.cs`
- `DataManagementEngineStandard/Editor/Migration/MigrationManager.cs`
- `DataManagementModelsStandard/Editor/IDataSourceHelper.cs`

## Example
```csharp
var migration = new MigrationManager(editor, dataSource);

var summary = migration.GetMigrationSummary("MyApp.Entities", null, detectRelationships: true);
if (summary.HasPendingMigrations)
{
    var result = migration.ApplyMigrations("MyApp.Entities", null, true, true, null);
    if (result.Flag != Errors.Ok)
    {
        throw new InvalidOperationException(result.Message);
    }
}
```

## Task-Specific Examples

### Explicit Types (No Discovery)
```csharp
var types = new[] { typeof(Customer), typeof(Order) };
var result = migration.EnsureDatabaseCreatedForTypes(types, true, null);
```

### Register Assemblies For Discovery
```csharp
migration.RegisterAssembly(typeof(Customer).Assembly);
var types = migration.DiscoverEntityTypes("MyApp.Entities");
```

## Related Skills
- [`beepdm`](../beepdm/SKILL.md)
- [`idatasource`](../idatasource/SKILL.md)
- [`configeditor`](../configeditor/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for quick migration calls, summaries, and explicit-type patterns.
