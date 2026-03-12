---
name: shared-context-nuget-source-tracking
description: Detailed guidance for shared-context NuGet operations, source persistence, driver-package tracking, and load statistics in BeepDM. Use when working on package search/load flows and source or provenance state in SharedContextAssemblyHandler.
---

# Shared Context NuGet, Sources, And Tracking

Use this skill for package-driven runtime extension workflows in shared-context mode.

## File Locations
- `Assembly_helpersStandard/SharedContextAssemblyHandler.cs`
- `DataManagementEngineStandard/AssemblyHandler/PluginSystem/NuggetPackageDownloader.cs`
- `DataManagementEngineStandard/AssemblyHandler/PluginSystem/NuggetPluginLoader.cs`

## Core APIs
- NuGet search/version/load methods
- source CRUD methods
- driver mapping and statistics methods

## Working Rules
1. Keep source and mapping mutations thread-safe and persisted after writes.
2. Keep NuGet API failures non-fatal with safe fallback returns.
3. Preserve default-source behavior and active-source filtering.
4. Keep stats updates aligned with install/load success and failure paths.

## Related Skills
- [`shared-context-assemblyhandler`](../shared-context-assemblyhandler/SKILL.md)
- [`shared-context-plugin-system`](../shared-context-plugin-system/SKILL.md)
- [`assemblyhandler-nuget-operations`](../assemblyhandler-nuget-operations/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for persistence contracts and verification checks.
