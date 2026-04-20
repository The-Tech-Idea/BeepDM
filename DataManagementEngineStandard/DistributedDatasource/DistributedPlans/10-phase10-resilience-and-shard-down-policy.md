# Phase 10 - Resilience & Shard-Down Policy

## Objective

Define how `DistributedDataSource` behaves when a shard becomes unhealthy or
disappears. Per-shard HA is delegated to `IProxyCluster`, but the distributed
tier still needs explicit policy for: read fallbacks, write quorum slack,
cross-shard call shedding, and shard-down events for operators.

## Dependencies

- All prior phases (router + executors + transactions).
- `Proxy/ProxyCluster.NodeProbing.cs` (per-shard health is reused).

## Scope

- `IShardHealthMonitor` aggregating each shard's `IProxyCluster` health into
  a `ShardHealthSnapshot`.
- `ShardDownPolicy` per distribution mode:
  - Routed:    fail the call (no replica). Optional designated `FailoverShardId`.
  - Replicated: drop unhealthy shard from candidate set; quorum still applies.
  - Broadcast:  skip unhealthy shard for writes (raise `OnPartialBroadcast`);
    reads use a healthy shard.
  - Sharded:   degrade scatter to known-healthy subset; document the gap.
- Cross-shard call shedding when too many shards are degraded
  (`MinimumHealthyShardRatio`).
- Re-routing on slow shards via timeout + circuit at distribution tier (in
  addition to per-cluster circuits).
- Events: `OnShardDown`, `OnShardRestored`, `OnDegradedMode`,
  `OnPartialBroadcast`.

## Out of Scope

- Automatic resharding on health change (Phase 11 explicit).
- Re-balancing live traffic to shadow shards.

## Target Files

Under `Distributed/Resilience/`:

- `IShardHealthMonitor.cs`.
- `ShardHealthMonitor.cs` (timer-driven, polls each `IProxyCluster`).
- `ShardHealthSnapshot.cs` (per-shard latency, alive flag, in-flight, role).
- `ShardDownPolicy.cs` enum + `ShardDownPolicyOptions.cs`.
- `DistributedCircuitBreaker.cs` (distribution-tier breaker per shard).
- `Events/ShardDownEventArgs.cs`, `Events/ShardRestoredEventArgs.cs`,
  `Events/DegradedModeEventArgs.cs`, `Events/PartialBroadcastEventArgs.cs`.

Update partials:

- `DistributedDataSource.Resilience.cs` (new partial wiring monitor + events).
- `DistributedReadExecutor.Scatter.cs` - filter unhealthy shards before
  fan-out per `ShardDownPolicy`.
- `DistributedWriteExecutor.FanOut.cs` - filter unhealthy shards; recompute
  quorum threshold.

## Design Notes

- Two layers of resilience to keep separate:
  - Inside one shard: `IProxyCluster` handles node failover, retries,
    circuits. The distributed tier never touches that.
  - Across shards: `DistributedDataSource` decides whether the call can
    continue with a degraded shard set.
- `MinimumHealthyShardRatio` (e.g. 0.5) gates scatter calls. Below the ratio
  the executor returns `DegradedShardSetException` instead of partial data.
- `DistributedCircuitBreaker` is a thin wrapper over the existing
  `Proxy/CircuitBreaker.cs` keyed by shard ID. When tripped, the shard is
  treated as unhealthy regardless of cluster reports (used for "shard returns
  successes but everything is wrong" scenarios).

## Implementation Steps

1. Create `Distributed/Resilience/` folder.
2. Implement `ShardHealthSnapshot` and `IShardHealthMonitor`.
3. Implement `ShardHealthMonitor` polling each cluster's metrics (reuse
   `IProxyCluster.GetClusterMetrics`).
4. Implement `ShardDownPolicy` enum + per-mode behavior table.
5. Implement `DistributedCircuitBreaker` reusing `Proxy/CircuitBreaker`.
6. Add `DistributedDataSource.Resilience.cs` partial wiring monitor lifetime
   into Open/Close/Dispose.
7. Update Read/Write scatter and fan-out partials to consult the monitor + the
   `DistributedCircuitBreaker` before dispatching.
8. Wire all new events.

## TODO Checklist

- [x] `IShardHealthMonitor.cs`, `ShardHealthMonitor.cs`,
      `ShardHealthSnapshot.cs` — immutable snapshot, polling monitor
      built over `IProxyCluster.ConnectionStatus` and hot-path
      counters, per-shard `OnShardDown` / `OnShardRestored` events.
- [x] `ShardDownPolicy.cs`, `ShardDownPolicyOptions.cs` — enum covers
      `FailFast` / `SkipShard` / `UseFailover` / `DegradeScatter`;
      options expose per-mode policy + `FailoverShardId` +
      distributed-breaker knobs (`CircuitFailureThreshold`,
      `CircuitResetTimeout`, `CircuitSuccessThreshold`).
- [x] `DistributedCircuitBreaker.cs` — thin wrapper over
      `Proxy/CircuitBreaker` keyed by shard id with `CanExecute`,
      `RecordSuccess`, `RecordFailure`, `Reset`, `GetState`, and
      `GetAllStates` surfaces for the adapter and tests.
- [x] Event-args files — `ShardDownEventArgs`,
      `ShardRestoredEventArgs`, `DegradedModeEventArgs`, and
      `PartialBroadcastEventArgs` declared on `IDistributedDataSource`
      and raised through safe-invoke helpers in
      `DistributedDataSource.Events.cs`.
- [x] `DistributedDataSource.Resilience.cs` partial — owns the
      monitor + breaker, wires their events through the datasource,
      exposes `ApplyResilienceOptions`, and provides hot-path helpers
      (`IsShardHealthyForDispatch`, `FilterHealthyShards`,
      `PassesScatterGate`, `EvaluateScatterGate`,
      `NotifyShardCallSucceeded/Failed`, `ReportPartialBroadcast`).
      Lifecycle wired into `Openconnection` / `Closeconnection` /
      `Dispose`.
- [x] Read/Write executors honor the health filter and per-mode
      policy. `IShardInvoker` gained default members
      `IsShardHealthy`, `FilterHealthyShards`,
      `EvaluateScatterGate`, `NotifyShardSuccess`, and
      `NotifyPartialBroadcast`; `ShardInvokerAdapter` overrides each
      to delegate to the resilience partial. Replicated reads promote
      healthy replicas via `PromoteHealthyShards`; scatter reads
      dispatch only to the healthy subset and record per-leg latency
      through `NotifyShardSuccess`. Broadcast / replicated writes use
      `RunWithSkips` to synthesize `WriteFanOutResult.Failure`
      entries for skipped shards so `WriteOutcome.QuorumAchieved`
      stays honest. Scatter-delete writes share the same gate check
      before dispatch.
- [x] `MinimumHealthyShardRatio` enforced at scatter entry —
      `EvaluateScatterGate` raises `OnDegradedMode` and throws
      `DegradedShardSetException` before any per-shard work starts.
      Applied uniformly to scatter reads
      (`ResolveHealthyShardsOrThrow`), broadcast / replicated writes
      (`ResolveHealthyFanOutShards` + gate), and opt-in scatter-delete
      writes.

## Verification Criteria

- [x] Routed entity on a down shard fails fast with a clear error and
      `OnShardDown` fires once. `ShardHealthMonitor.RecordFailure`
      flips the snapshot, raises `OnShardDown`, and the distribution
      tier feeds failures into the breaker so subsequent calls short
      circuit via `IsShardHealthyForDispatch`.
- [x] Replicated write with one down shard succeeds when remaining
      shards meet quorum, otherwise fails atomically. `RunWithSkips`
      keeps the attempted set intact for quorum while the healthy
      subset actually receives the write; `WriteOutcome` flags the
      quorum outcome correctly.
- [x] Broadcast write skips down shard, raises `OnPartialBroadcast`,
      and records the missed shard for operator repair.
      `ShardInvokerAdapter.NotifyPartialBroadcast` fires whenever
      `RunWithSkips` has a non-empty `skipped` list.
- [x] Sharded scatter aborts with `DegradedShardSetException` when
      the healthy shard ratio falls below configured threshold.
      `EvaluateScatterGate` returns the ready-to-throw exception and
      raises `OnDegradedMode`; scatter reads and fan-out writes
      rethrow before launching any leg.
- [x] `DistributedCircuitBreaker` opens after N consecutive failures
      even if cluster reports healthy. The breaker wraps per-shard
      `Proxy.CircuitBreaker` instances with the thresholds from
      `ShardDownPolicyOptions`, and
      `IsShardHealthyForDispatch` AND-combines breaker state with
      monitor state so an open breaker masks any "alive" cluster
      report.

## Risks / Open Questions

- Repair workflow for shards that missed broadcast writes. v1 logs the misses
  with enough detail (entity, key, payload reference) for an external repair
  job; Phase 13 audit completes the trail.
