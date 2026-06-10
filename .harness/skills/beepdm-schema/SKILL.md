---
name: beepdm-schema
description: Use when doing any schema work in BeepDM — data-source resolution, entity-structure loading, entity existence/creation, cross-datasource preflight, sync-draft production, and schema snapshots / drift comparison. The single source of truth for all schema concerns; MigrationManager, BeepSyncManager, DataImportManager, and ETLEditor all call into it.
---

# beepdm-schema

`ISchemaManager` is the **single-responsibility service for ALL schema concerns** in BeepDM. It does everything that used to be scattered across `DataImportManager.Migration.cs`, `MigrationManager.SchemaInspector`, `BeepSyncManager.RunPreflightAsync`, and ad-hoc resolution helpers in `ETLEditor`.

It does NOT move data, execute DDL plans, or run sync pipelines — those are downstream services. It only **plans, validates, and inspects**.

## When to use this skill

- Resolving a data source by name (with auto-open).
- Loading an entity structure by (data-source, entity) pair.
- Checking whether an entity exists.
- Creating an entity (thin wrapper over `IDataSource.CreateEntityAs`).
- Validating that a destination can accept a source's shape (cross-datasource preflight).
- Producing a `DataSyncSchema` draft describing what a sync would do.
- Capturing a `SchemaSnapshot` of a .NET type or live database.
- Comparing two snapshots to produce a `SchemaDriftReport`.
- Saving / loading baseline snapshots for future drift checks.

## Do NOT use this skill for

- DDL on a single datasource (plan → execute → rollback) → use **beepdm-migration** (owns plan execution; this service owns preflight/draft).
- Data movement between two datasources → use **beepdm-importing**.
- Executing a `DataSyncSchema` (retry, conflict, CDC) → use **beepdm-beepsync**.
- ETL pipelines → use **beepdm-etl**.

## File Locations

`DataManagementEngineStandard/Editor/Schema/`:

- `ISchemaManager.cs` — the contract
- `SchemaManager.cs` — default implementation
- `SchemaRequest.cs` — DTOs (`SchemaRequest`, `SchemaPreflightResult`, `SchemaDraftResult`, `SchemaEntityResult`, `SchemaResolutionResult`)
- `SchemaSnapshot.cs` — `SchemaSnapshot`, `SnapshotField`, `SchemaDriftReport`, `FieldTypeDrift`
- `SchemaComparator.cs` — `SchemaComparator.Compare(baseline, current)`
- `SchemaSnapshotStore.cs` — `ISchemaSnapshotStore` + `FileSchemaSnapshotStore` + `InMemorySchemaSnapshotStore`

DI registration: `DataManagementEngineStandard/Services/BeepServiceExtensions.Schema.cs` → `AddBeepSchemaManager()`

## API surface

```csharp
public interface ISchemaManager
{
    // Resolution & loading
    Task<SchemaResolutionResult> ResolveDataSourceAsync(string name, CancellationToken token = default);
    Task<EntityStructure?>       LoadEntityStructureAsync(string ds, string entity, bool refresh = false, CancellationToken token = default);
    Task<bool>                  EntityExistsAsync(string ds, string entity, CancellationToken token = default);
    Task<EntityStructure?>      TryGetEntityStructureAsync(Type type, CancellationToken token = default);

    // Preflight & draft
    Task<SchemaPreflightResult> RunPreflightAsync(SchemaRequest request, Action<string>? log = null, CancellationToken token = default);
    Task<SchemaDraftResult>     BuildSyncDraftAsync(SchemaRequest request, CancellationToken token = default);

    // Entity creation
    Task<SchemaEntityResult>    CreateEntityAsync(string ds, EntityStructure entity, CancellationToken token = default);

    // Snapshots & drift
    Task<SchemaSnapshot>        CaptureFromTypeAsync(Type type, string ds, CancellationToken token = default);
    Task<SchemaSnapshot>        CaptureFromDataSourceAsync(string ds, string entity, bool refresh = true, CancellationToken token = default);
    Task<SchemaDriftReport>     InspectAsync(Type type, string ds, string entity, CancellationToken token = default);
    Task<SchemaSnapshot>        SaveBaselineAsync(Type type, string ds, string entity, CancellationToken token = default);
    Task<SchemaDriftReport?>    DiffAgainstBaselineAsync(Type type, string ds, string entity, CancellationToken token = default);
    Task<SchemaSnapshot>        SaveDatabaseBaselineAsync(string ds, string entity, CancellationToken token = default);
    Task<Dictionary<string, SchemaDriftReport>> InspectManyAsync(IEnumerable<Type> types, string ds, CancellationToken token = default);
    Task<Dictionary<string, SchemaDriftReport>> InspectManyAsync(IEnumerable<Type> types, IDataSource ds, CancellationToken token = default);
}
```

## Typical Workflow

```csharp
var schema = new SchemaManager(dmeEditor);

// 1. Preflight
var pre = await schema.RunPreflightAsync(new SchemaRequest {
    SourceDataSourceName         = "Northwind",
    SourceEntityName             = "Customers",
    DestinationDataSourceName    = "Warehouse",
    DestinationEntityName        = "DimCustomer",
    AddMissingColumns            = false,   // strict
    CreateDestinationIfNotExists = false
});
if (pre.Status.Flag == Errors.Failed) return pre.Status;

// 2. Build a sync draft
var draft = await schema.BuildSyncDraftAsync(...);

// 3. Hand to BeepSyncManager for execution
syncManager.AddSyncSchema(draft.Draft);
await syncManager.SyncDataAsync(draft.Draft);
```

## How this skill works with the rest of the data-management layer

This service is the **single source of truth for schema concerns**. Other services call into it instead of doing their own resolution / structure loading / drift comparison.

| Direction | Layer | What flows |
|---|---|---|
| ← **beepdm-migration** | `MigrationManager` | `SchemaSetupStep` calls `ISchemaManager.InspectManyAsync` for schema drift; `MigrationManager` keeps its plan-execute internals (it operates on its configured datasource instance, not by name). |
| ← **beepdm-beepsync** | `BeepSyncManager` | `BeepSyncManager.SyncDataAsync` now calls `ISchemaManager.RunPreflightAsync` *before* the import starts, with `AddMissingColumns=false` (strict). Catches "destination doesn't have the column" before the import. |
| ← **beepdm-importing** | `DataImportManager` | `IDataImportManager.RunMigrationPreflightAsync` and `BuildSyncDraftAsync` are **back-compat shims** that delegate here. New code should call `ISchemaManager` directly. |
| ← **beepdm-etl** | `ETLEditor` | `ETLEditor.TryRunImportingPreflightAsync` calls `ISchemaManager.RunPreflightAsync` directly. No more spinning up a `DataImportManager` just for preflight. |
| → **beepdm-configuration** | `ConfigEditor` | Reads connections through `IDMEEditor`. Does not persist anything itself. |

## Design Rules

- **One responsibility, broad surface**: any schema concern goes here — no exceptions. If you find yourself adding `GetDataSource(name)` + `OpenDataSource(name)` + `GetEntityStructure(name)` in a service, route through `ISchemaManager` instead.
- The DTOs are datasource-agnostic. Do not specialise them for SQL Server, Mongo, etc. — that belongs in the per-dialect `IDataSourceHelper`.
- Return `IErrorsInfo` (Flag + Message) for expected failures, not exceptions.
- The service is **singleton-safe** — it holds only a reference to `IDMEEditor` and a snapshot store.
- Back-compat shims on `IDataImportManager` stay for now; new code should call `ISchemaManager` directly.

## Cross-references

- See **beepdm-migration** for the DDL-on-one-datasource counterpart.
- See **beepdm-beepsync** for the runtime executor of the draft this service produces.
- See **beepdm-importing** for the data-movement service that delegates here.
- See **beepdm-etl** for the pipeline that uses this service for preflight.

## Related shared helpers

- **`Common/Retry/IRetryPipeline`** — the common retry-loop primitive used by `BeepSyncManager` (whole-run), `MigrationManager` (per-step inside the plan loop), and `WebAPIErrorHelper` (circuit-breaker wrapper). See `.harness/skills/beepdm-retry/` for the design, the worked example, and the boundary cases where the pipeline is intentionally NOT used. The schema service itself does not use it (it's a preflight/validation surface, not a long-running operation), but its callers may compose it.
