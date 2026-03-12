---
name: shared-context-scanning-discovery
description: Detailed guidance for shared-context scanning and discovery in BeepDM. Use when modifying IScanningService, DriverDiscoveryAssistant, or SharedContextAssemblyHandler discovery of IDataSource, addins, workflow actions, loader extensions, and driver metadata.
---

# Shared Context Scanning And Discovery

Use this skill when touching discovery behavior in shared-context mode.

## File Locations
- `Assembly_helpersStandard/SharedContextAssemblyHandler.cs`
- `DataManagementEngineStandard/AssemblyHandler/PluginSystem/IScanningService.cs`
- `DataManagementEngineStandard/AssemblyHandler/PluginSystem/AssemblyScanningAssistant.cs`
- `DataManagementEngineStandard/AssemblyHandler/PluginSystem/DriverDiscoveryAssistant.cs`

## Discovery Components
- scanning service implementations
- driver discovery assistant
- shared-context discovered collections on `SharedContextManager`

## Working Rules
1. Keep discovery additive and resilient to per-type failures.
2. Preserve updates to both config lists and shared-context discovered snapshots.
3. Keep component type tags and metadata extraction stable.
4. Prefer targeted scans when full scans are not needed.

## Related Skills
- [`shared-context-assemblyhandler`](../shared-context-assemblyhandler/SKILL.md)
- [`shared-context-loading-resolution`](../shared-context-loading-resolution/SKILL.md)
- [`assemblyhandler-helpers-reflection`](../assemblyhandler-helpers-reflection/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for scan entry points, covered interfaces, and verification checks.
