---
name: assemblyhandler-helpers-reflection
description: Detailed guidance for AssemblyHandler helper methods: type resolution, reflection invocation, class definition metadata extraction, addin hierarchy building, and ADO.NET driver discovery/default-driver normalization.
---

# AssemblyHandler Helpers And Reflection

Use this skill when modifying `AssemblyHandler.Helpers.cs`.

## Additional Resources
- End-to-end scenarios: [reference.md](reference.md)

## Instance And Type APIs
- `CreateInstanceFromString(string typeName, params object[] args)`
- `CreateInstanceFromString(string dll, string typeName, params object[] args)`
- `GetInstance(string strFullyQualifiedName)`
- `GetType(string strFullyQualifiedName)`
- `RunMethod(object ObjInstance, string FullClassName, string MethodName)`

## Class Metadata APIs
- `GetAssemblyClassDefinition(TypeInfo type, string typename)`
- `GetAddinObjects(Assembly asm)`
- `GetAddinObjectsFromTree()`
- `RearrangeAddin(string p, string parentid, string objt)`

## Driver Discovery APIs
- `GetDrivers(Assembly asm)`
- `AddEngineDefaultDrivers()`
- `CheckDriverAlreadyExistinList()`

## Behavior Notes
- `GetType` performs multi-stage resolution: cache -> runtime assemblies -> referenced assemblies -> loaded assemblies.
- `GetAssemblyClassDefinition` extracts `AddinAttribute`, `AddinVisSchema`, and `CommandAttribute` metadata.
- `GetDrivers` scans ADO.NET-relevant types and builds `ConnectionDriversConfig`.
- `AddEngineDefaultDrivers` adds baseline engine/file drivers based on discovered data source metadata.

## Safe Modification Rules
1. Preserve type cache usage before expensive reflection.
2. Keep helper methods failure-tolerant and log-centric (avoid hard throws in scan loops).
3. Preserve metadata extraction for command/addin workflows.
4. Keep driver deduplication behavior after appending new driver sources.
5. Do not remove `DataViewReader` default registration unless replaced intentionally.

## Common Pitfalls
- Bypassing cache in type resolution increases startup cost.
- Changing grouping key in driver dedupe can drop valid variant entries.
- Adding strict exceptions in discovery loops can break full-scan completion.
- Overwriting addin tree nodes without parent consistency checks.

## Quick Verification
- Reflection create/resolve paths return expected runtime types.
- `GetAssemblyClassDefinition` includes command metadata entries.
- `GetDrivers` detects provider-specific connection/adapter/transaction types.
- `CheckDriverAlreadyExistinList` yields stable unique driver list.

