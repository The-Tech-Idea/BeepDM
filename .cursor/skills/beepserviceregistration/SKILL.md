---
name: beepserviceregistration
description: Modern fluent API for BeepService registration with environment-specific optimizations for Desktop, Web, and Blazor applications.
---

# BeepService Registration (Enhanced API)

Use this skill when registering BeepDM services using the modern fluent API with environment-specific helpers for Desktop (WinForms/WPF), Web (ASP.NET Core), and Blazor (Server/WASM) applications.

## Scope
- Fluent builder API with method chaining (`IBeepServiceBuilder`)
- Desktop-optimized registration (`AddBeepForDesktop`)
- Web-optimized registration (`AddBeepForWeb`)
- Blazor Server/WASM registration (`AddBeepForBlazorServer`, `AddBeepForBlazorWasm`)
- Enhanced validation with descriptive error messages
- Standardized property naming (`AppRepoName` replaces `Containername`)

## Core Principles

### 1. Environment-Specific Registration
Always use the environment-specific helper that matches your application type:
- **Desktop Apps** → `AddBeepForDesktop()` (Singleton lifetime)
- **Web APIs** → `AddBeepForWeb()` (Scoped lifetime)
- **Blazor Server** → `AddBeepForBlazorServer()` (Scoped + SignalR)
- **Blazor WASM** → `AddBeepForBlazorWasm()` (Singleton + Browser Storage)

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
- ✅ **Use:** `AppRepoName` (preferred property name)
- ⚠️ **Deprecated:** `Containername` / `ContainerName` (legacy)

## Registration Steps

### Desktop Applications (WinForms/WPF)

#### Quick Start (5 lines)
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

#### With BeepDesktopServices Abstraction
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

#### Fluent API (Advanced)
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

### Web Applications (ASP.NET Core)

#### Quick Start (5 lines)
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

#### Full Example with Middleware
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

#### Controller Usage
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

### Blazor Server Applications

#### Quick Start (5 lines)
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBeepForBlazorServer(opts =>
{
    opts.AppRepoName = "MyBlazorApp";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
});
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
```

#### Full Example with SignalR
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

#### Component Usage
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

### Blazor WebAssembly Applications

#### Quick Start (5 lines)
```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddBeepForBlazorWasm(opts =>
{
    opts.AppRepoName = "MyBlazorWasm";
    opts.EnableBrowserStorage = true; // Use browser's IndexedDB
});
await builder.Build().RunAsync();
```

#### Full Example with Browser Storage
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
The new API provides descriptive validation errors:

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
    // Descriptive error message:
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
// ❌ OLD (Deprecated)
options.Containername = "MyApp";
options.ContainerName = "MyApp";

// ✅ NEW (Preferred)
options.AppRepoName = "MyApp";
```

### Registration Method Changes
```csharp
// ❌ OLD (Legacy)
services.AddBeepServices(opts =>
{
    opts.Containername = "MyApp";
    opts.DirectoryPath = baseDir;
});

// ✅ NEW (Desktop)
services.AddBeepForDesktop(opts =>
{
    opts.AppRepoName = "MyApp";
    opts.DirectoryPath = baseDir;
});

// ✅ NEW (Web)
services.AddBeepForWeb(opts =>
{
    opts.AppRepoName = "MyApp";
    opts.DirectoryPath = baseDir;
});
```

### BeepDesktopServices (No Changes Required)
```csharp
// Still works - uses AddBeepForDesktop internally
BeepDesktopServices.RegisterServices(builder);
```

## Common Patterns

### Pattern 1: Desktop with Progress UI
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

### Pattern 2: Web API with Connection Pooling
```csharp
builder.Services.AddBeepForWeb(opts =>
{
    opts.AppRepoName = "ProductAPI";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
    opts.MaxConnectionPoolSize = 100;
    opts.ConnectionIdleTimeout = TimeSpan.FromMinutes(5);
    opts.EnableAutoMapping = true;
});

// Middleware cleans up idle connections automatically
app.UseBeepForWeb();
```

### Pattern 3: Blazor Server with Real-Time Updates
```csharp
builder.Services.AddBeepForBlazorServer(opts =>
{
    opts.AppRepoName = "Dashboard";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
    opts.EnableSignalRSupport = true;
    opts.SignalRMaxBufferSize = 128 * 1024; // Large messages for real-time data
});
```

### Pattern 4: Blazor WASM with Browser Storage
```csharp
builder.Services.AddBeepForBlazorWasm(opts =>
{
    opts.AppRepoName = "OfflineApp";
    opts.EnableBrowserStorage = true;
    opts.BrowserStorageQuota = 100 * 1024 * 1024; // 100MB for offline data
});
```

## Pitfalls & Best Practices

### ❌ Don't: Mix Environment-Specific Methods
```csharp
// BAD: Wrong lifetime for web apps
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBeepForDesktop(opts => { /* ... */ }); // Singleton - NOT thread-safe!
```

### ✅ Do: Use Correct Method for Environment
```csharp
// GOOD: Scoped lifetime for web apps
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBeepForWeb(opts => { /* ... */ }); // Scoped - thread-safe
```

### ❌ Don't: Use Legacy Property Names
```csharp
// BAD: Deprecated property
options.Containername = "MyApp"; // Obsolete warning
```

### ✅ Do: Use Standardized Names
```csharp
// GOOD: Modern property name
options.AppRepoName = "MyApp";
```

### ❌ Don't: Enable Conflicting Features (Blazor)
```csharp
// BAD: Can't use both in Blazor Server
builder.Services.AddBeepForBlazorServer(opts =>
{
    opts.EnableSignalRSupport = true;
    opts.EnableBrowserStorage = true; // Throws validation error!
});
```

### ✅ Do: Choose Appropriate Storage
```csharp
// GOOD: SignalR for Blazor Server
builder.Services.AddBeepForBlazorServer(opts =>
{
    opts.EnableSignalRSupport = true;
});

// GOOD: Browser Storage for Blazor WASM
builder.Services.AddBeepForBlazorWasm(opts =>
{
    opts.EnableBrowserStorage = true;
});
```

### ❌ Don't: Reconfigure Without Disposing
```csharp
// BAD: Second Configure call throws
beepService.Configure(/* ... */);
beepService.Configure(/* ... */); // BeepServiceStateException!
```

### ✅ Do: Dispose Before Reconfiguring
```csharp
// GOOD: Proper lifecycle management
beepService.Configure(/* ... */);
beepService.Dispose();
beepService.Configure(/* ... */);
```

## File Locations

### Core Services
- `DataManagementEngineStandard/Services/RegisterBeepinServiceCollection.cs` - Core registration API with fluent builder
- `DataManagementEngineStandard/Services/BeepService.cs` - Main BeepService implementation
- `DataManagementEngineStandard/Services/IBeepService.cs` - BeepService interface

### Environment-Specific Extensions
- `DataManagementEngineStandard/Services/BeepServiceExtensions.Desktop.cs` - Desktop helpers (288 lines)
- `DataManagementEngineStandard/Services/BeepServiceExtensions.Web.cs` - Web API helpers (280 lines)
- `DataManagementEngineStandard/Services/BeepServiceExtensions.Blazor.cs` - Blazor Server/WASM helpers (370 lines)

### Desktop Abstraction
- `Beep.Desktop/TheTechIdea.Beep.Desktop.Common/BeepServices.cs` - BeepDesktopServices wrapper

### Documentation
- `DataManagementEngineStandard/Services/README.md` - Comprehensive guide (580 lines)
- `DataManagementEngineStandard/Services/MIGRATION.md` - Migration guide (360 lines)
- `DataManagementEngineStandard/Services/IMPLEMENTATION_SUMMARY.md` - Implementation details

### Examples
- `DataManagementEngineStandard/Services/Examples/DesktopMinimalExample.cs` - Desktop example (150 lines)
- `DataManagementEngineStandard/Services/Examples/WebApiExample.cs` - Web API example (220 lines)
- `DataManagementEngineStandard/Services/Examples/BlazorServerExample.cs` - Blazor Server example (230 lines)

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

### Extension Methods
- `AddBeepForDesktop()` - Desktop registration
- `AddBeepForWeb()` - Web registration
- `AddBeepForBlazorServer()` - Blazor Server registration
- `AddBeepForBlazorWasm()` - Blazor WASM registration
- `UseBeepForWeb()` - Web middleware
- `UseBeepForDesktop()` - Desktop IHost integration

## Quick Reference

### Fluent API Methods (IBeepServiceBuilder)
| Method | Description |
|--------|-------------|
| `WithAppRepoName(string)` | Set application repository name |
| `WithDirectoryPath(string)` | Set base directory for configs/assemblies |
| `WithConfigType(BeepConfigType)` | Set configuration type |
| `WithAssemblyLoading()` | Enable automatic plugin discovery |
| `WithAutoMapping()` | Enable entity auto-mapping |
| `ConfigureLogging(Action)` | Configure ILoggingBuilder |
| `ConfigureOptions(Action)` | Advanced options configuration |

### Desktop-Specific Methods (IDesktopBeepServiceBuilder)
| Method | Description |
|--------|-------------|
| `WithProgressReporting()` | Enable IProgress<PassedArgs> support |
| `WithDesignTimeSupport()` | Enable Visual Studio design-time services |
| `WithWindowsFormsSupport()` | Enable WinForms-specific features |

### Service Lifetimes
| Environment | Lifetime | Reason |
|-------------|----------|--------|
| Desktop | Singleton | Persistent connections across app lifetime |
| Web API | Scoped | Thread-safe per HTTP request |
| Blazor Server | Scoped | Isolated per user session + SignalR |
| Blazor WASM | Singleton | Single user in browser |

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

## Related Skills
- `beepservice` - Legacy initialization patterns
- `connection` - Connection management
- `unitofwork` - Transactional operations
- `idatasource` - Data source operations

## Version
Enhanced API introduced: 2026-02-17  
Supports: .NET 8+, BeepDM 2.0+
