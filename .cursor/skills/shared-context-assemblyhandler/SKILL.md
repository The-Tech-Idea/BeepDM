---
name: shared-context-assemblyhandler
description: Entry-point guidance for SharedContextAssemblyHandler and AssemblyHandler/PluginSystem integration. Use when loading assemblies in shared context mode, scanning components, handling NuGet package operations, and coordinating plugin lifecycle managers.
---

# SharedContextAssemblyHandler Guide

Use this as the routing skill for `SharedContextAssemblyHandler` work.

## Additional Resources
- End-to-end scenarios: [reference.md](reference.md)

## Scope
- `DataManagementEngineStandard/AssemblyHandler/SharedContextAssemblyHandler.cs`
- `DataManagementEngineStandard/AssemblyHandler/PluginSystem/*.cs`

## What This Handler Does
- Implements `IAssemblyHandler` with shared-context-first loading behavior.
- Delegates core assembly isolation and cross-plugin resolution to `SharedContextManager`.
- Uses `IScanningService` and `DriverDiscoveryAssistant` for discovery.
- Integrates NuGet download/search through `NuggetPackageDownloader`.
- Persists NuGet source and driver-package mappings, and exposes load statistics.

## Primary API Surface
- Loading/discovery: `LoadAllAssembly`, `LoadAssembly`, `GetBuiltinClasses`
- Type helpers: `CreateInstanceFromString`, `GetType`, `RunMethod`
- NuGet: `SearchNuGetPackagesAsync`, `GetNuGetPackageVersionsAsync`, `LoadNuggetFromNuGetAsync`
- Source management: `GetNuGetSources`, `AddNuGetSource`, `RemoveNuGetSource`, `GetActiveSourceUrls`
- Driver mapping/stats: `TrackDriverPackage`, `GetAllDriverPackageMappings`, `IsDriverFromNuGet`, `GetLoadStatistics`

## Specialized Skills
- Loading and assembly resolution flow: [shared-context-loading-resolution](../shared-context-loading-resolution/SKILL.md)
- Scanning and discovery model: [shared-context-scanning-discovery](../shared-context-scanning-discovery/SKILL.md)
- PluginSystem manager architecture: [shared-context-plugin-system](../shared-context-plugin-system/SKILL.md)
- NuGet + source + driver mapping operations: [shared-context-nuget-source-tracking](../shared-context-nuget-source-tracking/SKILL.md)

## Working Rules
1. Keep `SharedContextManager` as the canonical runtime assembly visibility source.
2. Prefer `IAssemblyHandler` methods from callers instead of direct PluginSystem coupling.
3. Preserve logging and non-throwing behavior for discovery/install workflows.
4. Keep persistence contracts stable (`nuget_sources.json`, `driver_packages.json`).

