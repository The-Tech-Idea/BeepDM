# Migration Quick Reference

## Ensure Database Created
```csharp
var migration = new MigrationManager(editor, dataSource);
var result = migration.EnsureDatabaseCreated("MyApp.Entities", null, true, null);
```

## Apply Migrations
```csharp
var result = migration.ApplyMigrations("MyApp.Entities", null, true, true, null);
```

## Explicit Types
```csharp
var types = new[] { typeof(Customer), typeof(Order) };
var result = migration.ApplyMigrationsForTypes(types, true, true, null);
```

## Summary
```csharp
var summary = migration.GetMigrationSummary("MyApp.Entities");
if (summary.HasPendingMigrations)
{
    Console.WriteLine(summary.TotalPendingMigrations);
}
```
