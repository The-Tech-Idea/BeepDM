---
name: assemblyhandler-nuget-operations
description: Detailed guidance for AssemblyHandler NuGet operations: package search, version lookup, package download/load, and source management persistence. Use when implementing or consuming SearchNuGetPackagesAsync, GetNuGetPackageVersionsAsync, LoadNuggetFromNuGetAsync, and source CRUD.
---

# AssemblyHandler NuGet Operations

Use this skill when working on NuGet-backed plugin workflows.

## Additional Resources
- End-to-end scenarios: [reference.md](reference.md)

## Search, Versions, Download APIs
- `SearchNuGetPackagesAsync(string searchTerm, int skip = 0, int take = 20, bool includePrerelease = false, CancellationToken token = default)`
- `GetNuGetPackageVersionsAsync(string packageId, bool includePrerelease = false, CancellationToken token = default)`
- `LoadNuggetFromNuGetAsync(string packageName, string version = null, IEnumerable<string> sources = null, bool useSingleSharedContext = true, string appInstallPath = null, bool useProcessHost = false)`

## Source Management APIs
- `GetNuGetSources()`
- `AddNuGetSource(string name, string url, bool isEnabled = true)`
- `RemoveNuGetSource(string name)`
- `EnableNuGetSource(string name)`
- `DisableNuGetSource(string name)`
- `GetActiveSourceUrls()`

## Persistence Contracts
- NuGet sources file: `nuget_sources.json` under `ConfigEditor.ExePath` (or app base directory fallback).
- Default source is guaranteed: `https://api.nuget.org/v3/index.json`.
- Driver mapping persistence is separate (`driver_packages.json`).

## Runtime Notes
- Operations delegate to singleton `_packageDownloader`.
- `LoadNuggetFromNuGetAsync` downloads dependencies, loads nugget assemblies, optionally installs binaries into app folder, and can launch process-hosted plugins.
- Success/failure counters are recorded through statistics helpers.

## Safe Modification Rules
1. Keep source list thread-safe (`_sourcesLock`) and always persist after CRUD changes.
2. Do not remove default source auto-healing logic unless replacing it with equivalent behavior.
3. Preserve fallback to `GetActiveSourceUrls()` when caller does not provide explicit sources.
4. Keep download/load exceptions logged and non-crashing for caller.
5. Maintain assembly collection updates after nugget load.

## Common Pitfalls
- Returning null lists on errors instead of empty lists.
- Forgetting to save source file after enable/disable operations.
- Treating version list as `NuGetVersion` when interface currently returns normalized `List<string>`.
- Skipping install-path checks before process-host launch logic.

## Quick Verification
- Search returns results from active sources only.
- Added source appears in `GetNuGetSources()` and survives restart.
- Loaded nugget assemblies become visible in `LoadedAssemblies`.
- Statistics reflect package success/failure counters.

