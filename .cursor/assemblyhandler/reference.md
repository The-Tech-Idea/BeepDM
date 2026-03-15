# AssemblyHandler Reference

Updated reference for the current classic `AssemblyHandler` partial implementation in:
`BeepDM/DataManagementEngineStandard/AssemblyHandler/*`.

## Runtime Model (Current)
- `AssemblyHandler` owns caches, assembly collections, scan/registration lists, source persistence, tracking, and statistics.
- `NuggetManager` is the NuGet orchestration facade and SDK-first package loader.
- `AssemblyHandler.NuGetOperations` delegates NuGet operations to `NuggetManager`, then synchronizes handler collections and scan state.

## Scenario A: Full startup discovery + metrics snapshot

```csharp
using System;
using System.Threading;
using TheTechIdea.Beep.Tools;

public static class AssemblyStartup
{
    public static IErrorsInfo Bootstrap(IAssemblyHandler handler)
    {
        var progress = new Progress<PassedArgs>(p =>
            Console.WriteLine($"[AssemblyHandler] {p?.Messege}"));

        var result = handler.LoadAllAssembly(progress, CancellationToken.None);
        var s = handler.GetLoadStatistics();

        Console.WriteLine(
            $"Loaded={s.TotalAssembliesLoaded}, Failed={s.TotalAssembliesFailed}, " +
            $"Drivers={s.DriversFound}, DataSources={s.DataSourcesFound}, " +
            $"NuGetLoaded={s.NuGetPackagesLoaded}, Time={s.TotalLoadTime.TotalSeconds:F1}s");

        return result;
    }
}
```

## Scenario B: SDK-first NuGet flow + provenance check

```csharp
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Tools;

public static class AssemblyNuGetRuntime
{
    public static async Task<bool> InstallAndVerifyAsync(IAssemblyHandler handler, string term)
    {
        var packages = await handler.SearchNuGetPackagesAsync(term, take: 10);
        var pkg = packages.FirstOrDefault();
        if (pkg == null) return false;

        var versions = await handler.GetNuGetPackageVersionsAsync(pkg.PackageId);
        var version = versions.FirstOrDefault() ?? pkg.Version;

        var loaded = await handler.LoadNuggetFromNuGetAsync(
            packageName: pkg.PackageId,
            version: version,
            useSingleSharedContext: true);

        if (loaded.Count == 0) return false;

        // Automatic tracking occurs for discovered drivers during load.
        // Optional runtime check for a known driver class:
        var fromNuGet = handler.IsDriverFromNuGet("MyNamespace.MyDriverClass");
        return fromNuGet || loaded.Count > 0;
    }
}
```

## Scenario C: Source persistence + folder-based incremental scan

```csharp
using System;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;

public static class AssemblyOps
{
    public static void ConfigureAndLoad(IAssemblyHandler handler, string pluginFolder)
    {
        handler.AddNuGetSource("internal", "https://nuget.internal/v3/index.json", isEnabled: true);
        handler.DisableNuGetSource("internal"); // persisted but inactive
        handler.EnableNuGetSource("internal");

        var active = handler.GetActiveSourceUrls();
        var loaded = handler.LoadAssembliesFromFolder(pluginFolder, FolderFileTypes.OtherDLL, scanForDataSources: true);

        Console.WriteLine($"ActiveSources={active.Count}, FolderLoaded={loaded.Count}");
    }
}
```

## Scenario D: Unload behavior

```csharp
using TheTechIdea.Beep.Tools;

public static class AssemblyUnload
{
    public static bool RemovePackageAndMappings(IAssemblyHandler handler, string packageId)
    {
        // Unload runtime assemblies tracked under package.
        var unloaded = handler.UnloadNugget(packageId);

        // Optional explicit provenance cleanup on uninstall workflow.
        handler.UntrackDriverPackage(packageId);
        return unloaded;
    }
}
```

## Direct Answer: How data sources and extensions are added

At scan time, each discovered `TypeInfo` is routed in `ProcessTypeInfo(...)`:

```csharp
// IDataSource -> AssemblyClassDefinition in two collections
if (typeInfo.ImplementedInterfaces.Contains(typeof(IDataSource)))
{
    var xcls = GetAssemblyClassDefinition(typeInfo, "IDataSource");
    DataSourcesClasses.Add(xcls);
    ConfigEditor.DataSourcesClasses.Add(xcls);
}

// ILoaderExtention -> type list + AssemblyClassDefinition metadata list
if (typeInfo.ImplementedInterfaces.Contains(typeof(ILoaderExtention)))
{
    LoaderExtensions.Add(typeInfo);
    LoaderExtensionClasses.Add(GetAssemblyClassDefinition(typeInfo, "ILoaderExtention"));
}
```

`GetAssemblyClassDefinition(...)` is where `AddinAttribute`, `AddinVisSchema`, command methods, datasource/category, and ordering metadata are captured into a single `AssemblyClassDefinition`.

## Verification Checklist
- `LoadAllAssembly` populates `LoadedAssemblies`, `Assemblies`, `DataSourcesClasses`, and ConfigEditor registration lists.
- `GetLoadStatistics()` reflects current dynamic counts and folder-type grouping.
- `LoadNuggetFromNuGetAsync` updates handler tracking collections (via sync helper) and increments NuGet counters.
- Source mutations persist to `nuget_sources.json`; driver mappings persist to `driver_packages.json`.

