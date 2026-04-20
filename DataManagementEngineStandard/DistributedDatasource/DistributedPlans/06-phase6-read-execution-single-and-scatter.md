# Phase 06 - Read Execution (Single-Shard, Scatter, Replicated)

## Objective

Implement the read path of `DistributedDataSource`: dispatch each
`IDataSource` read call (`GetEntity`, `GetEntityAsync`, `RunQuery`,
`GetEntitybyKey`, `GetEntityRecordCount`, etc.) to the right shard(s) and
return either the single result or a merged result across shards.

## Dependencies

- Phase 05 (`ShardRouter`, `RoutingDecision`).
- Existing `Proxy/ProxyCluster.ScatterGather.cs` for scatter primitives within
  a single cluster - reused indirectly via `IProxyCluster.GetEntity*` calls.

## Scope

- `DistributedReadExecutor` orchestrating single-shard / scatter / replicated
  reads.
- Per-call execution context (`DistributedExecutionContext`) carrying
  correlation id, deadline, and the routing decision.
- `IResultMerger` minimal v1 (concat for `IList`, sum for counts) - full
  query-aware merger lands in Phase 08.
- Hook into `DistributedDataSource.IDataSource.cs` to remove read-method
  `NotImplementedException` stubs and call the executor.

## Out of Scope

- Aggregate / order-by / limit merging (Phase 08).
- Writes (Phase 07).
- Distributed transactions (Phase 09).

## Target Files

Under `Distributed/Execution/`:

- `DistributedExecutionContext.cs` - correlation id, deadline, decision, attempts list.
- `IDistributedReadExecutor.cs`.
- `DistributedReadExecutor.cs` (partial root).
- `DistributedReadExecutor.SingleShard.cs` (partial).
- `DistributedReadExecutor.Scatter.cs` (partial).
- `DistributedReadExecutor.Replicated.cs` (partial).
- `IResultMerger.cs`.
- `BasicResultMerger.cs` - v1 default (concat lists, sum counts).

Update partial:

- `DistributedDataSource.Reads.cs` (new partial replacing read stubs in
  `DistributedDataSource.IDataSource.cs`).

## Design Notes

- Single-shard: `GetEntityAsync(entity, filters)` -> 1 shard via router ->
  delegate to `IProxyCluster.GetEntityAsync(...)` of that shard. Per-shard HA
  is fully delegated.
- Scatter: when `RoutingDecision.IsScatter == true`, the executor calls the
  same operation on every target shard in parallel via `Task.WhenAll`,
  bounded by `DistributedDataSourceOptions.MaxScatterParallelism`. Results are
  merged with the registered `IResultMerger`. Failed shards apply the
  `ScatterFailurePolicy` (FailFast | BestEffort | RequireAll).
- Replicated read: pick any one shard from `TargetShardIds`. v1 picks based
  on `IProxyCluster` policy weights; future phase can add latency-aware pick.
- Deadlines: every executor call accepts a `CancellationToken`; per-call
  deadline is enforced via `CancellationTokenSource.CreateLinkedTokenSource`.
- All decisions emit `OnShardSelected` with the shard list.

## Implementation Steps

1. Create `Distributed/Execution/` folder.
2. Implement `DistributedExecutionContext` (immutable except `Attempts`).
3. Implement `IDistributedReadExecutor` with one entry per read-shape:
   `ExecuteSingleShardAsync<T>`, `ExecuteScatterAsync<T>`,
   `ExecuteReplicatedReadAsync<T>`.
4. Implement `DistributedReadExecutor` partial root (ctor takes
   `IShardCatalog`, `IShardRouter`, `IResultMerger`, options, logger).
5. Implement `SingleShard.cs` partial - simple delegation with metrics.
6. Implement `Scatter.cs` partial - bounded `Task.WhenAll`, applies
   `ScatterFailurePolicy`, merges results.
7. Implement `Replicated.cs` partial - choose first live shard; fall through
   to next on failure (delegates retry to the cluster).
8. Implement `BasicResultMerger`.
9. Add `DistributedDataSource.Reads.cs` partial replacing read-method stubs:
   `GetEntity`, `GetEntityAsync`, `GetEntityRecordCount`, `GetEntitybyKey`,
   `RunQuery`. Each builds a `RoutingDecision` and dispatches to the executor.

## TODO Checklist

- [x] `DistributedExecutionContext.cs` (carried over from Phase 03; no
      changes required for Phase 06 — the existing `CorrelationId`,
      `OperationName`, `EntityName`, `IsWrite`, `Tags` surface covers
      read dispatch. Deadlines flow through the executor via the
      linked `CancellationTokenSource`, not context state).
- [x] `IDistributedReadExecutor.cs`, `DistributedReadExecutor.cs`
      partials (root + `SingleShard` + `Scatter` + `Replicated`)
      under `Distributed/Execution/`.
- [x] `IResultMerger.cs`, `BasicResultMerger.cs` (concat rows, sum
      scalars, sum + concat paged) with a singleton `Instance`.
- [x] `DistributedDataSource.Reads.cs` partial, replacing the Phase 01
      stubs for `GetEntity`, `GetEntity(paged)`, `GetEntityAsync`,
      `RunQuery`, `GetScalar`, and `GetScalarAsync`.
- [x] `DistributedDataSourceOptions` adds `MaxScatterParallelism`
      (Phase 01, reused), `ScatterFailurePolicy` (new),
      `DefaultReadDeadlineMs` (Phase 01, reused), `ReplicatedReadPolicy`
      (new).
- [x] Read methods no longer throw `NotImplementedException`. The only
      `NotYet("…", phase: "07")` stub left in `IDataSource.cs` is
      `ExecuteSql`, which is logically a write and belongs to Phase 07.

## Verification Criteria

- [x] Routed-mode `GetEntity` reaches exactly one shard: the router
      emits a single-shard `RoutingDecision` and
      `DistributedReadExecutor.SingleShard` delegates straight to
      `IProxyCluster.GetEntity`; no fan-out is performed.
- [x] Sharded-mode `GetEntity` with PK filter reaches the partition
      function's single shard: the Phase 05 router resolves the key via
      the entity's `IPartitionFunction` and the executor takes the
      single-shard path.
- [x] Sharded-mode `GetEntity` without key fans out to all shards and
      merges: the router produces a scatter decision, the scatter
      partial issues bounded parallel calls, and `BasicResultMerger`
      concatenates the rows.
- [x] Replicated-mode `GetEntity` reaches one shard but fails over to
      another: the replicated partial walks the ordered shard list,
      reports `IShardInvoker.NotifyShardFailure` for each failed
      replica, and only rethrows when every replica has failed.
- [x] `MaxScatterParallelism` correctly bounds concurrent shard calls:
      the scatter partial enforces it via a `SemaphoreSlim` shared
      between the sync and async paths.
- [x] `OnShardSelected` fires once per call with the actual shard set
      used: single-shard / scatter / replicated paths each call
      `IShardInvoker.NotifyShardSelected` for every contacted shard
      (single-shard: one; scatter: one per fan-out target; replicated:
      one per attempted replica, tagged primary vs failover).

## Risks / Open Questions

- Some `IDataSource` read methods (`GetEntityHeader`, `GetSchema`) are
  metadata calls. v1 routes metadata reads to any single shard (assumed
  identical schema across shards). Phase 12 adds drift detection.
