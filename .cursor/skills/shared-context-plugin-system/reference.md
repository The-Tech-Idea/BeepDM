# Shared Context PluginSystem Reference

Larger examples for plugin lifecycle, events, manager orchestration, and health checks in shared context mode.

## Scenario A: Observe lifecycle events while loading plugins

```csharp
using System;
using System.Threading.Tasks;
using TheTechIdea.Beep.Tools;

public static class SharedContextPluginLifecycle
{
    public static async Task WireAndLoadAsync(SharedContextManager manager, string packageId, string version)
    {
        manager.PluginLoaded += (s, e) => Console.WriteLine($"Plugin loaded: {e?.PluginInfo?.PluginId}");
        manager.PluginUnloaded += (s, e) => Console.WriteLine($"Plugin unloaded: {e?.PluginInfo?.PluginId}");
        manager.NuggetLoaded += (s, e) => Console.WriteLine($"Nugget loaded: {e?.PackageId}");

        var loaded = await manager.LoadNuggetAsync(packageId, version);
        Console.WriteLine($"Assemblies loaded from nugget: {loaded.Count}");
    }
}
```

## Scenario B: Register preloaded assemblies into shared context

```csharp
using System;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Tools;

public static class SharedContextRegistrationReference
{
    public static void RegisterRuntimeAssemblies(SharedContextManager manager)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic))
        {
            manager.RegisterExistingAssembly(asm);
        }

        Console.WriteLine($"Shared loaded assemblies: {manager.LoadedAssemblies.Count}");
    }
}
```

## Scenario C: Health and service checks after plugin activation

```csharp
using System;
using TheTechIdea.Beep.Tools;

public static class SharedContextHealthChecks
{
    public static void VerifyRuntime(SharedContextManager manager)
    {
        var health = manager.HealthMonitor?.GetSystemHealth();
        Console.WriteLine($"System health: {health?.OverallStatus}");

        var registeredServices = manager.ServiceManager?.GetRegisteredServiceCount() ?? 0;
        Console.WriteLine($"Registered plugin services: {registeredServices}");
    }
}
```

