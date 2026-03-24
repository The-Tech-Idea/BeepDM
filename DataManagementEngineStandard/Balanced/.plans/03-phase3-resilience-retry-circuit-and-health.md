# Phase 3 - Resilience: Retry, Circuit Breaker, and Health

## Objective
Add enterprise resilience behavior for transient failure handling and safe failover.

## Scope
- Retry classification.
- Circuit breaker state management.
- Active health checks and recovery logic.

## File Targets (planned)
- `Balanced/BalancedDataSource.cs`
- `Balanced/Resilience/CircuitBreaker.cs`
- `Balanced/Resilience/HealthMonitor.cs`

## Planned Enhancements
- Error taxonomy (transient, persistent, auth, timeout, saturation).
- Retry with backoff profiles by operation class.
- Circuit states:
  - closed
  - open
  - half-open
- Health probes with bounded timeout and degradation threshold.
- Source refresh and reintegration flow:
  - when a datasource/connection fails, mark source unhealthy and open circuit
  - continue serving from healthy peers
  - periodic background health probes attempt reconnect
  - move source to half-open after N successful probes (or cooldown expiry)
  - allow limited trial traffic in half-open
  - on trial success, close circuit and restore normal routing weight
  - on trial failure, reopen circuit and extend backoff window
- Connection pool refresh behavior:
  - purge stale/broken pooled connections for failed source
  - rehydrate pool after source returns healthy
  - prevent poisoned connection reuse during recovery.

## Acceptance Criteria
- Failures trigger policy-driven retries/failover.
- Unhealthy sources are isolated and recover gracefully when healthy.
- Recovered sources rejoin routing automatically without service restart.
- Recovery transitions (`open` -> `half-open` -> `closed`) are logged and measurable.
