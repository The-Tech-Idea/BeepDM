# Service Registration Guide

## Overview

BeepDM provides modern fluent API for service registration with environment-specific optimizations for Desktop (WinForms/WPF), Web (ASP.NET Core), and Blazor (Server/WASM) applications.

## Core Principles

### 1. Environment-Specific Registration

Always use the environment-specific helper that matches your application type:

| Environment | Method | Lifetime |
|-------------|--------|----------|
| Desktop Apps | `AddBeepForDesktop()` | Singleton |
| Web APIs | `AddBeepForWeb()` | Scoped |
| Blazor Server | `AddBeepForBlazorServer()` | Scoped + SignalR |
| Blazor WASM | `AddBeepForBlazorWasm()` | Singleton + Browser Storage |

### 2. Fluent API Pattern

Chain configuration methods for better discoverability:

```csharp
services.AddBeepForDesktop()
    .WithAppRepoName("MyApp")
    .WithDirectoryPath(baseDir)
    .WithProgressReporting()
    .WithDesignTimeSupport()
    .WithAssemblyLoading();
```

### 3. Standardized Naming

- **Use:** `AppRepoName` (preferred property name)
- **Deprecated:** `Containername` / `ContainerName` (legacy)

## Desktop Applications (WinForms/WPF)

### Quick Start (5 lines)

```csharp
var builder = Host.CreateApplicationBuilder();
builder.Services.AddBeepForDesktop(opts =>
{
    opts.AppRepoName = "MyDesktopApp";
    opts.DirectoryPath = AppContext.BaseDirectory;
});
var host = builder.Build();
var beepService = host.Services.GetRequiredService<IBeepService>();
```

### With BeepDesktopServices Abstraction

```csharp
// In Program.cs
var builder = Host.CreateApplicationBuilder();
BeepDesktopServices.RegisterServices(builder); // Uses AddBeepForDesktop internally
var host = builder.Build();
BeepDesktopServices.ConfigureServices(host);
BeepDesktopServices.StartLoading(new[] { "MyCompany" }, showWaitForm: true);

// Access DMEEditor
var editor = BeepDesktopServices.AppManager.DMEEditor;
```

### Fluent API (Advanced)

```csharp
builder.Services.AddBeepForDesktop()
    .WithAppRepoName("MyApp")
    .WithDirectoryPath(baseDir)
    .WithProgressReporting()          // Enable IProgress<PassedArgs> reporting
    .WithDesignTimeSupport()          // Visual Studio design-time services
    .WithAssemblyLoading()            // Auto-discover plugins
    .ConfigureLogging(logging =>       // Standard ILoggingBuilder
    {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    });
```

### Desktop with Progress UI

```csharp
var builder = Host.CreateApplicationBuilder();

builder.Services.AddBeepForDesktop(opts =>
{
    opts.AppRepoName = "MyApp";
    opts.DirectoryPath = AppContext.BaseDirectory;
    opts.EnableProgressReporting = true; // Enable IProgress<PassedArgs>
});

var host = builder.Build();
var beepService = host.Services.GetRequiredService<IBeepService>();

// Report progress during loading
var progress = new Progress<PassedArgs>(args =>
{
    Console.WriteLine($"Loading: {args.Messege}");
});

beepService.LoadAssemblies(progress);
```

## Web Applications (ASP.NET Core)

### Quick Start (5 lines)

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBeepForWeb(opts =>
{
    opts.AppRepoName = "MyWebApi";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
});
var app = builder.Build();
app.UseBeepForWeb(); // Middleware for connection cleanup
```

### Full Example with Middleware

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBeepForWeb(opts =>
{
    opts.AppRepoName = "ProductCatalogAPI";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
    opts.EnableAutoMapping = true;
    opts.EnableAssemblyLoading = true;
    opts.MaxConnectionPoolSize = 100;
    opts.ConnectionIdleTimeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseBeepForWeb(); // Add connection cleanup middleware
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

### Controller Usage

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IBeepService _beepService;

    public ProductsController(IBeepService beepService)
    {
        _beepService = beepService; // Scoped - new instance per request
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var ds = _beepService.DMEEditor.GetDataSource("ProductsDB");
        var products = await ds.GetEntityAsync("Products", new List<AppFilter>());
        return Ok(products);
    }
}
```

## Blazor Server Applications

### Quick Start (5 lines)

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBeepForBlazorServer(opts =>
{
    opts.AppRepoName = "MyBlazorApp";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
});
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
```

### Full Example with SignalR

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBeepForBlazorServer(opts =>
{
    opts.AppRepoName = "CustomerPortal";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
    opts.EnableAutoMapping = true;
    opts.EnableAssemblyLoading = true;
    opts.EnableSignalRSupport = true; // Default true for Blazor Server
    opts.SignalRMaxBufferSize = 64 * 1024;
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

### Component Usage

```razor
@page "/products"
@using TheTechIdea.Beep.Container.Services
@inject IBeepService BeepService

<h3>Products</h3>

@if (products == null)
{
    <p>Loading...</p>
}
else
{
    <ul>
        @foreach (var product in products)
        {
            <li>@product.ToString()</li>
        }
    </ul>
}

@code {
    private List<object> products;

    protected override async Task OnInitializedAsync()
    {
        var ds = BeepService.DMEEditor.GetDataSource("ProductsDB");
        var result = await ds.GetEntityAsync("Products", new List<AppFilter>());
        products = result.ToList();
    }
}
```

## Blazor WebAssembly Applications

### Quick Start (5 lines)

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddBeepForBlazorWasm(opts =>
{
    opts.AppRepoName = "MyBlazorWasm";
    opts.EnableBrowserStorage = true; // Use browser's IndexedDB
});
await builder.Build().RunAsync();
```

### Full Example with Browser Storage

```csharp
// Program.cs
var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient 
    { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddBeepForBlazorWasm(opts =>
{
    opts.AppRepoName = "InventoryWasm";
    opts.EnableBrowserStorage = true; // Use IndexedDB for persistence
    opts.BrowserStorageQuota = 50 * 1024 * 1024; // 50MB
    opts.EnableAutoMapping = true;
    // Note: EnableAssemblyLoading defaults to false for WASM
});

await builder.Build().RunAsync();
```

## Validation & Error Handling

### Enhanced Validation

```csharp
try
{
    builder.Services.AddBeepForDesktop(opts =>
    {
        opts.AppRepoName = ""; // Invalid!
        opts.DirectoryPath = null; // Invalid!
    });
}
catch (BeepServiceValidationException ex)
{
    // "BeepService validation failed for property 'AppRepoName': AppRepoName cannot be null or empty"
    Console.WriteLine(ex.Message);
}
```

### Validation Rules

- **AppRepoName:** Cannot be null or whitespace
- **DirectoryPath:** Must be a valid directory path
- **ConfigType:** Must be a valid enum value
- **Blazor Server:** Cannot enable both SignalR and Browser Storage
- **Blazor WASM:** Cannot enable SignalR (WASM doesn't support server-side hubs)

### State Validation

```csharp
try
{
    beepService.Configure(/* ... */); // First call - OK
    beepService.Configure(/* ... */); // Second call - throws!
}
catch (BeepServiceStateException ex)
{
    // "BeepService has already been configured. Call Dispose() before reconfiguring."
    Console.WriteLine(ex.Message);
}
```

## Migration from Legacy API

### Property Name Changes

```csharp
// OLD (Deprecated)
options.Containername = "MyApp";
options.ContainerName = "MyApp";

// NEW (Preferred)
options.AppRepoName = "MyApp";
```

### Registration Method Changes

```csharp
// OLD (Legacy)
services.AddBeepServices(opts =>
{
    opts.Containername = "MyApp";
    opts.DirectoryPath = baseDir;
});

// NEW (Desktop)
services.AddBeepForDesktop(opts =>
{
    opts.AppRepoName = "MyApp";
    opts.DirectoryPath = baseDir;
});

// NEW (Web)
services.AddBeepForWeb(opts =>
{
    opts.AppRepoName = "MyApp";
    opts.DirectoryPath = baseDir;
});
```

## Key Types & Interfaces

### Core Interfaces

- `IBeepService` - Main service interface with DMEEditor, configuration, assembly loading
- `IBeepServiceBuilder` - Fluent builder interface with 12 configuration methods
- `IDMEEditor` - Data management orchestrator

### Options Classes

- `BeepServiceOptions` - Base options (6 properties)
- `DesktopBeepOptions` - Desktop-specific (9 properties)
- `WebBeepOptions` - Web-specific (8 properties)
- `BlazorBeepOptions` - Blazor-specific (10 properties)

### Exception Types

- `BeepServiceValidationException` - Configuration validation errors
- `BeepServiceStateException` - Invalid state errors (e.g., reconfiguration)

## Troubleshooting

### Issue: "AppRepoName cannot be null or empty"
**Cause:** Missing or empty `AppRepoName` in options  
**Fix:** Set `opts.AppRepoName = "YourAppName";`

### Issue: "BeepService has already been configured"
**Cause:** Calling `Configure()` multiple times  
**Fix:** Call `Dispose()` before reconfiguring, or use DI container

### Issue: Obsolete warning for "Containername"
**Cause:** Using legacy property name  
**Fix:** Replace with `AppRepoName`

### Issue: Connection pooling not working (Web)
**Cause:** Missing `UseBeepForWeb()` middleware  
**Fix:** Add `app.UseBeepForWeb();` after building app

### Issue: SignalR validation error (Blazor Server)
**Cause:** Both SignalR and Browser Storage enabled  
**Fix:** Choose one: SignalR for Server, Browser Storage for WASM

## File Locations

- `DataManagementEngineStandard/Services/RegisterBeepinServiceCollection.cs` - Core registration API
- `DataManagementEngineStandard/Services/BeepService.cs` - Main BeepService implementation
- `DataManagementEngineStandard/Services/BeepServiceExtensions.Desktop.cs` - Desktop helpers
- `DataManagementEngineStandard/Services/BeepServiceExtensions.Web.cs` - Web API helpers
- `DataManagementEngineStandard/Services/BeepServiceExtensions.Blazor.cs` - Blazor helpers
