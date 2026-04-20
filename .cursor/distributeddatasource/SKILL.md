---
name: distributeddatasource
description: >
  Guidance for implementing and operating DistributedDataSource in BeepDM.
  Use when routing entities across shards, configuring distributed execution,
  handling degraded mode and partial failures, and tuning Phase 14 performance
  controls such as backpressure, rate limits, and adaptive deadlines.
---

# DistributedDataSource Guide

`DistributedDataSource` keeps the familiar `IDataSource` surface while routing and executing operations across multiple shard clusters (`IProxyCluster`).

## Use this skill when

- Building or updating sharded/replicated/broadcast placement in `DistributionPlan`
- Wiring shard map (`shardId -> IProxyCluster`) for distributed runtime
- Tuning scatter/fan-out/read timeout behavior in `DistributedDataSourceOptions`
- Handling distributed runtime events (`OnShardDown`, `OnDegradedMode`, `OnPartialReplicationFailure`, `OnBackpressure`)
- Implementing or tuning Phase 14 capacity engineering (`PerformanceOptions`, rate limiter, concurrency gate)

## Do not use this skill when

- You only need per-node failover/load balancing without sharding. Use [`proxydatasource`](../proxydatasource/SKILL.md).
- You are only changing cluster-node orchestration. Use [`proxycluster`](../proxycluster/SKILL.md).
- You are only editing connection string/driver configuration. Use [`connection`](../connection/SKILL.md).

## Core Types

| Type | Role |
|------|------|
| `DistributedDataSource` | Main distributed implementation over shard clusters |
| `IDistributedDataSource` | Distributed contract extending `IDataSource` |
| `DistributionPlan` | Entity placement strategy (sharded/replicated/broadcast) |
| `DistributedDataSourceOptions` | Runtime behavior knobs for routing/execution/resilience |
| `PerformanceOptions` | Phase 14 capacity/backpressure tuning block |
| `BackpressureException` | Structured overload rejection with retry hint |
| `IProxyCluster` | Per-shard HA execution tier |

## Typical Workflow

1. Build a stable shard map (`IReadOnlyDictionary<string, IProxyCluster>`).
2. Load/build `DistributionPlan` and validate placement against available shard IDs.
3. Construct `DistributedDataSource` with plan + options.
4. Open connection once, use normal `IDataSource` calls.
5. Subscribe to operational events for observability and alerting.
6. Tune Phase 14 performance caps from real metrics, not guesses.

## Clear Examples

### 1) Minimal construction
```csharp
var distributed = new DistributedDataSource(
    dmeEditor: editor,
    shards: new Dictionary<string, IProxyCluster>
    {
        ["shard-a"] = shardA,
        ["shard-b"] = shardB
    },
    plan: distributionPlan,
    options: new DistributedDataSourceOptions
    {
        DefaultPerShardTimeoutMs = 15_000,
        DefaultReadDeadlineMs = 45_000,
        MaxScatterParallelism = 4
    });

distributed.Openconnection();
```

### 2) Backpressure-safe call handling (Phase 14)
```csharp
try
{
    var rows = distributed.GetEntity("Orders", filters);
}
catch (BackpressureException bp)
{
    logger.LogWarning(
        $"Backpressure at {bp.GateName}; retry after {bp.RetryAfter.TotalMilliseconds:F0} ms");
}
```

### 3) Recommended Phase 14 baseline options
```csharp
var options = new DistributedDataSourceOptions
{
    Performance = new PerformanceOptions
    {
        EnableCapacityGates = true,
        MaxConcurrentDistributedCalls = 64,
        DistributedPermitWait = TimeSpan.FromMilliseconds(500),
        MaxConcurrentCallsPerShard = 16,
        ShardPermitWait = TimeSpan.FromMilliseconds(300),
        ShardRateLimitPerSecond = 120,
        ShardRateLimitBurst = 240,
        AdaptiveDeadlineFactor = 1.3,
        MinAdaptiveDeadlineMs = 250,
        MaxAdaptiveDeadlineMs = 8_000,
        EnableHotShardReadShedding = true
    }
};
```

### 4) Event wiring for operations
```csharp
distributed.OnShardDown += (_, e) => logger.LogWarning($"Shard down: {e.ShardId}");
distributed.OnShardRestored += (_, e) => logger.LogInformation($"Shard restored: {e.ShardId}");
distributed.OnDegradedMode += (_, e) => logger.LogWarning($"Degraded mode ratio={e.HealthyRatio:F2}");
distributed.OnBackpressure += (_, e) =>
    logger.LogWarning($"Backpressure gate={e.GateName} retry={e.RetryAfter.TotalMilliseconds:F0} ms");
```

## Validation and Safety

- Keep shard IDs immutable and environment-consistent.
- Ensure every placement target exists in the shard map before applying a plan.
- Start with conservative parallelism and permit waits; increase only after measuring.
- Treat partial replication/broadcast events as reconciliation work, not success noise.
- Always surface backpressure events to telemetry dashboards.

## File Locations

| File | Purpose |
|------|---------|
| `DataManagementEngineStandard/DistributedDatasource/Distributed/DistributedDataSource.cs` | Root class and constructor wiring |
| `DataManagementEngineStandard/DistributedDatasource/Distributed/DistributedDataSourceOptions.cs` | All runtime options |
| `DataManagementEngineStandard/DistributedDatasource/Distributed/DistributedDataSource.Performance.cs` | Phase 14 wiring and backpressure event |
| `DataManagementEngineStandard/DistributedDatasource/Distributed/Performance/PerformanceOptions.cs` | Capacity controls |
| `DataManagementEngineStandard/DistributedDatasource/Distributed/Performance/DistributedConcurrencyGate.cs` | Global/per-shard permits |
| `DataManagementEngineStandard/DistributedDatasource/Distributed/Performance/TokenBucketRateLimiter.cs` | Per-shard token buckets |
| `DataManagementEngineStandard/DistributedDatasource/Distributed/Performance/AdaptiveTimeoutCalculator.cs` | p95-based adaptive deadlines |
| `DataManagementEngineStandard/DistributedDatasource/Distributed/Performance/HotShardMitigator.cs` | Hot shard read shedding |
| `DataManagementEngineStandard/DistributedDatasource/Distributed/Performance/BackpressureException.cs` | Structured overload exception |
| `Help/distributed-datasource.html` | User-facing help with examples |

## Related Skills

- [`proxydatasource`](../proxydatasource/SKILL.md)
- [`proxycluster`](../proxycluster/SKILL.md)
- [`beepdm`](../beepdm/SKILL.md)
- [`connection`](../connection/SKILL.md)
