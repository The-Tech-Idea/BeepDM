# ETLValidator examples

Example 1: Validate mapping configuration
```csharp
var validator = new ETLValidator(dmeEditor);
var result = validator.ValidateEntityMapping(entityMap);

if (result.Flag != Errors.Ok)
{
    Console.WriteLine("Mapping invalid:");
    foreach (var err in ((ErrorsInfo)result).Errors)
        Console.WriteLine("- " + err.Message);
}
```

Example 2: Validate mapped entity detail
```csharp
var detail = entityMap.MappedEntities.First();
var detailResult = validator.ValidateMappedEntity(detail);
```

Example 3: Validate entity consistency
```csharp
var consistency = validator.ValidateEntityConsistency(sourceDs, destDs, "Orders", "Orders");
```
