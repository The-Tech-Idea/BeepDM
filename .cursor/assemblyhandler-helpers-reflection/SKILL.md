---
name: assemblyhandler-helpers-reflection
description: Detailed guidance for AssemblyHandler reflection helpers. Use when changing type resolution, instance creation, method invocation, addin metadata extraction, addin-tree construction, or driver discovery helpers in AssemblyHandler.Helpers.cs.
---

# AssemblyHandler Helpers And Reflection

Use this skill when modifying the reflection-heavy helper layer of `AssemblyHandler`.

## File Locations
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Helpers.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Core.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Scanning.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Loaders.cs`

## Core APIs
- `CreateInstanceFromString(...)`
- `GetInstance(string strFullyQualifiedName)`
- `GetType(string strFullyQualifiedName)`
- `RunMethod(object ObjInstance, string FullClassName, string MethodName)`
- `GetAssemblyClassDefinition(TypeInfo type, string typename)`
- `GetAddinObjects(Assembly asm)`
- `GetAddinObjectsFromTree()`
- `RearrangeAddin(...)`
- `GetDrivers(Assembly asm)`
- `AddEngineDefaultDrivers()`
- `CheckDriverAlreadyExistinList()`
- `SyncNuggetAssembliesToHandlerCollections(...)` (helper used by NuGet load paths)

## Working Rules
1. Preserve cache-first type resolution before expensive reflection.
2. Preserve multi-stage type lookup order (`Type.GetType` -> current domain namespace matches -> referenced assemblies -> tracked assemblies).
3. Keep scan loops failure-tolerant and log-driven.
4. Preserve addin/command metadata extraction used by workflow and UI systems.
5. Keep addin-tree construction behavior for `IAddinVisSchema` (`ConfigEditor.AddinTreeStructure` updates).
6. Keep driver extraction/dedupe stable and avoid losing file-driver mappings from `DataSourcesClasses`.
7. Keep nugget synchronization helper responsible for collection/cache updates and post-load scanning.

## Related Skills
- [`assemblyhandler`](../assemblyhandler/SKILL.md)
- [`assemblyhandler-loading-scanning`](../assemblyhandler-loading-scanning/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for behavior notes, pitfalls, and verification checks.
