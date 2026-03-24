# ETLDataCopier examples

Example 1: Simple copy with defaults and mapping
```csharp
var copier = new ETLDataCopier(dmeEditor);

var srcDs = dmeEditor.GetDataSource("LegacyDB");
var destDs = dmeEditor.GetDataSource("ModernDB");

var mapTuple = MappingManager.CreateEntityMap(
    dmeEditor,
    "LegacyCustomers", "LegacyDB",
    "Customers", "ModernDB");
var selectedMap = mapTuple.Item2.MappedEntities.FirstOrDefault();

var progress = new Progress<PassedArgs>(p => Console.WriteLine(p.Messege));
using var cts = new CancellationTokenSource();

var result = await copier.CopyEntityDataAsync(
    sourceDs: srcDs,
    destDs: destDs,
    srcEntity: "LegacyCustomers",
    destEntity: "Customers",
    progress: progress,
    token: cts.Token,
    map_DTL: selectedMap,
    customTransformation: rec => rec, // optional last-mile changes
    batchSize: 250,
    enableParallel: true,
    maxRetries: 2);
```

Example 2: Transformation that stamps audit fields after defaults
```csharp
Func<object, object> addAudit = rec => {
    dmeEditor.Utilfunction.SetFieldValueFromObject("ImportedAt", rec, DateTime.UtcNow);
    dmeEditor.Utilfunction.SetFieldValueFromObject("ImportedBy", rec, Environment.UserName);
    return rec;
};

await copier.CopyEntityDataAsync(srcDs, destDs, "Orders", "Orders", progress, CancellationToken.None, null, addAudit, 200);
```
