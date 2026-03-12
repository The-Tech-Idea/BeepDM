---
name: shared-context-loading-resolution
description: Detailed guidance for SharedContextAssemblyHandler loading workflow and assembly resolution in BeepDM. Use when modifying LoadAllAssembly orchestration, framework-specific path resolution, AppDomain registration, or shared-context binding behavior.
---

# Shared Context Loading And Resolution

Use this skill when changing the runtime loading pipeline in shared-context mode.

## File Locations
- `Assembly_helpersStandard/SharedContextAssemblyHandler.cs`
- `DataManagementEngineStandard/AssemblyHandler/PluginSystem/SharedContextManager.cs`
- `DataManagementEngineStandard/AssemblyHandler/PluginSystem/SharedAssemblyTracker.cs`

## Core Methods
- `LoadAllAssembly(...)`
- `LoadAssembly(...)`
- `GetBuiltinClasses()`
- assembly resolve hooks and shared-context registration paths

## Working Rules
1. Keep shared-context registration aligned with assembly collections and resolver state.
2. Preserve framework/path resolution for multi-target package layouts.
3. Preserve cancellation and progress behavior in long-running loads.
4. Avoid duplicate type identity problems by keeping already-loaded assemblies registered in the shared context.

## Related Skills
- [`shared-context-assemblyhandler`](../shared-context-assemblyhandler/SKILL.md)
- [`shared-context-scanning-discovery`](../shared-context-scanning-discovery/SKILL.md)
- [`shared-context-plugin-system`](../shared-context-plugin-system/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for loader pipeline notes and verification checks.
