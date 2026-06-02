# MigrationManager: ORM / EF Core Interop

This example shows how to bridge an ORM-shaped model (the canonical example is
**Entity Framework Core**) into the existing migration plan / readiness / apply
pipeline.

> BeepDM does not take a hard dependency on any ORM package. Instead, callers
> populate a `MigrationModel` POCO at the call site and pass it to the engine.
> A future companion NuGet package (`TheTechIdea.Beep.DataManagementEngine.EFCore`)
> can ship a reusable `DbContext` → `MigrationModel` adapter without forcing
> every BeepDM consumer to take a hard EF Core dependency.

## Two entry points

| API | When to use | Source |
|---|---|---|
| `BuildMigrationPlanForModel(MigrationModel model, …)` | You already have a `MigrationModel` POCO (e.g. produced from EF Core, NHibernate, a hand-rolled dictionary, or a JSON snapshot). | ORM-agnostic. |
| `BuildMigrationPlanForTypesAnnotated(IEnumerable<Type>, …)` | You have live CLR `Type` objects with data annotations (`[Table]`, `[Column]`, `[Key]`, `[Required]`, `[MaxLength]`, …). | Pure reflection, no package dep. |
| `BuildMigrationPlanForModel` falls through to the annotated-types path when every CLR type name in the model resolves to a live `Type` in the current AppDomain. |

## Path A — Data-annotation path (no ORM package)

If your entity classes are decorated with data annotations, you can use the
engine without any ORM at all:

```csharp
using TheTechIdea.Beep.Editor.Migration;

var migration = new MigrationManager(editor, dataSource);

var entityTypes = new[] { typeof(Customer), typeof(Order), typeof(OrderItem) };
var plan = migration.BuildMigrationPlanForTypesAnnotated(entityTypes, detectRelationships: true);

Console.WriteLine($"Plan {plan.PlanId}: {plan.Operations.Count} operations, " +
                  $"policy={plan.PolicyEvaluation?.Decision}");
```

This is the same code path used by `BuildMigrationPlanForTypes` — the
"annotated" name just makes the data-annotation contract explicit.

## Path B — EF Core IModel → MigrationModel (recommended for EF Core users)

For EF Core you populate a `MigrationModel` from `dbContext.Model` at the call
site. The following helper shows the typical mapping. This file lives in
**your** project (the BeepDM engine has no EF Core reference):

```csharp
// File: MyApp.EfCoreMigrationAdapter.cs  (lives in your app, not in BeepDM)
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TheTechIdea.Beep.Editor.Migration;

public static class EfCoreMigrationAdapter
{
    public static MigrationModel ToMigrationModel(this DbContext dbContext)
    {
        var model = new MigrationModel
        {
            Source       = "EntityFrameworkCore",
            SourceVersion = typeof(DbContext).Assembly.GetName().Version?.ToString() ?? "",
            SourceId     = dbContext.GetType().FullName ?? dbContext.GetType().Name
        };

        foreach (var entityType in dbContext.Model.GetEntityTypes())
        {
            var entity = new MigrationModelEntity
            {
                ClrTypeFullName = entityType.ClrType.FullName ?? entityType.ClrType.Name,
                TableName       = entityType.GetTableName() ?? entityType.ClrType.Name,
                Schema          = entityType.GetSchema() ?? string.Empty,
                IsKeyless       = entityType.IsKeyless,
                IsAbstract      = entityType.ClrType.IsAbstract
            };

            // Properties
            foreach (var property in entityType.GetProperties())
            {
                entity.Properties.Add(new MigrationModelProperty
                {
                    PropertyName     = property.Name,
                    ColumnName       = property.GetColumnName() ?? property.Name,
                    FieldType        = property.ClrType.FullName ?? property.ClrType.Name,
                    ColumnType       = property.GetColumnType() ?? string.Empty,
                    IsNullable       = property.IsNullable,
                    MaxLength        = property.GetMaxLength(),
                    Precision        = property.GetPrecision(),
                    Scale            = property.GetScale(),
                    IsPrimaryKey     = property.IsPrimaryKey(),
                    IsIdentity       = property.ValueGenerated == ValueGenerated.OnAdd
                                       && property.ClrType == typeof(int)
                                       || property.ClrType == typeof(long),
                    IsRowVersion     = property.IsConcurrencyToken
                                       && (property.ClrType == typeof(byte[])
                                           || property.ClrType == typeof(Guid)),
                    IsUnique         = property.IsUnique(),
                    IsIndexed        = entityType.GetIndexes()
                                            .Any(i => i.Properties.Select(p => p.Name)
                                            .Contains(property.Name)),
                    DefaultValueSql  = property.GetDefaultValueSql() ?? string.Empty,
                    ComputedColumnSql = property.GetComputedColumnSql() ?? string.Empty
                });
            }

            // Indexes
            foreach (var index in entityType.GetIndexes())
            {
                entity.Indexes.Add(new MigrationModelIndex
                {
                    Name     = index.GetDatabaseName() ?? string.Empty,
                    Columns  = index.Properties.Select(p => p.Name).ToList(),
                    IsUnique = index.IsUnique
                });
            }

            // Foreign keys
            foreach (var fk in entityType.GetForeignKeys())
            {
                entity.ForeignKeys.Add(new MigrationModelForeignKey
                {
                    ConstraintName    = fk.GetConstraintName() ?? string.Empty,
                    Columns           = fk.Properties.Select(p => p.Name).ToList(),
                    PrincipalTable    = fk.PrincipalEntityType.GetTableName() ?? string.Empty,
                    PrincipalSchema   = fk.PrincipalEntityType.GetSchema() ?? string.Empty,
                    PrincipalColumns  = fk.PrincipalKey.Properties.Select(p => p.Name).ToList(),
                    OnDeleteBehavior  = fk.DeleteBehavior.ToString()
                });
            }

            model.Entities[entity.ClrTypeFullName] = entity;
        }

        return model;
    }
}
```

## Path C — Calling MigrationManager

Once you have a `MigrationModel`, the rest is engine-agnostic:

```csharp
using TheTechIdea.Beep.Editor.Migration;
using MyApp; // for the adapter above

var migration = new MigrationManager(editor, dataSource);

using var dbContext = new MyAppDbContext();
var model = dbContext.ToMigrationModel();

// 1) Plan
var plan = migration.BuildMigrationPlanForModel(model);
Console.WriteLine($"Plan {plan.PlanId}: {plan.Operations.Count} ops, " +
                  $"policy={plan.PolicyEvaluation?.Decision}");

// 2) Readiness
var readiness = migration.GetMigrationReadinessForModel(model);
if (readiness.HasBlockingIssues)
{
    foreach (var issue in readiness.Issues.Where(i => i.Severity == MigrationIssueSeverity.Error))
        Console.Error.WriteLine($"[{issue.Code}] {issue.Message}");
    return;
}

// 3) Dry-run
var dryRun = migration.GenerateDryRunReport(plan);
foreach (var op in dryRun.Operations)
    Console.WriteLine($"{op.EntityName} :: {op.Kind} :: {string.Join(" | ", op.DdlPreview)}");

// 4) Apply
var result = migration.ApplyMigrationsForModel(model, addMissingColumns: true);
if (result.Flag == Errors.Ok)
    Console.WriteLine("Schema is in sync with the EF Core model.");
else
    Console.Error.WriteLine($"Migration failed: {result.Message}");
```

## What the engine returns

`GetMigrationModelEvidence()` returns a per-entity record of what was reflected
from the supplied `MigrationModel`, including the model fingerprint hash:

```csharp
var evidence = migration.GetMigrationModelEvidence();
Console.WriteLine($"Source       = {evidence.Source} {evidence.SourceVersion}");
Console.WriteLine($"Entities     = {evidence.EntityTypeCount}");
Console.WriteLine($"Foreign keys = {evidence.ForeignKeyCount}");
Console.WriteLine($"Indexes      = {evidence.IndexCount}");
Console.WriteLine($"ModelHash    = {evidence.ModelHash}");

foreach (var record in evidence.Entities.Values)
{
    Console.WriteLine($"- {record.ClrTypeFullName} -> {record.TableName} " +
                      $"(PK=[{string.Join(",", record.PrimaryKey)}], " +
                      $"FKs={record.ForeignKeys.Count}, " +
                      $"hash={record.RecordHash})");
    foreach (var w in record.Warnings) Console.WriteLine($"   ! {w}");
}
```

## Operational notes

* **No live types available** — If the CLR types referenced by the `MigrationModel`
  cannot be resolved in the current `AppDomain`, the engine falls back to a
  streamlined EntityStructure-driven plan. It still produces a valid
  `MigrationPlanArtifact` with operations, readiness, and downstream reports,
  but adds a `model-clrtype-unresolved` warning to the plan's readiness issues.
  This is the path taken when the `MigrationModel` was loaded from a snapshot
  file (e.g. JSON) without the originating assemblies being present.

* **Source provenance** — Every operation that originated from the supplied
  model is tagged with `EntityMigrationSource.DiscoveryEFCoreModel` in its
  `EntityDecisionRecord.Source`. The plan's `UsesDiscovery` flag is `true`
  for the model-driven path.

* **Repeatability** — `GetMigrationModelEvidence().ModelHash` is a stable
  SHA-256 of the model's table+schema+column shape. Use it to detect drift
  between successive plans in CI or to gate releases ("only deploy if model
  hash matches the approved plan").

* **Adding a new ORM** — To plug a non-EF-Core ORM in, write a similar adapter
  that translates your ORM's metadata into a `MigrationModel`. The engine
  never sees the ORM package; only the POCO.

## Optional: applying foreign keys and indexes from the model

By default the engine treats **schema** as in scope and **relational artifacts**
(foreign keys, indexes) as out of scope. EF Core users typically let EF Core
own those constructs (via `OnModelCreating` + migration bundle) and only want
BeepDM to bring the columns in line.

When you want BeepDM to *also* drive the FK and index DDL, opt in explicitly:

```csharp
// 1. Plan still non-destructive — no DDL emitted
var plan = migration.BuildMigrationPlanForModel(
    model,
    detectRelationships: true,
    applyForeignKeys: true,   // record intent on the plan
    applyIndexes: true);

// 2. Apply: topologically ordered by FK dependencies, then FKs and
//    indexes are created after each entity's columns are in sync
var result = migration.ApplyMigrationsForModel(
    model,
    detectRelationships: true,
    addMissingColumns: true,
    progress: progress,
    applyForeignKeys: true,
    applyIndexes: true);
```

Behavior:

* The flags default to `false` to preserve the prior "schema only" contract.
* `ApplyMigrationsForModel` runs a topological sort over the entity
  structures so principal tables are created before any dependent table that
  references them via FK. Cycles fall back to source order and surface a
  `Foreign-key cycle detected` warning in the aggregate.
* The `OnDelete` / `OnUpdate` behaviors captured from EF Core (e.g. via the
  `OnDelete()` Fluent API) are translated into the appropriate `ON DELETE` /
  `ON UPDATE` SQL action (`CASCADE`, `RESTRICT`, `SET NULL`, `NO ACTION`).
* `EnsureEntity(EntityStructure, ...)` and `EnsureEntity(Type, ...)` also
  accept the same `applyForeignKeys` / `applyIndexes` flags. They are
  off by default.
* `GetMigrationReadinessForModel` and `GetMigrationSummaryForModel` accept the
  flags and stamp an `Info`-level entry on the report / a diagnostic on the
  summary so consumers can audit the intent.
* `BuildMigrationPlan` and `BuildMigrationPlanForTypes` (and the annotated
  `BuildMigrationPlanForTypesAnnotated`) also accept the flags; the plan
  emits one `AddForeignKey` and one `CreateIndex` plan op per declared
  relation / index so the dry-run, policy, and preflight reports preview
  them alongside the entity ops. Execution honors the intent when
  `ExecuteMigrationPlan` is called against the plan.
* `MigrationPlanOperationKind` adds four new values:
  `AddForeignKey`, `DropForeignKey`, `CreateIndex`, `DropIndex`. The execution
  orchestrator handles the *apply* path; the dry-run report emits the
  previews; rollback / compensation now classify them as `ReversibleDdl`;
  policy classifies `DropForeignKey` / `DropIndex` as destructive.
* `MigrationPlanOperation.TargetName` carries the constraint / index name
  through to the `MigrationExecutionStep.TargetName`, so the executor runs
  the right drop DDL without re-deriving the name from the (possibly
  absent) desired structure.
* `MigrationManager.DropIndex(entityName, indexName)` is the
  `DropIndex` counterpart to `CreateIndex`, mirroring the
  `AddForeignKey` / `DropForeignKey` pair. Backed by
  `RdbmsHelper.GenerateDropIndexSql` for the universal RDBMS helper and a
  reflection-based fallback for custom helpers.
* `MigrationSummary.Diagnostics` carries the recorded intent for CI gates.
* The file-system manifest path also accepts the flags:
  `ApplyMigrationsFromManifest(path, addMissingColumns, progress,
  applyForeignKeys, applyIndexes)` and the new
  `EnsureDatabaseCreatedFromManifest(path, progress, applyForeignKeys,
  applyIndexes)` counterpart.

When you do not opt in, the engine does not touch FKs or indexes — they
remain the responsibility of EF Core's migration bundle, your hand-rolled
DDL, or your ORM of choice. The "optional" wording reflects that contract.

## Where to read more

* `IMigrationManager.cs` — interface contracts and `MigrationModel` POCO definition
* `MigrationManager.ModelInterop.cs` — engine implementation
* `Editor/Migration/.plans/` — roadmap and rationale
* `Editor/Migration/Examples/01-plan-and-policy.md` — base plan flow
* `Editor/Migration/Examples/05-ci-and-artifacts.md` — CI integration
