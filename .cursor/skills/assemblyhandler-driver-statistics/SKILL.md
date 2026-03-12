---
name: assemblyhandler-driver-statistics
description: Detailed guidance for AssemblyHandler driver-package tracking and load statistics. Use when working with driver provenance persistence, IsDriverFromNuGet checks, and load timing/counter instrumentation in the classic AssemblyHandler and NuggetManager flow.
---

# AssemblyHandler Driver Tracking And Statistics

Use this skill when changing operational visibility and package provenance tracking.

## File Locations
- `Assembly_helpersStandard/AssemblyHandler.Core.cs`
- `Assembly_helpersStandard/AssemblyHandler.Loaders.cs`
- `Assembly_helpersStandard/NuggetManager.cs`

## Driver Tracking APIs
- `TrackDriverPackage(...)`
- `UntrackDriverPackage(...)`
- `GetDriverPackageMapping(...)`
- `GetAllDriverPackageMappings()`
- `IsDriverFromNuGet(string driverClassName)`

## Statistics APIs
- `GetLoadStatistics()`
- timing/counter helpers such as `StartLoadTiming`, `StopLoadTiming`, and failure/success recording

## Working Rules
1. Keep lock protection around mapping mutations and snapshot reads.
2. Persist mapping changes immediately unless batching is intentionally introduced.
3. Keep statistics monotonic within a load session.
4. Preserve log-and-continue behavior for persistence failures.

## Related Skills
- [`assemblyhandler`](../assemblyhandler/SKILL.md)
- [`assemblyhandler-nuget-operations`](../assemblyhandler-nuget-operations/SKILL.md)
- [`assemblyhandler-loading-scanning`](../assemblyhandler-loading-scanning/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for persistence contracts, pitfalls, and verification checks.
