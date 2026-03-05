# Shared Context NuGet, Sources, And Tracking Reference

End-to-end workflows for searching packages, configuring sources, loading nuggets, and tracking driver provenance.

## Scenario A: Search, choose version, and load nugget

```csharp
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Tools;

public static class SharedContextNuGetReference
{
    public static async Task<bool> InstallTopMatchAsync(IAssemblyHandler handler, string query)
    {
        var matches = await handler.SearchNuGetPackagesAsync(query, take: 15);
        var chosen = matches.FirstOrDefault();
        if (chosen == null)
        {
            return false;
        }

        var versions = await handler.GetNuGetPackageVersionsAsync(chosen.PackageId);
        var preferredVersion = versions.FirstOrDefault() ?? chosen.Version;

        var assemblies = await handler.LoadNuggetFromNuGetAsync(chosen.PackageId, preferredVersion);
        return assemblies.Count > 0;
    }
}
```

## Scenario B: Source CRUD and active-source diagnostics

```csharp
using System;
using TheTechIdea.Beep.Tools;

public static class SharedContextNuGetSourcesReference
{
    public static void ConfigureSources(IAssemblyHandler handler)
    {
        handler.AddNuGetSource("CompanyFeed", "https://pkgs.example.local/v3/index.json", "internal feed");
        handler.AddNuGetSource("Mirror", "https://mirror.example.local/v3/index.json", "mirror feed");
        handler.RemoveNuGetSource("Mirror");

        foreach (var source in handler.GetNuGetSources())
        {
            Console.WriteLine($"{source.Name} - Enabled={source.IsEnabled} - {source.Url}");
        }
    }
}
```

## Scenario C: Driver provenance tracking and load statistics

```csharp
using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Tools;

public static class SharedContextDriverTrackingReference
{
    public static void TrackAndReport(IAssemblyHandler handler)
    {
        handler.TrackDriverPackage(
            packageId: "TheTechIdea.DriverPack.Postgres",
            packageVersion: "1.2.0",
            driverClassNames: new List<string> { "Npgsql.NpgsqlFactory" });

        Console.WriteLine($"Is NuGet driver: {handler.IsDriverFromNuGet("Npgsql.NpgsqlFactory")}");

        var mappings = handler.GetAllDriverPackageMappings();
        Console.WriteLine($"Tracked mappings: {mappings.Count}");

        var stats = handler.GetLoadStatistics();
        Console.WriteLine($"Loads={stats.TotalAssembliesLoaded}, Failures={stats.LoadFailures}");
    }
}
```

