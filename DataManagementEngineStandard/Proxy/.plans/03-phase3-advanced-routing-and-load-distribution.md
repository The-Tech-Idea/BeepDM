# Phase 3 - Advanced Routing and Load Distribution

## Balanced Alignment
This phase is aligned with [Balanced -> Proxy adoption mapping](../../Balanced/.plans/proxydatasource-adoption-mapping.md).

## Objective
Enhance routing from basic weighted selection to policy-aware operation/entity/workload routing.

## Scope
- Route selection strategies.
- Policy hooks for workload and operation type.

## File Targets
- `Proxy/ProxyDataSource.cs`

## Planned Enhancements
- Routing policies by:
  - read vs write
  - entity
  - workload class
  - tenant/context (if available)
- Strategy options:
  - weighted latency-aware (existing+)
  - least outstanding requests
  - sticky session/key hash
- Health/routing hysteresis to prevent rapid oscillation.

## Audited Hotspots
- `ProxyDataSource.GetNextBalancedDataSource()`
- `ProxyDataSource.ExecuteWithLoadBalancing(...)`
- `ProxyDataSource.IsHealthy(...)` / `PerformHealthCheck(...)`

## Real Constraints to Address
- Health state is stored in non-thread-safe dictionary and read under concurrent request load.
- Routing candidate list can fall back to all sources without explicit degraded-mode policy.
- Weighted selection currently creates `new Random()` per call and can cause unstable distribution under high concurrency.

## Acceptance Criteria
- Route selection can be configured per operation profile.
- Routing decisions are explainable and traceable.
