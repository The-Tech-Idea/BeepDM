# AssemblyHandler Enhancement Notes

## Goal

`AssemblyHandler` and `NuggetManager` now run a full NuGet SDK-first flow while keeping loaded assemblies app-visible (shared/default load path by default), so packages and local assemblies can resolve each other.

## Final Architecture

- `NuggetPackageDownloader` is the only package acquisition engine (search, versions, download with dependencies, app install copy).
- `NuggetManager` is the orchestration facade that:
  - delegates all NuGet operations to the SDK downloader
  - loads package assemblies from SDK-resolved paths
  - tracks package/assembly ownership and unload state
- `AssemblyHandler` delegates NuGet operations to `NuggetManager`, then synchronizes handler collections (`LoadedAssemblies`, `Assemblies`, `_loadedAssemblyCache`) and scanning state.

## Implemented Changes

### NuggetManager

File: `DataManagementEngineStandard/AssemblyHandler/NuggetManager.cs`

- Added SDK facade methods:
  - `SearchNuGetPackagesAsync(...)`
  - `GetNuGetPackageVersionsAsync(...)`
  - `LoadNuggetFromNuGetAsync(...)`
  - `InstallPackageToAppDirectory(...)`
- Removed manual `.nupkg` runtime extraction behavior from load flow.
  - Direct `.nupkg` path in `LoadNugget(...)` is now rejected with guidance to use SDK-based `LoadNuggetFromNuGetAsync(...)`.
- Added package-aware key resolution so NuGet package identity is used for tracking instead of fragile folder names.
- Kept isolated `AssemblyLoadContext` support only as explicit opt-in; shared app-visible loading remains the default behavior.

### AssemblyHandler NuGet routing

Files:
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Core.cs`
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.NuGetOperations.cs`

- Removed direct `NuggetPackageDownloader` ownership from `AssemblyHandler.Core.cs`.
- `AssemblyHandler.NuGetOperations.cs` now routes:
  - package search -> `NuggetManager.SearchNuGetPackagesAsync`
  - version lookup -> `NuggetManager.GetNuGetPackageVersionsAsync`
  - package load -> `NuggetManager.LoadNuggetFromNuGetAsync`
- NuGet statistics (`RecordNuGetSuccess` / `RecordNuGetFailure`) are incremented exactly once per top-level operation result.

### Load/unload synchronization

Files:
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Helpers.cs`
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Loaders.cs`

- Added `SyncNuggetAssembliesToHandlerCollections(...)` helper to centralize:
  - collection updates
  - cache updates
  - assembly scanning
- Updated `LoadNugget(...)` to default to shared app-visible loading (`useIsolatedContext: false`) and use the sync helper.
- Updated `UnloadNugget(...)` to remove matching assemblies from handler tracking collections and cache after manager unload.

### Source, tracking, and provenance

Files:
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.NuGetSources.cs`
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.NuGetOperations.cs`
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.DriverTracking.cs`

- Active NuGet sources remain canonical via `GetActiveSourceUrls()`, now fed into manager SDK calls.
- Driver package tracking is now recorded from assembly ownership (`FindNuggetByAssemblyPath`) when available, preserving package provenance from actual loaded outputs.

## Behavior and Compatibility Notes

- Shared visibility default:
  - Assemblies loaded from NuGet packages are loaded into the shared app-visible path by default.
  - This supports cross-resolution with other assemblies loaded by classic `AssemblyHandler`.
- Isolated load mode:
  - Still available as explicit opt-in through manager-level call paths.
- Deprecated behavior:
  - Loading raw `.nupkg` files directly through `LoadNugget(path)` is deprecated and blocked.
  - Use `LoadNuggetFromNuGetAsync(packageName, ...)` for package acquisition and dependency handling.
