# DataImportManager - Enhanced with Helper Architecture

## Overview

The DataImportManager has been completely refactored to use a modern helper-based architecture similar to DefaultsManager, providing enhanced functionality, better maintainability, and seamless integration with the DefaultsManager for automatic default value handling.

## Key Features

### ??? **Helper-Based Architecture**
- **ValidationHelper**: Comprehensive validation of import configurations and data compatibility
- **TransformationHelper**: Advanced data transformation with field filtering, mapping, and default value application
- **BatchHelper**: Intelligent batch processing with optimal sizing and retry capabilities
- **ProgressHelper**: Detailed progress monitoring, logging, and performance metrics

### ?? **DefaultsManager Integration**
- Automatic loading and application of default values from DefaultsManager
- Support for both static and dynamic default values with rule-based resolution
- Seamless integration in the transformation pipeline

### ? **Enhanced Performance**
- Intelligent batch size calculation based on data characteristics and available memory
- Asynchronous processing with cancellation and pause/resume capabilities
- Memory-efficient data processing for large datasets

### ?? **Advanced Monitoring**
- Real-time progress reporting with performance metrics
- Comprehensive logging with different log levels (Info, Warning, Error, Success, Debug)
- Export capabilities for log analysis and auditing

## Architecture

```
DataImportManager (Main Class)
??? IDataImportValidationHelper (Validation operations)
??? IDataImportTransformationHelper (Data transformation)
??? IDataImportBatchHelper (Batch processing)
??? IDataImportProgressHelper (Progress & logging)
??? Integration with DefaultsManager
```

## Usage Examples

### Basic Import (Backward Compatible)

```csharp
using var importManager = new DataImportManager(dmeEditor);

// Configure using familiar properties
importManager.SourceEntityName = "SourceCustomers";
importManager.SourceDataSourceName = "ExternalCRM";
importManager.DestEntityName = "Customers";
importManager.DestDataSourceName = "MainDatabase";

// Load destination structure (auto-loads defaults)
var loadResult = importManager.LoadDestEntityStructure("Customers", "MainDatabase");

// Execute import
var progress = new Progress<IPassedArgs>(args => Console.WriteLine(args.Messege));
using var cts = new CancellationTokenSource();
var result = await importManager.RunImportAsync(progress, cts.Token, null, 100);
```

### Enhanced Configuration Import

```csharp
using var importManager = new DataImportManager(dmeEditor);

// Create enhanced configuration
var config = importManager.CreateImportConfiguration(
    "ProductsExport", "ExternalSystem",
    "Products", "MainDatabase");

// Configure advanced options
config.SourceFilters.Add(new AppFilter 
{ 
    FieldName = "ModifiedDate", 
    Operator = ">=", 
    FilterValue = DateTime.Today.AddDays(-7).ToString() 
});

config.SelectedFields = new List<string> { "ProductCode", "ProductName", "Price" };
config.BatchSize = 200;
config.ApplyDefaults = true; // Uses DefaultsManager automatically

// Custom transformation
config.CustomTransformation = (record) => 
{
    // Apply business logic
    if (record is Dictionary<string, object> dict)
    {
        dict["ImportedDate"] = DateTime.Now;
        dict["ImportedBy"] = Environment.UserName;
    }
    return record;
};

// Execute with enhanced features
var result = await importManager.RunImportAsync(config, progress, cancellationToken);
```

### Advanced Usage with Direct Helper Access

```csharp
using var importManager = new DataImportManager(dmeEditor);

// Use helpers directly for fine-grained control
var validation = importManager.ValidationHelper.ValidateImportConfiguration(config);
var optimalBatchSize = importManager.BatchHelper.CalculateOptimalBatchSize(50000, 2048);
var metrics = importManager.ProgressHelper.CalculatePerformanceMetrics(startTime, processed, total);

// Access detailed logging
var errors = importManager.ProgressHelper.GetLogEntriesByLevel(ImportLogLevel.Error);
var logText = importManager.ProgressHelper.ExportLogToText();
```

## Configuration Options

### DataImportConfiguration Properties

| Property | Description | Default |
|----------|-------------|---------|
| `SourceEntityName` | Name of source entity | Required |
| `DestEntityName` | Name of destination entity | Required |
| `SourceDataSourceName` | Source data source identifier | Required |
| `DestDataSourceName` | Destination data source identifier | Required |
| `SourceFilters` | Filters to apply to source data | Empty list |
| `SelectedFields` | Specific fields to import | All fields |
| `BatchSize` | Records per batch | Auto-calculated |
| `CreateDestinationIfNotExists` | Auto-create destination entity | `true` |
| `ApplyDefaults` | Use DefaultsManager for default values | `true` |
| `CustomTransformation` | Custom transformation function | `null` |

## DefaultsManager Integration

The DataImportManager automatically integrates with DefaultsManager to apply default values during the transformation process:

### Automatic Default Loading
```csharp
// Defaults are automatically loaded when ApplyDefaults = true
config.ApplyDefaults = true; // Default
var config = importManager.CreateImportConfiguration(...);
// config.DefaultValues is automatically populated from DefaultsManager
```

### Transformation Pipeline Order
1. **Field Filtering** (if `SelectedFields` specified)
2. **Entity Mapping** (if `Mapping` configured)
3. **Default Value Application** (using DefaultsManager integration)
4. **Custom Transformation** (if `CustomTransformation` provided)

### Default Value Resolution
```csharp
// Static defaults
new DefaultValue 
{ 
    PropertyName = "Status", 
    PropertyValue = "Active" 
}

// Dynamic defaults using DefaultsManager resolvers
new DefaultValue 
{ 
    PropertyName = "CreatedDate", 
    Rule = "NOW()" 
}
```

## Control Operations

### Pause/Resume Import
```csharp
importManager.PauseImport();  // Pauses current operation
importManager.ResumeImport(); // Resumes paused operation
```

### Cancel Import
```csharp
importManager.CancelImport(); // Requests cancellation
```

### Check Status
```csharp
var status = importManager.GetImportStatus();
Console.WriteLine($"Running: {status.IsRunning}, Paused: {status.IsPaused}");
```

## Validation and Testing

### Configuration Testing
```csharp
var testResult = await importManager.TestImportConfigurationAsync(config);
if (testResult.Flag == Errors.Ok)
{
    // Configuration is valid, proceed with import
}
```

### Entity Compatibility Validation
```csharp
var compatibility = importManager.ValidationHelper.ValidateEntityCompatibility(
    sourceEntity, destEntity);
```

## Performance Optimization

### Automatic Batch Size Calculation
```csharp
var optimalSize = importManager.BatchHelper.CalculateOptimalBatchSize(
    totalRecords: 100000,
    estimatedRecordSize: 2048, // 2KB per record
    availableMemory: 50 * 1024 * 1024 // 50MB
);
```

### Memory Usage Guidelines
- Small datasets (<1K records): 50-100 batch size
- Medium datasets (1K-100K): 100-500 batch size  
- Large datasets (>100K): 500-1000 batch size
- Memory-constrained: 25-50 batch size

## Logging and Monitoring

### Log Levels
- **Info**: General information
- **Warning**: Non-critical issues
- **Error**: Critical errors
- **Success**: Successful operations
- **Debug**: Detailed debugging information

### Log Analysis
```csharp
var summary = importManager.ProgressHelper.GetLogSummary();
Console.WriteLine($"Total: {summary.TotalEntries}, Errors: {summary.ErrorCount}");

var errorLogs = importManager.ProgressHelper.GetLogEntriesByLevel(ImportLogLevel.Error);
var logExport = importManager.ProgressHelper.ExportLogToText();
```

## Migration Guide

### From Old DataImportManager
The refactored version maintains backward compatibility:

```csharp
// Old way (still works)
importManager.SourceEntityName = "Source";
importManager.DestEntityName = "Dest";
await importManager.RunImportAsync(progress, token, transform, batchSize);

// New enhanced way
var config = importManager.CreateImportConfiguration("Source", "SourceDS", "Dest", "DestDS");
config.CustomTransformation = transform;
config.BatchSize = batchSize;
await importManager.RunImportAsync(config, progress, token);
```

## Best Practices

### Configuration
- Always test configuration before running imports on large datasets
- Use appropriate batch sizes based on data volume and memory constraints
- Configure filters to limit data scope and improve performance

### Error Handling
- Implement comprehensive progress reporting for user feedback
- Monitor error logs during and after import operations
- Use cancellation tokens for long-running operations

### Performance
- Let the system calculate optimal batch sizes for best performance
- Use field selection to reduce data transfer overhead
- Monitor memory usage during large imports

### DefaultsManager Integration
- Enable `ApplyDefaults = true` to leverage automatic default value application
- Configure appropriate default values in DefaultsManager for your entities
- Test default value rules before running large imports

## Threading and Concurrency

The DataImportManager is designed for single-threaded operation per instance but supports:
- Asynchronous execution with `async/await`
- Cancellation token support for graceful shutdown
- Pause/resume functionality for user control
- Thread-safe logging and progress reporting

For concurrent imports, create separate DataImportManager instances.

## Files Structure

```
DataManagementEngineStandard/Editor/Importing/
??? Interfaces/
?   ??? IDataImportInterfaces.cs          # Core interfaces
??? Helpers/
?   ??? DataImportValidationHelper.cs     # Validation operations
?   ??? DataImportTransformationHelper.cs # Data transformation
?   ??? DataImportBatchHelper.cs          # Batch processing
?   ??? DataImportProgressHelper.cs       # Progress monitoring
??? Examples/
?   ??? DataImportManagerExamples.cs      # Usage examples
??? DataImportManager.cs                  # Main manager class
??? DataImportManager.Core.cs             # Core functionality
```

This architecture provides a solid foundation for data import operations while maintaining compatibility with existing code and providing extensive new capabilities through the helper system and DefaultsManager integration.