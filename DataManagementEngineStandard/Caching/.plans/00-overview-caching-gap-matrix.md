# 00 - Overview: Caching Gap Matrix

## Objective
Baseline current caching behavior and define code-grounded phases for correctness, safety, and production operations.

## In Scope
- `Caching/CacheManager.cs`
- `Caching/CacheManager.Extensions.cs`
- `Caching/CacheConfiguration.cs`
- `Caching/ICacheProvider.cs`
- `Caching/Providers/*.cs`
- `Caching/DataSources/*.cs`

## Gap Matrix

| Capability | Current State | Gap | Target |
|---|---|---|---|
| Core value semantics | Functional cache wrapper exists | `default(T)` is treated as cache miss in multiple flows | Explicit hit/miss contract independent of value content |
| Atomic operations | Conditional/lock helpers exist | `SetIfNotExists` and `GetAndRemove` are check-then-act, not atomic | Provider-level atomic APIs or manager-level locking |
| Provider consistency | Multi-provider orchestration exists | Primary/fallback write and clear semantics can diverge without reconciliation | Defined consistency modes and deterministic behavior |
| Statistics integrity | Stats model exists | Item/memory counters can drift (replace path, clear path, race paths) | Accurate monotonic counters and verified accounting |
| Compression/serialization | Configurable flags exist | Compression methods are placeholders in `InMemoryCacheProvider` | Real compression pipeline with compatibility/versioning |
| Hybrid correctness | L1/L2 strategy exists | Shared mutable success flags in parallel tasks can race | Race-safe aggregation and deterministic outcome rules |
| Redis readiness | Redis provider scaffold exists | Provider is placeholder (`_isConnected=false`, simulated success paths) | Production-ready implementation or explicit non-prod gate |
| Datasource integration | In-memory and cached datasources exist | Large duplicate code surface and fire-and-forget writes | Shared helper core + reliable async completion policy |
| Observability and health | Health check/status API exists | Missing structured operation diagnostics and SLO-ready metrics | Unified telemetry envelope and alert-friendly metrics |
| Rollout safety | No formal gates | No KPI promotion policy | Staged rollout with hard-stop and rollback playbook |

## Concrete Constraints from Audit
- `GetOrCreateAsync`/`GetAsync` rely on `EqualityComparer<T>.Default.Equals(value, default(T))`.
- `SetIfNotExistsAsync` uses `ExistsAsync` then `SetAsync` (race window).
- `GetAndRemoveAsync` is non-atomic.
- `TryAcquireLockAsync` relies on non-atomic conditional set.
- `MemoryCacheProvider.SetAsync` has ambiguous `wasAdded` accounting logic.
- `SimpleCacheProvider` increments item count on overwrite path.
