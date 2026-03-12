---
name: importing
description: Guidance for DataImportManager usage in BeepDM, including configuration, incremental sync, data quality, schema drift, error store, run history, staging, profiling, and replay. Use when you need governed import execution rather than lower-level ETL script orchestration.
---

# Data Import Guide

Use this skill when importing data through `DataImportManager` and its helper ecosystem.

## Use this skill when
- Running governed imports with validation, transformation, batching, and progress tracking
- Managing replay, error stores, run history, watermarks, staging, or profiling
- Building sync/import flows that need policy-driven behavior

## Do not use this skill when
- The task is primarily schema/data copy via ETL scripts or direct datasource ETL helpers. Use [`etl`](../etl/SKILL.md).
- The task is specifically sync-schema orchestration. Use [`beepsync`](../beepsync/SKILL.md).

## Architecture
- `DataImportManager` is a partial-class orchestrator.
- Helpers:
  - `ValidationHelper`
  - `TransformationHelper`
  - `BatchHelper`
  - `ProgressHelper`
- Additional modules:
  - `ErrorStore`
  - `History`
  - `Quality`
  - `Schema`
  - `Sync` watermark storage
  - `Profiling`
  - `Staging`

## File Locations
- `DataManagementEngineStandard/Editor/Importing/DataImportManager.cs`
- `DataManagementEngineStandard/Editor/Importing/DataImportManager.Core.cs`
- `DataManagementEngineStandard/Editor/Importing/DataImportManager.Migration.cs`
- `DataManagementEngineStandard/Editor/Importing/DataImportManager.Replay.cs`
- `DataManagementEngineStandard/Editor/Importing/Interfaces/IDataImportInterfaces.cs`
- `DataManagementEngineStandard/Editor/Importing/Helpers/`
- `DataManagementEngineStandard/Editor/Importing/Quality/`
- `DataManagementEngineStandard/Editor/Importing/ErrorStore/`
- `DataManagementEngineStandard/Editor/Importing/History/`
- `DataManagementEngineStandard/Editor/Importing/Sync/`
- `DataManagementEngineStandard/Editor/Importing/Profiling/`

## Working Rules
1. Validate configuration before execution.
2. Use preflight when schema drift or migration alignment matters.
3. Treat error store, replay, and run history as first-class workflow pieces, not afterthoughts.
4. Use watermarks and sync mode intentionally; do not mix incremental semantics casually.

## Related Skills
- [`beepsync`](../beepsync/SKILL.md)
- [`etl`](../etl/SKILL.md)
- [`migration`](../migration/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for configuration properties, examples, and pitfalls.
