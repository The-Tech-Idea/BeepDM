# BeepService Migration Guide

This guide helps you migrate from legacy BeepService registration patterns to the new modern fluent API introduced in version 2.0.

## Table of Contents

- [Overview of Changes](#overview-of-changes)
- [Breaking Changes](#breaking-changes)
- [Property Name Changes](#property-name-changes)
- [Migration Scenarios](#migration-scenarios)
- [Deprecation Timeline](#deprecation-timeline)
- [FAQ](#faq)

---

## Overview of Changes

The BeepService registration system has been modernized with:

1. **Fluent Builder API** - Method chaining for better discoverability
2. **Environment-Specific Helpers** - Optimized methods for Desktop, Web, and Blazor
3. **Standardized Naming** - `AppRepoName` replaces `Containername`/`ContainerName`
4. **Enhanced Validation** - Better error messages and validation exceptions
5. **Removed Static Caching** - True DI container-managed lifetimes

---

## Breaking Changes

### 1. Static Caching Removed

**What Changed**: The static `_cachedBeepService` field has been removed from `RegisterBeepServicesInternal`.

**Impact**: Multiple calls to `AddBeepServices()` no longer return the same cached instance.

**Migration**:

**Before**:
```csharp
// First call
var beepService1 = services.AddBeepServices(opts => opts.DirectoryPath = path1);

// Second call returned cached instance
var beepService2 = services.AddBeepServices(opts => opts.DirectoryPath = path2);
// beepService2 was same as beepService1 (cached)
```

**After**:
```csharp
// Only register once
services.AddBeepServices(opts => opts.DirectoryPath = path);

// Get instance from service provider
var provider = services.BuildServiceProvider();
var beepService = provider.GetRequiredService<IBeepService>();
```

### 2. Validation Exceptions Changed

**What Changed**: Validation now throws `BeepServiceValidationException` instead of `ArgumentException`.

**Impact**: Catch blocks need updating.

**Migration**:

**Before**:
```csharp
try
{
    services.AddBeepServices(opts => opts.DirectoryPath = "");
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.Message);
}
```

**After**:
```csharp
try
{
    services.AddBeepServices(opts => opts.DirectoryPath = "");
}
catch (BeepServiceValidationException ex)
{
    Console.WriteLine($"Validation failed for {ex.PropertyName}: {ex.Message}");
}
```

### 3. AddBeepServices() Fluent Overload

**What Changed**: `AddBeepServices()` now has a parameterless overload returning `IBeepServiceBuilder`.

**Impact**: Existing `Action<BeepServiceOptions>` overload still works but fluent API is preferred.

**Migration**:

**Before**:
```csharp
services.AddBeepServices(opts =>
{
    opts.DirectoryPath = path;
    opts.AppRepoName = "MyApp";
    opts.EnableAutoMapping = true;
});
```

**After (Recommended)**:
```csharp
services.AddBeepServices()
    .WithDirectory(path)
    .WithAppRepo("MyApp")
    .WithMapping()
    .AsSingleton()
    .Build();
```

**After (Still Supported)**:
```csharp
// Old pattern still works
services.AddBeepServices(opts =>
{
    opts.DirectoryPath = path;
    opts.AppRepoName = "MyApp";
    opts.EnableAutoMapping = true;
});
```

---

## Property Name Changes

### Containername → AppRepoName

**What Changed**: `Containername` property marked obsolete, replaced with `AppRepoName`.

#### In IBeepService Interface

**Before**:
```csharp
public interface IBeepService
{
    string Containername { get; }
}
```

**After**:
```csharp
public interface IBeepService
{
    string AppRepoName { get; }
    
    [Obsolete("Use AppRepoName instead")]
    string Containername { get; } // Still works but shows warning
}
```

#### In Code Usage

**Before**:
```csharp
var containerName = beepService.Containername;
Console.WriteLine($"Container: {beepService.Containername}");
```

**After**:
```csharp
var appRepoName = beepService.AppRepoName;
Console.WriteLine($"App Repo: {beepService.AppRepoName}");
```

#### In Configuration

**Before**:
```csharp
// BeepServiceOptions
options.AppRepoName = "MyApp"; // Already correct

// DesktopServiceOptions
options.ContainerName = "MyApp"; // Now obsolete
```

**After**:
```csharp
// All options classes now use AppRepoName
options.AppRepoName = "MyApp";
```

---

## Migration Scenarios

### Scenario 1: Direct BeepService Instantiation

**Legacy Pattern** (❌ Deprecated):

```csharp
var beepService = new BeepService();
beepService.Configure(
    directoryPath: AppContext.BaseDirectory,
    containername: "MyApp",
    configType: BeepConfigType.Application,
    AddasSingleton: true
);
```

**Modern Pattern** (✅ Recommended):

```csharp
// Option A: Fluent Builder
services.AddBeepServices()
    .WithDirectory(AppContext.BaseDirectory)
    .WithAppRepo("MyApp")
    .WithConfigType(BeepConfigType.Application)
    .AsSingleton()
    .Build();

// Option B: Action-based
services.AddBeepServices(opts =>
{
    opts.DirectoryPath = AppContext.BaseDirectory;
    opts.AppRepoName = "MyApp";
    opts.ConfigType = BeepConfigType.Application;
    opts.ServiceLifetime = ServiceLifetime.Singleton;
});
```

### Scenario 2: RegisterContainer.AddContainer()

**Legacy Pattern** (❌ Deprecated):

```csharp
RegisterContainer.AddContainer(services, "MyApp", BeepConfigType.Application);
```

**Modern Pattern** (✅ Recommended):

```csharp
// Desktop App
services.AddBeepForDesktop(opts =>
{
    opts.AppRepoName = "MyApp";
    opts.ConfigType = BeepConfigType.Application;
});

// Web App
services.AddBeepForWeb(opts =>
{
    opts.AppRepoName = "MyApp";
    opts.ConfigType = BeepConfigType.Application;
});
```

### Scenario 3: RegisterBeep.Register()

**Legacy Pattern** (⚠️ Still supported but not preferred):

```csharp
RegisterBeep.Register(services, directoryPath, containerName, configType, addAsSingleton: true);
```

**Modern Pattern** (✅ Recommended):

```csharp
// Use environment-specific helpers
services.AddBeepForDesktop(opts =>
{
    opts.DirectoryPath = directoryPath;
    opts.AppRepoName = containerName;
    opts.ConfigType = configType;
});
```

### Scenario 4: Desktop WinForms Application

**Legacy Pattern**:

```csharp
[STAThread]
static void Main(string[] args)
{
    var services = new ServiceCollection();
    var beepService = new BeepService(services);
    beepService.Configure(AppContext.BaseDirectory, "MyApp", BeepConfigType.Application, true);
    beepService.LoadAssemblies();
    
    Application.Run(new MainForm(beepService));
}
```

**Modern Pattern**:

```csharp
[STAThread]
static void Main(string[] args)
{
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
            services.AddBeepForDesktop(opts => 
                opts.DirectoryPath = AppContext.BaseDirectory);
        })
        .Build();
    
    var progress = new Progress<PassedArgs>(args => Console.WriteLine(args.Messege));
    var beepService = host.UseBeepForDesktop(progress);
    
    Application.Run(new MainForm(beepService));
}
```

### Scenario 5: ASP.NET Core Web API

**Legacy Pattern**:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    var beepService = services.Register(
        directoryPath: Path.Combine(basePath, "Beep"),
        containerName: "WebApi",
        configType: BeepConfigType.Application,
        addAsSingleton: false // Scoped for web
    );
    
    services.AddControllers();
}

public void Configure(IApplicationBuilder app)
{
    app.UseRouting();
    app.UseEndpoints(endpoints => endpoints.MapControllers());
}
```

**Modern Pattern**:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBeepForWeb(opts =>
{
    opts.DirectoryPath = Path.Combine(builder.Environment.ContentRootPath, "Beep");
    opts.AppRepoName = "WebApi";
});

builder.Services.AddControllers();

var app = builder.Build();
app.UseBeepForWeb(); // Adds connection cleanup middleware
app.UseRouting();
app.MapControllers();
app.Run();
```

### Scenario 6: Blazor Server

**Legacy Pattern**:

```csharp
builder.Services.RegisterScoped();
// Manual configuration required
```

**Modern Pattern**:

```csharp
builder.Services.AddBeepForBlazorServer(opts =>
{
    opts.DirectoryPath = Path.Combine(builder.Environment.ContentRootPath, "Beep");
    opts.AppRepoName = "MyBlazorApp";
    opts.EnableSignalRProgress = true;
    opts.EnableCircuitHandlers = true;
});
```

---

## Deprecation Timeline

### Version 2.0 (Current)

**Deprecated** (with warnings):
- `IBeepService.Containername` - Use `AppRepoName`
- `BeepService.Configure()` direct calls - Use `AddBeepServices()`
- `RegisterContainer.AddContainer()` - Use environment-specific helpers

**Still Supported** (no warnings):
- `RegisterBeep.Register()` - Works but prefer environment-specific helpers
- `AddBeepServices(Action<BeepServiceOptions>)` - Still recommended alongside fluent API

### Version 3.0 (Planned)

**Will be Removed**:
- `IBeepService.Containername` property
- `BeepService.Configure()` public method
- `RegisterContainer` class entirely
- `RegisterBeep.Register()` legacy overloads

**Migration Required**:
- All code must use `AddBeepServices()` or environment-specific helpers
- Replace all `Containername` with `AppRepoName`

---

## FAQ

### Q: Do I need to change my code immediately?

**A**: No. The old patterns still work in version 2.0 but show compiler warnings. You should migrate to avoid breaking changes in version 3.0.

### Q: What's the difference between AddBeepServices() and AddBeepForDesktop()?

**A**: 
- `AddBeepServices()` - Generic registration, you specify all options
- `AddBeepForDesktop()` - Preconfigured for desktop (singleton lifetime, progress UI, design-time support)
- `AddBeepForWeb()` - Preconfigured for web (scoped lifetime, connection pooling, request isolation)
- `AddBeepForBlazorServer()` - Preconfigured for Blazor Server (scoped, SignalR, circuit handlers)

### Q: Can I still use Containername property?

**A**: Yes, but it's deprecated. It internally uses `AppRepoName`, so both properties point to the same value. Update your code to use `AppRepoName` to avoid warnings.

### Q: Will RegisterContainer.AddContainer() be removed?

**A**: Yes, in version 3.0. It's deprecated now and shows warnings. Migrate to `AddBeepForDesktop()` or `AddBeepForWeb()`.

### Q: How do I suppress obsolete warnings temporarily?

**A**: 

```csharp
#pragma warning disable CS0618 // Type or member is obsolete
var name = beepService.Containername;
#pragma warning restore CS0618
```

But it's better to migrate to `AppRepoName`.

### Q: Does the fluent API perform better?

**A**: No performance difference. It's purely for better developer experience and discoverability through IntelliSense.

### Q: Can I mix old and new patterns?

**A**: Yes, but not recommended. Choose one pattern and stick with it for consistency.

### Q: Are there any runtime breaking changes?

**A**: No. All breaking changes are compile-time (obsolete warnings). Your existing code will run fine, just with warnings.

---

## Quick Reference Table

| Old Pattern | New Pattern | Status |
|------------|------------|--------|
| `new BeepService()` + `Configure()` | `AddBeepServices()` | ⚠️ Deprecated |
| `RegisterContainer.AddContainer()` | `AddBeepForDesktop()/Web()` | ⚠️ Deprecated |
| `Containername` property | `AppRepoName` property | ⚠️ Deprecated |
| `ContainerName` option | `AppRepoName` option | ⚠️ Deprecated |
| `RegisterBeep.Register()` | `AddBeepServices()` | ✅ Supported (not preferred) |
| `AddBeepServices(Action)` | `AddBeepServices().Build()` | ✅ Both supported |

---

## Need Help?

- Review the [README.md](./README.md) for comprehensive documentation
- Check the [Examples](./Examples/) folder for code samples
- See [copilot-instructions.md](../../.github/copilot-instructions.md) for architecture details

---

**Last Updated**: 2026-02-17  
**Version**: 2.0
