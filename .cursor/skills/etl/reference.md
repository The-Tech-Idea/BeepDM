# ETL Quick Reference

## Basic Workflow

```csharp
var etl = new ETLEditor(dmeEditor);

// 1. Create script header
etl.CreateScriptHeader(sourceDs, progress, cancellationToken);

// 2. Filter/modify (optional)
etl.Script.ScriptDetails = etl.Script.ScriptDetails.Where(...).ToList();

// 3. Execute
await etl.RunCreateScript(progress, cancellationToken, copydata: true, useEntityStructure: true);
```

## Entity Creation

```csharp
// Create entities from source
etl.CreateScriptHeader(sourceDs, progress, cancellationToken);
await etl.RunCreateScript(progress, cancellationToken, copydata: false);

// Create specific entities
var scripts = etl.GetCreateEntityScript(sourceDs, entityNames, progress, cancellationToken);
```

## Data Copying

```csharp
// Copy data
await etl.RunCreateScript(progress, cancellationToken, copydata: true);

// Copy specific entity
await etl.RunCopyEntityScript(script, progress, cancellationToken);
```

## Import with Mapping

```csharp
// Create mapping
var (error, entityMap) = MappingManager.CreateEntityMap(dmeEditor, "Source", "SourceDB", "Dest", "DestDB");
var selected = entityMap.MappedEntities.First();

// Create and run import script
etl.CreateImportScript(entityMap, selected);
await etl.RunImportScript(progress, cancellationToken);
```

## Error Handling

```csharp
etl.StopErrorCount = 10; // Stop after 10 errors

// Check logs
foreach (var log in etl.LoadDataLogs)
{
    if (log.Flag == Errors.Failed)
        Console.WriteLine($"Error: {log.Message}");
}
```
