# Phase 7 - Datasource Integration and Schema Consistency

## Objective
Align cache-backed datasources with shared helpers, deterministic CRUD, and consistent schema handling.

## Scope
- Remove duplicated datasource logic.
- Standardize key generation, schema inference, and filter semantics.
- Make provider write-through behavior explicit and reliable.

## File Targets
- `Caching/DataSources/InMemoryCacheDataSource.cs`
- `Caching/DataSources/CachedMemoryDataSource.cs`
- `Caching/DataSources/MemoryCacheConnection.cs`

## Audited Hotspots
- `InsertEntity`, `UpdateEntity`, `DeleteEntity` in both datasource classes
- `GenerateEntityKey`, `CreateEntityStructureFromData`, `AutoDiscoverEntityStructure`
- fire-and-forget `_cacheProvider.SetAsync/RemoveAsync` calls

## Real Constraints to Address
- Datasource classes are near-duplicate and prone to drift.
- Async provider writes are not awaited, so consistency can lag silently.
- `MemoryCacheConnection` has `NotImplementedException` members that can leak into runtime.

## Acceptance Criteria
- Shared datasource core eliminates duplication drift.
- CRUD consistency behavior is deterministic and documented.
