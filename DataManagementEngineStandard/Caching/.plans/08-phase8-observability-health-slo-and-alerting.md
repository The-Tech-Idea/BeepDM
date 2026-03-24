# Phase 8 - Observability, Health, SLO, and Alerting

## Objective
Expose consistent telemetry for cache correctness, performance, and provider health.

## Scope
- Structured operation telemetry.
- Health-check contract hardening.
- SLO metrics and alert thresholds.

## File Targets
- `Caching/CacheManager.cs`
- `Caching/CacheManager.Extensions.cs`
- provider `Statistics` surfaces

## Audited Hotspots
- `CacheManager.CheckHealthAsync` and provider test round-trip path
- `GetStatistics` / `CacheManagerStatistics`
- broad swallow-catch blocks in manager/provider paths

## Real Constraints to Address
- Many exception paths are swallowed without structured diagnostics.
- Stats surfaces are present but operation-level traces are missing.
- Health checks can pass while functional behavior is still degraded.

## Acceptance Criteria
- Cache operation outcomes and failures are traceable.
- Health and SLO alerts are actionable and test-verified.
