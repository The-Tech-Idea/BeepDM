# ETLEntityCopyHelper examples

Example 1: Copy structure only
```csharp
var helper = new ETLEntityCopyHelper(dmeEditor);
helper.CopyEntityStructure(sourceDs, destDs, "Products", "Products", true,
    new Progress<PassedArgs>(p => Console.WriteLine(p.Messege)), CancellationToken.None);
```

Example 2: Copy data with optional mapping
```csharp
var mapT = MappingManager.CreateEntityMap(dmeEditor, "Products", "LegacyDB", "Products", "ModernDB");
var mapDtl = mapT.Item2.MappedEntities.FirstOrDefault();

helper.CopyEntityData(sourceDs, destDs, "Products", "Products",
    new Progress<PassedArgs>(p => Console.WriteLine(p.Messege)),
    CancellationToken.None, mapDtl);
```
