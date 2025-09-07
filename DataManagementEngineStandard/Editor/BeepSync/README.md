# BeepSyncManager Architecture

## Overview

BeepSyncManager is a modern, clean synchronization manager built using best practices and following the Single Responsibility Principle. It consolidates and improves upon the functionality from `DataSyncManager` and `DataSyncService` while providing a maintainable, testable architecture.

## Architecture

### Folder Structure
```
BeepSync/
├── BeepSyncManager.cs           # Main sync manager class
├── Interfaces/
│   └── ISyncHelpers.cs          # All helper interfaces
└── Helpers/
    ├── DataSourceHelper.cs      # IDataSource operations
    ├── FieldMappingHelper.cs    # Field mapping and transformation
    ├── SyncValidationHelper.cs  # Schema and operation validation
    ├── SyncProgressHelper.cs    # Progress reporting and logging
    └── SchemaPersistenceHelper.cs # Schema persistence operations
```

### Key Design Principles

1. **Single Responsibility**: Each helper class handles one specific aspect of synchronization
2. **Dependency Injection**: All helpers are injected into the main manager
3. **Async/Await**: Full async support for scalable operations
4. **Error Handling**: Comprehensive error handling and logging
5. **Progress Reporting**: Detailed progress reporting for long-running operations
6. **Testability**: Clean interfaces make unit testing straightforward

## Helper Classes

### DataSourceHelper
- Manages all `IDataSource` operations
- Handles connection validation
- Provides async data operations (Get, Insert, Update)
- Based on patterns from `DataSyncManager.GetDataSource()`

### FieldMappingHelper
- Handles field mapping between source and destination
- Creates destination entities
- Provides auto-mapping capabilities
- Type conversion with error handling
- Based on `DataSyncManager.MapFields()` and `CreateDestinationEntity()`

### SyncValidationHelper
- Validates sync schemas for completeness
- Validates data source accessibility
- Validates entity existence
- Pre-sync operation validation
- Based on `DataSyncManager.ValidateSchema()`

### SyncProgressHelper
- Progress reporting with detailed messages
- Comprehensive logging with different levels
- Sync run tracking and history
- Error logging with context
- Based on `DataSyncManager.SendMessage()` and `LogSyncRun()`

### SchemaPersistenceHelper
- Async schema persistence to JSON files
- Schema backup functionality
- Individual schema operations (save/delete)
- Based on `DataSyncManager.SaveSchemas()` and `LoadSchemas()`

## Usage Example

```csharp
// Initialize the manager
var syncManager = new BeepSyncManager(dmeEditor);

// Create a sync schema
var schema = new DataSyncSchema
{
    ID = Guid.NewGuid().ToString(),
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

// Add schema and sync
syncManager.AddSyncSchema(schema);
var result = await syncManager.SyncDataAsync(schema, progress: progressReporter);

// Save schemas
await syncManager.SaveSchemasAsync();
```

## Key Improvements Over Original Classes

1. **Clean Architecture**: Helper-based design vs. monolithic classes
2. **Async Operations**: Full async support throughout
3. **Better Error Handling**: Detailed error information and logging
4. **Progress Reporting**: Rich progress information for UI integration
5. **Testability**: Clean interfaces allow easy mocking for unit tests
6. **Resource Management**: Proper disposal and resource cleanup
7. **Validation**: Comprehensive pre-sync validation
8. **Persistence**: Robust schema persistence with backup support

## Migration from DataSyncManager

The BeepSyncManager provides the same core functionality as DataSyncManager but with improved architecture:

- `SyncData()` → `SyncDataAsync()` / `SyncData()`
- `ValidateSchema()` → `ValidateSchema()` (enhanced)
- `SaveSchemas()` → `SaveSchemasAsync()` / `SaveSchemas()`
- `LoadSchemas()` → `LoadSchemasAsync()` / `LoadSchemas()`
- `AddSyncSchema()` → `AddSyncSchema()`
- `RemoveSyncSchema()` → `RemoveSyncSchema()`
- `UpdateSyncSchema()` → `UpdateSyncSchema()`

## Thread Safety and Cancellation

- Full cancellation token support for long-running operations
- Pause/Resume functionality for user control
- Thread-safe operations where needed
- Proper async/await patterns throughout

## Logging and Monitoring

- Structured logging through `SyncProgressHelper`
- Detailed sync run history
- Error tracking and reporting  
- Progress reporting for UI integration

This architecture provides a solid foundation for data synchronization operations while maintaining compatibility with existing `DataSyncSchema` objects and providing significant improvements in maintainability and functionality.
