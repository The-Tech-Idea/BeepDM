# ETLEntityCopyHelper how-to

Purpose
- Thin helper to copy a single entity structure and/or rows between sources.
- Uses MappingManager and MappingDefaultsHelper for robust transformation.

Copy structure
```csharp
var helper = new ETLEntityCopyHelper(dmeEditor);
helper.CopyEntityStructure(sourceDs, destDs, "Orders", "Orders", createIfMissing: true,
    progress: new Progress<PassedArgs>(p => Console.WriteLine(p.Messege)), token: CancellationToken.None);
```

Copy data
```csharp
// Optional: build or load a mapping
var mappingTuple = MappingManager.CreateEntityMap(dmeEditor, "Orders", "Legacy", "Orders", "Modern");
var mapDtl = mappingTuple.Item2.MappedEntities.FirstOrDefault();

helper.CopyEntityData(sourceDs, destDs, "Orders", "Orders", progress, CancellationToken.None, mapDtl);
```

Notes
- Defaults are applied after mapping and only for null/default fields.
- FK constraints are disabled/enabled around copy for RDBMS targets.
- Supports DataTable, IEnumerable, and single-object inputs.
