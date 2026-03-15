---
name: assemblyhandler
description: Entry-point guidance for AssemblyHandler and IAssemblyHandler in BeepDM. Use when loading or scanning plugin assemblies, resolving types via reflection, managing NuGet-backed package loading, tracking driver-package provenance, or inspecting load statistics.
---

# AssemblyHandler Guide

Use this skill as the top-level router for all non-shared-context `AssemblyHandler` work.

## Use this skill when
- Bootstrapping full discovery (`LoadAllAssembly`) and diagnosing what was loaded/scanned
- Deciding whether a change belongs in loading/scanning, helpers/reflection, NuGet operations, or tracking/statistics
- Updating caller-facing `IAssemblyHandler` flows that must stay compatible across partial files
- Verifying SDK-first nugget behavior and handler-level synchronization after package loads

## Do not use this skill when
- The task is specifically about shared-context loading and plugin-system managers. Use [`shared-context-assemblyhandler`](../shared-context-assemblyhandler/SKILL.md).
- The task is specifically about RDBMS query-generation helpers. Use the `rdbms-*` skills.

## Responsibilities
- Keep `AssemblyHandler` as the single backend for classic assembly, scanning, and NuGet routing logic.
- Route specialized work to the narrowest assemblyhandler skill.
- Preserve `IAssemblyHandler` as the caller-facing abstraction.

## Main Files
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Core.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Loaders.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Scanning.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Helpers.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.NuGetOperations.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.NuGetSources.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.DriverTracking.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Statistics.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/NuggetManager.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandlerEnhancement.md`

## Typical Workflow
1. Construct `AssemblyHandler` with `IConfigEditor`, `IErrorsInfo`, `IDMLogger`, and `IUtil`.
2. Call `LoadAllAssembly(progress, token)` to initialize builtins, loader extensions, folders, scans, defaults, and extension passes.
3. Use helper/reflection APIs after load state is initialized (`LoadedAssemblies`, `Assemblies`, `ConfigEditor.*` lists).
4. Use NuGet APIs (`SearchNuGetPackagesAsync`, `GetNuGetPackageVersionsAsync`, `LoadNuggetFromNuGetAsync`) for dynamic acquisition.
5. Rely on automatic synchronization (`SyncNuggetAssembliesToHandlerCollections`) to keep caches and scans current.
6. Use source management + driver provenance + statistics for operational diagnostics.

## Core Capability Checklist
- Assembly resolution: `CurrentDomain_AssemblyResolve`, runtime+cache aware lookup
- Type cache lifecycle: `AddTypeToCache`, `GetTypeFromCache`, `ClearTypeCache`
- Folder/runtime loading: direct load, folder load, runtime registration, fallback folder discovery
- Scanning + registration: IDataSource, addins, workflow artifacts, print managers, global functions, loader extensions
- NuGet routing via `NuggetManager`: search, versions, package load/install, unload
- NuGet source persistence: add/remove/enable/disable sources and active source resolution
- Driver provenance persistence: mapping drivers to package/version/source
- Statistics lifecycle: reset/start/stop timing, load failure paths, NuGet success/failure counters

## Specialized Skills
- Loader orchestration and scanning: [`assemblyhandler-loading-scanning`](../assemblyhandler-loading-scanning/SKILL.md)
- NuGet search/download/source management: [`assemblyhandler-nuget-operations`](../assemblyhandler-nuget-operations/SKILL.md)
- Reflection helpers and driver extraction: [`assemblyhandler-helpers-reflection`](../assemblyhandler-helpers-reflection/SKILL.md)
- Driver package tracking and load metrics: [`assemblyhandler-driver-statistics`](../assemblyhandler-driver-statistics/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for end-to-end scenarios and API examples.
