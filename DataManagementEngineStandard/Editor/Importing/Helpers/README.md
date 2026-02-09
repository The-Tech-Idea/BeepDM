# Importing Helpers

## Purpose
This folder contains the concrete helper pipeline used by data-import managers: validation, transformation, batching/retry, and progress logging.

## Key Files
- `DataImportValidationHelper.cs`: Validates config, mappings, data-source compatibility, and entity structures.
- `DataImportTransformationHelper.cs`: Applies field filtering, entity mapping, default values, and custom transforms.
- `DataImportBatchHelper.cs`: Calculates optimal batch sizes and executes batch processing with retry.
- `DataImportProgressHelper.cs`: Progress reporting, log capture, export, and summary generation.

## Runtime Flow
1. Validate configuration and source/target compatibility.
2. Transform records through filtering, mapping, and default application.
3. Split data into batches and process with retry policy.
4. Emit progress and structured logs for monitoring and diagnostics.

## Extension Guidelines
- Keep transformation steps deterministic and composable.
- Ensure retry logic is idempotent for partially processed batches.
- Include enough context in logs to isolate failing records quickly.

## Testing Focus
- Batch-size calculation under memory constraints.
- Retry behavior on transient failures.
- Validation failures for incompatible entity maps.
