# 04 - Performance and Cache Behavior

## Goal
Use compiled plans and deterministic cache invalidation for repeated mapping runs.

## Example
```csharp
// First call builds compiled plan and accessor caches.
var mapped1 = MappingManager.MapObjectToAnother(editor, "Customers", detail, source1);

// Repeated calls hit compiled cache path.
var mapped2 = MappingManager.MapObjectToAnother(editor, "Customers", detail, source2);
var mapped3 = MappingManager.MapObjectToAnother(editor, "Customers", detail, source3);

// Explicit invalidation when needed (save does this automatically).
MappingManager.InvalidateMappingCaches("MainDb", "Customers");
```

## Notes
- Save operations trigger invalidation automatically in `SaveMapping(...)`.
- Caches are bounded internally (plan/accessor ceilings with eviction).
