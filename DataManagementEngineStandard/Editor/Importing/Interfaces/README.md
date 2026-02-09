# Importing Interfaces

## Purpose
This folder defines contracts and support models for data-import workflows: validation, transformation, batching, progress reporting, and execution metrics.

## Key Interfaces And Models
- `IDataImportManager`: Primary import orchestration contract.
- `IDataImportValidationHelper`: Import and mapping pre-flight checks.
- `IDataImportTransformationHelper`: Field selection, mapping, and transformation pipeline.
- `IDataImportBatchHelper`: Batch sizing and retry-aware processing.
- `IDataImportProgressHelper`: Structured logging and progress telemetry.
- `DataImportConfiguration`: Import run configuration and behavior flags.
- `ImportPerformanceMetrics`, `Importlogdata`, `ImportLogLevel`: Operational metrics and logs.

## Integration Notes
- Keep import contracts transport-agnostic so they can run in UI, service, or CLI hosts.
- Preserve rich logging fields for troubleshooting long-running loads.
- Maintain compatibility with `IProgress<PassedArgs>` for progress propagation.
