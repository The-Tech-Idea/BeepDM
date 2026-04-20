# Phase 07 - Write Execution (Sharded, Replicated, Broadcast)

## Objective

Implement the write path: dispatch `InsertEntity`, `UpdateEntity`,
`DeleteEntity`, `ExecuteSql`, and bulk variants to the right shard(s),
respecting the entity's distribution mode and quorum policy.

## Dependencies

- Phase 05 (router decisions for writes).
- Phase 06 (executor patterns).
- Existing `Proxy/ProxyDataSource.FanOut.cs` patterns are reused conceptually,
  but cross-shard fan-out is implemented at the distributed layer (per-shard
  fan-out remains the cluster's job).

## Scope

- `IDistributedWriteExecutor` + `DistributedWriteExecutor` partial class.
- Write paths:
  - Sharded write: route to exactly one shard (the partition function pick).
  - Sharded scatter write: when `AllowScatterWrite = true` and key is missing,
    raise an error by default, or fan out to every shard for "delete by
    filter" use cases (gated behind `AllowScatterWrite` per call).
  - Replicated write: fan out to every `TargetShardId`, require quorum.
  - Broadcast write: fan out to every shard in the catalog, require quorum =
    catalog size by default.
- Per-shard execution still flows through `IProxyCluster`, so per-shard HA
  (primary/replica fan-out, retry, circuit) is unchanged.
- `WriteOutcome<T>` record carries: per-shard success/failure list, quorum
  satisfied, first-error.

## Out of Scope

- Two-phase commit / saga (Phase 09).
- Online resharding writes (Phase 11 dual-write window).

## Target Files

Under `Distributed/Execution/`:

- `IDistributedWriteExecutor.cs`.
- `DistributedWriteExecutor.cs` (partial root).
- `DistributedWriteExecutor.SingleShard.cs` (partial).
- `DistributedWriteExecutor.FanOut.cs` (partial - replicated + broadcast).
- `DistributedWriteExecutor.ScatterDelete.cs` (partial - delete-by-filter).
- `WriteOutcome.cs`.
- `WriteFanOutResult.cs`.
- `QuorumPolicy.cs` enum: All | Majority | At-Least-N.

Update partial:

- `DistributedDataSource.Writes.cs` (new partial replacing write stubs).

## Design Notes

- Replicated writes fan out concurrently using `Task.WhenAll` bounded by
  `MaxFanOutParallelism`. Quorum behavior:
  - `All`     -> every target must succeed.
  - `Majority`-> > N/2 must succeed.
  - `At-Least-N` -> use `WriteQuorum` from `EntityPlacement`.
- Failure of non-quorum shards is logged and emitted as
  `OnPartialReplicationFailure` so the operator can repair.
- Broadcast writes share the replicated path with `TargetShardIds = catalog
  snapshot at call time`.
- Sharded scatter writes (delete-by-filter when key is missing) require an
  explicit per-call opt-in `DistributedWriteOptions { AllowScatterWrite = true }`
  to prevent accidental fan-out of mass deletes.
- `ExecuteSql` is special: see Phase 12 for DDL broadcast. Phase 07 routes
  parameterized DML SQL through the same write rules using the supplied
  entity-name hint; if no hint, error in v1.

## Implementation Steps

1. Add `DistributedWriteOptions.cs` record (per-call hints).
2. Implement `IDistributedWriteExecutor` and `DistributedWriteExecutor`
   partial root.
3. Implement `SingleShard.cs` partial - delegate to one cluster.
4. Implement `FanOut.cs` partial - parallel fan-out + quorum + outcome.
5. Implement `ScatterDelete.cs` partial - opt-in scatter for delete-by-filter.
6. Implement `WriteOutcome` and `WriteFanOutResult`.
7. Add `DistributedDataSource.Writes.cs` partial implementing
   `InsertEntity[Async]`, `UpdateEntity[Async]`, `DeleteEntity[Async]`,
   `UpdateEntities`, `InsertEntities`, `BeginTransaction` (v1 just delegates
   to a single shard if entity is single-shard; multi-shard tx error - Phase 09).
8. Wire `OnPartialReplicationFailure` event.

## TODO Checklist

- [x] `DistributedWriteOptions.cs` — per-call hints (AllowScatterWrite,
      QuorumOverride, AtLeastN, CorrelationId, EntityNameHint).
- [x] `WriteOutcome.cs`, `WriteFanOutResult.cs`, `QuorumPolicy.cs`
      (policy = All | Majority | AtLeastN).
- [x] `IDistributedWriteExecutor.cs` + partials
      (`DistributedWriteExecutor.cs` root, `.SingleShard.cs`,
      `.FanOut.cs`, `.ScatterDelete.cs`).
- [x] `DistributedDataSource.Writes.cs` partial implementing
      `InsertEntity`, `UpdateEntity`, `UpdateEntities`, `DeleteEntity`,
      and `ExecuteSql` (broadcast-under-All until Phase 08 SQL parser).
- [x] `OnPartialReplicationFailure` event added on
      `IDistributedDataSource` + raised via
      `DistributedDataSource.Events.cs`
      (`PartialReplicationFailureEventArgs`).
- [x] `MaxFanOutParallelism` reused from
      `DistributedDataSourceOptions` (already present from Phase 06);
      `AllowScatterWrite` similarly already present and consumed.

## Verification Criteria

- [x] Sharded insert with valid PK reaches exactly one shard —
      `DispatchWrite` -> `IsSingleShard` picks
      `ExecuteSingleShard` which short-circuits to one leg and always
      requires ack=1 / policy=All.
- [x] Sharded insert with missing PK is rejected — the Phase 05
      router flags `IsScatter=true` for missing keys under
      `Mode=Sharded`, and `ExecuteScatter` throws
      `ShardRoutingException("ScatterWriteRejected")` unless the
      caller sets `AllowScatterWrite` on options.
- [x] Replicated update with `Majority` quorum — `ResolveQuorum`
      maps `QuorumOverride=Majority` to `required = (N/2)+1`; the
      executor reports `QuorumSatisfied=true` when `SuccessCount >=
      required` so 2/3 succeeds and 1/3 fails.
- [x] Broadcast insert reaches every shard currently in the catalog —
      `Mode=Broadcast` placements resolve to all live shards via the
      router; `DispatchWrite` picks `ExecuteFanOut` which walks
      every id in `decision.ShardIds`.
- [x] Scatter delete requires explicit `AllowScatterWrite=true` —
      `ExecuteScatter` throws when neither the per-call
      `DistributedWriteOptions.AllowScatterWrite` nor the datasource
      `DistributedDataSourceOptions.AllowScatterWrite` is set.
- [x] Per-shard retry/circuit still applies — the executor delegates
      the per-shard call to `IProxyCluster.InsertEntity / UpdateEntity
      / DeleteEntity / ExecuteSql`, which continues to run the
      existing HA policies per shard (unchanged from Phase 06).
- [x] Partial-replication failures raise
      `OnPartialReplicationFailure` — `OutcomeToErrors` inspects
      `WriteOutcome.IsPartial` and raises the event via
      `RaisePartialReplicationFailure`; handler exceptions are
      swallowed into the standard pass-event logging path.

## Risks / Open Questions

- Identity / sequence collisions in sharded writes: a user inserting into a
  sharded table whose PK is a DB-generated identity will produce duplicate
  IDs across shards. Documented as a v1 constraint - require client-supplied
  PK (Guid, ULID, sequence-from-coordinator) for sharded entities. Phase 12
  schema management warns on identity columns in sharded entities.
