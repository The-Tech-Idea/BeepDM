---
name: assemblyhandler-driver-statistics
description: Detailed guidance for AssemblyHandler driver-package tracking and load statistics. Use when working with driver provenance persistence, IsDriverFromNuGet checks, and load timing/counter instrumentation in the classic AssemblyHandler and NuggetManager flow.
---

# AssemblyHandler Driver Tracking And Statistics

Use this skill when changing operational visibility and package provenance tracking.

## File Locations
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.DriverTracking.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Statistics.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Loaders.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.NuGetOperations.cs`
- `BeepDM/DataManagementModelsStandard/Tools/AssemblyHandlerDataClasses.cs`

## Driver Tracking APIs
- `TrackDriverPackage(...)`
- `UntrackDriverPackage(...)`
- `GetDriverPackageMapping(...)`
- `GetAllDriverPackageMappings()`
- `IsDriverFromNuGet(string driverClassName)`
- persistence helpers: `SaveDriverMappings`, `LoadDriverMappings`, `EnsureDriverMappingsLoaded`

## Statistics APIs
- `GetLoadStatistics()`
- timing/counter helpers such as `StartLoadTiming`, `StopLoadTiming`, and failure/success recording
- `RecordLoadFailure(path)`, `RecordNuGetSuccess()`, `RecordNuGetFailure()`, `ResetStatistics()`

## Working Rules
1. Keep lock protection around mapping mutations and snapshot reads.
2. Persist mapping changes immediately unless batching is intentionally introduced.
3. Preserve file locations for persistence defaults (`ConfigEditor.ExePath` fallback to `AppContext.BaseDirectory`).
4. Keep statistics monotonic within a load session and reset at session boundaries (`LoadAllAssembly` start).
5. Preserve log-and-continue behavior for persistence failures.
6. Keep runtime-derived counts (`LoadedAssemblies`, drivers, datasources) aligned with statistics snapshots.

## Data Contracts
- `DriverPackageMapping`: package/version/class/datasource-type/install timestamp
- `AssemblyLoadStatistics`: load counts, failure paths, per-folder counts, total load time, last timestamp
- `NuGetSourceConfig`: source name/url/enabled/date added (related context for NuGet diagnostics)

## Related Skills
- [`assemblyhandler`](../assemblyhandler/SKILL.md)
- [`assemblyhandler-nuget-operations`](../assemblyhandler-nuget-operations/SKILL.md)
- [`assemblyhandler-loading-scanning`](../assemblyhandler-loading-scanning/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for persistence contracts, pitfalls, and verification checks.
