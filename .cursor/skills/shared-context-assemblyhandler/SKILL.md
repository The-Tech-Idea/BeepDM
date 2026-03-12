---
name: shared-context-assemblyhandler
description: Entry-point guidance for SharedContextAssemblyHandler and the PluginSystem integration in BeepDM. Use when loading assemblies in shared-context mode, scanning components, handling NuGet package operations, or coordinating plugin lifecycle managers.
---

# SharedContextAssemblyHandler Guide

Use this as the routing skill for shared-context `IAssemblyHandler` work.

## File Locations
- `Assembly_helpersStandard/SharedContextAssemblyHandler.cs`
- `DataManagementEngineStandard/AssemblyHandler/PluginSystem/`

## Responsibilities
- Keep `SharedContextAssemblyHandler` as the top-level shared-context handler.
- Route subsystem work to loading/resolution, scanning/discovery, plugin-system, or NuGet/source tracking.
- Preserve shared-context-first loading behavior and `IAssemblyHandler` caller contracts.

## Specialized Skills
- [`shared-context-loading-resolution`](../shared-context-loading-resolution/SKILL.md)
- [`shared-context-scanning-discovery`](../shared-context-scanning-discovery/SKILL.md)
- [`shared-context-plugin-system`](../shared-context-plugin-system/SKILL.md)
- [`shared-context-nuget-source-tracking`](../shared-context-nuget-source-tracking/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for API examples and end-to-end scenarios.
