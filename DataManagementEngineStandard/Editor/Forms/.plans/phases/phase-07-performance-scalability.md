# Phase 07 — Performance & Scalability

**Status:** Complete (`15 / 15` in [todo-tracker.md](../todo-tracker.md))  
**Priority:** Medium  
**Depends on:** Phase 01 stable block registration and commit flows

---

## Objective

Scale FormsManager for large datasets, paging, lazy loading, and cache-aware navigation while preserving UoW correctness.

## Primary Implementation Seams

- Paging manager and paging APIs
- `DataBlockInfo` paging/lazy-load state
- Performance manager and cache statistics
- Query streaming and fetch controls

## Enhance / Update / Fix Rules

- Performance optimizations must never change business semantics. Paging and caching are delivery strategies, not alternate truth sources.
- The active UoW still owns current record, dirty state, and commit behavior even when the UI is showing only a page/window.
- Lazy loading should be explicit in `DataBlockInfo`, not hidden in helper-side heuristics.

## UoW and Primary-Key Rules

- Cache invalidation should key off stable PK values when available.
- New unsaved records need a deterministic temporary correlation key in memory until a real PK is assigned on insert/commit.
- Sequence values must not be allocated by paging, prefetch, warm-up, or lazy-load code.
- Identity-generated PK refresh after insert must invalidate or update page caches that still hold the pre-insert placeholder row.
- Master/detail virtual loading must use resolved FK mappings and committed/stable parent keys before prefetching detail pages.

## Done / Verify Checklist

- Paging APIs load and navigate pages without losing current-record semantics.
- Lazy-load settings live on block metadata and can be reasoned about per block.
- Cache hit/miss, TTL, eviction, and invalidation stay observable.
- Insert/commit flows reconcile cached rows correctly when PKs change from temporary to final values.

## Maintenance Notes

- Any performance change that touches current-record materialization should be reviewed together with audit, lock, and master/detail synchronization behavior.