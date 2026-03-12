---
name: beepsync
description: Guidance for data synchronization between BeepDM datasources using BeepSyncManager and DataSyncSchema. Use when managing sync schemas, translating sync definitions into DataImportManager runs, or implementing one-way and bidirectional sync flows.
---

# BeepSync Data Synchronization Guide

Use this skill when synchronizing data between two BeepDM datasources or managing reusable sync schemas.

## Use this skill when
- Building or validating `DataSyncSchema` definitions
- Running sync operations through `BeepSyncManager`
- Understanding how sync now delegates execution to `DataImportManager`
- Working with field mappings, filters, watermark fields, or schema persistence

## Do not use this skill when
- The task is primarily an import pipeline with staging, replay, or quality rules. Use [`importing`](../importing/SKILL.md).
- The task is primarily ETL script generation or direct datasource copy. Use [`etl`](../etl/SKILL.md).

## Architecture
- `BeepSyncManager` is the orchestrator.
- `SyncValidationHelper` validates schemas and runtime preconditions.
- `SchemaPersistenceHelper` saves and loads schemas.
- `SyncSchemaTranslator` converts a `DataSyncSchema` into `DataImportConfiguration`.
- `DataImportManager` performs the underlying data move.

## File Locations
- `DataManagementEngineStandard/Editor/BeepSync/BeepSyncManager.Orchestrator.cs`
- `DataManagementEngineStandard/Editor/BeepSync/Helpers/SyncValidationHelper.cs`
- `DataManagementEngineStandard/Editor/BeepSync/Helpers/SchemaPersistenceHelper.cs`
- `DataManagementEngineStandard/Editor/BeepSync/Helpers/SyncSchemaTranslator.cs`
- `DataManagementEngineStandard/Editor/BeepSync/Helpers/FieldMappingHelper.cs`
- `DataManagementEngineStandard/Editor/BeepSync/Helpers/SyncProgressHelper.cs`
- `DataManagementEngineStandard/Editor/BeepSync/Interfaces/ISyncHelpers.cs`

## Typical Workflow
1. Create or load a `DataSyncSchema`.
2. Define source/destination entities, sync keys, mappings, and optional filters.
3. Validate with `ValidateSchema` or full sync-operation validation.
4. Run `SyncDataAsync(...)`.
5. Save schemas for reuse and inspect `SyncStatus`, `SyncStatusMessage`, and `LastSyncDate`.

## Pitfalls
- Missing sync key fields produce duplicates or missed updates.
- Treating sync as standalone and ignoring that it now depends on `DataImportManager` behavior.
- Forgetting to save updated schemas or sync runs.
- Using bidirectional sync without carefully defining reverse-safe mappings.

## Related Skills
- [`importing`](../importing/SKILL.md)
- [`etl`](../etl/SKILL.md)
- [`mapping`](../mapping/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for examples, filters, and validation notes.
