---
name: beepsync
description: Guidance for data synchronization between datasources using DataSyncManager and DataSyncSchema.
---

# BeepSync Data Synchronization Guide

Use this skill when synchronizing data between two BeepDM datasources, managing sync schemas, or implementing bidirectional sync flows.

## Architecture
```
DataSyncManager (Coordinator)
+-- IDataSourceHelper
+-- IFieldMappingHelper
+-- ISyncValidationHelper
+-- ISyncProgressHelper
+-- ISchemaPersistenceHelper
```

## Core Types
- DataSyncManager: runs sync operations
- DataSyncSchema: defines source, destination, and mappings
- FieldSyncData: source to destination field mapping

## Workflow
1. Create a `DataSyncSchema` with source and destination info.
2. Add field mappings and optional filters.
3. Validate schema with `ValidateSchema`.
4. Run `SyncDataAsync` and monitor progress.
5. Save schemas for reuse.

## Validation
- `ValidateSchema(schema)` should return `Errors.Ok`.
- Check `schema.SyncStatus` and `schema.SyncStatusMessage` after a run.

## Pitfalls
- Missing sync key fields results in duplicate or skipped rows.
- Forgetting to save schemas means changes are lost between runs.
- Avoid mixing incremental sync with missing `LastSyncDate`.

## File Locations
- DataManagementEngineStandard/Editor/BeepSync/BeepSyncManager.cs
- DataManagementEngineStandard/Editor/BeepSync/Models/DataSyncSchema.cs
- DataManagementEngineStandard/Editor/BeepSync/README.md

## Example
```csharp
var syncManager = new DataSyncManager(editor);

var schema = new DataSyncSchema
{
    Id = Guid.NewGuid().ToString(),
    SourceDataSourceName = "SourceDB",
    DestinationDataSourceName = "DestDB",
    SourceEntityName = "Customers",
    DestinationEntityName = "Customers",
    SourceSyncDataField = "CustomerId",
    DestinationSyncDataField = "CustomerId",
    SyncType = "Full",
    SyncDirection = "SourceToDestination",
    MappedFields = new List<FieldSyncData>
    {
        new FieldSyncData { SourceField = "Name", DestinationField = "Name" },
        new FieldSyncData { SourceField = "Email", DestinationField = "Email" }
    }
};

var validation = syncManager.ValidateSchema(schema);
if (validation.Flag == Errors.Ok)
{
    await syncManager.SyncDataAsync(schema, CancellationToken.None, null);
    await syncManager.SaveSchemaAsync(schema);
}
```

## Task-Specific Examples

### Incremental Sync
```csharp
schema.SyncType = "Incremental";
schema.LastSyncDate = DateTime.UtcNow.AddDays(-1);

var validation = syncManager.ValidateSchema(schema);
if (validation.Flag == Errors.Ok)
{
    await syncManager.SyncDataAsync(schema, CancellationToken.None, null);
}
```

### Bidirectional Sync With Filters
```csharp
schema.SyncDirection = "Bidirectional";
schema.Filters = new List<AppFilter>
{
    new AppFilter { FieldName = "Status", Operator = "=", FilterValue = "Active" }
};

await syncManager.SyncDataAsync(schema, CancellationToken.None, null);
```