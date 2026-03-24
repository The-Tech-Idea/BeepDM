# Implementation Record — Phase 3: Advanced Routing and Load Distribution

_Plan file: [03-phase3-advanced-routing-and-load-distribution.md](03-phase3-advanced-routing-and-load-distribution.md)_  
_Status: ✅ Complete_  
_File: `ProxyDataSource.Routing.cs`_

---

## What Was Implemented

### `ProxyRoutingStrategy` enum (`ProxyotherClasses.cs`)

```csharp
public enum ProxyRoutingStrategy
{
    WeightedLatency,           // default — favors lowest average latency
    LeastOutstandingRequests,  // routes to fewest in-flight requests
    RoundRobin,                // simple sequential rotation
    HealthWeighted             // weight by healthy/unhealthy history score
}
```

---

### Candidate Selection Pipeline (`ProxyDataSource.Routing.cs`)

#### `SelectCandidates()` — entry point

```csharp
private IReadOnlyList<string> SelectCandidates()
{
    var healthy = _dataSourceNames
        .Where(n => IsHealthy(n) && !IsCircuitOpen(n))
        .ToList();

    if (healthy.Count == 0)
        healthy = _dataSourceNames.ToList();  // degraded-mode fallback

    return _policy.RoutingStrategy switch
    {
        ProxyRoutingStrategy.RoundRobin               => SelectRoundRobin(healthy),
        ProxyRoutingStrategy.LeastOutstandingRequests => SelectLeastOutstanding(healthy),
        ProxyRoutingStrategy.HealthWeighted           => SelectHealthWeighted(healthy),
        _                                             => SelectWeightedLatency(healthy)
    };
}
```

**Key behaviors:**
- Only considers sources where `IsHealthy(n) == true` AND circuit is not Open.
- Degraded-mode fallback: if NO source is healthy, includes all sources (prevents full blackout).
- Strategy dispatch is driven entirely by `_policy.RoutingStrategy`.

---

### Four Routing Strategies

#### 1. `SelectWeightedLatency` (default)
Orders candidates by `_metrics[n].AverageResponseTime ASC`. Sources with no measured latency (new or reset) sort first (assumed "unknown is fast").

#### 2. `SelectRoundRobin`
Uses `Interlocked.Increment` on a per-strategy counter modulo the candidate count. Stable rotation without `new Random()`.

#### 3. `SelectLeastOutstanding`
Orders by `_outstandingRequests[n] ASC`. Outstanding count incremented at start of execution, decremented on either success or failure, so the count reflects true in-flight requests.

#### 4. `SelectHealthWeighted`
Score formula: `healthyChecks / (healthyChecks + unhealthyChecks + 1)`. Higher score = more reliable source = preferred. Uses `_consecutiveHealthy`/`_consecutiveUnhealthy` counters maintained by `PerformHealthCheck`.

---

### Health Check with Hysteresis (`PerformHealthCheck`)

Prevents rapid oscillation (flapping) when a backend alternates between healthy and unhealthy.

```csharp
private void PerformHealthCheck(string dsName)
{
    bool alive = IsDataSourceHealthy(dsName);

    if (alive)
    {
        _consecutiveHealthy[dsName]   = _consecutiveHealthy.GetValueOrDefault(dsName) + 1;
        _consecutiveUnhealthy[dsName] = 0;

        // Only flip to healthy after N consecutive healthy checks
        if (_consecutiveHealthy[dsName] >= _options.HealthyThreshold)
            _healthStatus[dsName] = true;
    }
    else
    {
        _consecutiveUnhealthy[dsName] = _consecutiveUnhealthy.GetValueOrDefault(dsName) + 1;
        _consecutiveHealthy[dsName]   = 0;

        // Only flip to unhealthy after N consecutive unhealthy checks
        if (_consecutiveUnhealthy[dsName] >= _options.UnhealthyThreshold)
            _healthStatus[dsName] = false;
    }
}
```

Default thresholds: `HealthyThreshold = 2`, `UnhealthyThreshold = 2`.

---

### `RecordSuccess` / `RecordFailure` (`ProxyDataSource.Routing.cs`)

#### `RecordSuccess(string dsName, TimeSpan elapsed)`
- Updates running latency average: `avg = (avg * (n-1) + new) / n`
- Resets circuit breaker: `_circuitBreakers[dsName].RecordSuccess()`
- Decrements `_outstandingRequests[dsName]`

#### `RecordFailure(string dsName, ProxyErrorSeverity severity)`
- Calls `_circuitBreakers[dsName].RecordFailure(severity)` — severity-weighted
- Increments metrics failed count
- Decrements `_outstandingRequests[dsName]`

---

### `Failover()` (`ProxyDataSource.Routing.cs`)

Walk `_dataSourceNames` in order; return the first source that is both `IsHealthy` and has an open (non-circuit-tripped) state. Raises `OnFailover` with reason. Falls through gracefully if all sources are degraded.

---

### Bugs Fixed

| Bug | Original code | Fix |
|-----|--------------|-----|
| `new Random()` per call | `GetNextBalancedDataSource` created `Random` each invocation → same seed → biased distribution | Replaced with `ThreadLocal<Random>` in ExecutionHelpers; routing uses stable `Interlocked` counter |
| `_healthStatus` thread safety | Plain `Dictionary<string,bool>` mutated by timer + request threads | Replaced with `ConcurrentDictionary<string,bool>` |
| No degraded-mode fallback | All candidates unhealthy → empty list → `NullReferenceException` | `if (healthy.Count == 0) healthy = _dataSourceNames.ToList()` |

---

## Acceptance Criteria Check

| Criterion | Met? |
|-----------|------|
| Route selection configurable per operation profile | ✅ Via `_policy.RoutingStrategy`; hot-swappable via `ApplyPolicy()` |
| Routing decisions are explainable | ✅ Correlation ID in every log line; `ProxyAttemptRecord` records each attempt |
| Health state concurrent-safe | ✅ `ConcurrentDictionary` |
| Hysteresis prevents oscillation | ✅ Consecutive-check counters in `PerformHealthCheck` |

---

## Files Changed

- `ProxyotherClasses.cs` — `ProxyRoutingStrategy` enum
- `ProxyDataSource.Routing.cs` — complete rewrite: `SelectCandidates`, 4 strategy methods, hysteresis health, `RecordSuccess`/`RecordFailure`, `Failover`
