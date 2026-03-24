# Phase 6 - Performance, Indexing, and Pushdown

## Objective
Improve query performance through projection/filter pushdown and lightweight indexing strategies.

## Scope
- Predicate and projection pushdown where parser supports it.
- Optional file-side indexing for repeated queries.
- Caching policy for inferred schema and metadata.

## File Targets
- `FileManager/CSVDataSource.cs`
- `FileManager/CSVAnalyser.cs`
- `FileManager/CSVTypeMapper.cs`

## Planned Enhancements
- Fix header-to-field resolution so filter/pushdown uses stable indexes and avoids default-key fallbacks.
- Cache resolved column maps and entity metadata with file timestamp invalidation.
- Add optional lightweight value index metadata for repeated equality/range filters.

## Audited Hotspots
- `CSVDataSource.GetEntity(...)` / paged `GetEntity(...)` header mapping via `FirstOrDefault(...).Key`
- `CSVDataSource.GetEntityStructure(...)` repeated reload patterns
- `CSVDataSource.DetectDelimiter(...)` and metadata recalculation behavior

## Real Constraints to Address
- `FirstOrDefault(...).Key` can resolve to `0` for missing mapping and produce false positives.
- Repeated type/field resolution and reflection per row adds avoidable overhead.
- No explicit cache invalidation contract tied to file changes.

## Acceptance Criteria
- Repeated query workloads show measurable latency gains.
- Pushdown behavior is correct and test-covered.
- Cache/index lifecycle is bounded and invalidation-safe.
