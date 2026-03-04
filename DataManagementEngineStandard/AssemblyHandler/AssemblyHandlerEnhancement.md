# AssemblyHandler Enhancement Plan

> **Goal:** Make `AssemblyHandler` the single source of truth for ALL assembly and NuGet operations.  
> Other projects (Shell, Winform, Desktop) should only handle UI — no duplicate NuGet/assembly logic.

---

## Gap Analysis

The following capabilities exist in **Beep.Shell Infrastructure** but are **missing** from `AssemblyHandler`:

| # | Missing Capability | Shell Reimplementation | Priority |
|---|---|---|---|
| 1 | NuGet package search by keyword | `NuGetPackageService.SearchAndSelectPackageAsync()` | High |
| 2 | NuGet package version listing (public API) | `NuGetPackageService.SelectPackageVersionAsync()` | High |
| 3 | `LoadNuggetFromNuGetAsync` NOT on `IAssemblyHandler` | Shell builds own download pipeline | High |
| 4 | NuGet source management (CRUD + persist) | `NuGetSourceManager` (`nuget_sources.json`) | High |
| 5 | Driver-to-NuGet-package tracking | `DriverPackageTracker` (`installed_drivers.json`) | Medium |
| 6 | Plugin state history (bounded event log) | `PluginStateTracker` | Medium |
| 7 | Plugin load statistics | `PluginManager.GetLoadStatistics()` | Medium |
| 8 | Progress reporting during NuGet downloads | `NuGetPackageService` `progressCallback` | Medium |
| 9 | `IsPackageName()` heuristic | `NuGetPackageService.IsPackageName()` | Low |
| 10 | Hot-reload convenience method | `PluginManager.ReloadPluginAsync()` | Low |
| 11 | Async overloads for key methods | Not present on `IAssemblyHandler` | High |
| 12 | `GetDrivers` / `AddEngineDefaultDrivers` are stubs | Functional in `SharedContextAssemblyHandler` via delegation | High |

---

## Current Architecture

### AssemblyHandler (4 partial files, ~1875 lines)
```
AssemblyHandler.Core.cs       (243 lines)  — Fields, properties, constructor, resolve, dispose
AssemblyHandler.Helpers.cs    (527 lines)  — Instance creation, type resolution, class definitions
AssemblyHandler.Loaders.cs    (787 lines)  — Main orchestrator, folder loading, nugget management
AssemblyHandler.Scanning.cs   (363 lines)  — Parallel scanning, type processing (10 interfaces)
```

### Supporting Classes (already in BeepDM)
```
NuggetManager.cs              (689 lines)  — AssemblyLoadContext isolation, load/unload
NuggetPackageDownloader.cs    (559 lines)  — NuGet SDK (Protocol 7.3.0), download with deps
IAssemblyHandler.cs           (60 lines)   — Interface, 25 members, all sync
```

### NuGet SDK Already Referenced in BeepDM.csproj
- `NuGet.Common 7.3.0`
- `NuGet.Frameworks 7.3.0`
- `NuGet.Packaging 7.3.0`
- `NuGet.Protocol 7.3.0`
- `NuGet.Versioning 7.3.0`

---

## Enhancement Phases

### Phase 1: Enhance NuggetPackageDownloader with Search & Version Capabilities

**File:** `Tools/PluginSystem/NuggetPackageDownloader.cs`

Add the following methods:

```csharp
// 1. Search NuGet packages by keyword
public async Task<List<NuGetSearchResult>> SearchPackagesAsync(
    string searchTerm,
    int skip = 0,
    int take = 20,
    bool includePrerelease = false,
    IEnumerable<string> sources = null,
    CancellationToken token = default)

// 2. Get all versions for a package
public async Task<List<NuGetVersion>> GetPackageVersionsAsync(
    string packageId,
    bool includePrerelease = false,
    IEnumerable<string> sources = null,
    CancellationToken token = default)

// 3. Check if a string looks like a NuGet package name
public static bool IsPackageName(string input)

// 4. Add IProgress<PassedArgs> to DownloadPackageWithDependenciesAsync
public async Task<Dictionary<string, string>> DownloadPackageWithDependenciesAsync(
    string packageId,
    string version = null,
    IEnumerable<string> sources = null,
    IProgress<PassedArgs> progress = null,
    CancellationToken token = default)
```

**New model class:**
```csharp
public class NuGetSearchResult
{
    public string PackageId { get; set; }
    public string Version { get; set; }
    public string Description { get; set; }
    public string Authors { get; set; }
    public long TotalDownloads { get; set; }
    public string IconUrl { get; set; }
    public string ProjectUrl { get; set; }
    public List<string> Tags { get; set; }
}
```

---

### Phase 2: Add NuGet Source Management to AssemblyHandler

**New file:** `AssemblyHandler/AssemblyHandler.NuGetSources.cs` (partial class)

Manages custom NuGet sources with persistence:

```csharp
public partial class AssemblyHandler
{
    // Fields
    private List<NuGetSourceConfig> _nugetSources;
    private string _nugetSourcesFilePath;

    // Methods
    public List<NuGetSourceConfig> GetNuGetSources()
    public void AddNuGetSource(string name, string url, bool isEnabled = true)
    public void RemoveNuGetSource(string name)
    public void EnableNuGetSource(string name)
    public void DisableNuGetSource(string name)
    public void SaveNuGetSources()
    private void LoadNuGetSources()
    private List<string> GetActiveSourceUrls()
}
```

**New model:**
```csharp
public class NuGetSourceConfig
{
    public string Name { get; set; }
    public string Url { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime DateAdded { get; set; }
}
```

Persistence file: `{ConfigPath}/nuget_sources.json`  
Default source: `https://api.nuget.org/v3/index.json`

---

### Phase 3: Add Driver Package Tracking

**New file:** `AssemblyHandler/AssemblyHandler.DriverTracking.cs` (partial class)

Track which NuGet packages provide which drivers:

```csharp
public partial class AssemblyHandler
{
    // Fields
    private List<DriverPackageMapping> _driverPackageMappings;
    private string _driverMappingsFilePath;

    // Methods
    public void TrackDriverPackage(string packageId, string version, string driverClassName, DataSourceType dsType)
    public void UntrackDriverPackage(string packageId)
    public DriverPackageMapping GetDriverPackageMapping(string driverClassName)
    public List<DriverPackageMapping> GetAllDriverPackageMappings()
    public bool IsDriverFromNuGet(string driverClassName)
    public void SaveDriverMappings()
    private void LoadDriverMappings()
}
```

**New model:**
```csharp
public class DriverPackageMapping
{
    public string PackageId { get; set; }
    public string Version { get; set; }
    public string DriverClassName { get; set; }
    public DataSourceType DataSourceType { get; set; }
    public DateTime InstalledDate { get; set; }
    public string InstallPath { get; set; }
}
```

Persistence file: `{ConfigPath}/driver_packages.json`

---

### Phase 4: Add Load Statistics

**New file:** `AssemblyHandler/AssemblyHandler.Statistics.cs` (partial class)

```csharp
public partial class AssemblyHandler
{
    // Fields
    private AssemblyLoadStatistics _loadStatistics;

    // Methods
    public AssemblyLoadStatistics GetLoadStatistics()
    private void IncrementStatistic(string category)
    private void ResetStatistics()
}
```

**New model:**
```csharp
public class AssemblyLoadStatistics
{
    public int TotalAssembliesLoaded { get; set; }
    public int TotalAssembliesFailed { get; set; }
    public int DriversFound { get; set; }
    public int DataSourcesFound { get; set; }
    public int AddinsFound { get; set; }
    public int NuGetPackagesLoaded { get; set; }
    public int NuGetPackagesFailed { get; set; }
    public TimeSpan TotalLoadTime { get; set; }
    public DateTime LastLoadTimestamp { get; set; }
    public Dictionary<string, int> AssembliesByFolderType { get; set; }
    public List<string> FailedAssemblyPaths { get; set; }
}
```

Integrate `Stopwatch` into `LoadAllAssembly` and `LoadNuggetFromNuGetAsync` to populate stats.

---

### Phase 5: Expand IAssemblyHandler Interface

**File:** `DataManagementModelsStandard/Tools/IAssemblyHandler.cs`

Add the following members (non-breaking — all new):

```csharp
// NuGet Search & Download
Task<List<NuGetSearchResult>> SearchNuGetPackagesAsync(string searchTerm, int skip = 0, int take = 20, bool includePrerelease = false, CancellationToken token = default);
Task<List<NuGetVersion>> GetNuGetPackageVersionsAsync(string packageId, bool includePrerelease = false, CancellationToken token = default);
Task<List<Assembly>> LoadNuggetFromNuGetAsync(string packageName, string version = null, IEnumerable<string> sources = null, bool useSingleSharedContext = true, string appInstallPath = null, bool useProcessHost = false);

// NuGet Source Management
List<NuGetSourceConfig> GetNuGetSources();
void AddNuGetSource(string name, string url, bool isEnabled = true);
void RemoveNuGetSource(string name);

// Driver Package Tracking
void TrackDriverPackage(string packageId, string version, string driverClassName, DataSourceType dsType);
List<DriverPackageMapping> GetAllDriverPackageMappings();
bool IsDriverFromNuGet(string driverClassName);

// Statistics
AssemblyLoadStatistics GetLoadStatistics();

// Async overloads for existing methods
Task<IErrorsInfo> LoadAllAssemblyAsync(IProgress<PassedArgs> progress, CancellationToken token);
```

---

### Phase 6: Implement New Interface Members in AssemblyHandler

Wire up the new `IAssemblyHandler` methods:

| New Interface Method | Implementation |
|---|---|
| `SearchNuGetPackagesAsync` | Delegate to `NuggetPackageDownloader.SearchPackagesAsync()` |
| `GetNuGetPackageVersionsAsync` | Delegate to `NuggetPackageDownloader.GetPackageVersionsAsync()` |
| `LoadNuggetFromNuGetAsync` | **Already implemented** — just add to interface |
| `GetNuGetSources` / `AddNuGetSource` / `RemoveNuGetSource` | Delegate to Phase 2 partial |
| `TrackDriverPackage` / `GetAllDriverPackageMappings` / `IsDriverFromNuGet` | Delegate to Phase 3 partial |
| `GetLoadStatistics` | Delegate to Phase 4 partial |
| `LoadAllAssemblyAsync` | Wrap `LoadAllAssembly` in `Task.Run` with async scanning |

**Reuse single `NuggetPackageDownloader` instance** (currently creates new one per call in `LoadNuggetFromNuGetAsync`):

```csharp
// In AssemblyHandler.Core.cs
private NuggetPackageDownloader _packageDownloader;

// In constructor
_packageDownloader = new NuggetPackageDownloader(Logger);
```

---

### Phase 7: Fix Existing Issues

1. **`GetDrivers()` stub** — Implement real driver enumeration from `DataDriversConfig` and scanned types
2. **`AddEngineDefaultDrivers()` stub** — Implement default driver registration (SQLite, InMemory, CSV, etc.)
3. **`SendMessege` typo** — Rename to `SendMessage` (internal method, safe to rename)
4. **New `NuggetPackageDownloader` per call** — Reuse singleton instance (Phase 6)
5. **Missing null checks** — Add defensive null checks matching `SharedContextAssemblyHandler` patterns

---

## File Summary

| Phase | File | Action |
|---|---|---|
| 1 | `Tools/PluginSystem/NuggetPackageDownloader.cs` | Enhance |
| 1 | `Tools/PluginSystem/NuGetSearchResult.cs` | New |
| 2 | `AssemblyHandler/AssemblyHandler.NuGetSources.cs` | New |
| 2 | `Models/NuGetSourceConfig.cs` | New (or in DataManagementModelsStandard) |
| 3 | `AssemblyHandler/AssemblyHandler.DriverTracking.cs` | New |
| 3 | `Models/DriverPackageMapping.cs` | New (or in DataManagementModelsStandard) |
| 4 | `AssemblyHandler/AssemblyHandler.Statistics.cs` | New |
| 4 | `Models/AssemblyLoadStatistics.cs` | New (or in DataManagementModelsStandard) |
| 5 | `DataManagementModelsStandard/Tools/IAssemblyHandler.cs` | Enhance |
| 6 | `AssemblyHandler/AssemblyHandler.Core.cs` | Enhance (singleton downloader) |
| 6 | `AssemblyHandler/AssemblyHandler.Loaders.cs` | Enhance (wire new methods) |
| 7 | `AssemblyHandler/AssemblyHandler.Helpers.cs` | Fix stubs |
| 7 | `AssemblyHandler/AssemblyHandler.Loaders.cs` | Fix typo, reuse downloader |

---

## Dependency Order

```
Phase 1 (NuggetPackageDownloader enhancements)
    ↓
Phase 2 (NuGet Source Management)    — depends on Phase 1 (GetActiveSourceUrls)
    ↓
Phase 3 (Driver Tracking)           — independent, can run in parallel with Phase 2
    ↓
Phase 4 (Statistics)                 — independent
    ↓
Phase 5 (IAssemblyHandler interface) — depends on Phase 1-4 models
    ↓
Phase 6 (Wire implementations)       — depends on Phase 1-5
    ↓
Phase 7 (Fixes)                      — can be done anytime
```

---

## Success Criteria

- [ ] `IAssemblyHandler` is the only interface other projects need for assembly/NuGet operations
- [ ] Shell Infrastructure can delete `NuGetPackageService`, `NuGetSourceManager`, `DriverPackageTracker` and use `IAssemblyHandler` instead
- [ ] Winform NuggetManager UI delegates all logic to `IAssemblyHandler`
- [ ] No NuGet SDK references needed outside BeepDM
- [ ] All existing `AssemblyHandler` functionality preserved
- [ ] New async methods with `CancellationToken` and `IProgress<PassedArgs>` support
