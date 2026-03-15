---
name: assemblyhandler-nuget-operations
description: Detailed guidance for AssemblyHandler NuGet operations. Use when implementing or consuming package search, version lookup, package download/load, and source management persistence through NuggetManager and AssemblyHandler.
---

# AssemblyHandler NuGet Operations

Use this skill when working on NuGet-backed plugin workflows in the classic handler.

## File Locations
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.NuGetOperations.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.NuGetSources.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Loaders.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.DriverTracking.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/NuggetManager.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandlerEnhancement.md`

## Core APIs
- `SearchNuGetPackagesAsync(...)`
- `GetNuGetPackageVersionsAsync(...)`
- `LoadNuggetFromNuGetAsync(...)`
- `LoadNugget(path)` / `UnloadNugget(name)` / `GetAllNuggets()`
- source CRUD methods: `GetNuGetSources`, `AddNuGetSource`, `RemoveNuGetSource`, `EnableNuGetSource`, `DisableNuGetSource`, `GetActiveSourceUrls`
- Nugget manager extras used by orchestration: `InstallPackageToAppDirectory`, `FindNuggetByAssemblyPath`, `IsNuggetLoaded`, `GetNuggetAssemblies`

## Working Rules
1. Keep source mutations thread-safe and persisted after writes.
2. Preserve default-source fallback behavior.
3. Keep SDK-first routing (`AssemblyHandler` -> `NuggetManager`) as the primary NuGet acquisition path.
4. Keep download/load failures non-fatal to callers unless the contract explicitly changes.
5. Keep assembly collections and statistics updated after package load (`SyncNuggetAssembliesToHandlerCollections`, `RecordNuGetSuccess/Failure`).
6. Preserve shared app-visible loading default (`useSingleSharedContext: true`) unless explicit isolation is requested.
7. Keep driver provenance tracking tied to actual loaded assembly ownership (`FindNuggetByAssemblyPath`).

## Source And Persistence Artifacts
- `nuget_sources.json` (NuGet source state; default nuget.org source ensured)
- `driver_packages.json` (driver-to-package mappings, updated during NuGet load flows)

## Related Skills
- [`assemblyhandler`](../assemblyhandler/SKILL.md)
- [`assemblyhandler-driver-statistics`](../assemblyhandler-driver-statistics/SKILL.md)
- [`shared-context-nuget-source-tracking`](../shared-context-nuget-source-tracking/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for persistence contracts, pitfalls, and verification checks.
