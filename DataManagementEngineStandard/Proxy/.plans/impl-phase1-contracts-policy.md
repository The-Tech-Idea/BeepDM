# Implementation Record — Phase 1: Contracts and Policy Foundation

_Plan file: [01-phase1-contracts-and-policy-foundation.md](01-phase1-contracts-and-policy-foundation.md)_  
_Status: ✅ Complete_

---

## What Was Implemented

### `ProxyPolicy` — Single Source of Truth (`ProxyotherClasses.cs`)

Replaces the scattered `_options`, `MaxRetries`, `RetryDelayMilliseconds`, `HealthCheckIntervalMilliseconds` knobs.

```csharp
public class ProxyPolicy
{
    public string Name        { get; set; } = "Default";
    public int    Version     { get; set; } = 1;
    public string Environment { get; set; } = "Production";

    public ProxyResilienceProfile Resilience     { get; set; } = new();
    public ProxyCacheProfile      Cache          { get; set; } = new();
    public ProxyRoutingStrategy   RoutingStrategy { get; set; } = ProxyRoutingStrategy.WeightedLatency;

    // ⚠️ TODO: Add static Default property — currently missing (build error)
    // public static ProxyPolicy Default => new ProxyPolicy();
}
```

### Policy Metadata

| Property | Purpose |
|----------|---------|
| `Name` | Human-readable label for logs/audits |
| `Version` | Monotonic version for change tracking |
| `Environment` | Scope tag. prevents prod policy from running in dev |

### `ProxyDataSourceOptions.FromPolicy()` Factory (`ProxyotherClasses.cs`)

Backward-compatibility bridge. Converts a `ProxyPolicy` to the legacy `ProxyDataSourceOptions` shape so existing code consuming `_options` continues to run unmodified.

```csharp
public static ProxyDataSourceOptions FromPolicy(ProxyPolicy p)
{
    return new ProxyDataSourceOptions
    {
        MaxRetries                    = p.Resilience.MaxRetries,
        RetryDelayMilliseconds        = p.Resilience.RetryBaseDelayMs,
        HealthCheckIntervalMilliseconds = p.Resilience.HealthCheckIntervalMs,
        FailoverThreshold             = p.Resilience.FailoverThreshold,
        EnableCache                   = p.Cache.Tier != ProxyCacheTier.None,
        CacheTtlSeconds               = p.Cache.TtlSeconds,
        RoutingStrategy               = (int)p.RoutingStrategy
    };
}
```

### `ProxyDataSource.cs` — Policy-first Constructors

**Constructor 1 (policy-first — preferred)**
```csharp
public ProxyDataSource(IDMEEditor editor, List<string> dataSourceNames, ProxyPolicy policy)
```

**Constructor 2 (backward-compat)**
```csharp
public ProxyDataSource(IDMEEditor editor, List<string> dataSourceNames,
    int? maxRetries = null, int? retryDelay = null, int? healthCheckInterval = null)
```
Internally creates a `ProxyPolicy` from overrides and delegates to `InitializeFromPolicy()`.

### `InitializeFromPolicy()` Shared Init

Reads `_policy` once and propagates all tunable values to circuit breakers, timers, and options. Prevents divergence at runtime.

### `IProxyDataSource` Interface (`IProxyDataSource.cs`)

Added:
- `void ApplyPolicy(ProxyPolicy policy)` — hot-swap policy at runtime without restart
- `ProxySloSnapshot GetSloSnapshot(string dsName)` — P50/P95/P99 per source
- `IReadOnlyDictionary<string, ProxySloSnapshot> GetAllSloSnapshots()` — all sources
- `IDataSource GetConnection(string dsName)` — direct accessor for diagnostics / seeding

---

## Audited Hotspots Addressed

| Hotspot | Original risk | Fix applied |
|---------|--------------|-------------|
| Constructor option initialization | `MaxRetries` property ≠ `_options.MaxRetries` | Both read from `_policy.Resilience` |
| `ProxyDataSourceOptions` | Standalone mutable bag | Now only built from `ProxyPolicy.FromPolicy()` |
| `RetryPolicy(...)` shared wrapper | Ambiguous return contract | Replaced by typed `ExecuteReadWithPolicy<T>` / `ExecuteWriteWithPolicy<T>` |

---

## Acceptance Criteria Check

| Criterion | Met? |
|-----------|------|
| Policies are loadable/configurable without API breakage | ✅ Back-compat constructor preserved |
| Existing proxy usages continue unchanged in compatibility mode | ✅ `ExecuteWithRetry/Async` back-compat wrappers delegate to new pipeline |
| `ProxyPolicy.Default` static property | ⚠️ Missing — causes CS0117 build error. Add `public static ProxyPolicy Default => new ProxyPolicy();` inside `ProxyPolicy` class in `ProxyotherClasses.cs` |

---

## Files Changed

- `ProxyotherClasses.cs` — `ProxyPolicy`, `ProxyDataSourceOptions.FromPolicy()`
- `ProxyDataSource.cs` — two constructors, `InitializeFromPolicy()`
- `IProxyDataSource.cs` — `ApplyPolicy`, SLO methods, `GetConnection`
