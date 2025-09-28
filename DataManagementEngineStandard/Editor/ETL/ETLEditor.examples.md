# ETLEditor examples

Example 1: Full migration (create entities + copy data)
```csharp
var etl = new ETLEditor(dmeEditor);
var srcDs = dmeEditor.GetDataSource("LegacyDB");
var destDs = dmeEditor.GetDataSource("ModernDB");

// Build header from source
etl.CreateScriptHeader(srcDs, new Progress<PassedArgs>(p => Console.WriteLine(p.Messege)), CancellationToken.None);

// Filter to entities you want
etl.Script.ScriptDTL = etl.Script.ScriptDTL
    .Where(s => new[]{"Customers","Orders"}.Contains(s.sourceentityname))
    .ToList();

// Execute
var result = await etl.RunCreateScript(new Progress<PassedArgs>(p => Console.WriteLine(p.Messege)), CancellationToken.None, copydata: true);
```

Example 2: Import with mapping
```csharp
var map = MappingManager.CreateEntityMap(dmeEditor, "LegacyOrders", "LegacyDB", "Orders", "ModernDB");
var selected = map.Item2.MappedEntities.First();

etl.CreateImportScript(map.Item2, selected);
var res = await etl.RunImportScript(new Progress<PassedArgs>(p => Console.WriteLine(p.Messege)), CancellationToken.None);
```
