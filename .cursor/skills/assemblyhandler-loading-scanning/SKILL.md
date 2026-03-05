---
name: assemblyhandler-loading-scanning
description: Detailed guidance for AssemblyHandler loading orchestration and assembly scanning. Use when working with LoadAllAssembly, folder loading, runtime loading, extension scanner discovery, and ScanAssembly processing.
---

# AssemblyHandler Loading And Scanning

Use this skill when changing assembly discovery flow or type scanning behavior.

## Additional Resources
- End-to-end scenarios: [reference.md](reference.md)

## Core APIs
- `LoadAllAssembly(IProgress<PassedArgs> progress, CancellationToken token)`
- `LoadAssembly(string path)`
- `LoadAssembly(string path, FolderFileTypes fileTypes)`
- `LoadAssemblyFormRunTime()`
- `LoadAssembliesFromFolder(string folderPath, FolderFileTypes folderFileType, bool scanForDataSources = true)`
- `GetBuiltinClasses()`
- `GetExtensionScanners(IProgress<PassedArgs> progress, CancellationToken token)`
- `ScanAssembly(Assembly asm)`

## Orchestration Notes
- `LoadAllAssembly` resets state, starts timing, loads from configured folders, scans for drivers/data sources/addins, then runs extension scanning.
- Folder loading has fallback behavior when configured folder entries are missing.
- `LoadAssembliesFromFolder` recursively scans `.dll` files and skips native `runtimes` payloads.
- Scanning uses parallel paths for larger type sets.

## Interface Registration Surface In Scan
`ProcessTypeInfo(...)` routes implementations into config/editor lists for:
- `ILoaderExtention`
- `IDataSource`
- `IWorkFlowAction`
- `IDM_Addin`
- `IWorkFlowStep`
- `IBeepViewModel`
- `IWorkFlowEditor`
- `IWorkFlowRule`
- `IFunctionExtension`
- `IPrintManager`
- `IAddinVisSchema` (addin tree path)

## Safe Modification Rules
1. Preserve idempotency of load operations (avoid duplicate assembly/type entries).
2. Keep `System`/`Microsoft` skip logic unless intentionally changing scope.
3. Ensure progress callback remains non-blocking.
4. Preserve cancellation awareness when adding expensive loops.
5. Keep `LoadedAssemblies`, `Assemblies`, and cache updates consistent.

## Common Pitfalls
- Adding scan paths without duplicate checks causes repeated class registration.
- Removing subfolder scans breaks NuGet-downloaded plugin discovery.
- Calling full `ScanAssembly` for every scenario can over-scan where data-source-only scan is enough.
- Forgetting to update timing/statistics hooks in new load paths.

## Quick Verification
- `LoadAllAssembly` returns `ErrorObject` with `Errors.Ok` on healthy config.
- `DataSourcesClasses` and `ConfigEditor.DataSourcesClasses` are populated.
- `LoaderExtensionInstances` is non-empty when extension assemblies are present.
- `GetLoadStatistics().TotalAssembliesLoaded` reflects current `LoadedAssemblies`.

