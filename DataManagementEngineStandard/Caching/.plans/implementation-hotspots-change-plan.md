# Caching Implementation Hotspots Change Plan

This document lists exact planned code changes from the audited Caching hotspots.

## 1) `CacheManager.GetOrCreateAsync/GetAsync` default-value miss ambiguity

### Current risk
- Values equal to `default(T)` (for example `0`, `false`, empty structs) can be misclassified as cache misses.

### Exact change
- Introduce explicit get result model (`found + value`) internally.
- Refactor manager logic to use explicit hit state, not value equality.

## 2) `SetIfNotExistsAsync` check-then-set race

### Current risk
- Exists-check followed by set is not atomic under concurrent calls.

### Exact change
- Add atomic conditional-set contract in provider layer.
- Update manager extension to use single atomic operation.

## 3) `GetAndRemoveAsync` non-atomic read/remove

### Current risk
- Value can change between get and remove.

### Exact change
- Add provider-level atomic get-and-remove where available.
- Use lock-guarded fallback where provider lacks native support.

## 4) Lock helpers (`TryAcquireLockAsync`, `ReleaseLockAsync`) race windows

### Current risk
- Lock acquire/release are built from non-atomic primitives.

### Exact change
- Implement atomic compare-and-set semantics for lock keys.
- Add ownership-safe release with compare-and-delete semantics.

## 5) `SimpleCacheProvider.SetAsync` overwrite accounting drift

### Current risk
- Item/memory counters can overcount on overwrite path.

### Exact change
- Distinguish add vs replace explicitly.
- Recompute memory deltas on replacement instead of unconditional increment.

## 6) `MemoryCacheProvider.SetAsync` `wasAdded` ambiguity

### Current risk
- Add/update accounting logic is ambiguous and can drift under concurrency.

### Exact change
- Replace with deterministic add-or-update result path.
- Centralize size/counter updates in one guarded method.

## 7) `HybridCacheProvider` shared mutable success flags

### Current risk
- Parallel task writes to shared booleans can race.

### Exact change
- Aggregate per-task results as immutable values (`Task<bool>` / `Task<long>`).
- Combine outcomes after `WhenAll`.

## 8) `InMemoryCacheProvider` compression placeholders

### Current risk
- Compression is configurable but methods are effectively no-op placeholders.

### Exact change
- Implement real compression/decompression path.
- Add format marker/version handling for backward compatibility.

## 9) `RedisCacheProvider` placeholder success semantics

### Current risk
- Provider currently simulates operations and may report misleading success.

### Exact change
- Add explicit capability/state guard so placeholder cannot be treated production-ready.
- Implement real backend path or hard-disable non-implemented flows.

## 10) `InMemoryCacheDataSource` and `CachedMemoryDataSource` duplication

### Current risk
- Large duplicated logic surface invites drift and inconsistent behavior.

### Exact change
- Extract shared base/helper for CRUD/filter/schema/key-generation logic.
- Keep provider-specific policy in thin wrappers only.

## 11) Fire-and-forget provider writes in datasources

### Current risk
- `_ = _cacheProvider.SetAsync/RemoveAsync` hides failures and consistency lag.

### Exact change
- Introduce explicit consistency mode:
  - awaited write-through
  - best-effort async with durable diagnostics
- Emit operation outcome telemetry for provider write path.

## 12) `MemoryCacheConnection` not implemented members

### Current risk
- `NotImplementedException` members can surface at runtime through interface usage.

### Exact change
- Implement safe minimal behavior for required members.
- Add guardrails/documented unsupported features where needed.
