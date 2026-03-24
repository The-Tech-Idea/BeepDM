# Phase 2 - Correctness: Default/Null and Atomicity Semantics

## Objective
Eliminate correctness bugs caused by ambiguous miss detection and non-atomic helper operations.

## Scope
- Replace `default(T)`-based miss detection with explicit hit metadata.
- Add atomic semantics for conditional set/get-remove/lock workflows.

## File Targets
- `Caching/CacheManager.cs`
- `Caching/CacheManager.Extensions.cs`
- `Caching/ICacheProvider.cs`

## Audited Hotspots
- `CacheManager.GetOrCreateAsync`, `GetAsync`, `GetManyAsync`
- `CacheManager.Extensions.SetIfNotExistsAsync`
- `CacheManager.Extensions.GetAndRemoveAsync`
- `CacheManager.Extensions.TryAcquireLockAsync` / `ReleaseLockAsync`

## Real Constraints to Address
- Cache entries with valid `default(T)` values are treated as misses.
- Lock helper relies on non-atomic check-then-set behavior.
- `ReleaseLockAsync` compare-then-remove is not atomic under contention.

## Acceptance Criteria
- Hit/miss is represented explicitly (value-independent).
- Atomic helper operations are race-safe under parallel tests.
