---
name: importing
description: Guidance for DataImportManager usage, configuration, batch processing, and DefaultsManager integration in BeepDM.
---

# Data Import Guide

Use this skill when importing data from external sources, mapping fields, or running batched import operations.

## Core Types
- DataImportManager
- ImportConfiguration (created via `CreateImportConfiguration`)
- DefaultsManager for default value rules

## Workflow
1. Create `DataImportManager`.
2. Create configuration with source and destination.
3. Set filters, mappings, and batch size.
4. Validate configuration.
5. Run `RunImportAsync` and inspect progress/errors.

## Validation
- Use `ValidationHelper.ValidateConfiguration(config)`.
- Check `IErrorsInfo.Flag` on import result.
- Verify destination entity structure is loaded.

## Pitfalls
- Skipping defaults can cause missing required fields.
- Very large batch sizes can impact memory and lock durations.
- Mismatched field names without mappings lead to null fields.

## File Locations
- DataManagementEngineStandard/Editor/Importing/DataImportManager.cs
- DataManagementEngineStandard/Editor/Importing/README.md

## Example
```csharp
using var importManager = new DataImportManager(editor);

var config = importManager.CreateImportConfiguration(
    "LegacyCustomers",
    "LegacyDB",
    "Customers",
    "MainDB");

config.SelectedFields = new List<string> { "Name", "Email" };
config.BatchSize = 200;
config.ApplyDefaults = true;

var validation = importManager.ValidationHelper.ValidateConfiguration(config);
if (validation.Flag == Errors.Ok)
{
    await importManager.RunImportAsync(config, new Progress<IPassedArgs>(), CancellationToken.None);
}
```

## Task-Specific Examples

### Custom Transformation
```csharp
config.CustomTransformation = record =>
{
    if (record is Dictionary<string, object> dict)
    {
        dict["ImportedAt"] = DateTime.UtcNow;
    }
    return record;
};

await importManager.RunImportAsync(config, new Progress<IPassedArgs>(), CancellationToken.None);
```

### Batch Retry Configuration
```csharp
config.BatchSize = 200;
config.MaxRetries = 3;
config.RetryDelay = TimeSpan.FromSeconds(5);

await importManager.RunImportAsync(config, new Progress<IPassedArgs>(), CancellationToken.None);
```