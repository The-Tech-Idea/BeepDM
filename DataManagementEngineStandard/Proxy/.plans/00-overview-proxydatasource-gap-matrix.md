# ProxyDataSource Enhancement Program - Overview and Gap Matrix

## Balanced Alignment
This overview is aligned with [Balanced -> Proxy adoption mapping](../../Balanced/.plans/proxydatasource-adoption-mapping.md).

## Objective
Define a phased enterprise enhancement roadmap for `ProxyDataSource` to align with best practices from resilient data access/proxy systems.

## Current Baseline
- `ProxyDataSource` already provides:
  - failover
  - circuit breaker integration
  - health checks
  - weighted/latency-aware load balancing
  - retry policy support
  - cache support
  - per-source metrics and connection pooling
- Core files:
  - `ProxyDataSource.cs`
  - `CircuitBreaker.cs`
  - `ProxyotherClasses.cs`
  - `README.md`

## Enterprise Gaps
- Core retry wrappers execute many operations twice, creating correctness risk for writes.
- Async/sync boundary is inconsistent (`.Result/.Wait()` over async retry paths).
- Health/routing state uses mixed thread-safety primitives (plain `Dictionary` + timer + concurrent callers).
- Constructor options and `_options` policy object can diverge at runtime.
- Transaction/result contracts are inconsistent (wrappers return `Current.ErrorObject` vs operation result).

## Gap Matrix

| Area | Current | Target | Priority |
|---|---|---|---|
| Execution correctness | Retries exist | No duplicate side effects under retry wrappers | P0 |
| Resilience Policy | Basic options/thresholds | Single source of truth policy profile per environment | P0 |
| Routing Intelligence | Weighted + latency-aware | Policy-based routing by operation/entity/workload | P1 |
| Retry Semantics | Generic transient retry | Error-class + idempotency-safe behavior | P0 |
| Concurrency safety | Mixed thread-safe/non-thread-safe state | Deterministic concurrent health/metrics behavior | P0 |
| Caching | Entity-level cache support | Consistency-safe cache policy and invalidation semantics | P1 |
| Observability | Metrics available | SLOs, alerting contracts, trace correlation | P1 |
| Governance | Runtime config usage | Versioned policy rollout + approval gates | P1 |
| Scale | Pooling and balancing present | Capacity policies, adaptive throttling, stress-tested profiles | P2 |
| **Proxy tier scale** | Single ProxyDataSource instance | ProxyCluster with N nodes, shared circuit state, node failover | P2 |

## Planned Phases
1. Contracts and Policy Foundation
2. Resilience Profiles and Error Taxonomy
3. Advanced Routing and Load Distribution
4. Retry, Idempotency, and Failover Semantics
5. Cache Strategy and Consistency Controls
6. Observability, SLO, and Alerting
7. Security, Audit, and Compliance
8. Performance and Capacity Engineering
9. DevEx and CI/CD Safety Gates
10. Rollout Governance and KPI Gates
11. **Proxy Tier Scaling** — Load balancing and failover for ProxyDataSource *itself*
    (`ProxyCluster`, shared circuit state, node affinity, rolling restart)
    → see [11-phase11-proxy-tier-scaling.md](11-phase11-proxy-tier-scaling.md)

## Success Criteria
- Proxy behavior is policy-driven, testable, and environment-aware.
- Failover/retry/circuit interactions are deterministic and auditable.
- SLO and risk gates control production rollout of proxy behavior changes.

## Real Code Constraints
- `ExecuteSql`, `RunQuery`, `CreateEntityAs`, and several CRUD wrappers invoke `Current.<op>` inside retry probe and then invoke again after success.
- `RetryPolicy` is async but frequently consumed via `.Result/.Wait()`, increasing blocking and deadlock risk.
- `_healthStatus` is `Dictionary<string,bool>` mutated by timer and read by request routing without synchronization.
