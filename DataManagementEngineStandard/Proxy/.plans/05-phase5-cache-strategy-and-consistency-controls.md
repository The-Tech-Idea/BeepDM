# Phase 5 - Cache Strategy and Consistency Controls

## Balanced Alignment
This phase is aligned with [Balanced -> Proxy adoption mapping](../../Balanced/.plans/proxydatasource-adoption-mapping.md).

## Objective
Upgrade proxy caching to an enterprise strategy with explicit consistency and invalidation policies.

## Scope
- Cache policy by operation/entity.
- Invalidation, TTL, and staleness controls.

## File Targets
- `Proxy/ProxyDataSource.cs`
- `Proxy/ProxyotherClasses.cs`

## Planned Enhancements
- Cache tiers:
  - per-request bypass
  - short-lived query cache
  - entity-level cache profiles
- Consistency controls:
  - write-through invalidation
  - stale-while-revalidate option
  - cache miss fallback behavior
- Cache safety:
  - max item/size controls
  - eviction policy and observability metrics.

## Audited Hotspots
- `ProxyDataSource.GetEntityWithCache(...)`
- `ProxyDataSource.GenerateCacheKey(...)`
- `ProxyDataSource.InvalidateCache(...)`

## Real Constraints to Address
- Cache key currently ignores some filter dimensions (`FilterValue1`, operator normalization nuances), creating collision risk.
- No explicit stale policy under failover paths; cached results can outlive backend health transitions.
- No bounded cache size/eviction policy other than TTL.

## Acceptance Criteria
- Cache behavior is predictable by policy.
- Writes correctly invalidate or bypass stale entries.
