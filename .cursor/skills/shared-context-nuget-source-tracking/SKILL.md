---
name: shared-context-nuget-source-tracking
description: Detailed guidance for SharedContextAssemblyHandler NuGet operations, source persistence, driver package tracking, and load statistics reporting. Use when working on SearchNuGetPackagesAsync, LoadNuggetFromNuGetAsync, source CRUD, driver provenance, and stats aggregation.
---

# Shared Context NuGet, Sources, And Tracking

Use this skill for package-driven runtime extension workflows.

## Additional Resources
- End-to-end scenarios: [reference.md](reference.md)

## NuGet APIs
- `SearchNuGetPackagesAsync(...)`
- `GetNuGetPackageVersionsAsync(...)`
- `LoadNuggetFromNuGetAsync(...)`

## Source APIs
- `GetNuGetSources()`
- `AddNuGetSource(...)`
- `RemoveNuGetSource(...)`
- `GetActiveSourceUrls()`

## Driver Mapping / Statistics APIs
- `TrackDriverPackage(...)`
- `GetAllDriverPackageMappings()`
- `IsDriverFromNuGet(...)`
- `GetLoadStatistics()`

## Persistence Contracts
- Sources: `nuget_sources.json`
- Driver mappings: `driver_packages.json`
- Paths rooted in `ConfigEditor.ExePath` with fallback behavior

## Safe Change Rules
1. Keep source and mapping mutations lock-protected and persisted after writes.
2. Keep NuGet API failures non-fatal (log + safe fallback returns).
3. Preserve default source fallback behavior and active-source filtering.
4. Keep stats updates aligned with load/nuget success-failure paths.

## Common Pitfalls
- Returning null collections on errors instead of empty lists.
- Failing to persist source enable/disable or remove operations.
- Forgetting to map installed driver classes after package load.
- Drift between in-memory counters and externally reported statistics.

## Verification Checklist
- Search and version calls work against configured active sources.
- Installed nugget assemblies appear in shared/loaded assembly collections.
- Driver mapping lookup returns tracked package provenance.
- Statistics show updated counts after load/install cycles.

