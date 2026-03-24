# Phase 4 - Retry, Idempotency, and Failover Semantics

## Balanced Alignment
This phase is aligned with [Balanced -> Proxy adoption mapping](../../Balanced/.plans/proxydatasource-adoption-mapping.md).

## Objective
Formalize retry and failover semantics with idempotency-aware safeguards.

## Scope
- Distinguish read/write retry behavior.
- Failover strategy based on operation safety.

## File Targets
- `Proxy/ProxyDataSource.cs`
- `Proxy/CircuitBreaker.cs`

## Planned Enhancements
- Idempotency policy:
  - safe retries for read-only operations
  - guarded retries for writes
  - optional idempotency keys for critical writes
- Failover policy:
  - immediate failover classes
  - retry-before-failover classes
- Clear terminal failure diagnostics with attempted path history.

## Audited Hotspots
- `ProxyDataSource.RunQuery(...)`
- `ProxyDataSource.ExecuteSql(...)`
- `ProxyDataSource.CreateEntityAs(...)`
- `ProxyDataSource.UpdateEntity(...)` / `DeleteEntity(...)` / `InsertEntity(...)`

## Real Constraints to Address
- Multiple wrappers execute operations once in retry probe and again after success, duplicating side effects.
- Current failover/retry wrappers often lose attempted-route history and error classification.
- Read/write safety boundaries are not explicitly enforced by retry wrapper APIs.

## Acceptance Criteria
- Retry behavior is deterministic and operation-aware.
- Write operations avoid unsafe duplicate side effects.
