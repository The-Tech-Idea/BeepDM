# Importing

Data import subsystem built around `DataImportManager`.

## Main Type
- `DataImportManager` (partial class: core + enhanced API)

## Helper-Based Architecture
- `IDataImportValidationHelper`
- `IDataImportTransformationHelper`
- `IDataImportBatchHelper`
- `IDataImportProgressHelper`

These helpers are instantiated and exposed by `DataImportManager` to keep validation, transformation, batch processing, and progress concerns isolated.

## APIs
Backward-compatible path:
- Set legacy properties (`SourceEntityName`, `DestEntityName`, etc.)
- `LoadDestEntityStructure(...)`
- `RunImportAsync(progress, token, transformation, batchSize)`

Enhanced path:
- `CreateImportConfiguration(...)`
- `SetImportConfiguration(...)`
- `RunImportAsync(DataImportConfiguration, progress, token)`

## Pipeline
1. Validate config and datasource state.
2. Load/init source and destination structures.
3. Optionally validate mapping.
4. Fetch source data.
5. Split and process batches.
6. Apply transforms/defaults and write destination records.
7. Report progress and log import events.

## Integration Notes
- Defaults integration uses `DefaultsManager` when `ApplyDefaults = true`.
- Batch size can be explicit or optimized by helper logic.
- See `README_DataImportManager.md` for extended examples.
