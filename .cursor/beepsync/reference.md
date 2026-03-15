# BeepSync Quick Reference

## Basic Usage

```csharp
// Initialize
var syncManager = new DataSyncManager(dmeEditor);

// Create schema
var schema = new DataSyncSchema
{
    Id = Guid.NewGuid().ToString(),
    SourceDataSourceName = "SourceDB",
    DestinationDataSourceName = "DestDB",
    SourceEntityName = "Customers",
    DestinationEntityName = "Customers",
    SourceSyncDataField = "CustomerId",
    SyncType = "Full"
};

// Add field mappings
schema.MappedFields.Add(new FieldSyncData { SourceField = "Name", DestinationField = "CustomerName" });

// Sync
syncManager.AddSyncSchema(schema);
await syncManager.SyncDataAsync(schema, cancellationToken, progress);

// Save
await syncManager.SaveSchemasAsync();
```

## Sync Types

```csharp
schema.SyncType = "Full";          // All records
schema.SyncType = "Incremental";   // Since LastSyncDate
schema.SyncDirection = "Bidirectional"; // Both directions
```

## Validation

```csharp
var result = syncManager.ValidateSchema(schema);
var result = syncManager.ValidateDataSource("MyDB");
var result = syncManager.ValidateEntity("MyDB", "Customers");
```

## Schema Management

```csharp
syncManager.AddSyncSchema(schema);
syncManager.UpdateSyncSchema(schema);
syncManager.RemoveSyncSchema(schemaId);
var schemas = await syncManager.LoadSchemasAsync();
await syncManager.SaveSchemasAsync();
```

