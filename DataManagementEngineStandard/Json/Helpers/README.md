# Json Helpers

## Purpose
This folder contains the JSON engine internals used by JSON-backed data sources: CRUD operations, filter compilation, graph hydration, schema synchronization, and async paging.

## Key Files
- `JsonDataHelper.cs` and `JsonCrudHelper.cs`: Query, paging, insert, update, and delete helpers.
- `JsonFilterHelper.cs` and `JsonPathNavigator.cs`: Filter predicate compilation and JSON path resolution.
- `JsonGraphHelper.cs`, `JsonRelationHelper.cs`, `JsonDeepEntityResolver.cs`: Parent/child traversal and graph materialization.
- `JsonSchemaHelper.cs`, `JsonSchemaPersistenceHelper.cs`, `JsonSchemaSyncHelper.cs`: Metadata reconciliation and persistence.
- `JsonCacheManager.cs`: Compiled predicate/property-map/entity cache reuse.
- `JsonAsyncDataHelper.cs`: Async streaming and page extraction.
- `GraphHydrationOptions.cs`: Graph loading behavior controls.

## Runtime Flow
1. Compile filters and resolve the target array/token path.
2. Materialize records or full graphs based on entity metadata.
3. Apply CRUD changes and sync schema metadata when field drift is detected.
4. Reuse cached predicates and reflection maps to reduce overhead.

## Extension Guidelines
- Invalidate cache entries (`JsonCacheManager`) whenever schema-affecting behavior changes.
- Keep filter semantics identical between sync and async paths.
- Protect graph traversal from cycles and oversized object graphs.

## Testing Focus
- Nested relation loading with mixed parent/child cardinalities.
- Schema drift detection and primary-key integrity checks.
- Predicate compilation parity with `AppFilter` behavior.
