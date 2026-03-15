---
name: shared-context-plugin-system
description: Detailed guidance for PluginSystem components used by SharedContextAssemblyHandler and SharedContextManager in BeepDM. Use when working on plugin isolation, lifecycle, versioning, messaging, service orchestration, health monitoring, registry, or install flows.
---

# Shared Context PluginSystem

Use this skill when modifying classes under the shared-context plugin system.

## File Locations
- `DataManagementEngineStandard/AssemblyHandler/PluginSystem/SharedContextManager.cs`
- `DataManagementEngineStandard/AssemblyHandler/PluginSystem/PluginRegistry.cs`
- `DataManagementEngineStandard/AssemblyHandler/PluginSystem/PluginInstaller.cs`
- `DataManagementEngineStandard/AssemblyHandler/PluginSystem/PluginIsolationManager.cs`
- `DataManagementEngineStandard/AssemblyHandler/PluginSystem/PluginLifecycleManager.cs`
- `DataManagementEngineStandard/AssemblyHandler/PluginSystem/PluginVersionManager.cs`
- `DataManagementEngineStandard/AssemblyHandler/PluginSystem/PluginMessageBus.cs`
- `DataManagementEngineStandard/AssemblyHandler/PluginSystem/PluginServiceManager.cs`
- `DataManagementEngineStandard/AssemblyHandler/PluginSystem/PluginHealthMonitor.cs`

## Working Rules
1. Preserve separation between isolation, lifecycle, registry, and service responsibilities.
2. Keep thread-safe collection and event semantics intact.
3. Maintain compatibility of plugin load/unload event contracts.
4. Keep registry/install state consistent across restarts.

## Related Skills
- [`shared-context-assemblyhandler`](../shared-context-assemblyhandler/SKILL.md)
- [`shared-context-loading-resolution`](../shared-context-loading-resolution/SKILL.md)
- [`shared-context-nuget-source-tracking`](../shared-context-nuget-source-tracking/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for manager responsibilities and verification checks.
