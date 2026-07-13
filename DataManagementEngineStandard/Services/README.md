# BeepService Registration & Configuration

Modern, user-friendly service registration for BeepDM with comprehensive support for desktop, web, and Blazor applications. This guide covers the enhanced fluent API, environment-specific helpers, and best practices.

## Table of Contents

- [Quick Start](#quick-start)
- [Fluent Builder API](#fluent-builder-api)
- [Environment-Specific Helpers](#environment-specific-helpers)
  - [Desktop Applications](#desktop-applications)
  - [Web Applications](#web-applications)
  - [Blazor Applications](#blazor-applications)
- [Configuration Options](#configuration-options)
- [Advanced Scenarios](#advanced-scenarios)
- [Migration Guide](#migration-guide)
- [Troubleshooting](#troubleshooting)

---

## Quick Start

### Desktop Application (5 Lines)

```csharp
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) => 
        services.AddBeepForDesktop(opts => opts.DirectoryPath = AppContext.BaseDirectory))
    .Build();

var beepService = host.UseBeepForDesktop();
Application.Run(new MainForm());
```

### Web API (5 Lines)

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBeepForWeb(opts => opts.DirectoryPath = Path.Combine(builder.Environment.ContentRootPath, "Beep"));

var app = builder.Build();
app.UseBeepForWeb();
app.MapControllers();
app.Run();
```

### Blazor Server (5 Lines)

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBeepForBlazorServer(opts => 
{
    opts.DirectoryPath = Path.Combine(builder.Environment.ContentRootPath, "Beep");
    opts.EnableSignalRProgress = true;
});

var app = builder.Build();
app.MapBlazorHub();
app.Run();
```

---

## Fluent Builder API

The fluent builder API provides discoverable configuration through IntelliSense with method chaining.

### Basic Usage

```csharp
services.AddBeepServices()
    .WithDirectory(AppContext.BaseDirectory)
    .WithAppRepo("MyApp")
    .WithMapping()
    .WithAssemblyLoading()
    .AsSingleton()
    .Build();
```

### Available Methods

| Method | Description | Example |
|--------|-------------|---------|
| `WithDirectory(path)` | Sets the directory for Beep data | `.WithDirectory(AppContext.BaseDirectory)` |
| `WithAppRepo(name)` | Sets the application repository name | `.WithAppRepo("MyApp")` |
| `WithConfigType(type)` | Sets the configuration type | `.WithConfigType(BeepConfigType.Application)` |
| `WithMapping(enable)` | Enables/disables auto-mapping | `.WithMapping(true)` |
| `WithAssemblyLoading(enable)` | Enables/disables assembly loading | `.WithAssemblyLoading(true)` |
| `WithTimeout(timeout)` | Sets initialization timeout | `.WithTimeout(TimeSpan.FromMinutes(5))` |
| `WithValidation(enable)` | Enables/disables validation | `.WithValidation(true)` |
| `WithProperty(key, value)` | Adds custom property | `.WithProperty("CustomKey", value)` |
| `AsSingleton()` | Registers as singleton | `.AsSingleton()` |
| `AsScoped()` | Registers as scoped | `.AsScoped()` |
| `AsTransient()` | Registers as transient | `.AsTransient()` |
| `Build()` | Builds and registers | `.Build()` |

### Traditional Action-Based Configuration

```csharp
services.AddBeepServices(options =>
{
    options.DirectoryPath = AppContext.BaseDirectory;
    options.AppRepoName = "MyApp";
    options.ConfigType = BeepConfigType.Application;
    options.ServiceLifetime = ServiceLifetime.Singleton;
    options.EnableAutoMapping = true;
    options.EnableAssemblyLoading = true;
});
```

---

## Environment-Specific Helpers

### Desktop Applications

**Design Philosophy**: Singleton lifetime, persistent connections, progress UI support.

#### Simple Registration

```csharp
services.AddBeepForDesktop(opts => 
{
    opts.DirectoryPath = AppContext.BaseDirectory;
    opts.AppRepoName = "MyDesktopApp";
    opts.EnableProgressReporting = true;
    opts.EnableDesignTimeSupport = true;
});
```

#### Fluent Builder

```csharp
services.AddBeepForDesktop()
    .InDirectory(AppContext.BaseDirectory)
    .WithAppRepo("MyDesktopApp")
    .WithProgressUI()
    .WithDesignTimeSupport()
    .WithAutoInitialize()
    .Build();
```

#### IHost Integration

```csharp
static void Main(string[] args)
{
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
            services.AddBeepForDesktop(opts => 
                opts.DirectoryPath = AppContext.BaseDirectory);
            services.AddSingleton<IAppManager, AppManager>();
        })
        .Build();

    var progress = new Progress<PassedArgs>(args => 
        Console.WriteLine(args.Messege));
    
    var beepService = host.UseBeepForDesktop(progress);
    
    Application.Run(new MainForm());
}
```

#### Desktop Options

| Property | Default | Description |
|----------|---------|-------------|
| `DirectoryPath` | `AppContext.BaseDirectory` | Directory for Beep data |
| `AppRepoName` | `"DesktopApp"` | Application repository name |
| `ConfigType` | `Application` | Configuration type |
| `EnableAutoMapping` | `true` | Auto-create mappings |
| `EnableAssemblyLoading` | `true` | Auto-load assemblies |
| `EnableProgressReporting` | `true` | Enable progress UI elements |
| `EnableDesignTimeSupport` | `true` | Enable VS designer support |
| `AutoInitializeForms` | `false` | Auto-initialize forms on startup |
| `InitializationTimeout` | `5 minutes` | Initialization timeout |

---

### Web Applications

**Design Philosophy**: Scoped lifetime, request isolation, connection pooling.

#### Simple Registration

```csharp
services.AddBeepForWeb(opts => 
{
    opts.DirectoryPath = Path.Combine(basePath, "Beep");
    opts.AppRepoName = "MyWebApi";
    opts.EnableConnectionPooling = true;
    opts.EnableRequestIsolation = true;
});
```

#### Fluent Builder

```csharp
services.AddBeepForWeb()
    .InDirectory(Path.Combine(basePath, "Beep"))
    .WithAppRepo("MyWebApi")
    .WithConnectionPooling()
    .WithRequestIsolation()
    .WithApiDiscovery()
    .Build();
```

#### Middleware Integration

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBeepForWeb(opts => 
    opts.DirectoryPath = Path.Combine(builder.Environment.ContentRootPath, "Beep"));

var app = builder.Build();
app.UseBeepForWeb(); // Adds connection cleanup middleware
app.UseRouting();
app.UseEndpoints(endpoints => endpoints.MapControllers());
app.Run();
```

#### Controller Usage

```csharp
[ApiController]
[Route("api/[controller]")]
public class DataSourceController : ControllerBase
{
    private readonly IBeepService _beepService;
    
    public DataSourceController(IBeepService beepService)
    {
        _beepService = beepService; // Scoped - different per request
    }
    
    [HttpGet("entities/{connectionName}")]
    public async Task<IActionResult> GetEntities(string connectionName)
    {
        var ds = _beepService.DMEEditor.GetDataSource(connectionName);
        var entities = await ds.GetEntitiesAsync();
        return Ok(entities);
    }
}
```

#### Web Options

| Property | Default | Description |
|----------|---------|-------------|
| `DirectoryPath` | `BaseDirectory/Beep` | Directory for Beep data |
| `AppRepoName` | `"WebApp"` | Application repository name |
| `ConfigType` | `Application` | Configuration type |
| `EnableAutoMapping` | `true` | Auto-create mappings |
| `EnableAssemblyLoading` | `true` | Auto-load assemblies |
| `EnableRequestIsolation` | `true` | Enable per-request isolation |
| `EnableConnectionPooling` | `true` | Enable connection pooling |
| `EnableApiDiscovery` | `false` | Enable API endpoint discovery |
| `InitializationTimeout` | `5 minutes` | Initialization timeout |

---

### Blazor Applications

**Design Philosophy**: Server=Scoped + SignalR, WASM=Singleton + Browser Storage.

#### Blazor Server

```csharp
builder.Services.AddBeepForBlazorServer(opts => 
{
    opts.DirectoryPath = Path.Combine(basePath, "Beep");
    opts.AppRepoName = "MyBlazorApp";
    opts.EnableSignalRProgress = true;
    opts.EnableCircuitHandlers = true;
});
```

**Fluent Builder**:

```csharp
builder.Services.AddBeepForBlazorServer()
    .InDirectory(basePath)
    .WithAppRepo("MyBlazorApp")
    .WithSignalR()
    .WithCircuitHandlers()
    .Build();
```

#### Blazor WebAssembly

```csharp
builder.Services.AddBeepForBlazorWasm(opts => 
{
    opts.DirectoryPath = "Beep"; // Browser path
    opts.AppRepoName = "MyBlazorWasmApp";
    opts.UseBrowserStorage = true;
});
```

**Fluent Builder**:

```csharp
builder.Services.AddBeepForBlazorWasm()
    .InDirectory("Beep")
    .WithAppRepo("MyBlazorWasmApp")
    .WithBrowserStorage()
    .Build();
```

#### Component Usage

```razor
@inject IBeepService BeepService

@code {
    private List<EntityStructure> entities;
    
    protected override async Task OnInitializedAsync()
    {
        var ds = await BeepService.DMEEditor.GetDataSource("mydb");
        entities = await ds.GetEntitiesAsync();
    }
}
```

#### Blazor Options

| Property | Default | Description |
|----------|---------|-------------|
| `DirectoryPath` | `BaseDirectory/Beep` | Directory for Beep data |
| `AppRepoName` | `"BlazorApp"` | Application repository name |
| `ConfigType` | `Application` | Configuration type |
| `EnableAutoMapping` | `true` | Auto-create mappings |
| `EnableAssemblyLoading` | `true` | Auto-load assemblies |
| `EnableSignalRProgress` | `false` | Enable SignalR progress (Server only) |
| `UseBrowserStorage` | `false` | Use browser storage (WASM only) |
| `EnableCircuitHandlers` | `false` | Enable circuit handlers (Server only) |
| `ServiceLifetime` | `Scoped` | Service lifetime (Server=Scoped, WASM=Singleton) |

---

## Configuration Options

### BeepServiceOptions (Base Class)

```csharp
public class BeepServiceOptions
{
    public string DirectoryPath { get; set; } = "";
    public string AppRepoName { get; set; } = "DefaultContainer";
    public BeepConfigType ConfigType { get; set; } = BeepConfigType.Application;
    public ServiceLifetime ServiceLifetime { get; set; } = ServiceLifetime.Singleton;
    public bool EnableAutoMapping { get; set; } = true;
    public bool EnableAssemblyLoading { get; set; } = true;
    public TimeSpan InitializationTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnableConfigurationValidation { get; set; } = true;
    public Dictionary<string, object> AdditionalProperties { get; set; } = new();
}
```

### Service Lifetime Guidelines

| Application Type | Recommended Lifetime | Rationale |
|-----------------|---------------------|-----------|
| **Desktop** | `Singleton` | Persistent connections across forms |
| **Web API** | `Scoped` | Request isolation, thread safety |
| **Blazor Server** | `Scoped` | Per-circuit isolation |
| **Blazor WASM** | `Singleton` | Single-user browser instance |
| **Console** | `Singleton` | Single execution context |

---

## Advanced Scenarios

### Custom Configuration Properties

```csharp
services.AddBeepServices()
    .WithDirectory(path)
    .WithProperty("CustomCacheSize", 1000)
    .WithProperty("EnableDebugLogging", true)
    .Build();

// Access later
var customValue = beepService.DMEEditor
    .ConfigEditor
    .AdditionalProperties["CustomCacheSize"];
```

### Manual Assembly Loading with Progress

```csharp
var beepService = services.BuildServiceProvider()
    .GetRequiredService<IBeepService>();

var progress = new Progress<PassedArgs>(args =>
{
    Console.WriteLine($"{args.Messege} - {args.ParameterInt1}% complete");
});

await beepService.LoadAssembliesAsync(progress);
```

### Validation and Error Handling

```csharp
try
{
    services.AddBeepServices(options =>
    {
        options.DirectoryPath = invalidPath;
        options.AppRepoName = "";
    });
}
catch (BeepServiceValidationException ex)
{
    Console.WriteLine($"Validation failed for {ex.PropertyName}: {ex.Message}");
}
```

---

## Migration Guide

### From Legacy Direct Instantiation

**Before**:
```csharp
var beepService = new BeepService();
beepService.Configure(directoryPath, containerName, configType, true);
```

**After**:
```csharp
services.AddBeepServices()
    .WithDirectory(directoryPath)
    .WithAppRepo(containerName)  // Note: containerName → AppRepoName
    .WithConfigType(configType)
    .AsSingleton()
    .Build();
```

### From RegisterContainer.AddContainer()

**Before**:
```csharp
RegisterContainer.AddContainer(services, "MyApp", configType);
```

**After**:
```csharp
services.AddBeepServices(opts => 
{
    opts.AppRepoName = "MyApp";
    opts.ConfigType = configType;
});
```

### Property Name Changes

| Old Property | New Property | Notes |
|-------------|-------------|-------|
| `Containername` | `AppRepoName` | `Containername` marked obsolete, use `AppRepoName` |
| `ContainerName` | `AppRepoName` | Desktop options now use `AppRepoName` |

### Breaking Changes

1. ✂️ **Removed**: Static caching mechanism in RegisterBeepServicesInternal
2. ⚠️ **Deprecated**: `IBeepService.Containername` (use `AppRepoName`)
3. ⚠️ **Deprecated**: `BeepService.Configure()` direct calls
4. ✅ **Changed**: `AddBeepServices()` now has fluent overload returning `IBeepServiceBuilder`

---

## Troubleshooting

### Common Issues

#### "DirectoryPath cannot be null or empty"

**Cause**: DirectoryPath not set or empty string.  
**Solution**: Always specify a valid directory path.

```csharp
services.AddBeepForDesktop(opts => 
    opts.DirectoryPath = AppContext.BaseDirectory); // ✅ Correct
```

#### "Beep services have not been registered"

**Cause**: Trying to resolve `IBeepService` before registration.  
**Solution**: Call `AddBeepServices()` or environment-specific helper before building service provider.

```csharp
services.AddBeepForDesktop(); // Must come first
var provider = services.BuildServiceProvider();
var beepService = provider.GetRequiredService<IBeepService>();
```

#### "Multiple concurrent registrations are not supported"

**Cause**: Calling `AddBeepServices()` multiple times simultaneously.  
**Solution**: Register BeepService once during startup. For multi-tenant scenarios, use keyed services (planned feature).

#### SignalR Error in Blazor WASM

**Cause**: Attempting to use `WithSignalR()` in Blazor WebAssembly.  
**Solution**: Use `AddBeepForBlazorServer()` for SignalR support or remove SignalR option for WASM.

```csharp
// ✅ Blazor Server - SignalR supported
builder.Services.AddBeepForBlazorServer()
    .WithSignalR()
    .Build();

// ❌ Blazor WASM - SignalR not supported
builder.Services.AddBeepForBlazorWasm()
    .WithSignalR(); // Throws InvalidOperationException
```

---

## Add-in & View Model Discovery (`BeepServiceAddinDiscoveryExtensions`)

Reflection-based discovery of `IDM_Addin` views and `IBeepViewModel` view models lives in
**`BeepServiceExtensions.AddinDiscovery.cs`**. These extensions are cross-platform
(no Windows / WinForms dependency), so any host — Desktop, Web API, Blazor, or Console on
any OS — can use them.

### Standalone usage

```csharp
using TheTechIdea.Beep.Services;

// Scan every relevant loaded assembly, register each concrete IBeepViewModel as a
// keyed transient (keyed on the type name).
services.AddBeepViewModels();

// Scan and register each [AddinAttribute]-decorated IDM_Addin. Lifetime follows
// AddinAttribute.ScopeCreateType (Single => singleton, otherwise transient). Each is also
// keyed on the type name against IDM_Addin.
services.AddBeepViews();

// Optional: constrain the scan, and route per-type failures to a logger.
services.AddBeepViewModels(assembliesToScan: new[] { typeof(MyVm).Assembly },
                           logger: hostLogger);
```

Single-item registration helpers are also available for imperative registration:

```csharp
services.AddBeepView(existingAddin);
services.AddBeepViewModel(existingViewModel);
```

### Fluent builder usage

The discovery hooks are wired into the fluent builder. `Build()` loads plugin assemblies
**first** (via `IBeepService.LoadAssemblies()` → `LLoader.LoadAllAssembly()`), then runs the
reflection scan against `LLoader.LoadedAssemblies`. This means folder-loaded plugin add-ins
(`Addin` / `DataSources` / `ConnectionDriver`) are discoverable, not just statically-referenced ones.

```csharp
services.AddBeepServices()
    .WithDirectory(AppContext.BaseDirectory)
    .WithAppRepo("MyApp")
    .WithMapping()
    .WithAssemblyLoading()              // Build() loads plugin DLLs before discovery (default: on)
    .WithViewDiscovery()                // then scans LLoader.LoadedAssemblies (default: on)
    .WithAssembliesToScan(new[] { asm })// optional; overrides the LLoader/AppDomain scan with an explicit list
    .AsSingleton()
    .Build();
```

The load step is idempotent — `IBeepService.LoadAssemblies` is guarded by an internal flag,
so calling it again later (e.g. from a desktop `StartLoading` flow) is a safe no-op.

### Explicit load-first call order

If you prefer to drive the order imperatively, use the `Action<BeepServiceOptions>` overload
(which returns the `IBeepService`) and pass its `LLoader` to the discovery extensions:

```csharp
var beep = services.AddBeepServices(opts =>
{
    opts.DirectoryPath = AppContext.BaseDirectory;
    opts.AppRepoName   = "MyApp";
});

beep.LoadAssemblies();                  // load plugin DLLs NOW
services.AddBeepViewModels(beep.LLoader); // scan what LLoader loaded
services.AddBeepViews(beep.LLoader);
```

### Discovery events

Two events previously declared (but never raised) on the WinForms-only `BeepDesktopServices`
have moved here and now actually fire:

```csharp
BeepServiceAddinDiscoveryExtensions.OnRegisterViewModels += services => { /* register extra VMs */ };
BeepServiceAddinDiscoveryExtensions.OnRegisterAddins     += services => { /* register extra add-ins */ };
```

They fire at the end of `AddBeepViewModels` / `AddBeepViews` respectively.

---

## Data Source Drivers (`BeepServiceDataSourceExtensions`)

Each `IDataSource` implementation project (under `BeepDataSources/DataSourcesPluginsCore/<Name>DataSourceCore/`)
ships a tiny fluent extension that registers its driver config. The method **only appears in IntelliSense
when the developer has referenced that datasource's project / NuGet** — a true opt-in surface.

### Usage

```csharp
var beep = services.AddBeepServices()
    .WithDirectory(AppContext.BaseDirectory)
    .WithAppRepo("MyApp")
    .Build();

// Register only the drivers your app needs. Each is idempotent (deduped by classHandler).
beep.AddOracleDatabase();
beep.AddSqliteDatabase();
beep.AddMySqlDatabase();
beep.AddPostgreDatabase();
```

### How it works

- The base helper `AddDataSourceDriver(config)` lives in
  **`BeepServiceExtensions.DataSources.cs`** (this project). It appends the
  `ConnectionDriversConfig` to `ConfigEditor.DataDriversClasses`, deduped by
  `classHandler` (case-insensitive — the same field `DMEEditor.CreateNewDataSourceConnection`
  uses to resolve the `IDataSource` class at runtime).
- Each per-datasource extension file (e.g. `OracleDataSourceServiceExtensions.cs`)
  is a one-liner that calls `ConnectionHelper.CreateOracleConfig()` and forwards
  to the base helper.
- The `classHandler` field on each config **is** the `IDataSource` implementation
  class name (e.g. `"OracleDataSource"`), so deduping on it keeps the config list
  consistent with runtime driver lookup.

### Available RDBMS drivers (Slice 1)

| Fluent method | Project folder | `classHandler` |
|---|---|---|
| `AddOracleDatabase` | `OracleDataSourceCore` | `OracleDataSource` |
| `AddSqlServerDatabase` | `SQlServerDataSourceCore` | `SQLServerDataSource` |
| `AddSqliteDatabase` | `SqliteDatasourceCore` | `SQLiteDataSource` |
| `AddMySqlDatabase` | `MySqlDataSourceCore` | `MySQLDataSource` |
| `AddPostgreDatabase` | `PostgreDataSourceCore` | `PostgreDataSource` |
| `AddFirebirdDatabase` | `FirebirdDataSourceCore` | `FireBirdDataSource` |
| `AddSnowFlakeDatabase` | `SnowFlakeDataSource` | `SnowFlakeDataSource` |
| `AddHanaDatabase` | `HanaDataSource` | `HanaDataSource` |
| `AddSpannerDatabase` | `SpannerDataSourceCore` | `SpannerDataSource` |
| `AddCockroachDatabase` | `CockroachDBDataSourceCore` | `CockroachDBDataSource` |
| `AddTimeScaleDatabase` | `TimeScaleDBDataSource` | `TimeScaleDBDataSource` |
| `AddTerraDataDatabase` | `TerraDataDataSource` | `TerraDataDataSource` |
| `AddVerticaDatabase` | `VerticaDataSource` | `VerticaDataSource` |
| `AddSqlCompactDatabase` | `SqlCompactDatasourceCore` | `SQLCompactDataSource` |

> **Note:** `DuckDB`, `DB2`, `VistaDB`, `AWSRDS`, `AzureSQL` have `Create*Config()` entries in
> `ConnectionHelper` but no corresponding `IDataSource` implementation project yet, so they have
> no fluent method. Add the project first, then drop in the template file.

### Adding a new datasource to the fluent surface

1. Create `<Name>DataSourceServiceExtensions.cs` in the datasource's project folder.
2. Use this template (substitute `<Name>`, the namespace suffix, and the `Create*Config` method):

```csharp
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataBase.<Name>
{
    public static class <Name>DataSourceServiceExtensions
    {
        public static IBeepService Add<Name>Database(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.Create<Name>Config);
    }
}
```

3. The project must reference `TheTechIdea.Beep.DataManagementEngine` (already the case for all
   `DataSourcesPluginsCore` projects).

### Build note

Each datasource project references the engine via NuGet (`TheTechIdea.Beep.DataManagementEngine`).
After adding `AddDataSourceDriver` to the engine, **publish a new engine NuGet** (or switch the
datasource projects to a `ProjectReference`) so they can resolve the new method.

---

## Key Files

- **`BeepService.cs`**: Core BeepService implementation
- **`RegisterBeepinServiceCollection.cs`**: Core registration API and fluent builder
- **`BeepServiceExtensions.AddinDiscovery.cs`**: Cross-platform add-in / view-model discovery
- **`BeepServiceExtensions.DataSources.cs`**: Base helper for fluent data-source driver registration
- **`BeepServiceExtensions.Desktop.cs`**: Desktop-specific helpers
- **`BeepServiceExtensions.Web.cs`**: Web-specific helpers
- **`BeepServiceExtensions.Blazor.cs`**: Blazor-specific helpers
- **`EnvironmentService.cs`**: Environment configuration and folder management

---

## Additional Resources

- [BeepDM Main Documentation](../../README.md)
- [Integration Examples](./Examples/)
- [API Reference](../../Docs/)

---

**Last Updated**: 2026-02-17  
**Version**: 2.0 (Enhanced Fluent API)
