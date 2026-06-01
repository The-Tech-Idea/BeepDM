# Search Helper

## Purpose
Helper logic for search-engine style providers including ElasticSearch. Translates BeepDM query semantics to search-engine query DSL with full-text, faceted search, and relevance ranking.

## Key Files
- `SearchEngineHelper.cs`: Search helper implementing `IDataSourceHelper` for query translation, ranking, and capability reporting.

## Features
- Full-text search query generation
- Faceted/filtered search
- Result highlighting
- Relevance ranking normalization
- Capability signaling: `SupportsFullTextSearch`, `SupportsFacetedSearch`, `SupportsHighlighting`

## Usage Notes
- Distinguish full-text semantics from exact-match relational filtering
- Normalize ranking and relevance behavior for caller expectations
- Map provider-specific search errors into standard BeepDM error contracts

## Related Documentation
- [Datasource Types Reference](../../../Help/datasource-types-reference.html)
