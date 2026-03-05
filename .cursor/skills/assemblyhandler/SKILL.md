---
name: assemblyhandler
description: Entry-point guidance for AssemblyHandler in DataManagementEngineStandard/AssemblyHandler. Use when loading/scanning assemblies, managing nuggets and NuGet packages, configuring sources, tracking driver-package mappings, and retrieving load statistics.
---

# AssemblyHandler Guide

Use this skill as the top-level routing guide for all `AssemblyHandler` work.

## Additional Resources
- End-to-end scenarios: [reference.md](reference.md)

## Purpose
- Keep `AssemblyHandler` as the single backend for assembly and NuGet logic.
- Avoid duplicating NuGet/search/download/source-management logic in UI projects.
- Route most operations through `IAssemblyHandler` first.

## Main Files
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Core.cs`
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Loaders.cs`
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Scanning.cs`
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Helpers.cs`
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.NuGetOperations.cs`
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.NuGetSources.cs`
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.DriverTracking.cs`
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Statistics.cs`
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandlerEnhancement.md`

## Recommended Usage Order
1. Construct `AssemblyHandler` with valid `IConfigEditor`, `IErrorsInfo`, `IDMLogger`, and `IUtil`.
2. Call `LoadAllAssembly(progress, token)` for full discovery.
3. Use `SearchNuGetPackagesAsync` and `LoadNuggetFromNuGetAsync` for package-driven plugin loading.
4. Track package->driver mapping with `TrackDriverPackage`.
5. Inspect `GetLoadStatistics()` for runtime quality checks.

## Specialized Skills
- Loader orchestration and scanning: [assemblyhandler-loading-scanning](../assemblyhandler-loading-scanning/SKILL.md)
- NuGet search/download/source management: [assemblyhandler-nuget-operations](../assemblyhandler-nuget-operations/SKILL.md)
- Reflection helpers and driver extraction: [assemblyhandler-helpers-reflection](../assemblyhandler-helpers-reflection/SKILL.md)
- Driver package tracking and load metrics: [assemblyhandler-driver-statistics](../assemblyhandler-driver-statistics/SKILL.md)

## Enhancement Alignment
`AssemblyHandlerEnhancement.md` defines the architecture target:
- NuGet source CRUD + persistence
- NuGet search and version APIs
- Driver package tracking
- Load statistics and timing
- Consolidation behind `IAssemblyHandler`

Follow that plan when introducing new methods or changing signatures.

