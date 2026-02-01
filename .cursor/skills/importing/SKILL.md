---
name: importing
description: Expert guidance for data import operations using DataImportManager. Use when importing data from external sources, transforming data during import, batch processing large datasets, or implementing data migration workflows.
---

# Data Import Guide

Expert guidance for data import operations using DataImportManager, a helper-based architecture with DefaultsManager integration for advanced data import capabilities.

## Overview

**DataImportManager** provides comprehensive data import capabilities:
- **Helper-Based Architecture**: Modular design with specialized helpers
- **DefaultsManager Integration**: Automatic default value application
- **Batch Processing**: Intelligent batch sizing and retry capabilities
- **Data Transformation**: Field filtering, mapping, and custom transformations
- **Progress Monitoring**: Detailed progress reporting and performance metrics
- **Validation**: Comprehensive validation of import configurations

## Architecture

### Helper-Based Design

```
DataImportManager (Main Class)
├── IDataImportValidationHelper - Validation operations
├── IDataImportTransformationHelper - Data transformation
├── IDataImportBatchHelper - Batch processing
├── IDataImportProgressHelper - Progress & logging
└── Integration with DefaultsManager
```

## Basic Usage

### Simple Import (Backward Compatible)

```csharp
using var importManager = new DataImportManager(dmeEditor);

// Configure using properties
importManager.SourceEntityName = "SourceCustomers";
importManager.SourceDataSourceName = "ExternalCRM";
importManager.DestEntityName = "Customers";
importManager.DestDataSourceName = "MainDatabase";

// Load destination structure (auto-loads defaults)
var loadResult = importManager.LoadDestEntityStructure("Customers", "MainDatabase");

// Execute import
var progress = new Progress<IPassedArgs>(args => Console.WriteLine(args.Messege));
using var cts = new CancellationTokenSource();
var result = await importManager.RunImportAsync(progress, cts.Token, null, batchSize: 100);
```

## Enhanced Configuration Import

### Create Import Configuration

```csharp
using var importManager = new DataImportManager(dmeEditor);

// Create enhanced configuration
var config = importManager.CreateImportConfiguration(
    sourceEntityName: "ProductsExport",
    sourceDataSourceName: "ExternalSystem",
    destEntityName: "Products",
    destDataSourceName: "MainDatabase"
);

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
```

### Custom Transformation

```csharp
// Custom transformation function
config.CustomTransformation = (record) => 
{
    // Apply business logic
    if (record is Dictionary<string, object> dict)
    {
        dict["ImportedDate"] = DateTime.Now;
        dict["ImportedBy"] = Environment.UserName;
        
        // Transform price
        if (dict.ContainsKey("Price"))
        {
            var price = Convert.ToDecimal(dict["Price"]);
            dict["Price"] = price * 1.1m; // Add 10% markup
        }
    }
    return record;
};

// Execute with enhanced features
var result = await importManager.RunImportAsync(config, progress, cancellationToken);
```

## Field Mapping

### Field Selection

```csharp
var config = importManager.CreateImportConfiguration(...);

// Select specific fields to import
config.SelectedFields = new List<string>
{
    "ProductCode",
    "ProductName",
    "Price",
    "Category"
};

// Fields not in list will be excluded
```

### Field Mapping

```csharp
// Map source fields to destination fields
config.FieldMappings = new Dictionary<string, string>
{
    { "ProdCode", "ProductCode" },
    { "ProdName", "ProductName" },
    { "ProdPrice", "Price" }
};
```

## Batch Processing

### Automatic Batch Sizing

```csharp
var config = importManager.CreateImportConfiguration(...);

// Let DataImportManager determine optimal batch size
config.BatchSize = 0; // 0 = auto-calculate

// Or specify batch size
config.BatchSize = 500;
```

### Batch Processing with Retry

```csharp
config.BatchSize = 200;
config.MaxRetries = 3;
config.RetryDelay = TimeSpan.FromSeconds(5);

// Failed batches will be retried up to MaxRetries times
var result = await importManager.RunImportAsync(config, progress, cancellationToken);
```

## Progress Monitoring

### Progress Callback

```csharp
var progress = new Progress<IPassedArgs>(args =>
{
    Console.WriteLine($"[{args.CurrentRecord}/{args.TotalRecords}] {args.Messege}");
    
    if (args.Percentage > 0)
    {
        Console.WriteLine($"Progress: {args.Percentage}%");
    }
    
    // Check for errors
    if (args.Flag == Errors.Failed)
    {
        Console.WriteLine($"Error: {args.Message}");
    }
});

await importManager.RunImportAsync(config, progress, cancellationToken);
```

### Performance Metrics

```csharp
// After import, check performance metrics
var metrics = importManager.ProgressHelper.GetPerformanceMetrics();

Console.WriteLine($"Total Records: {metrics.TotalRecords}");
Console.WriteLine($"Processed: {metrics.ProcessedRecords}");
Console.WriteLine($"Failed: {metrics.FailedRecords}");
Console.WriteLine($"Success Rate: {metrics.SuccessRate}%");
Console.WriteLine($"Average Time per Record: {metrics.AverageTimePerRecord}ms");
```

## Validation

### Validate Configuration

```csharp
var config = importManager.CreateImportConfiguration(...);

// Validate configuration before import
var validationResult = importManager.ValidationHelper.ValidateConfiguration(config);

if (validationResult.Flag != Errors.Ok)
{
    Console.WriteLine($"Validation failed: {validationResult.Message}");
    return;
}

// Proceed with import
await importManager.RunImportAsync(config, progress, cancellationToken);
```

### Validate Data Compatibility

```csharp
// Validate data compatibility between source and destination
var compatibilityResult = importManager.ValidationHelper.ValidateDataCompatibility(
    config.SourceEntityStructure,
    config.DestEntityStructure
);

if (compatibilityResult.Flag != Errors.Ok)
{
    Console.WriteLine($"Compatibility issues: {compatibilityResult.Message}");
}
```

## DefaultsManager Integration

### Automatic Default Application

```csharp
var config = importManager.CreateImportConfiguration(...);

// Enable default value application
config.ApplyDefaults = true;

// DefaultsManager will automatically apply defaults during import
// - CreatedAt, CreatedBy (for new records)
// - ModifiedAt, ModifiedBy (for updates)
// - Status, IsActive (static defaults)
// - Any custom defaults configured

await importManager.RunImportAsync(config, progress, cancellationToken);
```

### Custom Default Rules

```csharp
// Configure defaults before import
DefaultsManager.SetColumnDefault(
    dmeEditor, 
    "MainDatabase", 
    "Products", 
    "ImportedDate", 
    "NOW", 
    isRule: true
);

DefaultsManager.SetColumnDefault(
    dmeEditor, 
    "MainDatabase", 
    "Products", 
    "Status", 
    "Active", 
    isRule: false
);

// Defaults will be applied during import
config.ApplyDefaults = true;
await importManager.RunImportAsync(config, progress, cancellationToken);
```

## Cancellation and Pause/Resume

### Cancellation Support

```csharp
using var cts = new CancellationTokenSource();

// Cancel after 5 minutes
cts.CancelAfter(TimeSpan.FromMinutes(5));

try
{
    await importManager.RunImportAsync(config, progress, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Import operation was cancelled");
}
```

### Pause/Resume

```csharp
// Pause import
importManager.PauseImport();

// Resume import
importManager.ResumeImport();
```

## Error Handling

### Error Handling

```csharp
var result = await importManager.RunImportAsync(config, progress, cancellationToken);

if (result.Flag != Errors.Ok)
{
    Console.WriteLine($"Import failed: {result.Message}");
    
    // Check detailed errors
    var errors = importManager.ProgressHelper.GetErrors();
    foreach (var error in errors)
    {
        Console.WriteLine($"Error at record {error.RecordIndex}: {error.Message}");
    }
}
```

### Stop on Error Count

```csharp
config.StopOnErrorCount = 10; // Stop after 10 errors

await importManager.RunImportAsync(config, progress, cancellationToken);
```

## Advanced Patterns

### Pattern 1: Incremental Import

```csharp
public async Task IncrementalImportAsync(
    DataImportManager importManager,
    string entityName,
    DateTime lastImportDate)
{
    var config = importManager.CreateImportConfiguration(
        $"Export_{entityName}",
        "ExternalSystem",
        entityName,
        "MainDatabase"
    );
    
    // Filter for records modified since last import
    config.SourceFilters.Add(new AppFilter
    {
        FieldName = "ModifiedDate",
        Operator = ">=",
        FilterValue = lastImportDate.ToString()
    });
    
    await importManager.RunImportAsync(config, progress, cancellationToken);
}
```

### Pattern 2: Multi-Entity Import

```csharp
public async Task ImportMultipleEntitiesAsync(
    DataImportManager importManager,
    List<(string source, string dest)> entities)
{
    foreach (var (sourceEntity, destEntity) in entities)
    {
        var config = importManager.CreateImportConfiguration(
            sourceEntity,
            "ExternalSystem",
            destEntity,
            "MainDatabase"
        );
        
        config.ApplyDefaults = true;
        
        var result = await importManager.RunImportAsync(config, progress, cancellationToken);
        
        if (result.Flag != Errors.Ok)
        {
            Console.WriteLine($"Failed to import {sourceEntity} -> {destEntity}");
        }
    }
}
```

### Pattern 3: Import with Validation

```csharp
public async Task ImportWithValidationAsync(
    DataImportManager importManager,
    DataImportConfiguration config)
{
    // Validate configuration
    var configValidation = importManager.ValidationHelper.ValidateConfiguration(config);
    if (configValidation.Flag != Errors.Ok)
    {
        throw new InvalidOperationException($"Configuration invalid: {configValidation.Message}");
    }
    
    // Validate data compatibility
    var compatibilityValidation = importManager.ValidationHelper.ValidateDataCompatibility(
        config.SourceEntityStructure,
        config.DestEntityStructure
    );
    
    if (compatibilityValidation.Flag != Errors.Ok)
    {
        Console.WriteLine($"Warning: {compatibilityValidation.Message}");
    }
    
    // Execute import
    var result = await importManager.RunImportAsync(config, progress, cancellationToken);
    
    // Check results
    var metrics = importManager.ProgressHelper.GetPerformanceMetrics();
    if (metrics.SuccessRate < 95)
    {
        Console.WriteLine($"Low success rate: {metrics.SuccessRate}%");
    }
}
```

## Best Practices

### 1. Always Load Destination Structure
```csharp
// Load destination structure to enable defaults and validation
var loadResult = importManager.LoadDestEntityStructure("Customers", "MainDatabase");
if (loadResult.Flag != Errors.Ok)
{
    throw new InvalidOperationException("Failed to load destination structure");
}
```

### 2. Use Batch Processing for Large Datasets
```csharp
// For large datasets, use appropriate batch size
config.BatchSize = 500; // Adjust based on data size and memory
```

### 3. Enable Defaults Application
```csharp
// Always enable defaults for audit fields and business rules
config.ApplyDefaults = true;
```

### 4. Monitor Progress
```csharp
// Always provide progress callback for long-running imports
var progress = new Progress<IPassedArgs>(args => UpdateUI(args));
await importManager.RunImportAsync(config, progress, cancellationToken);
```

### 5. Handle Errors Gracefully
```csharp
// Check results and handle errors
var result = await importManager.RunImportAsync(config, progress, cancellationToken);
if (result.Flag != Errors.Ok)
{
    var errors = importManager.ProgressHelper.GetErrors();
    // Log or handle errors appropriately
}
```

## Related Skills

- **@beepdm** - Core BeepDM architecture
- **@defaults** - DefaultsManager for default values
- **@mapping** - Entity mapping for field transformations
- **@etl** - ETLEditor for complex ETL operations

## Key Files

- `DataImportManager.cs` - Main import manager
- `DataImportManager.Core.cs` - Core functionality
- `Helpers/DataImportValidationHelper.cs` - Validation
- `Helpers/DataImportTransformationHelper.cs` - Transformation
- `Helpers/DataImportBatchHelper.cs` - Batch processing
- `Helpers/DataImportProgressHelper.cs` - Progress monitoring
