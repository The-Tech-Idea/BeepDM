# Docs

Static HTML documentation site for DataManagementEngineStandard.

## Main Assets
- `index.html`: documentation landing page.
- `navigation.html`, `navigation.js`: sidebar/navigation model.
- `sphinx-style.css`: documentation styling.
- Topic pages such as `configeditor.html`, `dmeeditor.html`, `defaultsmanager.html`, `formsmanager.html`, `dataimportmanager.html`, `mappingmanager.html`, and `migrationmanager.html`.
- Dedicated runtime pages: `rules-engine-parser.html` and `services-registration-lifetimes.html`.
- Helpers runtime page: `helpers-runtime-reference.html`.
- ETL internals pages: `etl-engine-pipeline.html`, `etl-engine-scheduling.html`, and `etl-engine-observability.html`.
- Assembly loading/NuGet/extensions page: `assemblyhandler-loading-nuget-extensions.html`.
- Split examples pages: `examples-editor-classes.html` and `examples-runtime-surfaces.html` (while `examples.html` remains as aggregate reference).
- `filemanager-reader-host.html`: FileDataSource reader discovery and runtime switching guide (type, implementation, and reader ID selection).
- `dataviewdatasource.html`: signature-verified DataViewDataSource API page.
- `caching-api.html`: signature-verified ICacheProvider and CacheManager API page.
- `proxy-datasource.html`: signature-verified IProxyDataSource and IProxyCluster API page.
- `distributed-datasource.html`: shard-aware DistributedDataSource guide with clear construction, routing, events, and Phase 14 backpressure/capacity examples.
- `technical-folder-guides.html`: index page for deep, folder-level technical guides under `DataManagementEngineStandard`.

## Maintenance Guidance
- Keep examples aligned with current API names and persisted file names from `ConfigUtil` and `Editor`.
- Prefer linking to concrete source files when describing behavior.

## Related
- `NAVIGATION_README.md`
- `../README.md`
- `../FOLDER_REFERENCE.md`

## 2026 Refresh Notes
- Added cross-links from docs site into hand-written module guides in `Editor`, `Helpers`, `ConfigUtil`, `WebAPI`, `Caching`, `Json`, and `Tools`.
- Replaced stale landing-page links that pointed to missing pages.

