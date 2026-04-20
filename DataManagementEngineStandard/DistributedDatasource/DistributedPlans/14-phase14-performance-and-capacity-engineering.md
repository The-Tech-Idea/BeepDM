# Phase 14 - Performance & Capacity Engineering

## Objective

Bound resource use and tail latency under load. Cap parallelism per call and
in aggregate, throttle scatter / fan-out, mitigate hot shards, and provide
predictable degradation paths instead of cliff failures.

## Dependencies

- Phase 06 (read scatter), Phase 07 (write fan-out), Phase 10 (resilience),
  Phase 13 (metrics).
- Existing `Proxy/ProxyCluster` connection-count semaphore (per-cluster).

## Scope

- Cross-shard parallelism caps:
  - `MaxScatterParallelism` (per call).
  - `MaxFanOutParallelism` (per call).
  - `MaxConcurrentDistributedCalls` (per `DistributedDataSource` instance).
- Per-shard token-bucket rate limiter at the distribution tier (in addition
  to the cluster's own).
- Hot-shard mitigation: when `HotShardDetector` flags a shard, optionally
  shed reads (Replicated/Broadcast) to peers and lower scatter weight.
- Adaptive timeouts: per-shard p95-based deadline (`DeadlineFactor * p95`)
  with a hard cap.
- Connection budget: cluster-wide semaphore mirrored at the distributed tier
  (`MaxBackendConnectionsAcrossShards`).
- Backpressure: when caps are exhausted return `BackpressureException` with
  a hint instead of queueing unboundedly.

## Out of Scope

- Auto-tuning of caps (manual config in v1).
- Disk / IO budgeting (datasource-driver concern).

## Target Files

Under `Distributed/Performance/`:

- `IDistributedRateLimiter.cs`, `TokenBucketRateLimiter.cs`.
- `DistributedConcurrencyGate.cs`.
- `AdaptiveTimeoutCalculator.cs`.
- `BackpressureException.cs`.
- `HotShardMitigator.cs`.
- `PerformanceOptions.cs` (per-instance tuning record).

Update partials:

- `DistributedReadExecutor.Scatter.cs` / `Replicated.cs` - acquire gates,
  apply adaptive timeouts, consult mitigator.
- `DistributedWriteExecutor.FanOut.cs` - same.
- `DistributedDataSource.cs` - constructs the gates with options.

## Design Notes

- `DistributedConcurrencyGate` wraps a `SemaphoreSlim`; `AcquireAsync(ct)`
  honors deadlines.
- Rate limiter is keyed by shard ID and refreshed on policy change.
- Adaptive timeout: `min(MaxDeadlineMs, max(MinDeadlineMs, p95 * Factor))`.
- Hot shard mitigator shifts read load: if shard A is hot and entity is
  Replicated/Broadcast, route reads to other shards in the placement set
  until detector clears the flag.

## Implementation Steps

1. Create `Distributed/Performance/` folder.
2. Implement `IDistributedRateLimiter` + `TokenBucketRateLimiter`.
3. Implement `DistributedConcurrencyGate`.
4. Implement `AdaptiveTimeoutCalculator` consuming
   `DistributionMetricsSnapshot`.
5. Implement `HotShardMitigator` consuming `HotShardDetector` events.
6. Add `PerformanceOptions` and wire into `DistributedDataSourceOptions`.
7. Update read/write executors to acquire gates and apply timeouts.
8. Add `BackpressureException` and surface it through an event.

## TODO Checklist

- [x] `IDistributedRateLimiter.cs`, `TokenBucketRateLimiter.cs`.
- [x] `DistributedConcurrencyGate.cs`.
- [x] `AdaptiveTimeoutCalculator.cs`.
- [x] `HotShardMitigator.cs`.
- [x] `BackpressureException.cs` (+ `BackpressureEventArgs.cs`).
- [x] `PerformanceOptions.cs` and option wiring
      (`DistributedDataSourceOptions.Performance` /
      `DistributedDataSourceOptions.RateLimiter`).
- [x] Executors honor gates, limiters, adaptive timeouts
      (scatter read + replicated read + single-shard read +
      fan-out write + single-shard write + scatter-delete write).

## Verification Criteria

- [x] Setting `MaxConcurrentDistributedCalls = N` caps active
      distributed calls at <= N; additional callers see
      `BackpressureException` (thrown from
      `DistributedConcurrencyGate.AcquireDistributed`) after
      `PerformanceOptions.DistributedPermitWait`. Per-call
      scatter parallelism remains capped by the existing
      `MaxScatterParallelism` semaphore.
- [x] Per-shard rate limit enforced by `TokenBucketRateLimiter`;
      burst then pause drains the bucket and subsequent calls are
      delayed (async) or rejected (after
      `PerformanceOptions.ShardPermitWait`).
- [x] `AdaptiveTimeoutCalculator` reads shard p95 from the Phase 13
      metrics aggregator and emits
      `clamp(factor * p95, minDeadline, maxDeadline)`, with the
      caller's `DefaultPerShardTimeoutMs` acting as a floor. The
      scatter, replicated, and single-shard async paths apply the
      adaptive deadline on a per-leg CTS linked to the scatter
      deadline.
- [x] `HotShardMitigator` subscribes to
      `IDistributedMetricsAggregator.OnHotShardDetected` and tracks
      a per-shard cooldown. `ShardInvokerAdapter.ShedHotShards`
      filters hot shards from replicated/broadcast candidate lists;
      the replicated executor skips hot replicas unless it's the
      last one remaining. Scatter reads over sharded entities are
      never shed (that would drop data partitions).
- [x] Exhausted concurrency surfaces `BackpressureException`
      cleanly with a `RetryAfter` hint and a `GateName`
      (`DistributedCall` or `Shard:<id>`), and raises the new
      `OnBackpressure` event so operators can wire metrics/alerts
      without catching the exception upstream.

## Completion Summary

Phase 14 ships four tightly-composed capacity controls:

1. **Concurrency gate (`DistributedConcurrencyGate`)** — global +
   per-shard `SemaphoreSlim` pairs with `IDisposable` permits. The
   gate is acquired once per distributed call in every executor
   path so a burst of scatters or fan-outs cannot exceed
   `MaxConcurrentDistributedCalls`. Per-shard acquisitions layer on
   top so one hot shard can't starve the others.
2. **Rate limiter (`TokenBucketRateLimiter`)** — per-shard token
   buckets with configurable `ratePerSecond` + `burst`, refilled
   continuously. Async acquisition waits up to
   `ShardPermitWait` before throwing
   `BackpressureException` with an accurate `RetryAfter`.
3. **Adaptive timeout (`AdaptiveTimeoutCalculator`)** — reads the
   Phase 13 per-shard p95 and clamps the adaptive deadline into
   `[MinAdaptiveDeadlineMs, MaxAdaptiveDeadlineMs]`, never shorter
   than the caller's fallback. Applied per-leg via a linked CTS
   in the scatter, replicated, and single-shard async paths.
4. **Hot-shard mitigator (`HotShardMitigator`)** — attaches to
   `IDistributedMetricsAggregator.OnHotShardDetected` and sheds
   replicated/broadcast reads for the configured cooldown. Keeps
   the last-remaining replica even when it's hot so no data is
   orphaned.

Wiring:

- `IShardInvoker` grew four default-implemented hooks
  (`AcquireDistributedCallPermit`, `AcquireShardCallPermit`,
  `ShedHotShards`, `ComputeShardDeadlineMs`) and a private
  `NullDisposable` sentinel. Existing callers see no behaviour
  change.
- `ShardInvokerAdapter` (inside `DistributedDataSource`) forwards
  each hook to the new `DistributedDataSource.Performance.cs`
  partial, which lazy-builds the gate, limiter, calculator, and
  mitigator, and attaches the mitigator to the live aggregator.
- Read executors: `Scatter.cs` and `Replicated.cs` acquire the
  distributed-call permit at entry, acquire a per-shard permit +
  rate-limit token per leg, apply the adaptive per-shard
  deadline, and release in `finally`. `SingleShard.cs` does the
  same for the single-shard path.
- Write executors: `FanOut.cs`, `SingleShard.cs`, and
  `ScatterDelete.cs` acquire the distributed permit at entry;
  `ExecuteLeg` in `DistributedWriteExecutor.cs` acquires the
  per-shard permit.
- Backpressure surfaced via the new `OnBackpressure` event on
  `DistributedDataSource` (fired from
  `AcquireDistributedCallPermit` / `AcquireShardCallPermit` /
  `AcquireShardCallPermitAsync`).
- `Dispose()` detaches the mitigator from the aggregator and
  disposes the concurrency gate.

Build: `DataManagementEngineStandard.csproj` built clean with
0 errors on 2026-04-19.

## Risks / Open Questions

- Interaction with cluster-tier rate limits (already in
  `ProxyCluster`): distributed gate runs first; both must be
  satisfied for a call to proceed. Operators should expose the
  effective combined limit via metrics dashboards.
- Auto-tuning of caps is out-of-scope (manual config in v1); a
  later phase could feed the metrics aggregator back into
  `PerformanceOptions` at runtime.
- Hot-shard cooldown is currently hard-coded to 30 s; a future
  option could expose it and attach jitter to avoid synchronized
  shed/restore waves across a fleet.
