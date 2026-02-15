---
name: migration
description: Guidance for MigrationManager usage, entity discovery, and schema migration workflows in BeepDM.
---

# Migration Guide

Use this skill when creating or applying schema migrations using MigrationManager.

## Core Capabilities
- Discover entity types across assemblies
- Ensure database created from entity types
- Apply migrations for missing entities or columns
- Entity-level operations (create, drop, alter)

## Workflow
1. Create `MigrationManager(editor, datasource)`.
2. Discover entity types (or pass explicit types).
3. Get migration summary.
4. Apply migrations or ensure database created.

## Validation
- Ensure `MigrateDataSource` is set.
- Check `IErrorsInfo.Flag` for each operation.
- Use `GetMigrationSummary` to inspect pending migrations.

## Pitfalls
- Pre-mapping types breaks CreateEntityAs; pass .NET types.
- Missing assemblies cause discovery to return zero entities.
- File-based datasources do not support DDL operations.

## File Locations
- DataManagementEngineStandard/Editor/Migration/IMigrationManager.cs
- DataManagementEngineStandard/Editor/Migration/MigrationManager.cs
- DataManagementEngineStandard/Editor/Migration/README.md

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
