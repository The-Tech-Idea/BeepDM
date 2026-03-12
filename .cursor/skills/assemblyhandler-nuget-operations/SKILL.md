---
name: assemblyhandler-nuget-operations
description: Detailed guidance for AssemblyHandler NuGet operations. Use when implementing or consuming package search, version lookup, package download/load, and source management persistence through NuggetManager and AssemblyHandler.
---

# AssemblyHandler NuGet Operations

Use this skill when working on NuGet-backed plugin workflows in the classic handler.

## File Locations
- `Assembly_helpersStandard/NuggetManager.cs`
- `Assembly_helpersStandard/AssemblyHandler.Core.cs`
- `Assembly_helpersStandard/AssemblyHandler.Loaders.cs`

## Core APIs
- `SearchNuGetPackagesAsync(...)`
- `GetNuGetPackageVersionsAsync(...)`
- `LoadNuggetFromNuGetAsync(...)`
- source CRUD methods such as `GetNuGetSources`, `AddNuGetSource`, `RemoveNuGetSource`, `EnableNuGetSource`, `DisableNuGetSource`, and `GetActiveSourceUrls`

## Working Rules
1. Keep source mutations thread-safe and persisted after writes.
2. Preserve default-source fallback behavior.
3. Keep download/load failures non-fatal to callers unless the contract explicitly changes.
4. Keep assembly collections and statistics updated after package load.

## Related Skills
- [`assemblyhandler`](../assemblyhandler/SKILL.md)
- [`assemblyhandler-driver-statistics`](../assemblyhandler-driver-statistics/SKILL.md)
- [`shared-context-nuget-source-tracking`](../shared-context-nuget-source-tracking/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for persistence contracts, pitfalls, and verification checks.
