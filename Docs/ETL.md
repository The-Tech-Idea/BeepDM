# ETL Operations Guide

## Overview

BeepDM provides comprehensive ETL (Extract, Transform, Load) capabilities through the `ETLEditor` and related components for migrating data between datasources, generating ETL scripts, and running direct schema/data copy operations.

## Core Runtime Types

- `ETLEditor` - Main ETL orchestrator
- `ETLScriptHDR` - Script header definition
- `ETLScriptDet` - Script detail/line items
- `ETLValidator` - ETL validation logic
- `ETLDataCopier` - Direct data copy between datasources
- `ETLEntityCopyHelper` - Entity-level copy operations
- `ETLScriptBuilder` - Script generation
- `ETLScriptManager` - Script persistence and execution
- `ETLEntityProcessor` - Entity processing pipeline

## File Locations

- `DataManagementEngineStandard/Editor/ETL/ETLEditor.cs`
- `DataManagementEngineStandard/Editor/ETL/ETLDataCopier.cs`
- `DataManagementEngineStandard/Editor/ETL/ETLEntityCopyHelper.cs`
- `DataManagementEngineStandard/Editor/ETL/ETLEntityProcessor.cs`
- `DataManagementEngineStandard/Editor/ETL/ETLScriptBuilder.cs`
- `DataManagementEngineStandard/Editor/ETL/ETLScriptManager.cs`
- `DataManagementEngineStandard/Editor/ETL/ETLValidator.cs`

## Working Rules

1. Resolve and open source/destination datasources before high-cost runs.
2. Keep destination names and entity mappings explicit.
3. Treat ETL script persistence and execution as part of the workflow, not incidental output.
4. Use ETL for direct copy/script flows; hand off to importing when governance features dominate.

## Basic Data Copy

```csharp
// Get source and destination datasources
var sourceDs = editor.GetDataSource("SourceDB");
var targetDs = editor.GetDataSource("TargetDB");

// Ensure both are open
sourceDs.Openconnection();
targetDs.Openconnection();

// Copy data directly
var copier = new ETLDataCopier(editor);
var result = await copier.CopyDataAsync(
    sourceDs, 
    "Customers", 
    targetDs, 
    "Customers",
    progress: progressReporter);

if (result.Success)
{
    Console.WriteLine($"Copied {result.RecordsProcessed} records");
}
```

## Entity Copy with Mapping

```csharp
var entityCopyHelper = new ETLEntityCopyHelper(editor);

// Copy with field mapping
var mapping = new Dictionary<string, string>
{
    ["CustomerName"] = "Name",
    ["CustomerEmail"] = "Email",
    ["PhoneNumber"] = "Phone"
};

var result = await entityCopyHelper.CopyEntityAsync(
    sourceDs, "LegacyCustomers",
    targetDs, "Customers",
    fieldMapping: mapping,
    progress: progressReporter);
```

## Script-Based ETL

```csharp
// Build ETL script
var scriptBuilder = new ETLScriptBuilder(editor);
var script = scriptBuilder.CreateScript()
    .WithSource("SourceDB", "Customers")
    .WithTarget("TargetDB", "Customers")
    .WithTransformation(transform =>
    {
        // Apply transformations
        transform.MapField("OldName", "NewName");
        transform.AddComputedField("FullName", row => $"{row["FirstName"]} {row["LastName"]}");
        transform.Filter(row => row["IsActive"].ToString() == "true");
    })
    .Build();

// Save script for later
var scriptManager = new ETLScriptManager(editor);
scriptManager.SaveScript(script, "CustomerMigration");

// Execute script
var executionResult = await scriptManager.ExecuteScriptAsync("CustomerMigration");
```

## Validation

```csharp
var validator = new ETLValidator(editor);

// Validate before running
var validationResult = validator.ValidateETLJob(
    sourceDs, "Customers",
    targetDs, "Customers");

if (!validationResult.IsValid)
{
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"Validation Error: {error}");
    }
}
```

## Entity Processor Pipeline

```csharp
var processor = new ETLEntityProcessor(editor);

// Configure processing pipeline
processor.ConfigurePipeline(pipeline =>
{
    pipeline.AddStep(new DataCleaningStep());
    pipeline.AddStep(new ValidationStep());
    pipeline.AddStep(new TransformationStep());
    pipeline.AddStep(new LoadStep(targetDs));
});

// Process entities
var result = await processor.ProcessAsync(sourceDs.GetEntity("Customers", null));
```

## Working with ETLScriptDet

```csharp
// Create script detail line
var scriptDet = new ETLScriptDet
{
    EntityName = "Customers",
    SourceEntityName = "LegacyCustomers",
    Mapping = new List<Mapping_rep_fields>
    {
        new Mapping_rep_fields 
        { 
            FromField = "OldName", 
            ToField = "NewName" 
        }
    }
};

// Add to script header
var scriptHdr = new ETLScriptHDR
{
    ScriptName = "MigrationScript",
    Details = new List<ETLScriptDet> { scriptDet }
};
```

## Progress Reporting

```csharp
var progress = new Progress<PassedArgs>(args =>
{
    Console.WriteLine($"ETL Progress: {args.Messege}");
    Console.WriteLine($"Records: {args.ObjectsProcessed}");
});

var result = await copier.CopyDataAsync(
    sourceDs, "Customers",
    targetDs, "Customers",
    progress: progress);
```

## Error Handling

```csharp
try
{
    var result = await etlOperation.ExecuteAsync();
    if (!result.Success)
    {
        editor.Logger.WriteLog($"ETL Failed: {result.ErrorMessage}");
        editor.ErrorObject.Flag = Errors.Failed;
        editor.ErrorObject.Message = result.ErrorMessage;
    }
}
catch (Exception ex)
{
    editor.Logger.WriteLog($"ETL Exception: {ex.Message}");
    editor.ErrorObject.Flag = Errors.Failed;
    editor.ErrorObject.Message = ex.Message;
    editor.ErrorObject.Ex = ex;
}
```

## Related Documentation

- [Core Architecture](CoreArchitecture.md) - IDMEEditor overview
- [Data Source Implementation](HowToCreateNewDataSource.md) - Building custom data sources
- [Unit of Work Pattern](UnitOfWork.md) - Transactional operations
- [Forms Manager](FormsManager.md) - UI orchestration
