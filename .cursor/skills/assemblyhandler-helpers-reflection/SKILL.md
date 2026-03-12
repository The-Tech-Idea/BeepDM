---
name: assemblyhandler-helpers-reflection
description: Detailed guidance for AssemblyHandler reflection helpers. Use when changing type resolution, instance creation, method invocation, addin metadata extraction, addin-tree construction, or driver discovery helpers in AssemblyHandler.Helpers.cs.
---

# AssemblyHandler Helpers And Reflection

Use this skill when modifying the reflection-heavy helper layer of `AssemblyHandler`.

## File Locations
- `Assembly_helpersStandard/AssemblyHandler.Helpers.cs`
- `Assembly_helpersStandard/AssemblyHandler.Core.cs`

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

## Working Rules
1. Preserve cache-first type resolution before expensive reflection.
2. Keep scan loops failure-tolerant and log-driven.
3. Preserve addin/command metadata extraction used by workflow and UI systems.
4. Keep driver deduplication stable after discovery.

## Related Skills
- [`assemblyhandler`](../assemblyhandler/SKILL.md)
- [`assemblyhandler-loading-scanning`](../assemblyhandler-loading-scanning/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for behavior notes, pitfalls, and verification checks.
