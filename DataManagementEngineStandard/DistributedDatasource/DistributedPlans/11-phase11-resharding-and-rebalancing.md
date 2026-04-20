# Phase 11 - Resharding & Rebalancing

## Objective

Allow the distribution topology to change online: add a shard, remove a shard,
move an entity from one shard to another, or split/merge a sharded entity's
key space. Done with a dual-write window so reads stay consistent during the
transition.

## Dependencies

- Phase 02 (`DistributionPlan` versioning).
- Phase 04 (partition functions; Hash uses virtual slots so adding shards is
  cheap).
- Phase 07 (write executor; dual-write reuses fan-out).
- Phase 09 (transactions; per-key copy uses single-shard tx).

## Scope

- `IReshardingService` exposing operations:
  - `AddShardAsync(ShardSpec)` - add empty shard to catalog and (optionally)
    assign placements.
  - `RemoveShardAsync(string shardId, RemoveShardPlan)` - drain entities off
    the shard.
  - `MoveEntityAsync(string entityName, string fromShard, string toShard)` -
    entity-level move (Routed mode).
  - `RepartitionEntityAsync(string entityName, PartitionFunctionRef
    newFunction, IReadOnlyList<string> newShardIds)` - row-level rebalance.
- `DualWriteWindow` state machine: Off -> Shadow -> DualWrite -> Cutover ->
  Off. Drives the router to write to both old and new placements while data
  is being copied.
- `EntityCopyService` - bulk-copy rows from source to target shard(s) using
  paged reads + writes, idempotent on a `CopyCheckpoint` table.
- `PlanDiff` calculator - compares two `DistributionPlan` versions and
  emits the per-entity migration steps.
- All operations are observable via `OnReshardStarted`, `OnReshardProgress`,
  `OnReshardCompleted`, `OnReshardFailed`.

## Out of Scope

- Auto-resharding triggered by load (manual only in v1).
- Online schema migration (Phase 12).

## Target Files

Under `Distributed/Resharding/`:

- `IReshardingService.cs`.
- `ReshardingService.cs` (partial root).
- `ReshardingService.AddShard.cs` (partial).
- `ReshardingService.RemoveShard.cs` (partial).
- `ReshardingService.MoveEntity.cs` (partial).
- `ReshardingService.Repartition.cs` (partial).
- `DualWriteWindow.cs`.
- `DualWriteState.cs` enum.
- `IEntityCopyService.cs`, `EntityCopyService.cs`.
- `CopyCheckpoint.cs` (record + tiny store via ConfigEditor).
- `PlanDiff.cs`, `PlanDiffEntry.cs`.
- `Events/ReshardProgressEventArgs.cs`.

Update partials:

- `DistributedDataSource.Plan.cs` - apply a new plan via the resharder when
  `PlanDiff` is non-empty.
- `ShardRouter` - consult `DualWriteWindow` so writes during transition
  target both old and new placements.

## Design Notes

- Dual-write window guarantees that during `DualWrite` state every write goes
  to both placements; reads still go to the old placement until `Cutover`.
- Copy phase is throttled (`MaxCopyRowsPerSecond`) and chunked
  (`CopyBatchSize`); resumes from the last checkpoint on restart.
- Hash repartitioning relies on the virtual-slot ring property: only ~1/N
  keys need to move when N shards become N+1.
- `PlanDiff` outputs the smallest set of `MoveEntity` /
  `RepartitionEntity` actions required to go from `planA` to `planB`.
- Resharding is fully cancellable via `CancellationToken`; cancel rolls the
  window back to `Shadow` then `Off` and removes the partial copy.

## Implementation Steps

1. Create `Distributed/Resharding/` folder.
2. Implement `DualWriteWindow` + `DualWriteState`.
3. Implement `CopyCheckpoint` + `IEntityCopyService` + `EntityCopyService`
   (paged GetEntity loop -> InsertEntities on target).
4. Implement `IReshardingService` and `ReshardingService` partial root.
5. Implement `AddShard`, `RemoveShard`, `MoveEntity`, `Repartition`
   partials, each running:
   a. Write `Shadow` plan; let writes go to old placement.
   b. Switch to `DualWrite`; start copy.
   c. Wait until copy finishes + tail-replication catches up.
   d. Switch reads via plan version bump; complete `Cutover`.
   e. Drop `Off` and clean up old placement.
6. Implement `PlanDiff` for plan-to-plan migrations.
7. Wire `ShardRouter` to consult `DualWriteWindow` for active transitions.
8. Wire all reshard events.

## TODO Checklist

- [x] `DualWriteWindow.cs`, `DualWriteState.cs`, `IDualWriteCoordinator.cs`,
      `DualWriteCoordinator.cs` (in-memory thread-safe registry).
- [x] `IEntityCopyService.cs`, `EntityCopyService.cs`, `CopyCheckpoint.cs`,
      plus `IEntityCopyCheckpointStore.cs`, `InMemoryCopyCheckpointStore.cs`,
      `EntityCopyOptions.cs`, `CopyResult.cs`.
- [x] `IReshardingService.cs` and partials (`ReshardingService.cs` root,
      `.AddShard.cs`, `.RemoveShard.cs`, `.MoveEntity.cs`, `.Repartition.cs`)
      plus support records `ShardSpec.cs`, `RemoveShardPlan.cs`,
      `ReshardOutcome.cs`.
- [x] `PlanDiff.cs`, `PlanDiffEntry.cs`, `PlanDiffKind.cs`.
- [x] `Events/ReshardProgressEventArgs.cs` + `RaiseReshardProgress` helper
      and `OnReshardProgress` event on `IDistributedDataSource`.
- [x] `ShardRouter.DualWrite.cs` partial — consults
      `IDualWriteCoordinator.TryGetWindow` and unions window target
      shards onto the write decision while the window is in
      `DualWrite` / `Cutover`.
- [x] `DistributedDataSource.Resharding.cs` partial — exposes
      `Resharder`, `DualWriteCoordinator`, `EntityCopyService`, async
      wrappers for every primitive, and `ApplyDistributionPlanAsync`
      that routes non-empty diffs through the reshard pipeline.

## Verification Criteria

- [x] Build: `dotnet build DataManagementEngine.csproj` completes with
      0 errors across net8.0 / net9.0 / net10.0 target frameworks
      (pre-existing XML-comment / nullable-annotation warnings unchanged).
- [ ] Adding a new shard to a Hash-partitioned entity moves approximately
      1/N keys (within 10% tolerance for 150 virtual slots) — to be
      covered by the Phase 11 integration test harness.
- [ ] During the `DualWrite` state, every successful write reaches both old
      and new placements (verified via stub clusters) — integration test.
- [ ] Cancelling mid-copy restores the prior plan and cleans copied rows
      — integration test.
- [ ] Reads remain correct (no missing rows, no double rows) at every
      transition between dual-write states — integration test.
- [x] Checkpoint resume supported by `InMemoryCopyCheckpointStore`;
      `EntityCopyService.ResumePageFrom` picks up from the last page
      recorded for (`reshardId`, entity, from, to).

## Risks / Open Questions

- Tail-replication catch-up requires the source cluster to support change
  capture or polling deltas. v1 supports the polling approach with a
  `LastCopiedKey` checkpoint and a final lock window where writes are
  briefly serialized through the coordinator. CDC-based tailing is a future
  enhancement.
