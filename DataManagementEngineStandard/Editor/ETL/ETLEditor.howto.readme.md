# ETLEditor how-to

Purpose
- Orchestrates ETL: script generation, entity creation, data copying, and imports.

Basic workflow
```csharp
var etl = new ETLEditor(dmeEditor);

// 1) Build script header from source
etl.CreateScriptHeader(sourceDs, new Progress<PassedArgs>(p => Console.WriteLine(p.Messege)), token);

// 2) Optionally filter/modify scripts
// etl.Script.ScriptDTL = etl.Script.ScriptDTL.Where(...).ToList();

// 3) Execute (create entities, then optionally copy data)
var result = await etl.RunCreateScript(progress, token, copydata: true, useEntityStructure: true);
```

Import workflow
```csharp
// Prepare mapping
var mapTuple = MappingManager.CreateEntityMap(dmeEditor, "LegacyCustomers", "LegacyDB", "Customers", "MainDB");
var entityMap = mapTuple.Item2;
var selected = entityMap.MappedEntities.First();

// Create and run import script
etl.CreateImportScript(entityMap, selected);
var importResult = await etl.RunImportScript(progress, token);
```

Notes
- Entity creation uses destination datasource’s `CreateEntityAs`.
- CopyData steps call internal `RunCopyEntityScript` which fetches, maps, applies defaults (via MappingDefaultsHelper inside the copier path) and inserts.
- Use `StopErrorCount` to guard execution.
- Check `LoadDataLogs` for detailed run logs.
