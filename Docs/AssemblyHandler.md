# Assembly Handler Guide

## Overview

`AssemblyHandler` is the single backend for classic assembly loading, scanning, and NuGet routing logic. It discovers plugins, loads assemblies, manages NuGet packages, and tracks driver statistics.

## Architecture

```
IAssemblyHandler (Caller-facing abstraction)
    |
    v
AssemblyHandler (Implementation)
├── AssemblyHandler.Core.cs          # Core resolution and type cache
├── AssemblyHandler.Loaders.cs     # Folder/runtime loading
├── AssemblyHandler.Scanning.cs    # Plugin discovery
├── AssemblyHandler.Helpers.cs     # Reflection helpers
├── AssemblyHandler.NuGetOperations.cs  # NuGet search/download
├── AssemblyHandler.NuGetSources.cs     # Source management
├── AssemblyHandler.DriverTracking.cs   # Driver provenance
└── AssemblyHandler.Statistics.cs       # Load metrics
```

## File Locations

- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Core.cs`
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Loaders.cs`
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Scanning.cs`
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Helpers.cs`
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.NuGetOperations.cs`
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.NuGetSources.cs`
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.DriverTracking.cs`
- `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Statistics.cs`
- `DataManagementEngineStandard/AssemblyHandler/NuggetManager.cs`

## Core Capabilities

- Assembly resolution: `CurrentDomain_AssemblyResolve`, runtime+cache aware lookup
- Type cache lifecycle: `AddTypeToCache`, `GetTypeFromCache`, `ClearTypeCache`
- Folder/runtime loading: direct load, folder load, runtime registration, fallback folder discovery
- Scanning + registration: IDataSource, addins, workflow artifacts, print managers, global functions, loader extensions
- NuGet routing via `NuggetManager`: search, versions, package load/install, unload
- NuGet source persistence: add/remove/enable/disable sources and active source resolution
- Driver provenance persistence: mapping drivers to package/version/source
- Statistics lifecycle: reset/start/stop timing, load failure paths, NuGet success/failure counters

## Typical Workflow

1. Construct `AssemblyHandler` with `IConfigEditor`, `IErrorsInfo`, `IDMLogger`, and `IUtil`.
2. Call `LoadAllAssembly(progress, token)` to initialize builtins, loader extensions, folders, scans, defaults, and extension passes.
3. Use helper/reflection APIs after load state is initialized (`LoadedAssemblies`, `Assemblies`, `ConfigEditor.*` lists).
4. Use NuGet APIs (`SearchNuGetPackagesAsync`, `GetNuGetPackageVersionsAsync`, `LoadNuggetFromNuGetAsync`) for dynamic acquisition.
5. Rely on automatic synchronization (`SyncNuggetAssembliesToHandlerCollections`) to keep caches and scans current.
6. Use source management + driver provenance + statistics for operational diagnostics.

## Basic Usage

```csharp
// Create assembly handler
var assemblyHandler = new AssemblyHandler(
    configEditor: editor.ConfigEditor,
    errorsInfo: editor.ErrorObject,
    logger: editor.Logger,
    util: editor.Utilfunction);

// Load all assemblies
var progress = new Progress<PassedArgs>(args =>
    Console.WriteLine($"Loading: {args.Messege}"));

assemblyHandler.LoadAllAssembly(progress, CancellationToken.None);

// Access loaded assemblies
foreach (var assembly in assemblyHandler.LoadedAssemblies)
{
    Console.WriteLine($"Loaded: {assembly.FullName}");
}
```

## Type Cache Management

```csharp
// Add type to cache
assemblyHandler.AddTypeToCache(typeof(MyDataSource));

// Get type from cache
var type = assemblyHandler.GetTypeFromCache("MyNamespace.MyDataSource");

// Clear cache
assemblyHandler.ClearTypeCache();
```

## Plugin Discovery

```csharp
// Scan for data sources
var dataSources = assemblyHandler.ScanForDataSources();
foreach (var ds in dataSources)
{
    Console.WriteLine($"Found datasource: {ds.PackageName}");
}

// Scan for add-ins
var addins = assemblyHandler.ScanForAddins();
foreach (var addin in addins)
{
    Console.WriteLine($"Found addin: {addin.AddinName}");
}

// Scan for workflow artifacts
var workflows = assemblyHandler.ScanForWorkflows();
```

## NuGet Operations

```csharp
// Search for packages
var packages = await assemblyHandler.SearchNuGetPackagesAsync(
    "TheTechIdea.Beep.DataSources", 
    take: 20);

// Get package versions
var versions = await assemblyHandler.GetNuGetPackageVersionsAsync(
    "TheTechIdea.Beep.DataSources.SQLite");

// Load package from NuGet
var result = await assemblyHandler.LoadNuggetFromNuGetAsync(
    packageId: "TheTechIdea.Beep.DataSources.SQLite",
    version: "2.0.0",
    progress: progressReporter);

if (result.Success)
{
    Console.WriteLine("Package loaded successfully");
}
```

## NuGet Source Management

```csharp
// Add source
assemblyHandler.AddNuGetSource(
    name: "MyCompanyFeed",
    url: "https://nuget.mycompany.com/v3/index.json",
    enabled: true);

// Remove source
assemblyHandler.RemoveNuGetSource("MyCompanyFeed");

// Enable/disable source
assemblyHandler.EnableNuGetSource("MyCompanyFeed", true);

// Get active source
var activeSource = assemblyHandler.GetActiveNuGetSource();
```

## Driver Tracking

```csharp
// Track driver provenance
assemblyHandler.TrackDriverPackage(
    driverName: "SQLite",
    packageId: "TheTechIdea.Beep.DataSources.SQLite",
    version: "2.0.0",
    source: "nuget.org");

// Get driver info
var driverInfo = assemblyHandler.GetDriverTrackingInfo("SQLite");
Console.WriteLine($"Package: {driverInfo.PackageId}");
Console.WriteLine($"Version: {driverInfo.Version}");
```

## Statistics

```csharp
// Start timing
assemblyHandler.StartLoadTimer("FullLoad");

// ... load operations ...

// Stop timing
assemblyHandler.StopLoadTimer("FullLoad");
var elapsed = assemblyHandler.GetLoadElapsed("FullLoad");
Console.WriteLine($"Load took: {elapsed.TotalSeconds}s");

// Get load statistics
var stats = assemblyHandler.GetLoadStatistics();
Console.WriteLine($"Assemblies loaded: {stats.AssembliesLoaded}");
Console.WriteLine($"Failures: {stats.FailureCount}");
Console.WriteLine($"NuGet packages: {stats.NuGetPackagesLoaded}");

// Reset statistics
assemblyHandler.ResetStatistics();
```

## Loading from Specific Paths

```csharp
// Load from specific folder
assemblyHandler.LoadExtensionsFromPaths(
    paths: new[] { "./Plugins", "./CustomDrivers" },
    namespaces: new[] { "TheTechIdea", "MyCompany" });

// Load single assembly
assemblyHandler.LoadAssembly("./Plugins/MyCustomPlugin.dll");
```

## Error Handling

```csharp
try
{
    assemblyHandler.LoadAllAssembly(progress, token);
}
catch (Exception ex)
{
    editor.Logger.WriteLog($"Assembly load failed: {ex.Message}");
    editor.ErrorObject.Flag = Errors.Failed;
    editor.ErrorObject.Message = ex.Message;
}

// Check for load errors
if (assemblyHandler.HasLoadErrors)
{
    foreach (var error in assemblyHandler.LoadErrors)
    {
        Console.WriteLine($"Load Error: {error}");
    }
}
```

## Integration with DMEEditor

```csharp
// Access through DMEEditor
var handler = editor.assemblyHandler;

// Load assemblies with progress
var progress = new Progress<PassedArgs>(args =>
    editor.progress?.Report(args));

handler.LoadAllAssembly(progress, CancellationToken.None);

// Access discovered types
foreach (var dsClass in editor.ConfigEditor.DataDriversClasses)
{
    Console.WriteLine($"Driver: {dsClass.PackageName}");
}
```

## Pitfalls

- Always check `HasLoadErrors` after `LoadAllAssembly`.
- Use `CancellationToken` for long-running loads.
- Cache types after first lookup to avoid repeated reflection.
- Synchronize NuGet assemblies to handler collections after package loads.
- Track driver provenance for debugging and compliance.

## Related Documentation

- [Core Architecture](CoreArchitecture.md) - IDMEEditor overview
- [Data Source Implementation](HowToCreateNewDataSource.md) - Building custom plugins
- [Configuration Management](Configuration.md) - ConfigEditor deep dive
