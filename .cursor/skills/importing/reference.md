# Data Import Quick Reference

## Basic Import

```csharp
using var importManager = new DataImportManager(dmeEditor);

// Configure
importManager.SourceEntityName = "SourceCustomers";
importManager.SourceDataSourceName = "ExternalCRM";
importManager.DestEntityName = "Customers";
importManager.DestDataSourceName = "MainDatabase";

// Load structure
importManager.LoadDestEntityStructure("Customers", "MainDatabase");

// Execute
await importManager.RunImportAsync(progress, cancellationToken, null, batchSize: 100);
```

## Enhanced Configuration

```csharp
var config = importManager.CreateImportConfiguration("Source", "SourceDB", "Dest", "DestDB");

// Configure
config.SourceFilters.Add(new AppFilter { FieldName = "Status", Operator = "=", FilterValue = "Active" });
config.SelectedFields = new List<string> { "Name", "Email" };
config.BatchSize = 200;
config.ApplyDefaults = true;

// Custom transformation
config.CustomTransformation = (record) => { /* transform */ return record; };

// Execute
await importManager.RunImportAsync(config, progress, cancellationToken);
```

## Field Mapping

```csharp
config.SelectedFields = new List<string> { "ProductCode", "ProductName" };
config.FieldMappings = new Dictionary<string, string>
{
    { "ProdCode", "ProductCode" },
    { "ProdName", "ProductName" }
};
```

## Validation

```csharp
var result = importManager.ValidationHelper.ValidateConfiguration(config);
var result = importManager.ValidationHelper.ValidateDataCompatibility(sourceStructure, destStructure);
```

## Progress & Metrics

```csharp
var metrics = importManager.ProgressHelper.GetPerformanceMetrics();
var errors = importManager.ProgressHelper.GetErrors();
```

## Cancellation & Pause

```csharp
importManager.PauseImport();
importManager.ResumeImport();
// Cancellation via CancellationToken
```
