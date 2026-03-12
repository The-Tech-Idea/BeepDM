---
name: assemblyhandler
description: Entry-point guidance for AssemblyHandler and IAssemblyHandler in BeepDM. Use when loading or scanning plugin assemblies, resolving types via reflection, managing NuGet-backed package loading, tracking driver-package provenance, or inspecting load statistics.
---

# AssemblyHandler Guide

Use this skill as the top-level router for all non-shared-context `AssemblyHandler` work.

## Use this skill when
- Bootstrapping plugin and driver discovery in the classic assembly handler
- Deciding whether a change belongs in loading, reflection helpers, NuGet operations, or statistics
- Reviewing `IAssemblyHandler` usage from callers and services

## Do not use this skill when
- The task is specifically about shared-context loading and plugin-system managers. Use [`shared-context-assemblyhandler`](../shared-context-assemblyhandler/SKILL.md).
- The task is specifically about RDBMS query-generation helpers. Use the `rdbms-*` skills.

## Responsibilities
- Keep `AssemblyHandler` as the single backend for classic assembly and NuGet logic.
- Route specialized work to the narrowest assemblyhandler skill.
- Preserve `IAssemblyHandler` as the caller-facing abstraction.

## Main Files
- `Assembly_helpersStandard/AssemblyHandler.Core.cs`
- `Assembly_helpersStandard/AssemblyHandler.Loaders.cs`
- `Assembly_helpersStandard/AssemblyHandler.Scanning.cs`
- `Assembly_helpersStandard/AssemblyHandler.Helpers.cs`
- `Assembly_helpersStandard/NuggetManager.cs`

## Typical Workflow
1. Construct `AssemblyHandler` with `IConfigEditor`, `IErrorsInfo`, `IDMLogger`, and `IUtil`.
2. Call `LoadAllAssembly(progress, token)` for full discovery.
3. Use helper/reflection APIs only after core load state is initialized.
4. Use NuGet/package APIs for dynamic plugin acquisition.
5. Use driver tracking and statistics APIs for operational visibility.

## Specialized Skills
- Loader orchestration and scanning: [`assemblyhandler-loading-scanning`](../assemblyhandler-loading-scanning/SKILL.md)
- NuGet search/download/source management: [`assemblyhandler-nuget-operations`](../assemblyhandler-nuget-operations/SKILL.md)
- Reflection helpers and driver extraction: [`assemblyhandler-helpers-reflection`](../assemblyhandler-helpers-reflection/SKILL.md)
- Driver package tracking and load metrics: [`assemblyhandler-driver-statistics`](../assemblyhandler-driver-statistics/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for end-to-end scenarios and API examples.
