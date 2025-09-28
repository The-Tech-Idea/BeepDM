# ETLEntityProcessor examples

Example 1: Validate and split
```csharp
var processor = new ETLEntityProcessor();
var (valid, invalid) = processor.ValidateRecords(records, r =>
{
    var email = r.GetType().GetProperty("Email")?.GetValue(r)?.ToString();
    return !string.IsNullOrWhiteSpace(email) && email.Contains("@");
});
```

Example 2: Transform
```csharp
var transformed = processor.TransformRecords(valid, r =>
{
    // Uppercase a field
    var nameProp = r.GetType().GetProperty("Name");
    var val = nameProp?.GetValue(r)?.ToString();
    if (val != null) nameProp?.SetValue(r, val.ToUpperInvariant());
    return r;
});
```

Example 3: Async process
```csharp
await processor.ProcessRecordsAsync(transformed, async r =>
{
    // Do something async, e.g., insert
    await Task.CompletedTask;
}, parallel: true);
```
