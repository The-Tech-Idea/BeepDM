---
name: etl
description: Expert guidance for Extract, Transform, Load (ETL) operations using ETLEditor. Use when migrating databases, copying data between datasources, creating entity structures, or implementing data transformation pipelines.
---

# ETL Operations Guide

Expert guidance for Extract, Transform, Load (ETL) operations using ETLEditor, which orchestrates script generation, entity creation, data copying, and imports across datasources.

## Overview

**ETLEditor** provides comprehensive ETL capabilities:
- **Script Generation**: Creates scripts for entity creation and data copying
- **Entity Creation**: Creates entities in destination datasource
- **Data Copying**: Copies data from source to destination with transformation
- **Import Operations**: Handles data import with mapping support
- **Validation**: Validates operations before execution

## Core Components

### ETLEditor
Main orchestrator for ETL operations.

### ETLScriptHDR
Header containing script metadata and list of script details.

### ETLScriptDet
Individual script detail defining source/destination and operation type.

### ETL Components
- **ETLDataCopier**: Handles data copying between entities
- **ETLEntityProcessor**: Processes entities during ETL operations
- **ETLEntityCopyHelper**: Helper for entity copying operations
- **ETLScriptBuilder**: Builds ETL scripts programmatically
- **ETLScriptManager**: Manages ETL script execution
- **ETLValidator**: Validates ETL operations

## Basic Workflow

### Step 1: Create Script Header

```csharp
var etl = new ETLEditor(dmeEditor);

// Create script header from source datasource
var progress = new Progress<PassedArgs>(p => Console.WriteLine(p.Messege));
var cancellationToken = new CancellationTokenSource().Token;

etl.CreateScriptHeader(sourceDataSource, progress, cancellationToken);
```

### Step 2: Filter/Modify Scripts (Optional)

```csharp
// Filter scripts to specific entities
etl.Script.ScriptDetails = etl.Script.ScriptDetails
    .Where(s => s.SourceEntityName == "Customers" || s.SourceEntityName == "Orders")
    .ToList();

// Modify script details if needed
foreach (var script in etl.Script.ScriptDetails)
{
    script.DestinationEntityName = $"New_{script.SourceEntityName}";
}
```

### Step 3: Execute Script

```csharp
// Execute: create entities, then optionally copy data
var result = await etl.RunCreateScript(
    progress, 
    cancellationToken, 
    copydata: true,           // Copy data after creating entities
    useEntityStructure: true  // Use EntityStructure for creation
);
```

## Entity Creation

### Create Entities from Source

```csharp
var etl = new ETLEditor(dmeEditor);

// Get source datasource
var sourceDs = dmeEditor.GetDataSource("SourceDatabase");

// Create script header (includes entity creation scripts)
etl.CreateScriptHeader(sourceDs, progress, cancellationToken);

// Execute entity creation only (no data copy)
var result = await etl.RunCreateScript(
    progress, 
    cancellationToken, 
    copydata: false,
    useEntityStructure: true
);
```

### Create Specific Entities

```csharp
// Get create script for specific entities
var entities = new List<string> { "Customers", "Orders", "Products" };
var scripts = etl.GetCreateEntityScript(
    sourceDs, 
    entities, 
    progress, 
    cancellationToken,
    copydata: false
);

// Add to script details
etl.Script.ScriptDetails.AddRange(scripts);

// Execute
await etl.RunCreateScript(progress, cancellationToken, copydata: false);
```

## Data Copying

### Copy Data Between Entities

```csharp
var etl = new ETLEditor(dmeEditor);

// Create script header
etl.CreateScriptHeader(sourceDs, progress, cancellationToken);

// Execute with data copying
var result = await etl.RunCreateScript(
    progress, 
    cancellationToken, 
    copydata: true,  // Copy data after creating entities
    useEntityStructure: true
);
```

### Copy Specific Entity Data

```csharp
// Create script for specific entity with data copy
var scripts = etl.GetCreateEntityScript(
    sourceDs, 
    new List<string> { "Customers" }, 
    progress, 
    cancellationToken,
    copydata: true  // Include data copy script
);

// Execute copy script
foreach (var script in scripts.Where(s => s.ScriptType == DDLScriptType.CopyData))
{
    await etl.RunCopyEntityScript(script, progress, cancellationToken);
}
```

## Import Operations

### Import with Mapping

```csharp
var etl = new ETLEditor(dmeEditor);

// Prepare mapping using MappingManager
var mapTuple = MappingManager.CreateEntityMap(
    dmeEditor, 
    "LegacyCustomers",    // Source entity
    "LegacyDB",           // Source datasource
    "Customers",          // Destination entity
    "MainDB"              // Destination datasource
);

var entityMap = mapTuple.Item2;
var selected = entityMap.MappedEntities.First();

// Create import script from mapping
etl.CreateImportScript(entityMap, selected);

// Execute import
var importResult = await etl.RunImportScript(progress, cancellationToken);
```

### Import with Custom Transformation

```csharp
// Create import script
etl.CreateImportScript(entityMap, selected);

// Modify script details for custom transformation
foreach (var script in etl.Script.ScriptDetails)
{
    // Add custom transformation logic
    script.CustomTransformation = (record) =>
    {
        if (record is Dictionary<string, object> dict)
        {
            // Transform data
            dict["ImportedDate"] = DateTime.Now;
            dict["ImportedBy"] = Environment.UserName;
        }
        return record;
    };
}

// Execute import
await etl.RunImportScript(progress, cancellationToken);
```

## Script Management

### Get Create Entity Script

```csharp
// Get script for specific entities
var entities = new List<string> { "Customers", "Orders" };
var scripts = etl.GetCreateEntityScript(
    sourceDs, 
    entities, 
    progress, 
    cancellationToken,
    copydata: false
);

// Review scripts before execution
foreach (var script in scripts)
{
    Console.WriteLine($"Source: {script.SourceEntityName}");
    Console.WriteLine($"Destination: {script.DestinationEntityName}");
    Console.WriteLine($"Type: {script.ScriptType}");
}
```

### Run Copy Entity Script

```csharp
// Run copy script for specific entity
var copyScript = new ETLScriptDet
{
    SourceDataSourceName = "SourceDB",
    SourceEntityName = "Customers",
    DestinationDataSourceName = "DestDB",
    DestinationEntityName = "Customers",
    ScriptType = DDLScriptType.CopyData
};

var result = await etl.RunCopyEntityScript(copyScript, progress, cancellationToken);
```

## Error Handling

### Stop Error Count

```csharp
// Set maximum errors before stopping
etl.StopErrorCount = 10;  // Stop after 10 errors

// Execute with error limit
await etl.RunCreateScript(progress, cancellationToken, copydata: true);
```

### Check Load Data Logs

```csharp
// After execution, check logs
foreach (var log in etl.LoadDataLogs)
{
    if (log.Flag == Errors.Failed)
    {
        Console.WriteLine($"Error in {log.EntityName}: {log.Message}");
    }
    else
    {
        Console.WriteLine($"Success: {log.EntityName} - {log.RecordsProcessed} records");
    }
}
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

await etl.RunCreateScript(progress, cancellationToken, copydata: true);
```

## Advanced Patterns

### Pattern 1: Database Migration

```csharp
public async Task MigrateDatabaseAsync(
    IDMEEditor editor,
    string sourceDbName,
    string destDbName)
{
    var etl = new ETLEditor(editor);
    
    // Get source datasource
    var sourceDs = editor.GetDataSource(sourceDbName);
    
    // Create script header
    var progress = new Progress<PassedArgs>(args => Console.WriteLine(args.Messege));
    var cancellationToken = new CancellationTokenSource().Token;
    
    etl.CreateScriptHeader(sourceDs, progress, cancellationToken);
    
    // Update destination datasource name
    foreach (var script in etl.Script.ScriptDetails)
    {
        script.DestinationDataSourceName = destDbName;
    }
    
    // Execute migration
    var result = await etl.RunCreateScript(
        progress, 
        cancellationToken, 
        copydata: true,
        useEntityStructure: true
    );
    
    // Check results
    var errors = etl.LoadDataLogs.Where(l => l.Flag == Errors.Failed).ToList();
    if (errors.Any())
    {
        Console.WriteLine($"Migration completed with {errors.Count} errors");
    }
}
```

### Pattern 2: Selective Entity Copy

```csharp
public async Task CopySelectedEntitiesAsync(
    IDMEEditor editor,
    string sourceDb,
    string destDb,
    List<string> entityNames)
{
    var etl = new ETLEditor(editor);
    var sourceDs = editor.GetDataSource(sourceDb);
    
    // Get scripts for selected entities only
    var scripts = etl.GetCreateEntityScript(
        sourceDs, 
        entityNames, 
        new Progress<PassedArgs>(_ => {}), 
        CancellationToken.None,
        copydata: true
    );
    
    // Update destination
    foreach (var script in scripts)
    {
        script.DestinationDataSourceName = destDb;
    }
    
    // Execute each script
    foreach (var script in scripts)
    {
        if (script.ScriptType == DDLScriptType.CreateEntity)
        {
            // Create entity first
            await etl.RunCreateEntityScript(script, progress, cancellationToken);
        }
        else if (script.ScriptType == DDLScriptType.CopyData)
        {
            // Then copy data
            await etl.RunCopyEntityScript(script, progress, cancellationToken);
        }
    }
}
```

### Pattern 3: Incremental Data Copy

```csharp
public async Task IncrementalCopyAsync(
    IDMEEditor editor,
    string sourceDb,
    string destDb,
    string entityName,
    DateTime lastSyncDate)
{
    var etl = new ETLEditor(editor);
    
    // Create copy script
    var copyScript = new ETLScriptDet
    {
        SourceDataSourceName = sourceDb,
        SourceEntityName = entityName,
        DestinationDataSourceName = destDb,
        DestinationEntityName = entityName,
        ScriptType = DDLScriptType.CopyData
    };
    
    // Add filter for incremental copy
    copyScript.SourceFilters = new List<AppFilter>
    {
        new AppFilter
        {
            FieldName = "ModifiedDate",
            Operator = ">=",
            FilterValue = lastSyncDate.ToString()
        }
    };
    
    // Execute copy
    await etl.RunCopyEntityScript(copyScript, progress, cancellationToken);
}
```

## Best Practices

### 1. Always Validate Before Execution
```csharp
// Check datasource accessibility
var sourceDs = editor.GetDataSource("SourceDB");
if (sourceDs == null || sourceDs.ConnectionStatus != ConnectionState.Open)
{
    throw new InvalidOperationException("Source datasource not accessible");
}
```

### 2. Use EntityStructure for Creation
```csharp
// Always use EntityStructure for accurate entity creation
await etl.RunCreateScript(progress, cancellationToken, copydata: false, useEntityStructure: true);
```

### 3. Set Appropriate Stop Error Count
```csharp
// For production, set reasonable error limit
etl.StopErrorCount = 10;  // Stop after 10 errors

// For development, allow more errors for debugging
etl.StopErrorCount = 100;
```

### 4. Monitor Load Data Logs
```csharp
// Always check logs after execution
var errors = etl.LoadDataLogs.Where(l => l.Flag == Errors.Failed).ToList();
if (errors.Any())
{
    // Handle errors appropriately
    foreach (var error in errors)
    {
        _logger.LogError($"ETL Error: {error.EntityName} - {error.Message}");
    }
}
```

### 5. Handle Cancellation
```csharp
try
{
    await etl.RunCreateScript(progress, cancellationToken, copydata: true);
}
catch (OperationCanceledException)
{
    Console.WriteLine("ETL operation was cancelled");
    // Clean up partial results if needed
}
```

## Related Skills

- **@beepdm** - Core BeepDM architecture and IDataSource usage
- **@mapping** - Entity mapping for import operations
- **@importing** - DataImportManager for advanced import scenarios
- **@connection** - Connection management for datasources

## Key Files

- `ETLEditor.cs` - Main ETL orchestrator
- `ETLDataCopier.cs` - Data copying operations
- `ETLEntityProcessor.cs` - Entity processing
- `ETLEntityCopyHelper.cs` - Entity copy helper
- `ETLScriptBuilder.cs` - Script building
- `ETLScriptManager.cs` - Script management
- `ETLValidator.cs` - ETL validation
