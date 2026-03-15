# AssemblyHandler NuGet Operations Reference

Large examples for package search, version selection, source CRUD, SDK-first nugget load, and runtime synchronization.

## Scenario A: Search + pick version + load nugget

```csharp
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Tools;

public static class AssemblyNuGetExamples
{
    public static async Task<bool> InstallFromNuGetAsync(IAssemblyHandler handler, string searchTerm)
    {
        var packages = await handler.SearchNuGetPackagesAsync(
            searchTerm,
            skip: 0,
            take: 15,
            includePrerelease: false,
            token: CancellationToken.None);

        var selected = packages.FirstOrDefault();
        if (selected == null)
        {
            return false;
        }

        var versions = await handler.GetNuGetPackageVersionsAsync(
            selected.PackageId,
            includePrerelease: false,
            token: CancellationToken.None);

        var version = versions.FirstOrDefault() ?? selected.Version;

        var loaded = await handler.LoadNuggetFromNuGetAsync(
            packageName: selected.PackageId,
            version: version,
            sources: null,
            useSingleSharedContext: true,
            appInstallPath: null,
            useProcessHost: false);

        return loaded.Count > 0;
    }
}
```

## Scenario B: Source CRUD with persistence

```csharp
using System;
using System.Linq;
using TheTechIdea.Beep.Tools;

public static class AssemblyNuGetSourceExamples
{
    public static void ConfigureSources(IAssemblyHandler handler)
    {
        handler.AddNuGetSource("mirror1", "https://api.nuget.org/v3/index.json", isEnabled: true);
        handler.AddNuGetSource("internal", "https://nuget.internal/v3/index.json", isEnabled: true);

        // Optional: disable but keep persisted
        handler.DisableNuGetSource("mirror1");
        handler.EnableNuGetSource("mirror1");

        var active = handler.GetActiveSourceUrls();
        var all = handler.GetNuGetSources();

        Console.WriteLine($"Sources total={all.Count}, active={active.Count}");
    }
}
```

## Scenario C: End-user package picker flow

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Tools;

public static class AssemblyNuGetPicker
{
    public static async Task<List<NuGetSearchResult>> GetTopMatchesAsync(IAssemblyHandler handler, string term)
    {
        var results = await handler.SearchNuGetPackagesAsync(term, take: 20);
        return results
            .OrderByDescending(r => r.TotalDownloads)
            .Take(5)
            .ToList();
    }
}
```

## Scenario D: Unload and verify nugget state

```csharp
using System.Linq;
using TheTechIdea.Beep.Tools;

public static class AssemblyNuGetUnload
{
    public static bool UnloadPackage(IAssemblyHandler handler, string packageName)
    {
        var unloaded = handler.UnloadNugget(packageName);
        var stillTracked = handler.GetAllNuggets()
            .Any(n => n.NuggetName.Equals(packageName, System.StringComparison.OrdinalIgnoreCase));
        return unloaded && !stillTracked;
    }
}
```

## Notes
- `GetNuGetPackageVersionsAsync` currently returns normalized version strings.
- Use `GetActiveSourceUrls()` to confirm effective source list before installs.
- `LoadNuggetFromNuGetAsync` automatically synchronizes loaded assemblies into handler collections and triggers scanning.
- NuGet success/failure counters are maintained through `RecordNuGetSuccess` / `RecordNuGetFailure`.
- Driver provenance can be derived from owning package via `FindNuggetByAssemblyPath(...)` during load.

