# ETLEntityProcessor how-to

Purpose
- Utilities for validating, transforming, and processing record batches.

Validate
```csharp
var processor = new ETLEntityProcessor();
var (valid, invalid) = processor.ValidateRecords(records, r => /* your rule */ true);
```

Transform
```csharp
var transformed = processor.TransformRecords(records, r => {
    // map or enrich the record
    return r;
});
```

Process asynchronously
```csharp
await processor.ProcessRecordsAsync(transformed, async r => {
    // e.g., insert to destination
    await Task.CompletedTask;
}, parallel: true);
```

Notes
- Keep `transformDelegate` side-effect free for predictability.
- Use parallel mode when downstream operations are I/O bound and thread-safe.
