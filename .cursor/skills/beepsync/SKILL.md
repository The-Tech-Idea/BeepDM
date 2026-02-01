---
name: beepsync
description: Expert guidance for data synchronization between datasources using BeepSyncManager. Use when synchronizing data between different datasources, managing sync schemas, or implementing bidirectional data sync operations.
---

# BeepSync Data Synchronization Guide

Expert guidance for synchronizing data between different datasources using BeepSyncManager, a modern helper-based architecture for reliable data synchronization.

## Overview

**BeepSyncManager** provides data synchronization capabilities between any two datasources supported by BeepDM. It uses a helper-based architecture following Single Responsibility Principle for maintainability and testability.

## Architecture

### Helper-Based Design

```
BeepSyncManager (Main Coordinator)
├── IDataSourceHelper - DataSource operations
├── IFieldMappingHelper - Field mapping and transformation
├── ISyncValidationHelper - Schema and operation validation
├── ISyncProgressHelper - Progress reporting and logging
└── ISchemaPersistenceHelper - Schema persistence operations
```

### Key Components

- **DataSourceHelper**: Manages IDataSource operations, connection validation, async data operations
- **FieldMappingHelper**: Handles field mapping, creates destination entities, auto-mapping capabilities
- **SyncValidationHelper**: Validates sync schemas, data source accessibility, entity existence
- **SyncProgressHelper**: Progress reporting, comprehensive logging, sync run tracking
- **SchemaPersistenceHelper**: Async schema persistence, backup functionality, individual schema operations

## DataSyncSchema

### Schema Structure

```csharp
public class DataSyncSchema
{
    public string Id { get; set; }
    public string SourceDataSourceName { get; set; }
    public string DestinationDataSourceName { get; set; }
    public string SourceEntityName { get; set; }
    public string DestinationEntityName { get; set; }
    public string SourceSyncDataField { get; set; }  // Field used to identify records
    public string DestinationSyncDataField { get; set; }
    public string SyncType { get; set; }  // "Full", "Incremental"
    public string SyncDirection { get; set; }  // "SourceToDestination", "Bidirectional"
    public DateTime LastSyncDate { get; set; }
    public string SyncStatus { get; set; }
    public string SyncStatusMessage { get; set; }
    public List<FieldSyncData> MappedFields { get; set; }
    public List<AppFilter> Filters { get; set; }
}
```

### FieldSyncData

```csharp
public class FieldSyncData
{
    public string SourceField { get; set; }
    public string DestinationField { get; set; }
    public string Transformation { get; set; }  // Optional transformation rule
}
```

## Basic Usage

### Initialization

```csharp
var syncManager = new BeepSyncManager(dmeEditor);
```

### Create Sync Schema

```csharp
var schema = new DataSyncSchema
{
    Id = Guid.NewGuid().ToString(),
    SourceDataSourceName = "SourceDB",
    DestinationDataSourceName = "DestinationDB",
    SourceEntityName = "Customers",
    DestinationEntityName = "Customers",
    SourceSyncDataField = "CustomerId",
    DestinationSyncDataField = "CustomerId",
    SyncType = "Full",
    SyncDirection = "SourceToDestination"
};

// Add field mappings
schema.MappedFields.Add(new FieldSyncData
{
    SourceField = "Name",
    DestinationField = "CustomerName"
});

schema.MappedFields.Add(new FieldSyncData
{
    SourceField = "Email",
    DestinationField = "EmailAddress"
});

// Add filters (optional)
schema.Filters.Add(new AppFilter
{
    FieldName = "Status",
    Operator = "=",
    FilterValue = "Active"
});
```

### Add and Sync Schema

```csharp
// Add schema to manager
syncManager.AddSyncSchema(schema);

// Synchronize data
var progress = new Progress<PassedArgs>(args => Console.WriteLine(args.Messege));
var cancellationToken = new CancellationTokenSource().Token;

var result = await syncManager.SyncDataAsync(schema, cancellationToken, progress);

// Save schemas
await syncManager.SaveSchemasAsync();
```

## Sync Operations

### Full Sync

```csharp
schema.SyncType = "Full";

// Syncs all records from source to destination
await syncManager.SyncDataAsync(schema, cancellationToken, progress);
```

### Incremental Sync

```csharp
schema.SyncType = "Incremental";

// Syncs only records modified since LastSyncDate
await syncManager.SyncDataAsync(schema, cancellationToken, progress);

// LastSyncDate is automatically updated after successful sync
```

### Bidirectional Sync

```csharp
schema.SyncDirection = "Bidirectional";

// Syncs changes in both directions
await syncManager.SyncDataAsync(schema, cancellationToken, progress);
```

## Schema Management

### Add Schema

```csharp
syncManager.AddSyncSchema(schema);
```

### Update Schema

```csharp
schema.MappedFields.Add(new FieldSyncData { SourceField = "Phone", DestinationField = "PhoneNumber" });
syncManager.UpdateSyncSchema(schema);
```

### Remove Schema

```csharp
syncManager.RemoveSyncSchema(schemaId);
```

### Load Schemas

```csharp
var schemas = await syncManager.LoadSchemasAsync();
```

### Save Schemas

```csharp
// Save all schemas
await syncManager.SaveSchemasAsync();

// Save single schema
await syncManager.SaveSchemaAsync(schema);
```

## Validation

### Validate Schema

```csharp
var validationResult = syncManager.ValidateSchema(schema);
if (validationResult.Flag == Errors.Ok)
{
    // Schema is valid, proceed with sync
    await syncManager.SyncDataAsync(schema, cancellationToken, progress);
}
else
{
    Console.WriteLine($"Validation failed: {validationResult.Message}");
}
```

### Validate DataSource

```csharp
var validationResult = syncManager.ValidateDataSource("MyDatabase");
if (validationResult.Flag == Errors.Ok)
{
    // DataSource is accessible
}
```

### Validate Entity

```csharp
var validationResult = syncManager.ValidateEntity("MyDatabase", "Customers");
if (validationResult.Flag == Errors.Ok)
{
    // Entity exists and is accessible
}
```

## Field Mapping

### Manual Field Mapping

```csharp
schema.MappedFields = new List<FieldSyncData>
{
    new FieldSyncData { SourceField = "FirstName", DestinationField = "First_Name" },
    new FieldSyncData { SourceField = "LastName", DestinationField = "Last_Name" },
    new FieldSyncData { SourceField = "Email", DestinationField = "EmailAddress" }
};
```

### Auto-Mapping

```csharp
// Automatically map fields with matching names
var autoMappedFields = syncManager.AutoMapFields(
    "SourceDB", "SourceCustomers",
    "DestDB", "DestCustomers"
);

schema.MappedFields = autoMappedFields;
```

### Field Transformation

```csharp
schema.MappedFields.Add(new FieldSyncData
{
    SourceField = "FullName",
    DestinationField = "FirstName",
    Transformation = "SPLIT(0)"  // Split and take first part
});

schema.MappedFields.Add(new FieldSyncData
{
    SourceField = "FullName",
    DestinationField = "LastName",
    Transformation = "SPLIT(1)"  // Split and take second part
});
```

## Progress Reporting

### Progress Callback

```csharp
var progress = new Progress<PassedArgs>(args =>
{
    Console.WriteLine($"[{args.CurrentRecord}/{args.TotalRecords}] {args.Messege}");
    
    if (args.Percentage > 0)
    {
        Console.WriteLine($"Progress: {args.Percentage}%");
    }
});

await syncManager.SyncDataAsync(schema, cancellationToken, progress);
```

### Sync Status

```csharp
// Check sync status after operation
if (schema.SyncStatus == "Success")
{
    Console.WriteLine($"Sync completed: {schema.SyncStatusMessage}");
    Console.WriteLine($"Last sync: {schema.LastSyncDate}");
}
else if (schema.SyncStatus == "Failed")
{
    Console.WriteLine($"Sync failed: {schema.SyncStatusMessage}");
}
```

## Error Handling

### Cancellation Support

```csharp
using var cts = new CancellationTokenSource();

// Cancel after 30 seconds
cts.CancelAfter(TimeSpan.FromSeconds(30));

try
{
    await syncManager.SyncDataAsync(schema, cts.Token, progress);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Sync operation was cancelled");
}
```

### Error Handling

```csharp
try
{
    var result = await syncManager.SyncDataAsync(schema, cancellationToken, progress);
    
    if (schema.SyncStatus == "Failed")
    {
        // Handle sync failure
        Console.WriteLine($"Sync failed: {schema.SyncStatusMessage}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
    // Sync status will be set to "Failed" automatically
}
```

## Advanced Patterns

### Pattern 1: Scheduled Sync

```csharp
public class ScheduledSyncService
{
    private readonly BeepSyncManager _syncManager;
    private Timer _syncTimer;

    public ScheduledSyncService(IDMEEditor editor)
    {
        _syncManager = new BeepSyncManager(editor);
    }

    public void StartScheduledSync(string schemaId, TimeSpan interval)
    {
        _syncTimer = new Timer(async _ =>
        {
            var schemas = await _syncManager.LoadSchemasAsync();
            var schema = schemas.FirstOrDefault(s => s.Id == schemaId);
            
            if (schema != null)
            {
                var progress = new Progress<PassedArgs>(args => Console.WriteLine(args.Messege));
                await _syncManager.SyncDataAsync(schema, CancellationToken.None, progress);
            }
        }, null, TimeSpan.Zero, interval);
    }
}
```

### Pattern 2: Multi-Entity Sync

```csharp
public async Task SyncMultipleEntitiesAsync(
    BeepSyncManager syncManager,
    List<DataSyncSchema> schemas,
    CancellationToken cancellationToken,
    IProgress<PassedArgs> progress)
{
    foreach (var schema in schemas)
    {
        // Validate before sync
        var validation = syncManager.ValidateSchema(schema);
        if (validation.Flag != Errors.Ok)
        {
            Console.WriteLine($"Schema {schema.Id} validation failed: {validation.Message}");
            continue;
        }

        // Sync entity
        await syncManager.SyncDataAsync(schema, cancellationToken, progress);
        
        // Check result
        if (schema.SyncStatus == "Success")
        {
            Console.WriteLine($"Successfully synced {schema.SourceEntityName} -> {schema.DestinationEntityName}");
        }
    }
    
    // Save all schemas
    await syncManager.SaveSchemasAsync();
}
```

### Pattern 3: Conditional Sync with Filters

```csharp
var schema = new DataSyncSchema
{
    // ... basic configuration
};

// Add conditional filters
schema.Filters.Add(new AppFilter
{
    FieldName = "ModifiedDate",
    Operator = ">=",
    FilterValue = DateTime.Today.AddDays(-7).ToString()  // Last 7 days
});

schema.Filters.Add(new AppFilter
{
    FieldName = "Status",
    Operator = "=",
    FilterValue = "Active"
});

await syncManager.SyncDataAsync(schema, cancellationToken, progress);
```

## Best Practices

### 1. Always Validate Before Sync
```csharp
var validation = syncManager.ValidateSchema(schema);
if (validation.Flag != Errors.Ok)
{
    // Fix schema issues before syncing
    return;
}
```

### 2. Use Incremental Sync for Large Datasets
```csharp
// For large datasets, use incremental sync
schema.SyncType = "Incremental";
schema.LastSyncDate = DateTime.Now.AddDays(-1);  // Sync last 24 hours
```

### 3. Handle Cancellation Gracefully
```csharp
try
{
    await syncManager.SyncDataAsync(schema, cancellationToken, progress);
}
catch (OperationCanceledException)
{
    // Clean up and save partial progress
    await syncManager.SaveSchemasAsync();
}
```

### 4. Monitor Sync Status
```csharp
// Check status after sync
if (schema.SyncStatus == "Failed")
{
    // Log error and retry logic
    _logger.LogError($"Sync failed: {schema.SyncStatusMessage}");
}
```

### 5. Save Schemas Regularly
```csharp
// Save after each sync operation
await syncManager.SaveSchemasAsync();

// Or save periodically
await syncManager.SaveSchemaAsync(schema);
```

## Related Skills

- **@beepdm** - Core BeepDM architecture and IDataSource usage
- **@connection** - Connection management for datasources
- **@mapping** - Entity mapping for field transformations

## Key Files

- `BeepSyncManager.cs` - Main sync manager
- `Interfaces/ISyncHelpers.cs` - Helper interfaces
- `Helpers/DataSourceHelper.cs` - DataSource operations
- `Helpers/FieldMappingHelper.cs` - Field mapping
- `Helpers/SyncValidationHelper.cs` - Validation
- `Helpers/SyncProgressHelper.cs` - Progress and logging
- `Helpers/SchemaPersistenceHelper.cs` - Schema persistence
