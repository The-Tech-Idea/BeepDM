# Forms Performance And Configuration Reference

This reference shows practical setup for configuration persistence and cache/metrics tuning.

## Scenario A: Bootstrap Configuration + Performance Policy

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Models;

public static class FormsPerformanceConfigurationExamples
{
    public static async Task<FormsManager> BuildConfiguredManagerAsync(IDMEEditor editor)
    {
        // Load persisted config
        var cfgManager = new ConfigurationManager();
        cfgManager.LoadConfiguration();
        if (!cfgManager.ValidateConfiguration())
        {
            cfgManager.ResetToDefaults();
        }

        // Customize defaults for this environment
        cfgManager.Configuration.EnableLogging = true;
        cfgManager.Configuration.ValidateBeforeCommit = true;
        cfgManager.Configuration.ClearCacheOnFormClose = false;
        cfgManager.Configuration.MaxRecordsPerBlock = 2000;
        cfgManager.SaveConfiguration();

        // Build manager with explicit configuration manager instance
        var forms = new FormsManager(editor, configurationManager: cfgManager);

        // Register sample blocks
        using var customerUow = new UnitofWork<Customer>(editor, "MyDb", "Customers", "Id");
        var customerStructure = editor.GetDataSource("MyDb").GetEntityStructure("Customers", true);
        forms.RegisterBlock("CUSTOMERS", customerUow, customerStructure, "MyDb", isMasterBlock: true);

        using var orderUow = new UnitofWork<Order>(editor, "MyDb", "Orders", "Id");
        var orderStructure = editor.GetDataSource("MyDb").GetEntityStructure("Orders", true);
        forms.RegisterBlock("ORDERS", orderUow, orderStructure, "MyDb", isMasterBlock: false);

        return forms;
    }
}
```

## Scenario B: Runtime Metrics Loop

```csharp
public static void EvaluateAndTune(FormsManager forms)
{
    var stats = forms.PerformanceManager.GetPerformanceStatistics();
    var efficiency = forms.PerformanceManager.GetCacheEfficiencyMetrics();

    Console.WriteLine($"Hits={stats.CacheHits}, Misses={stats.CacheMisses}, Ratio={stats.CacheHitRatio:P2}");
    Console.WriteLine($"Top blocks tracked={efficiency?.TopAccessedBlocks?.Count ?? 0}");

    // Example threshold policy
    if (stats.CacheHitRatio < 0.60)
    {
        forms.PerformanceManager.OptimizeBlockAccess();
    }
}
```

## Scenario C: Controlled Preload For Frequent Blocks

```csharp
public static void PreloadCommonBlocks(FormsManager forms)
{
    var topBlocks = new[] { "CUSTOMERS", "ORDERS", "ORDER_LINES", "INVOICES" };
    forms.PerformanceManager.PreloadFrequentBlocks(topBlocks);
}
```

## Notes
- Tune by telemetry; avoid blind cache invalidation.
- Keep config updates explicit and persisted.
- Avoid oversized preload lists unless data justifies it.

