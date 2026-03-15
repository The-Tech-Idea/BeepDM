# ETL Quick Reference

## ETLEditor Initialization

```csharp
var etl = new ETLEditor(dmeEditor);
var progress = new Progress<PassedArgs>(p => Console.WriteLine(p.Messege));
var token = CancellationToken.None;
```

## Full Migration (Create + Copy Data)

```csharp
etl.CreateScriptHeader(sourceDs, progress, token);

// Point scripts to destination datasource
foreach (var step in etl.Script.ScriptDetails)
{
    step.DestinationDataSourceName = destDs.DatasourceName;
}

// Optional: migrate only selected entities
etl.Script.ScriptDetails = etl.Script.ScriptDetails
    .Where(s => s.SourceEntityName == "Customers" || s.SourceEntityName == "Orders")
    .ToList();

var result = await etl.RunCreateScript(progress, token, copydata: true, useEntityStructure: true);
if (result.Flag != Errors.Ok)
{
    Console.WriteLine(result.Message);
}
```

## Structure-Only Migration

```csharp
var createOnlyScripts = etl.GetCreateEntityScript(
    sourceDs,
    new List<string> { "Customers", "Orders" },
    progress,
    token,
    copydata: false);

foreach (var s in createOnlyScripts)
{
    s.DestinationDataSourceName = destDs.DatasourceName;
}

etl.Script.ScriptDetails = createOnlyScripts;
await etl.RunCreateScript(progress, token, copydata: false, useEntityStructure: true);
```

## Data-Only Copy into Existing Schema

```csharp
var entityStructures = new List<EntityStructure>
{
    sourceDs.GetEntityStructure("Customers", true),
    sourceDs.GetEntityStructure("Orders", true)
}.Where(e => e != null).ToList();

var copyScripts = etl.GetCopyDataEntityScript(destDs, entityStructures, progress, token);
etl.Script.ScriptDetails = copyScripts;

await etl.RunCreateScript(progress, token, copydata: true, useEntityStructure: true);
```

## Direct Copy Methods (No Script Editing)

```csharp
// Copy one entity structure
etl.CopyEntityStructure(sourceDs, destDs, "Customers", "Customers", progress, token, CreateMissingEntity: true);

// Copy one entity data set
etl.CopyEntityData(sourceDs, destDs, "Customers", "Customers", progress, token, CreateMissingEntity: true);

// Copy all datasource entities
etl.CopyDatasourceData(sourceDs, destDs, progress, token, CreateMissingEntity: true);
```

## Mapping Import with Preflight Validation

```csharp
// Optional caller-side validation (ETL runtime also performs preflight internally).
var validator = new ETLValidator(dmeEditor);
var mappingCheck = validator.ValidateEntityMapping(entityMap);
if (mappingCheck.Flag != Errors.Ok) throw new InvalidOperationException(mappingCheck.Message);

var selectedMapping = entityMap.MappedEntities.First();
etl.CreateImportScript(entityMap, selectedMapping);
var importResult = await etl.RunImportScript(progress, token, useEntityStructure: true);
```

## Entity Consistency Check Before Copy

```csharp
var validator = new ETLValidator(dmeEditor);
var consistency = validator.ValidateEntityConsistency(sourceDs, destDs, "Customers", "Customers");
if (consistency.Flag != Errors.Ok)
{
    foreach (var err in consistency.Errors)
    {
        Console.WriteLine(err.Message);
    }
}
```

## Runtime Controls and Logging

```csharp
etl.StopErrorCount = 10; // stop after 10 failures

var result = await etl.RunCreateScript(progress, token, copydata: true, useEntityStructure: true);

foreach (var line in etl.LoadDataLogs)
{
    Console.WriteLine(line.InputLine);
}

if (result.Flag != Errors.Ok)
{
    Console.WriteLine($"ETL failed: {result.Message}");
}
```

## Save and Reload ETL Scripts

```csharp
// SaveETL validates and persists through ETLScriptManager canonical path.
etl.SaveETL(destDs.DatasourceName);

// LoadETL resolves manager path first, then legacy fallback path.
etl.LoadETL(destDs.DatasourceName);

// Rerun loaded script
await etl.RunCreateScript(progress, token, copydata: true, useEntityStructure: true);
```

## Review ETL Evidence Artifacts

```csharp
var evidenceRoot = Path.Combine(dmeEditor.ConfigEditor.ExePath, "Scripts", "ETL_Evidence");
var rolling = Path.Combine(evidenceRoot, "ETL_EVIDENCE_SUMMARY.md");
var weekly = Path.Combine(evidenceRoot, "ETL_EVIDENCE_CURRENT_WEEK.md");
var monthly = Path.Combine(evidenceRoot, "ETL_EVIDENCE_CURRENT_MONTH.md");
```

## ETLDataCopier for Async Batch Copy

```csharp
var copier = new ETLDataCopier(dmeEditor);

var copyResult = await copier.CopyEntityDataAsync(
    sourceDs,
    destDs,
    srcEntity: "Customers",
    destEntity: "Customers",
    progress: progress,
    token: token,
    map_DTL: null,
    customTransformation: null,
    batchSize: 500,
    enableParallel: true,
    maxRetries: 3);
```
