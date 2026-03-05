---
name: shared-context-loading-resolution
description: Detailed guidance for SharedContextAssemblyHandler loading workflow and assembly resolution. Use when modifying LoadAllAssembly orchestration, framework-specific path resolution, AppDomain registration, and shared context assembly binding behavior.
---

# Shared Context Loading And Resolution

Use this skill when changing the runtime loading pipeline.

## Additional Resources
- End-to-end scenarios: [reference.md](reference.md)

## Core Methods
- `LoadAllAssembly(IProgress<PassedArgs> progress, CancellationToken token)`
- `LoadAssembly(string path, FolderFileTypes fileTypes)`
- `LoadAssembly(string path)`
- `GetBuiltinClasses()`
- `CurrentDomain_AssemblyResolve(...)`

## Loader Pipeline (High Level)
1. Builtin class scan
2. Runtime assembly registration (`LoadAssemblyFromRuntime`)
3. Framework extension load
4. Folder-type loads (drivers, data sources, project/addin, other dlls)
5. Driver/data source/addin scans
6. Default drivers + dedupe
7. Extension processing + addin hierarchy

## Resolution/Interop Details
- `LoadAssembly(path, fileTypes)` loads via `SharedContextManager.LoadNuggetAsync(...)`.
- Runtime/AppDomain assemblies are registered into shared context to avoid duplicate type identities.
- Resolver flow combines shared context visibility and app-domain fallback behavior.
- Multi-target path handling uses framework-specific resolution before load.

## Safe Change Rules
1. Keep `_assemblies` and `_loadedAssemblies` updates synchronized.
2. Preserve subfolder scanning for downloaded package layouts.
3. Do not bypass registration of already-loaded AppDomain assemblies.
4. Keep progress reporting non-blocking.
5. Keep cancellation token plumbing when extending long-running loops.

## Common Pitfalls
- Removing runtime registration causes duplicate load/type conflicts.
- Loading from path without framework resolution breaks multi-target package layouts.
- Skipping dedupe on assembly collections causes repeated scanning work.
- Throwing on a single folder failure aborts full discovery unnecessarily.

## Verification Checklist
- `LoadAllAssembly` completes with populated `LoadedAssemblies`.
- Drivers and data source classes are discovered for configured folders.
- Shared-context resolver can serve already-loaded project references.
- `GetLoadStatistics` reflects recent load operation.

