# Caching Enhancement Plans

Phased enhancement program for `DataManagementEngineStandard/Caching`, based on direct audit of:
- `CacheManager.cs`
- `CacheManager.Extensions.cs`
- `CacheConfiguration.cs`
- `ICacheProvider.cs`
- `Providers/*`
- `DataSources/*`

## Execution Order

| # | Document | Status |
|---|---|---|
| 1 | [00-overview-caching-gap-matrix.md](./00-overview-caching-gap-matrix.md) | in-progress |
| 2 | [01-phase1-contracts-and-provider-lifecycle-baseline.md](./01-phase1-contracts-and-provider-lifecycle-baseline.md) | planned |
| 3 | [02-phase2-correctness-default-null-and-atomicity-semantics.md](./02-phase2-correctness-default-null-and-atomicity-semantics.md) | planned |
| 4 | [03-phase3-thread-safety-and-statistics-integrity.md](./03-phase3-thread-safety-and-statistics-integrity.md) | planned |
| 5 | [04-phase4-expiration-eviction-and-memory-accounting.md](./04-phase4-expiration-eviction-and-memory-accounting.md) | planned |
| 6 | [05-phase5-hybrid-provider-consistency-and-backfill-controls.md](./05-phase5-hybrid-provider-consistency-and-backfill-controls.md) | planned |
| 7 | [06-phase6-distributed-locking-and-concurrency-controls.md](./06-phase6-distributed-locking-and-concurrency-controls.md) | planned |
| 8 | [07-phase7-datasource-integration-and-schema-consistency.md](./07-phase7-datasource-integration-and-schema-consistency.md) | planned |
| 9 | [08-phase8-observability-health-slo-and-alerting.md](./08-phase8-observability-health-slo-and-alerting.md) | planned |
| 10 | [09-phase9-security-serialization-and-fault-isolation.md](./09-phase9-security-serialization-and-fault-isolation.md) | planned |
| 11 | [10-phase10-rollout-governance-and-kpi-gates.md](./10-phase10-rollout-governance-and-kpi-gates.md) | planned |
| 12 | [implementation-hotspots-change-plan.md](./implementation-hotspots-change-plan.md) | planned |
| 13 | [standards-traceability-matrix.md](./standards-traceability-matrix.md) | planned |
| 14 | [risk-register-and-cutover-checklists.md](./risk-register-and-cutover-checklists.md) | planned |

## Audited Hotspot Files
- `CacheManager.GetOrCreateAsync`, `GetAsync`, `SetAsync`, `GetManyAsync`, `SetManyAsync`
- `CacheManager.Extensions.SetIfNotExistsAsync`, `GetAndRemoveAsync`, `TryAcquireLockAsync`, `ReleaseLockAsync`
- `Providers.SimpleCacheProvider.SetAsync/ClearAsync`
- `Providers.InMemoryCacheProvider` compression placeholders and count/memory updates
- `Providers.MemoryCacheProvider.SetAsync` size/accounting path
- `Providers.HybridCacheProvider` parallel write/remove/refresh and shared flags
- `Providers.RedisCacheProvider` placeholder success semantics
- `DataSources.InMemoryCacheDataSource` and `CachedMemoryDataSource` duplicate logic

## Primary Outcomes
- Correct cache-hit semantics for `default(T)` values.
- Deterministic atomicity for conditional set/get-remove/lock helpers.
- Accurate statistics and memory accounting under concurrency.
- Safer hybrid and datasource integration behavior with auditable rollout gates.
