# Migration Guide: DataSyncManager to BeepSyncManager

## Overview
BeepSyncManager replaces DataSyncManager with a modern, helper-based architecture that follows SOLID principles and provides better maintainability, testability, and performance.

## Key Architectural Changes

### 1. **Helper-Based Architecture**
BeepSyncManager uses specialized helper classes instead of a monolithic approach:

- `IDataSourceHelper` - Handles all data source operations
- `IFieldMappingHelper` - Manages field mapping and entity creation  
- `ISyncValidationHelper` - Validates schemas and operations
- `ISyncProgressHelper` - Handles progress reporting and logging
- `ISchemaPersistenceHelper` - Manages schema persistence

### 2. **Improved Async Patterns**
- Consistent async/await throughout the codebase
- Better cancellation token handling
- Proper resource disposal

### 3. **Enhanced Error Handling**
- Centralized error creation methods
- Consistent error reporting patterns
- Better exception handling with retry mechanisms

## API Changes

### Constructor
```csharp
// Old (DataSyncManager)
var syncManager = new DataSyncManager(dmeEditor);

// New (BeepSyncManager) - Same interface
var syncManager = new BeepSyncManager(dmeEditor);
```

### Core Sync Operations
```csharp
// All existing methods are supported for backward compatibility:

// Basic sync operations
syncManager.SyncData(schema);
syncManager.SyncData(schemaId);
syncManager.SyncAllData();

// Enhanced async operations (recommended)
await syncManager.SyncDataAsync(schema, cancellationToken, progress);
await syncManager.SyncAllSchemasAsync(cancellationToken, progress);

// New bulk operations
var metrics = await syncManager.BulkSyncAsync(schema, batchSize: 100, cancellationToken, progress);
```

### Schema Management
```csharp
// All existing methods work the same:
syncManager.AddSyncSchema(schema);
syncManager.UpdateSyncSchema(schema);
syncManager.RemoveSyncSchema(schemaId);
syncManager.ValidateSchema(schema);

// Enhanced methods
var schema = syncManager.GetSchema(schemaId);
```

### Control Operations
```csharp
// Same interface as before:
syncManager.PauseSync();
syncManager.ResumeSync();
syncManager.CancelSync(); // Enhanced with better cancellation

// New properties for status checking:
bool isPaused = syncManager.IsPaused;
bool isCancelled = syncManager.IsCancelled;
```

## New Features

### 1. **Enhanced Progress Reporting**
```csharp
var progress = new Progress<PassedArgs>(args => 
{
    Console.WriteLine($"Progress: {args.Messege}");
});

await syncManager.SyncDataAsync(schema, CancellationToken.None, progress);
```

### 2. **Bulk Operations with Metrics**
```csharp
var metrics = await syncManager.BulkSyncAsync(schema, batchSize: 50);
Console.WriteLine($"Total: {metrics.TotalRecords}, Success: {metrics.SuccessfulRecords}, Failed: {metrics.FailedRecords}");
```

### 3. **Incremental Sync Methods**
```csharp
// Get only new records since last sync
var newRecords = await syncManager.GetNewRecordsFromSourceData(schema);

// Get updated records since last sync  
var updatedRecords = await syncManager.GetUpdatedRecordsFromSourceData(schema);

// Get records with custom filter operator
var records = await syncManager.GetRecordsFromSourceData(schema, ">=");
```

### 4. **Enhanced Validation**
```csharp
var validationResult = syncManager.ValidateSchema(schema);
if (validationResult.Flag == Errors.Failed)
{
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine(error.Message);
    }
}
```

## Performance Improvements

### 1. **Batch Processing**
- Records are processed in configurable batches (default 100)
- Better memory management for large datasets
- Progress reporting at batch intervals

### 2. **Async Operations**
- Full async/await support throughout
- Non-blocking operations
- Better thread utilization

### 3. **Resource Management**
- Proper disposal of resources
- Cancellation token support
- Memory efficient processing

## Breaking Changes

### None for Basic Usage
The BeepSyncManager maintains backward compatibility for all basic operations. Existing code should work without modification.

### Advanced Scenarios
- Internal methods that were accessing private fields may need updates
- Custom error handling code may benefit from the new centralized patterns
- Progress reporting callbacks receive more detailed information

## Migration Steps

### Step 1: Update References
```csharp
// Change namespace if needed (should be the same)
using TheTechIdea.Beep.Editor.BeepSync;

// Replace class instantiation (constructor is the same)
var syncManager = new BeepSyncManager(dmeEditor); // instead of DataSyncManager
```

### Step 2: Update Async Calls (Recommended)
```csharp
// Old synchronous pattern
syncManager.SyncAllData();

// New async pattern (recommended)
await syncManager.SyncAllSchemasAsync(cancellationToken, progress);
```

### Step 3: Leverage New Features
```csharp
// Use bulk operations for better performance
var metrics = await syncManager.BulkSyncAsync(schema, batchSize: 100);

// Use enhanced validation
var validation = syncManager.ValidateSchema(schema);

// Use better progress reporting
var progress = new Progress<PassedArgs>(UpdateUI);
```

### Step 4: Update Error Handling
```csharp
// Enhanced error handling with detailed messages
var result = await syncManager.SyncDataAsync(schema);
if (result.Flag == Errors.Failed)
{
    logger.LogError($"Sync failed: {result.Message}");
}
```

## Testing Benefits

The new architecture provides better testability:

```csharp
// Mock helper interfaces for unit testing
var mockDataSourceHelper = new Mock<IDataSourceHelper>();
var mockValidationHelper = new Mock<ISyncValidationHelper>();
// ... other mocks

// BeepSyncManager can be tested with mocked dependencies
// DataSyncManager was harder to test due to tight coupling
```

## Performance Comparison

| Aspect | DataSyncManager | BeepSyncManager |
|--------|----------------|-----------------|
| Memory Usage | Higher (no batching) | Lower (batch processing) |
| Error Handling | Basic | Comprehensive with retry |
| Progress Reporting | Limited | Detailed with metrics |
| Async Support | Mixed patterns | Full async/await |
| Testability | Poor (tight coupling) | Excellent (DI) |
| Maintainability | Monolithic | Modular helpers |

## Conclusion

BeepSyncManager provides the same functionality as DataSyncManager while offering:
- Better performance and scalability
- Enhanced error handling and logging
- Improved testability and maintainability
- Modern async patterns
- Backward compatibility for existing code

The migration should be straightforward for most use cases, with the option to gradually adopt new features and patterns.