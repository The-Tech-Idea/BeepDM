---
name: etl
description: Guidance for ETLEditor usage, script generation, entity creation, and data copy operations in BeepDM.
---

# ETL Operations Guide

Use this skill when migrating data between datasources, creating entities, or running ETL copy scripts.

## Core Types
- ETLEditor: orchestrates ETL operations
- ETLScriptHDR: script header
- ETLScriptDet: individual steps (create, copy)

## Workflow
1. Create `ETLEditor` with editor.
2. Call `CreateScriptHeader(sourceDs, progress, token)`.
3. Optionally filter or edit `ScriptDetails`.
4. Run `RunCreateScript(progress, token, copydata, useEntityStructure)`.

## Validation
- Check returned `IErrorsInfo.Flag` and `LoadDataLogs` for failures.
- Use `ETLValidator` when validating preconditions.

## Pitfalls
- Copying data without creating entities first will fail.
- Large copy without batching can be slow or memory heavy.
- Forgetting to update destination datasource name in scripts can copy back to source.

## File Locations
- DataManagementEngineStandard/Editor/ETL/ETLEditor.cs
- DataManagementEngineStandard/Editor/ETL/ETLScriptBuilder.cs
- DataManagementEngineStandard/Editor/ETL/ETLScriptManager.cs

## Example
```csharp
var etl = new ETLEditor(editor);
var progress = new Progress<PassedArgs>(p => Console.WriteLine(p.Messege));
var token = CancellationToken.None;

etl.CreateScriptHeader(sourceDs, progress, token);

// Optional: keep only some entities
etl.Script.ScriptDetails = etl.Script.ScriptDetails
    .Where(s => s.SourceEntityName == "Customers")
    .ToList();

await etl.RunCreateScript(progress, token, copydata: true, useEntityStructure: true);
```

## Task-Specific Examples

### Migrate Specific Entities Only
```csharp
var etl = new ETLEditor(editor);
etl.CreateScriptHeader(sourceDs, progress, token);

etl.Script.ScriptDetails = etl.Script.ScriptDetails
    .Where(s => s.SourceEntityName == "Customers" || s.SourceEntityName == "Orders")
    .ToList();

await etl.RunCreateScript(progress, token, copydata: true, useEntityStructure: true);
```

### Import Using Mapping
```csharp
var etl = new ETLEditor(editor);
var (errorInfo, map) = MappingManager.CreateEntityMap(
    editor,
    "LegacyCustomers",
    "LegacyDB",
    "Customers",
    "MainDB");

if (errorInfo.Flag == Errors.Ok)
{
    var selected = map.MappedEntities.First();
    etl.CreateImportScript(map, selected);
    await etl.RunImportScript(progress, token);
}
```