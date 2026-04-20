# Phase 12 - Schema Management & DDL Broadcast

## Objective

Coordinate schema across shards. DDL must be broadcast to every shard that
hosts the target entity, drift must be detectable, and entity-creation must
respect the plan (Sharded entities created on every member shard,
Replicated/Broadcast on all listed shards, Routed on exactly one).

## Dependencies

- Phase 02 (`EntityPlacement`).
- Phase 07 (write executor for DDL).
- Existing `BeepDM` `MigrationManager` patterns (skill: migration).

## Scope

- `IDistributedSchemaService` exposing:
  - `CreateEntityAsync(EntityStructure structure)` - per placement mode.
  - `AlterEntityAsync(string entity, AlterEntityChange change)` - broadcast
    to every shard hosting the entity.
  - `DropEntityAsync(string entity)` - drop on every owning shard.
  - `DetectSchemaDriftAsync()` - compare per-shard `EntityStructure` and
    return a drift report.
- DDL fan-out reuses the write executor's broadcast/fan-out path.
- Identity / sequence safety: warn (and optionally error) when creating a
  Sharded entity with a DB-generated identity column.
- `IDistributedSequenceProvider` (optional) for client-side ID generation
  (Snowflake or HiLo) for sharded inserts.

## Out of Scope

- Online column type changes that require data rewrite (deferred).
- Full migration scripts; we delegate to existing `MigrationManager` per
  shard and orchestrate fan-out only.

## Target Files

Under `Distributed/Schema/`:

- `IDistributedSchemaService.cs`.
- `DistributedSchemaService.cs` (partial root).
- `DistributedSchemaService.Create.cs` (partial).
- `DistributedSchemaService.Alter.cs` (partial).
- `DistributedSchemaService.Drop.cs` (partial).
- `DistributedSchemaService.DriftDetection.cs` (partial).
- `SchemaDriftReport.cs`, `SchemaDriftEntry.cs`.
- `AlterEntityChange.cs` (record union: AddColumn, DropColumn, AlterColumn,
  AddIndex, DropIndex).
- `IDistributedSequenceProvider.cs` + `SnowflakeSequenceProvider.cs` +
  `HiLoSequenceProvider.cs`.
- `IdentityColumnPolicy.cs` enum: WarnOnly | RejectShardedIdentity.

Update partials:

- `DistributedDataSource.Schema.cs` (new partial) - implements `CreateEntity`,
  `AlterEntity`, `DropEntity` from `IDataSource`.
- `DistributedDataSource.Writes.cs` - sharded inserts can call
  `IDistributedSequenceProvider` when configured.

## Design Notes

- `CreateEntity` for Sharded mode iterates the entity's `TargetShardIds`
  (which is the catalog snapshot for newly-created shards) and runs the
  per-shard `MigrationManager` create.
- Drift detection samples each shard's `EntityStructure` (column names,
  types, nullability, indexes) and reports differences with a stable format
  suitable for CI gating.
- Sequence provider is opt-in. Snowflake uses (epoch | nodeId | counter)
  and is collision-free across shards as long as nodeIds are unique.
- DDL operations always update the audit log (Phase 13).

## Implementation Steps

1. Create `Distributed/Schema/` folder.
2. Implement `IDistributedSchemaService` and partials for each operation.
3. Implement `SchemaDriftReport` + `DriftDetection.cs` partial.
4. Implement `AlterEntityChange` discriminated record.
5. Implement `IDistributedSequenceProvider` and the two reference providers.
6. Add `IdentityColumnPolicy` to options; enforce on `CreateEntity` for
   Sharded entities.
7. Add `DistributedDataSource.Schema.cs` partial; remove DDL stubs.
8. Wire DDL audit events.

## TODO Checklist

- [x] `IDistributedSchemaService.cs` and partials
      (`DistributedSchemaService.cs` root + `Create` / `Alter` / `Drop` /
      `DriftDetection` partials).
- [x] `SchemaDriftReport.cs`, `SchemaDriftEntry.cs`, `SchemaDriftKind.cs`.
- [x] `AlterEntityChange.cs` + `AlterEntityChangeKind.cs`.
- [x] `IDistributedSequenceProvider.cs`,
      `SnowflakeSequenceProvider.cs`, `HiLoSequenceProvider.cs`.
- [x] `IdentityColumnPolicy.cs` and option wiring
      (`DistributedDataSourceOptions.IdentityColumnPolicy` +
      `DistributedDataSourceOptions.SequenceProvider`).
- [x] `DistributedDataSource.Schema.cs` partial (DDL `IDataSource`
      members + typed async wrappers + lazy `SchemaService` accessor).
- [x] DDL paths emit audit events (placement violations raised through
      `RaisePlacementViolation`; per-shard failures surfaced via
      `PassEvent`; full outcome captured in
      `SchemaOperationOutcome`).
- [x] `SchemaOperationOutcome.cs` terminal-outcome record for audit.

## Verification Criteria

- [x] Clean build of `DataManagementEngineStandard\DataManagementEngine.csproj`
      across net8.0 / net9.0 / net10.0 with 0 errors (pre-existing
      CS1591/CA1416/CA2022 warnings in unrelated files are unchanged).
- [ ] `CreateEntity` for a Sharded entity creates the table on every
      currently-listed shard (covered by integration tests).
- [ ] `AlterEntity` adding a column reaches every owner shard;
      per-shard failures surface in
      `SchemaOperationOutcome.Errors` and the follow-up
      `DetectSchemaDriftAsync` report (integration tests).
- [ ] `DetectSchemaDriftAsync` reports differences in column types,
      nullability, identity, and primary-key membership across mixed
      shards (integration tests).
- [x] `Sharded` + identity column under `RejectShardedIdentity` policy
      returns a `SchemaOperationOutcome` with a populated
      `TerminalError` and remediation message.

## Risks / Open Questions

- Atomic DDL across shards is impossible without 2PC for DDL (most engines
  do not support it). Documented as "best-effort with drift detection";
  Phase 11 patterns (dual-write) do not apply to schema. Repair path is
  manual via the drift report.
