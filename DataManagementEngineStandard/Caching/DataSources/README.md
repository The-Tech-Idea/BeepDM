# Caching DataSources

## Purpose
This folder provides `IDataSource` implementations that operate against in-process caches instead of external databases. These classes let BeepDM use a cache tier through the same data-source contract used by SQL, file, and API providers.

## Key Files
- `CachedMemoryDataSource.cs`: Cache-backed `IDataSource` wrapper with entity lookup, filtering, paging, and async query entry points.
- `InMemoryCacheDataSource.cs`: In-memory variant with a similar surface for test and local workloads.
- `MemoryCacheConnection.cs`: Connection lifecycle adapter (`OpenConnection`, `CloseConn`) for cache data sources.

## Runtime Flow
1. `DMEEditor` resolves the connection and opens `MemoryCacheConnection`.
2. The cache data source serves `GetEntity`, `GetEntityAsync`, and `RunQuery` against cached entities.
3. Metadata APIs (`GetEntitesList`, `GetEntityStructure`) keep behavior aligned with the broader BeepDM contracts.

## Extension Guidelines
- Keep `IDataSource` semantics aligned with other providers, especially error handling and `IErrorsInfo` reporting.
- Preserve filter and paging behavior so callers can switch providers without changing query code.
- Implement async methods as true async paths when I/O is introduced.

## Testing Focus
- Verify parity between sync and async entity retrieval.
- Validate entity-structure resolution for cached and non-cached entities.
- Validate connection open/close state transitions.
