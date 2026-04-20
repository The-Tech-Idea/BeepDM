# Phase 00 - Overview & Gap Matrix

## Objective

Define the scope of `DistributedDataSource`, identify the gap between the
existing `Proxy/` layer and the requested distribution capabilities, and
inventory which existing components are reused versus newly introduced.

## Reused vs New

| Concern                          | Reused (existing)                                                                  | New (this program)                                                |
|----------------------------------|------------------------------------------------------------------------------------|-------------------------------------------------------------------|
| Per-shard HA pool                | `Proxy/ProxyCluster.cs` (`IProxyCluster`)                                          | -                                                                 |
| Per-node failover/circuit/health | `Proxy/ProxyDataSource.cs`, `Proxy/CircuitBreaker.cs`, `ProxyCluster.NodeProbing` | -                                                                 |
| Entity-affinity routing          | `Proxy/EntityAffinityMap.cs` (per-cluster)                                         | `EntityPlacementMap` (per-distribution-plan, multi-target & mode) |
| Write fan-out                    | `Proxy/ProxyDataSource.FanOut.cs` (per pool)                                       | `DistributedDataSource.Writes.cs` (cross-shard fan-out)           |
| Scatter-gather reads             | `Proxy/ProxyCluster.ScatterGather.cs` (per cluster)                                | `DistributedDataSource.Reads.cs` (cross-shard scatter)            |
| Routing rules / traffic split    | `Proxy/ProxyCluster.RoutingRules.cs`                                               | `ShardRouter` (distribution-aware, key-based)                     |
| Persistence of topology          | `ProxyCluster.SaveNodesToConfig`                                                   | `ShardCatalog` + `DistributionPlan` (via `ConfigEditor`)          |
| Partition functions              | -                                                                                  | `IPartitionFunction` (Hash/Range/List/Composite)                  |
| Cross-shard query planning       | -                                                                                  | `CrossShardQueryPlanner`, `ResultMerger`                          |
| Distributed transactions         | -                                                                                  | `DistributedTransactionCoordinator` (2PC / saga)                  |
| Resharding / rebalance           | -                                                                                  | `ReshardingService`, `DualWriteWindow`                            |
| Schema/DDL broadcast             | -                                                                                  | `DistributedSchemaService`                                        |

## Gap Matrix (Priority)

| Area                                  | Current | Target                                                                 | Priority |
|---------------------------------------|---------|------------------------------------------------------------------------|----------|
| `IDataSource` distribution facade     | None    | `DistributedDataSource : IDataSource` composing `IProxyCluster` shards | P0       |
| Distribution plan & shard catalog     | None    | Persistent, versioned plan with per-entity mode + partition function   | P0       |
| Entity-level placement (Routed/Replicated/Broadcast) | None | Map entities to one or many shards by mode               | P0       |
| Row-level sharding (Sharded mode)     | None    | Pluggable partition functions + key extraction from request            | P0       |
| Cross-shard read scatter & merge      | Per-cluster only | Cross-shard scatter, union/aggregate/order/limit merge          | P0       |
| Replicated/broadcast write fan-out    | Per-cluster only | Cross-shard fan-out with quorum policy                          | P0       |
| Distributed transactions              | None    | Single-shard fast path; 2PC where supported; saga otherwise            | P1       |
| Resharding / rebalancing              | None    | Online split/merge with dual-write window                              | P1       |
| Schema management (DDL broadcast)     | None    | Coordinated DDL across shards with drift detection                     | P1       |
| Observability & SLO at distribution tier | Per-cluster | Per-shard + per-entity metrics, hot-shard detection             | P1       |
| Security & audit at distribution tier | Per-cluster | Per-entity placement audit, per-shard credential isolation        | P1       |
| Performance / capacity                | Per-pool | Cross-shard parallelism caps, hot-shard mitigation                    | P1       |
| DevEx / fault injection / CI gates    | Per-proxy | Shard-outage simulation, partition-function fuzzing                   | P1       |
| Rollout governance                    | None    | KPI-gated rollout, shadow mode, dark launch                            | P1       |

## Phased Plan (16 phases)

1. Phase 01 - Core contracts & `DistributedDataSource` skeleton.
2. Phase 02 - `DistributionPlan` & `ShardCatalog` (persistence via `ConfigEditor`).
3. Phase 03 - Entity placement & replication modes (Routed / Replicated / Broadcast).
4. Phase 04 - Partition functions for row-level sharding.
5. Phase 05 - `ShardRouter` & key extraction from requests.
6. Phase 06 - Read execution (single-shard / scatter / replicated read).
7. Phase 07 - Write execution (sharded write / replicated fan-out / broadcast DDL).
8. Phase 08 - Cross-shard query planner & result merger.
9. Phase 09 - Distributed transactions (single-shard fast path, 2PC, saga).
10. Phase 10 - Resilience integration & shard-down policy.
11. Phase 11 - Resharding / rebalancing with dual-write window.
12. Phase 12 - Schema management & DDL broadcast.
13. Phase 13 - Observability, security, audit at distribution tier.
14. Phase 14 - Performance & capacity engineering.
15. Phase 15 - DevEx, testing, CI gates, & rollout governance.

## Success Criteria

- `DistributedDataSource` registers in `IDMEEditor` like any other datasource.
- A 1000-table workload can be split 500/500 across two backend clusters with
  zero call-site changes.
- Entities can be marked `Replicated` for HA replication with quorum writes.
- Row-level sharding routes a single entity's reads/writes by partition key.
- Resharding can move ownership without downtime via dual-write window.
- All decisions (placement, fan-out, scatter, failover) emit structured audit
  events and metrics.

## Non-Goals (v1)

- Full SQL parser / optimizer for arbitrary cross-shard joins.
- Strongly consistent global secondary indexes.
- Automatic data movement triggered by load (manual reshard only in v1).
