# Balanced Plans -> ProxyDataSource Adoption Mapping

## Purpose
Map `Balanced/.plans` phases to concrete implementation targets in `ProxyDataSource` so the existing proxy can adopt balanced architecture incrementally without API breakage.

## Core Principle
- Keep `IDataSource` behavior stable.
- Implement balanced capabilities through proxy-specific contract (`IProxyDataSource`) and internal helpers/partials.
- Prioritize correctness fixes before advanced routing/performance.

## Phase Mapping

| Balanced Phase | ProxyDataSource Targets | Primary Outcome |
|---|---|---|
| 01 Core contracts and architecture | `Proxy/IProxyDataSource.cs`, `ProxyDataSource` class contract | Explicit proxy capabilities beyond `IDataSource` |
| 02 Source pool management and routing | `GetPooledConnection`, `ReturnConnection`, `GetNextBalancedDataSource`, candidate selection paths | Deterministic pool + routing behavior |
| 03 Resilience/retry/circuit/health | `RetryPolicy`, `ShouldRetry`, `ExecuteWithPolicy`, `CircuitBreaker`, `PerformHealthCheck` | Explicit resilience profile behavior |
| 04 Operation semantics read/write/transaction | `RunQuery`, `ExecuteSql`, CRUD wrappers, `BeginTransaction/Commit/EndTransaction` | Single-invocation safe operation semantics |
| 05 Caching and consistency controls | `GetEntityWithCache`, `InvalidateCache`, cache key generation | Predictable cache consistency and invalidation |
| 06 Security/governance/audit | logging and failover event payloads | Auditable route/failover decisions |
| 07 Observability/SLO/alerting | `RecordSuccess`, `RecordFailure`, `GetMetrics` | SLO-ready metrics and structured signals |
| 08 Performance and capacity engineering | pool cleanup, balancing, health-check execution model | Throughput and saturation stability |
| 09 DevEx/testing/CI gates | proxy wrapper regression suites | Prevent duplicate execution regressions |
| 10 Rollout governance and KPI gates | rollout docs + hard-stop KPIs | Safe staged production adoption |
| 11 Distributed balanced cluster architecture | (future) cluster-aware routing and state sharing | multi-node balancing readiness |

## Current High-Risk Hotspots to Fix First
1. Wrapper double execution pattern in several `IDataSource` methods.
2. Mixed async/sync waiting (`.Result`/`.Wait()`) inside retry paths.
3. Health-state concurrency (`Dictionary` access across timer + request paths).

## Recommended Implementation Order
1. **Phase 04 subset first:** fix single-invocation wrappers and result propagation.
2. **Phase 01:** finalize proxy-specific contract and surface area.
3. **Phases 02-03:** routing + resilience policy normalization.
4. Remaining phases in order with KPI gates.
