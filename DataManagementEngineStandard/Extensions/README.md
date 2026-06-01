# Extensions

Extension methods and entity comparison helpers used throughout the engine.

## Key Files
- `BatchExtensions.cs`: Batch-processing extensions (Partition, ForEachAsync, TrySelect)
- `EntitiesExtensions.cs`: Entity-level helpers (property copying, comparison)
- `EntitiesExtensions.Additions.cs`: Additional entity operations (diff, merge, deep clone)

## How It Fits
Provides reusable extension-based utilities consumed by editor, helper, and datasource layers. Batch extensions are used by DataImportManager, ETL operations, and sync workflows for efficient bulk processing.

## Related Documentation
- [DataImportManager Help](../Help/dataimportmanager.html)
- [ETL Editor Help](../Help/etleditor.html)
