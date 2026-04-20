# Phase 13 - Observability, Security, & Audit

## Objective

Make `DistributedDataSource` operable in production: aggregated metrics with a
distribution slice, structured audit trail of routing/placement decisions,
durable transaction log, secret isolation per shard, and per-entity ACLs.

## Dependencies

- Phase 06 / 07 / 09 (executors + transaction coordinator).
- Existing `Proxy/IProxyAuditSink` and `Proxy/ProxyLogRedactor.cs`.

## Scope

- Metrics aggregator: per-shard, per-entity, per-mode counters + latency
  histograms, hot-shard / hot-entity detection.
- Audit trail extending `IProxyAuditSink` with distribution-tier event types
  (`PlacementDecided`, `Scattered`, `FannedOut`, `ReshardStarted`,
  `ReshardCompleted`, `DDLBroadcast`).
- Durable transaction log promoting Phase 09's `InMemoryTransactionLog` to a
  `FileTransactionLog` in `BeepDataPath` for crash recovery.
- Per-shard credential isolation: each `Shard` owns its `IProxyCluster`
  whose nodes carry their own `ConnectionProperties` - no shared credential
  bag at the distributed tier.
- Optional per-entity ACLs (`IDistributedAccessPolicy`) to allow/deny
  read/write/DDL by caller principal.

## Out of Scope

- External tracing exporters (open hooks via `Activity` source for callers
  to wire OpenTelemetry).
- Encryption-at-rest for the transaction log (rely on filesystem ACLs).

## Target Files

Under `Distributed/Observability/`:

- `IDistributedMetricsAggregator.cs`.
- `DistributedMetricsAggregator.cs`.
- `DistributionMetricsSnapshot.cs` (per-shard, per-entity, per-mode).
- `HotShardDetector.cs`.
- `DistributedActivitySource.cs` (`System.Diagnostics.Activity` source name).

Under `Distributed/Audit/`:

- `IDistributedAuditSink.cs` (extends or wraps `IProxyAuditSink`).
- `DistributedAuditEvent.cs` + concrete event records.
- `FileDistributedAuditSink.cs`.
- `NullDistributedAuditSink.cs`.

Under `Distributed/Transactions/` (additions for Phase 13):

- `FileTransactionLog.cs` - promotes `InMemoryTransactionLog` to durable.

Under `Distributed/Security/`:

- `IDistributedAccessPolicy.cs`, `AllowAllAccessPolicy.cs`,
  `EntityAclAccessPolicy.cs`.
- `DistributedSecurityException.cs`.

Update partials:

- `DistributedDataSource.Observability.cs` (new) - subscribes executors and
  monitor to the aggregator.
- `DistributedDataSource.Audit.cs` (new) - emits events at decision points.

## Design Notes

- `DistributedMetricsAggregator` polls each shard's `IProxyCluster` metrics
  and merges them with distribution-tier counters into a single snapshot.
- Hot shard / hot entity detection uses rolling-window p95 latency and
  request rate; emits `OnHotShardDetected` / `OnHotEntityDetected`.
- `Activity` source name: `Beep.Distributed.DataSource`. Each call creates an
  activity with tags: `shard.ids`, `entity`, `mode`, `match.kind`, `key`.
- Audit events are append-only JSON Lines for easy ingestion.
- ACLs are evaluated at the distribution tier before any executor call so
  denied requests never touch a shard.

## Implementation Steps

1. Create folders `Observability/`, `Audit/`, `Security/`.
2. Implement `DistributedMetricsAggregator` and `HotShardDetector`.
3. Implement `DistributedActivitySource`.
4. Implement `IDistributedAuditSink` + `FileDistributedAuditSink` +
   `NullDistributedAuditSink` reusing `Proxy/ProxyLogRedactor` for sensitive
   field masking.
5. Implement `FileTransactionLog`; switch coordinator's default log when
   `BeepDataPath` is available.
6. Implement `IDistributedAccessPolicy` + the two starter implementations.
7. Wire all aggregation/audit/security calls into the executors via
   partials `DistributedDataSource.Observability.cs` and
   `DistributedDataSource.Audit.cs`.

## TODO Checklist

- [x] `IDistributedMetricsAggregator.cs`, `DistributedMetricsAggregator.cs`,
      `DistributionMetricsSnapshot.cs`, `HotShardDetector.cs`.
- [x] `DistributedActivitySource.cs`.
- [x] `IDistributedAuditSink.cs` + concrete sinks
      (`NullDistributedAuditSink`, `FileDistributedAuditSink`) +
      `DistributedAuditEvent` + `DistributedAuditEventKind`.
- [x] `FileTransactionLog.cs` (JSON-lines append-only, `closed/`
      sub-folder for finalised scopes, wired into the coordinator via
      `DistributedDataSourceOptions.DurableTransactionLogDirectory`).
- [x] `IDistributedAccessPolicy.cs` + `AllowAllAccessPolicy` +
      `EntityAclAccessPolicy` + `DistributedAccessKind` +
      `DistributedSecurityException`.
- [x] `DistributedDataSource.Observability.cs`,
      `DistributedDataSource.Audit.cs` partials wired end-to-end
      (reads, writes, DDL, resharding, transaction begin/commit/rollback,
      resilience success/failure hooks).

## Verification Criteria

- [x] Snapshot returns merged per-shard + per-entity counters (executor
      resilience callbacks feed the aggregator via
      `RecordDistributedRequest`; `IProxyCluster.GetClusterMetrics` is
      merged in `DistributedMetricsAggregator.Snapshot`).
- [x] Hot shard detection raises event when p95 > threshold for N
      consecutive windows (`HotShardDetector.RecordLatency`; event
      is forwarded through `OnHotShardDetected` and audited as
      `HotShardDetected`).
- [x] `FileTransactionLog` persists every append/close to disk so
      `IDistributedTransactionCoordinator` can recover on restart;
      falls back to in-memory log silently when the configured
      directory is unwritable.
- [x] Denied request via `EntityAclAccessPolicy` never reaches an
      executor (`EnsureAccess` is evaluated at the start of
      `RouteForRead`, `RouteForWrite`, and every DDL entry point;
      audited as `AccessDenied` before the throw).
- [x] Audit log lines are valid JSON (`FileDistributedAuditSink`
      writes JSON-lines through `System.Text.Json`) and contain
      redacted partition keys / messages via
      `Proxy.ProxyLogRedactor` when
      `DistributedDataSourceOptions.RedactAuditFields` is `true`.

## Risks / Open Questions

- Metric cardinality: per-entity per-shard tags can explode. Cap exported
  series by `MaxObservedEntities` with LRU eviction; document the bound.
  Implemented via `DistributedMetricsAggregator.EvictOldestShard /
  EvictOldestEntity` with configurable caps.

## Completion Summary (2026-04-19)

- **New folders**: `Distributed/Observability/`, `Distributed/Audit/`,
  `Distributed/Security/`.
- **New files**: 12 (aggregator, snapshot, interface, detector,
  activity source, audit sink interface + two sinks, event record +
  kind enum, access policy interface + two policies, security
  exception, access kind enum, file transaction log).
- **Options extension**: `DistributedDataSourceOptions` gains
  `EnableDistributedMetrics`, `MetricsAggregator`,
  `HotShardP95ThresholdMs`, `HotShardConsecutiveWindows`,
  `HotEntityThresholdRps`, `AuditSink`, `RedactAuditFields`,
  `AccessPolicy`, `DurableTransactionLogDirectory`.
- **Wiring**: two new partials
  (`DistributedDataSource.Observability.cs`,
  `DistributedDataSource.Audit.cs`); updates to
  `DistributedDataSource.cs` constructor (durable log),
  `DistributedDataSource.Reads.cs` / `Writes.cs`
  (access checks on routing), `DistributedDataSource.Routing.cs`
  (`PlacementDecided` / `Scattered` / `FannedOut` audit events),
  `DistributedDataSource.Schema.cs` (`DDLBroadcast` audit + access
  checks), `DistributedDataSource.Events.cs` (`ReshardStarted` /
  `ReshardCompleted` audit), `DistributedDataSource.Transactions.cs`
  (`TransactionBegan` / `TransactionCommit` / `TransactionRollback`
  audit), and `DistributedDataSource.Resilience.cs` (metrics recorded
  from `NotifyShardCallSucceeded` / `NotifyShardCallFailed`).
- **Build**: `DataManagementEngineStandard` compiles clean across
  net8.0 / net9.0 / net10.0 with 0 errors (warnings in unrelated
  WebAPI / CSV_DataSource / UserContextResolver projects are
  pre-existing and outside Phase 13 scope).
