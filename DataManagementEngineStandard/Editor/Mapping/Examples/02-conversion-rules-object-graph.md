# 02 - Conversion, Rules, and Object Graph

## Goal
Apply conversion policy, rule-based field behavior, and nested object graph mapping.

## Example
```csharp
MappingManager.SetConversionPolicy(
    destinationDataSource: "MainDb",
    destinationEntity: "Customers",
    policy: new MappingConversionPolicy
    {
        TrimStrings = true,
        StringCasing = MappingStringCasing.UpperInvariant,
        NullHandling = MappingNullHandling.SetDefaultForValueTypes
    });

MappingManager.SetFieldTransformChain(
    "MainDb",
    "Customers",
    "Email",
    new MappingFieldTransformChain
    {
        Steps = new List<FieldTransformStep>
        {
            new FieldTransformStep { Name = "trim" },
            new FieldTransformStep { Name = "lower" }
        }
    });

var detail = map.MappedEntities.First();
detail.FieldMapping.First(f => f.ToFieldName == "Status").Rules =
    "when Source.IsDeleted = true then default 'INACTIVE'";

var (dest, graphResult) = MappingManager.MapObjectGraph(
    editor,
    "Customers",
    detail,
    sourceObject,
    new ObjectGraphMappingOptions
    {
        MaxDepth = 6,
        DetectCycles = true,
        CollectionMode = CollectionMappingMode.MergeByKey,
        CollectionMergeKeyPropertyName = "Id"
    });
```

## Outcome
- Transform and type conversion behavior is deterministic.
- Nested paths and collections can be mapped with cycle/merge controls.
