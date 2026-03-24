# Phase 11 — ProxyCluster Tier Scaling: Index

This folder contains the detailed implementation plan for scaling `ProxyDataSource`
into a full cluster tier (`ProxyCluster`).

After this phase, callers use `dme.GetDataSource("my-cluster")` exactly as they
do today for a single datasource — but behind that name sits N load-balanced,
fault-tolerant proxy nodes sharing circuit state, audit trails, and consistent policy.

Return types throughout follow `IDataSource` contracts:
- Reads: `IEnumerable<object>` / `Task<IEnumerable<object>>`
- Queries: `IEnumerable<object>`
- Never `DataTable` in the cluster API.

---

## Sub-Phase Index

| # | File | Goals | Depends on | Status |
|---|------|-------|-----------|--------|
| 11.1 | [11.1-proxycluster-skeleton.md](11.1-proxycluster-skeleton.md) | G1, G3 | Phases 1–10 | ☐ |
| 11.2 | [11.2-shared-state-wiring.md](11.2-shared-state-wiring.md) | G2, G5 | 11.1 | ☐ |
| 11.3 | [11.3-affinity-and-drain.md](11.3-affinity-and-drain.md) | G6, G7 | 11.1, 11.2 | ☐ |
| 11.4 | [11.4-metrics-aggregation.md](11.4-metrics-aggregation.md) | G4 | 11.1, 11.2 | ☐ |
| 11.5 | [11.5-redis-circuit-state.md](11.5-redis-circuit-state.md) | G2 (dist.) | 11.1 | ☐ |
| 11.6 | [11.6-entity-affinity-routing.md](11.6-entity-affinity-routing.md) | G8 | 11.1, 11.3 | ☐ |
| 11.7 | [11.7-query-routing-rules-traffic-splitting.md](11.7-query-routing-rules-traffic-splitting.md) | G10, G16 | 11.1 | ☐ |
| 11.8 | [11.8-hedging-outlier-detection-slow-start.md](11.8-hedging-outlier-detection-slow-start.md) | G9, G12, G13 | 11.1, 11.4 | ☐ |
| 11.9 | [11.9-scatter-gather-replica-lag.md](11.9-scatter-gather-replica-lag.md) | G11, G15 | 11.1, 11.4 | ☐ |
| 11.10 | [11.10-fault-injection-rate-limiting.md](11.10-fault-injection-rate-limiting.md) | G14, G17 | 11.1 | ☐ |
| 11.11 | [11.11-consistent-hashing-connection-multiplexing.md](11.11-consistent-hashing-connection-multiplexing.md) | G18, G19 | 11.1, 11.10 | ☐ |

---

## Goal Reference

| ID | Goal | Inspiration |
|----|------|-------------|
| G1 | Horizontal scaling — add/remove nodes at runtime | HAProxy |
| G2 | Shared circuit state across nodes (in-proc or Redis) | Envoy, Netflix Hystrix |
| G3 | Proxy-cluster failover — dead node removed from routing | HAProxy, Envoy |
| G4 | Aggregate metrics + SLO snapshot | Prometheus, Grafana |
| G5 | Policy fan-out — one `ApplyPolicy` reaches all nodes | Envoy xDS |
| G6 | Session affinity — same user/sessionKey sticks to same node | HAProxy stick-table |
| G7 | Zero-downtime rolling restart via `DrainNode` | HAProxy, Kubernetes drain |
| G8 | Entity-affinity routing (partition leadership) | Kafka consumer groups, Vitess VSchema |
| G9 | Request hedging | Envoy, gRPC hedging |
| G10 | Declarative query routing rules (regex on op / entity) | ProxySQL query_rules, MaxScale |
| G11 | Scatter-gather reads across shards | Vitess ScatterGather |
| G12 | Slow-start weight ramp for new nodes | HAProxy slow-start |
| G13 | Outlier detection — auto-eject misbehaving nodes | Envoy outlier detection RFC |
| G14 | Fault injection for chaos engineering | Istio fault injection, Netflix Chaos Monkey |
| G15 | Replica lag guard — skip stale replicas | AWS Aurora replica lag |
| G16 | Traffic splitting / canary | Istio VirtualService weights, Argo Rollouts |
| G17 | Per-node rate limiting | Nginx limit_req, HAProxy filter rate-limit |
| G18 | Consistent hash ring routing | Redis Cluster, DynamoDB |
| G19 | Backend connection multiplexing / quota | Pgpool-II, RDS Proxy |

---

## New Files Created by Phase 11

| File (relative to `Proxy/`) | Introduced in |
|---------------------------|--------------|
| `ProxyCluster.cs` | 11.1 |
| `ProxyCluster.NodeManagement.cs` | 11.1 |
| `ProxyCluster.NodeRouting.cs` | 11.1 |
| `ProxyCluster.NodeProbing.cs` | 11.1 (structure), 11.8 (outlier), 11.9 (lag probe) |
| `ProxyNode.cs` | 11.1 |
| `ProxyClusterEvents.cs` | 11.1 |
| `EntityAffinityMap.cs` | 11.6 |
| `ProxyDataSource.RedisCircuitStateStore.cs` | 11.5 |

---

## Files Modified by Phase 11

| File | Modified in |
|------|------------|
| `IProxyDataSource.cs` | 11.1 (`IProxyCluster` interface) |
| `ProxyotherClasses.cs` | 11.1 (enum + policy fields), 11.6, 11.7, 11.8, 11.9, 11.10, 11.11 |
| `ProxyDataSource.cs` | 11.2 (`CircuitStateStore` property) |

---

## Minimum Implementation Order

For a working, production-usable cluster, implement in this order:

```
11.1 → 11.2 → 11.3 → 11.4
```

Everything after 11.4 is independent and can be delivered in any order based on
priority:
- **High value, low complexity:** 11.6, 11.7
- **High value, moderate complexity:** 11.8, 11.9
- **Optional / environment-specific:** 11.5 (Redis), 11.10 (chaos), 11.11 (consistent hash)

---

## Usage After Phase 11.1–11.4 Are Complete

```csharp
// 1. Build the cluster
var policy = new ProxyPolicy
{
    NodeRoutingStrategy = ProxyNodeRoutingStrategy.LeastConnections,
    EnableNodeAffinity  = true,
    NodeAffinityTtlSeconds = 300
};

var cluster = new ProxyCluster(policy);

cluster.AddNode(new ProxyNode("primary",  primaryProxy,  weight: 2,
    role: ProxyDataSourceRole.Primary));
cluster.AddNode(new ProxyNode("replica1", replicaProxy1, weight: 1,
    role: ProxyDataSourceRole.Replica));
cluster.AddNode(new ProxyNode("replica2", replicaProxy2, weight: 1,
    role: ProxyDataSourceRole.Replica));

// 2. Register as a named datasource (identical to registering a single IDataSource)
dme.ConfigEditor.RegisterProxyCluster("orders-cluster", cluster);

// 3. Use it — caller code is 100% unchanged from single-node usage
IDataSource ds = dme.GetDataSource("orders-cluster");
var orders = await ds.GetEntityAsync("Orders", filters);

// 4. Zero-downtime node replacement
await cluster.DrainNodeAsync("primary", timeoutMs: 30_000);
cluster.RemoveNode("primary");
cluster.AddNode(new ProxyNode("primary-v2", newPrimaryProxy, weight: 2,
    role: ProxyDataSourceRole.Primary));
```

---

## Master Plan Reference

Full architectural context, all design options considered, and the extended
feature list are documented in the parent folder:

[`../.plans/11-phase11-proxy-tier-scaling.md`](../11-phase11-proxy-tier-scaling.md)
