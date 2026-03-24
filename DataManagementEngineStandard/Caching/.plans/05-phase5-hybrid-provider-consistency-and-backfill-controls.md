# Phase 5 - Hybrid Provider Consistency and Backfill Controls

## Objective
Harden hybrid L1/L2 behavior so reads/writes/backfills are consistent and bounded.

## Scope
- Define consistency modes (write-through, write-around, read-through).
- Harden backfill behavior and cancellation/timeout boundaries.
- Clarify reconciliation when one tier fails.

## File Targets
- `Caching/Providers/HybridCacheProvider.cs`
- `Caching/CacheManager.cs`

## Audited Hotspots
- `HybridCacheProvider.GetAsync` L2-to-L1 backfill task
- `HybridCacheProvider.GetManyAsync` backfill path
- `HybridCacheProvider.SetManyAsync` success aggregation

## Real Constraints to Address
- Fire-and-forget backfills have no bounded telemetry or policy controls.
- Success count chooses max across providers, which can hide consistency gaps.
- Tier failure behavior is not explicitly surfaced to callers.

## Acceptance Criteria
- Hybrid mode behavior is policy-driven and test-covered.
- Backfill/reconciliation outcomes are observable and deterministic.
