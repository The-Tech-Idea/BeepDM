# AssemblyHandler Driver Tracking And Statistics Reference

End-to-end examples for package provenance tracking and runtime statistics checks.

## Scenario A: Track mappings after nugget installation

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.DriversConfigurations;

public static class AssemblyDriverTrackingExamples
{
    public static void TrackKnownDrivers(IAssemblyHandler handler, string packageId, string version, IEnumerable<string> driverClassNames)
    {
        foreach (var className in driverClassNames)
        {
            handler.TrackDriverPackage(
                packageId: packageId,
                version: version,
                driverClassName: className,
                dsType: (DataSourceType)0);
        }

        var mappings = handler.GetAllDriverPackageMappings();
        Console.WriteLine($"Driver mappings tracked: {mappings.Count}");
    }
}
```

## Scenario B: Validate driver provenance in runtime

```csharp
using System;
using TheTechIdea.Beep.Tools;

public static class AssemblyDriverProvenanceCheck
{
    public static bool IsNuGetBacked(IAssemblyHandler handler, string driverClassName)
    {
        var fromNuGet = handler.IsDriverFromNuGet(driverClassName);
        Console.WriteLine($"{driverClassName}: from NuGet = {fromNuGet}");
        return fromNuGet;
    }
}
```

## Scenario C: Collect and report load statistics

```csharp
using System;
using TheTechIdea.Beep.Tools;

public static class AssemblyStatisticsExamples
{
    public static void Report(IAssemblyHandler handler)
    {
        var s = handler.GetLoadStatistics();

        Console.WriteLine($"Assemblies loaded: {s.TotalAssembliesLoaded}");
        Console.WriteLine($"Assemblies failed: {s.TotalAssembliesFailed}");
        Console.WriteLine($"NuGet loaded: {s.NuGetPackagesLoaded}");
        Console.WriteLine($"NuGet failed: {s.NuGetPackagesFailed}");
        Console.WriteLine($"Drivers found: {s.DriversFound}");
        Console.WriteLine($"DataSources found: {s.DataSourcesFound}");
        Console.WriteLine($"Total load time: {s.TotalLoadTime.TotalSeconds:F2}s");

        foreach (var kv in s.AssembliesByFolderType)
        {
            Console.WriteLine($"FolderType={kv.Key}, Count={kv.Value}");
        }
    }
}
```

## Scenario D: Untrack package on uninstall

```csharp
using TheTechIdea.Beep.Tools;

public static class AssemblyDriverUninstallExamples
{
    public static void UntrackPackage(IAssemblyHandler handler, string packageId)
    {
        // Removes all mappings attached to package
        handler.UntrackDriverPackage(packageId);
    }
}
```

## Persistence And Diagnostics Notes
- Driver mappings are persisted to `driver_packages.json` with lock-protected reads/writes.
- NuGet source context is separately persisted in `nuget_sources.json` and can explain package source provenance.
- Statistics are refreshed dynamically in `GetLoadStatistics()` from current runtime collections.
- `LoadAllAssembly` resets and restarts statistics timing each session.

## Quick Validation Checklist
- `TrackDriverPackage` immediately appears in `GetAllDriverPackageMappings()`.
- `IsDriverFromNuGet` returns true for tracked driver class names.
- After uninstall, `UntrackDriverPackage` removes package mappings.
- `GetLoadStatistics` reflects current loaded assembly/driver/datasource counts and folder distribution.

