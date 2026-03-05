---
name: assemblyhandler-driver-statistics
description: Detailed guidance for AssemblyHandler driver package tracking and load statistics. Use when working with driver->NuGet mapping persistence, IsDriverFromNuGet checks, and load timing/counter instrumentation.
---

# AssemblyHandler Driver Tracking And Statistics

Use this skill when changing operational visibility and package provenance tracking.

## Additional Resources
- End-to-end scenarios: [reference.md](reference.md)

## Driver Tracking APIs
- `TrackDriverPackage(string packageId, string version, string driverClassName, DataSourceType dsType)`
- `UntrackDriverPackage(string packageId)`
- `GetDriverPackageMapping(string driverClassName)`
- `GetAllDriverPackageMappings()`
- `IsDriverFromNuGet(string driverClassName)`

## Statistics APIs
- `GetLoadStatistics()`
- Internal timing/counter hooks:
  - `StartLoadTiming()`
  - `StopLoadTiming()`
  - `RecordLoadFailure(string path)`
  - `RecordNuGetSuccess()`
  - `RecordNuGetFailure()`
  - `ResetStatistics()`

## Persistence Contracts
- Driver mappings file: `driver_packages.json` under `ConfigEditor.ExePath` (or app base fallback).
- Mappings are lock-protected and persisted after track/untrack operations.
- Statistics object computes dynamic counts using current in-memory collections.

## Safe Modification Rules
1. Keep lock protection (`_driverMappingsLock`) around list mutations and snapshot reads.
2. Ensure track/untrack always persists unless a deliberate batching strategy is introduced.
3. Keep statistics counters monotonic during a load session.
4. If adding new counters, update reset and readout paths together.
5. Preserve non-throwing behavior for persistence I/O failures (log and continue).

## Common Pitfalls
- Tracking by package only and losing class-level provenance.
- Forgetting to initialize mappings path before save/load.
- Returning internal list instance directly (must return a copy).
- Resetting statistics mid-run without caller intent.

## Quick Verification
- Tracked driver appears in `GetAllDriverPackageMappings()` and persists to disk.
- `IsDriverFromNuGet` returns true for tracked class names.
- `GetLoadStatistics()` shows correct loaded assemblies/drivers/datasources counts.
- NuGet load attempts increment success/failure counters as expected.

