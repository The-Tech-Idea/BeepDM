# Phase 1 - Contracts and Provider Lifecycle Baseline

## Objective
Establish explicit cache contracts and provider lifecycle behavior before deeper correctness refactors.

## Scope
- Clarify hit/miss contract.
- Clarify provider availability and fallback behavior.
- Normalize initialization/disposal lifecycle.

## File Targets
- `Caching/ICacheProvider.cs`
- `Caching/CacheManager.cs`
- `Caching/Providers/*.cs`

## Audited Hotspots
- `CacheManager.Initialize`, `SetProvider`, `EnsureInitialized`
- `ICacheProvider` contract shape
- provider `IsAvailable` and `Dispose` behavior

## Real Constraints to Address
- Current hit semantics are inferred from returned value equality to `default(T)`.
- Fallback write/read behavior is implicit and not mode-driven.
- Lifecycle behavior is not explicitly documented for provider replacement and reuse.

## Acceptance Criteria
- Contract doc and API semantics are explicit and testable.
- Provider initialization/fallback/disposal behavior is deterministic.
