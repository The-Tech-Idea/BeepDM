# Phase 2 - Resilience Profiles and Error Taxonomy

## Balanced Alignment
This phase is aligned with [Balanced -> Proxy adoption mapping](../../Balanced/.plans/proxydatasource-adoption-mapping.md).

## Objective
Introduce robust resilience profiles and standardized error taxonomy for consistent proxy behavior.

## Scope
- Error classification (transient, persistent, auth, timeout, saturation).
- Resilience profile presets by environment/workload.

## File Targets
- `Proxy/ProxyDataSource.cs`
- `Proxy/CircuitBreaker.cs`

## Planned Enhancements
- Error taxonomy model driving:
  - retry eligibility
  - circuit increment severity
  - failover decision
- Resilience profiles:
  - conservative
  - balanced
  - aggressive failover
- Profile-specific thresholds:
  - retries
  - backoff
  - failure threshold
  - reset timeout

## Audited Hotspots
- `ProxyDataSource.ShouldRetry(...)`
- `ProxyDataSource.ExecuteWithPolicy(...)`
- `CircuitBreaker.RecordFailure()/CanExecute()`

## Real Constraints to Address
- Retry classification is currently narrow (`TimeoutException`/`IOException`) and not operation-aware.
- `_options` profile fields used by `ExecuteWithPolicy` can diverge from externally set retry properties.
- Circuit decisions need explicit mapping to error taxonomy and operation type.

## Acceptance Criteria
- Retry/circuit/failover decisions are driven by explicit error classes.
- Profiles are switchable per environment and testable.
