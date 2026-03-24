# ETLValidator how-to

Purpose
- Validate mappings, entities, and ETL consistency before execution.

Validate a full mapping
```csharp
var validator = new ETLValidator(dmeEditor);
var result = validator.ValidateEntityMapping(entityMap);
if (result.Flag != Errors.Ok)
{
    foreach (var err in ((ErrorsInfo)result).Errors)
        Console.WriteLine(err.Message);
}
```

Validate mapped entity detail
```csharp
var mapped = entityMap.MappedEntities.First();
var res = validator.ValidateMappedEntity(mapped);
```

Validate source/destination entities
```csharp
var res = validator.ValidateEntityConsistency(sourceDs, destDs, "Customers", "Customers");
```

Check mapping entity exists
```csharp
var res = validator.CheckIfMappingEntityExists(entityMap, "Orders");
```

Notes
- Ensures required fields are present and names are non-empty.
- Field-level checks confirm mapping entries have both sides defined.
