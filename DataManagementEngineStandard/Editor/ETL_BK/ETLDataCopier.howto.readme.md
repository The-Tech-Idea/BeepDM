# ETLDataCopier how-to

Purpose
- High-throughput data copier with batching, optional parallelism, and retry.
- Applies mapping and centralized defaults (via DefaultsManager) to each record before insert.

Quick start
```csharp
var copier = new ETLDataCopier(dmeEditor);

var result = await copier.CopyEntityDataAsync(
    sourceDs: dmeEditor.GetDataSource("Legacy"),
    destDs:   dmeEditor.GetDataSource("Modern"),
    srcEntity: "Customers",
    destEntity: "Customers",
    progress: new Progress<PassedArgs>(p => Console.WriteLine(p.Messege)),
    token: CancellationToken.None,
    map_DTL: selectedMapping,     // optional EntityDataMap_DTL
    customTransformation: r => r, // optional transformation delegate
    batchSize: 200,
    enableParallel: true,
    maxRetries: 2);
```

Defaults application
- Defaults are applied per transformed record using `MappingDefaultsHelper.ApplyDefaultsToObject`.
- Only null/default destination fields are filled; mapped values are preserved.
- Defaults resolution goes through `DefaultsManager` (static values and rule-based values).

Tips
- Use `batchSize` and `enableParallel` according to dataset size and destination capabilities.
- Provide a `customTransformation` for last-mile enrichment/cleansing after mapping/defaults.
- Monitor `progress` for per-record insert feedback.
