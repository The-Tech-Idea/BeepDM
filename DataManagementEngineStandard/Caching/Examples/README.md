# Caching Examples

## Purpose
This folder contains executable examples that demonstrate how to wire cache connections, cache managers, and cache data sources into BeepDM workflows.

## Key Files
- `CacheConnectionHelperExample.cs`: Connection initialization and helper usage examples.
- `CacheManagerExamples.cs`: End-to-end cache-manager scenarios.
- `CacheMemoryDataSourceExample.cs`: Query and metadata examples against cache data sources.

## How To Use
1. Start with connection setup from `CacheConnectionHelperExample`.
2. Reuse cache-manager patterns for lifecycle and invalidation.
3. Run the data-source example to validate filter and paging behavior.

## Extension Guidelines
- Keep examples aligned with current public APIs before copying into docs or tests.
- Use examples as regression references when cache contracts change.
- Prefer deterministic sample data so outputs stay stable in CI.
