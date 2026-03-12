---
name: assemblyhandler-loading-scanning
description: Detailed guidance for AssemblyHandler loading orchestration and assembly scanning. Use when modifying LoadAllAssembly, folder/runtime loading, extension scanner discovery, or ScanAssembly registration behavior in the classic AssemblyHandler.
---

# AssemblyHandler Loading And Scanning

Use this skill when changing assembly discovery flow or type scanning behavior in the classic handler.

## File Locations
- `Assembly_helpersStandard/AssemblyHandler.Loaders.cs`
- `Assembly_helpersStandard/AssemblyHandler.Scanning.cs`
- `Assembly_helpersStandard/AssemblyHandler.Core.cs`

## Core APIs
- `LoadAllAssembly(IProgress<PassedArgs> progress, CancellationToken token)`
- `LoadAssembly(string path)`
- `LoadAssembly(string path, FolderFileTypes fileTypes)`
- `LoadAssemblyFormRunTime()`
- `LoadAssembliesFromFolder(string folderPath, FolderFileTypes folderFileType, bool scanForDataSources = true)`
- `GetBuiltinClasses()`
- `GetExtensionScanners(IProgress<PassedArgs> progress, CancellationToken token)`
- `ScanAssembly(Assembly asm)`

## Working Rules
1. Preserve idempotent loading and duplicate checks.
2. Keep runtime/folder load paths aligned with statistics and progress reporting.
3. Preserve cancellation and non-blocking progress behavior in long scans.
4. Keep class registration consistent between handler collections and config lists.

## Related Skills
- [`assemblyhandler`](../assemblyhandler/SKILL.md)
- [`assemblyhandler-helpers-reflection`](../assemblyhandler-helpers-reflection/SKILL.md)
- [`assemblyhandler-driver-statistics`](../assemblyhandler-driver-statistics/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for scan order, registration targets, and quick verification checks.
