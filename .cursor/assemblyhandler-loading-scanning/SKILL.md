---
name: assemblyhandler-loading-scanning
description: Detailed guidance for AssemblyHandler loading orchestration and assembly scanning. Use when modifying LoadAllAssembly, folder/runtime loading, extension scanner discovery, or ScanAssembly registration behavior in the classic AssemblyHandler.
---

# AssemblyHandler Loading And Scanning

Use this skill when changing assembly discovery flow or type scanning behavior in the classic handler.

## File Locations
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Loaders.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Scanning.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Core.cs`
- `BeepDM/DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Statistics.cs`

## Core APIs
- `LoadAllAssembly(IProgress<PassedArgs> progress, CancellationToken token)`
- `LoadAssembly(string path)`
- `LoadAssembly(string path, FolderFileTypes fileTypes)`
- `LoadAssemblyFormRunTime()`
- `LoadAssembliesFromFolder(string folderPath, FolderFileTypes folderFileType, bool scanForDataSources = true)`
- `GetBuiltinClasses()`
- `GetExtensionScanners(IProgress<PassedArgs> progress, CancellationToken token)`
- `ScanAssembly(Assembly asm)`
- `ScanAssemblyForDataSources(Assembly asm)` (internal targeted scan)
- `ScanExtensions()` / `ScanExtension(Assembly assembly)`
- `ScanForDrivers()` / `ScanForDataSources()` / `ScanProjectAndAddinAssemblies()`

## Working Rules
1. Preserve idempotent loading and duplicate checks.
2. Keep folder fallback logic (`Config.Folders` -> config paths -> default folders under `ExePath`) intact.
3. Keep runtime/folder load paths aligned with statistics and progress reporting.
4. Preserve cancellation and non-blocking progress behavior in long scans.
5. Keep class registration consistent between handler collections and `ConfigEditor` lists.
6. Keep parallel scan behavior for large assemblies and fallback type loading (`GetTypes` -> `ReflectionTypeLoadException` handling -> `GetExportedTypes`).

## Full LoadAllAssembly Sequence (Current)
1. Reset runtime lists and statistics; start timing.
2. Builtins: `GetBuiltinClasses()` + `LoadAssemblyFormRunTime()`.
3. Loader extensions: `GetExtensionScanners(...)`.
4. Folder loads: connection drivers, data sources, and (non-DataConnector) project/other DLLs.
5. Scan passes: drivers, data sources, addins/project assemblies.
6. Post-scan: default drivers, dedupe drivers, extension scans.
7. Build addin tree hierarchy (non-DataConnector), stop timing, log summary.

## Related Skills
- [`assemblyhandler`](../assemblyhandler/SKILL.md)
- [`assemblyhandler-helpers-reflection`](../assemblyhandler-helpers-reflection/SKILL.md)
- [`assemblyhandler-driver-statistics`](../assemblyhandler-driver-statistics/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for scan order, registration targets, and quick verification checks.
