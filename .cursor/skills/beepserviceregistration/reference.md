# BeepService Registration Quick Reference

Quick reference for the modern fluent API with environment-specific registration helpers.

## Quick Start Examples

### Desktop (WinForms/WPF) - 5 Lines
```csharp
var builder = Host.CreateApplicationBuilder();
builder.Services.AddBeepForDesktop(opts => {
    opts.AppRepoName = "MyDesktopApp";
    opts.DirectoryPath = AppContext.BaseDirectory;
});
var host = builder.Build();
var beepService = host.Services.GetRequiredService<IBeepService>();
```

### Web API - 5 Lines
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBeepForWeb(opts => {
    opts.AppRepoName = "MyWebApi";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
});
var app = builder.Build();
app.UseBeepForWeb(); // Middleware for connection cleanup
```

### Blazor Server - 5 Lines
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBeepForBlazorServer(opts => {
    opts.AppRepoName = "MyBlazorApp";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
});
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
```

### Blazor WASM - 5 Lines
```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddBeepForBlazorWasm(opts => {
    opts.AppRepoName = "MyBlazorWasm";
    opts.EnableBrowserStorage = true;
});
await builder.Build().RunAsync();
```

## Fluent API Patterns

### Desktop - Basic Fluent
```csharp
builder.Services.AddBeepForDesktop()
    .WithAppRepoName("MyApp")
    .WithDirectoryPath(baseDir)
    .WithAssemblyLoading()
    .WithAutoMapping();
```

### Desktop - Advanced with Progress
```csharp
builder.Services.AddBeepForDesktop()
    .WithAppRepoName("MyApp")
    .WithDirectoryPath(baseDir)
    .WithProgressReporting()
    .WithDesignTimeSupport()
    .WithAssemblyLoading()
    .ConfigureLogging(logging => {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    });
```

### Web - With Connection Pooling
```csharp
builder.Services.AddBeepForWeb(opts => {
    opts.AppRepoName = "ProductAPI";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
    opts.MaxConnectionPoolSize = 100;
    opts.ConnectionIdleTimeout = TimeSpan.FromMinutes(5);
    opts.EnableAutoMapping = true;
});

app.UseBeepForWeb(); // Required for connection cleanup
```

### Blazor Server - With SignalR
```csharp
builder.Services.AddBeepForBlazorServer(opts => {
    opts.AppRepoName = "Dashboard";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
    opts.EnableSignalRSupport = true;
    opts.SignalRMaxBufferSize = 128 * 1024;
});
```

### Blazor WASM - With Browser Storage
```csharp
builder.Services.AddBeepForBlazorWasm(opts => {
    opts.AppRepoName = "OfflineApp";
    opts.EnableBrowserStorage = true;
    opts.BrowserStorageQuota = 100 * 1024 * 1024; // 100MB
});
```

## BeepDesktopServices Pattern

### Standard Registration
```csharp
// Program.cs
var builder = Host.CreateApplicationBuilder();
BeepDesktopServices.RegisterServices(builder);
var host = builder.Build();
BeepDesktopServices.ConfigureServices(host);

// With loading screen
BeepDesktopServices.StartLoading(
    new[] { "MyCompany", "TheTechIdea" }, 
    showWaitForm: true
);

// Access DMEEditor
var editor = BeepDesktopServices.AppManager.DMEEditor;
```

### Custom Options
```csharp
var builder = Host.CreateApplicationBuilder();
BeepDesktopServices.RegisterServices(builder, options => {
    options.ContainerName = "CustomApp";
    options.DirectoryPath = "C:\\MyApp\\Beep";
    options.ShowWaitForm = true;
    options.EnableAssemblyLoading = true;
});
```

## Usage in Different Application Types

### WinForms - Full Example
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.Desktop.Common;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Create host
        var builder = Host.CreateApplicationBuilder();
        
        // Register BeepServices
        builder.Services.AddBeepForDesktop(opts => {
            opts.AppRepoName = "MyWinFormsApp";
            opts.DirectoryPath = AppContext.BaseDirectory;
            opts.EnableProgressReporting = true;
            opts.EnableDesignTimeSupport = true;
            opts.EnableAssemblyLoading = true;
        });
        
        // Build host
        var host = builder.Build();
        var beepService = host.Services.GetRequiredService<IBeepService>();
        
        // Load assemblies with progress
        var progress = new Progress<PassedArgs>(args => {
            // Show in splash screen or status bar
            Console.WriteLine($"{args.Messege}");
        });
        beepService.LoadAssemblies(progress);
        
        // Start main form
        Application.Run(new MainForm(beepService));
    }
}

// MainForm.cs
public partial class MainForm : Form
{
    private readonly IBeepService _beepService;
    
    public MainForm(IBeepService beepService)
    {
        _beepService = beepService;
        InitializeComponent();
    }
    
    private async void LoadData()
    {
        var ds = _beepService.DMEEditor.GetDataSource("MyDB");
        var data = await ds.GetEntityAsync("Products", new List<AppFilter>());
        // Bind to UI...
    }
}
```

### ASP.NET Core Web API - Full Example
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register BeepServices
builder.Services.AddBeepForWeb(opts => {
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

app.UseBeepForWeb(); // Connection cleanup middleware
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

// Controllers/ProductsController.cs
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IBeepService _beepService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IBeepService beepService, 
        ILogger<ProductsController> logger)
    {
        _beepService = beepService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        try
        {
            var ds = _beepService.DMEEditor.GetDataSource("ProductsDB");
            var products = await ds.GetEntityAsync("Products", new List<AppFilter>());
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var ds = _beepService.DMEEditor.GetDataSource("ProductsDB");
        var filters = new List<AppFilter> {
            new AppFilter { FieldName = "Id", Operator = "=", FilterValue = id.ToString() }
        };
        var products = await ds.GetEntityAsync("Products", filters);
        var product = products.FirstOrDefault();
        
        return product != null ? Ok(product) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] Product product)
    {
        var ds = _beepService.DMEEditor.GetDataSource("ProductsDB");
        var result = ds.InsertEntity("Products", product);
        
        return result.Flag == Errors.Ok 
            ? CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product)
            : BadRequest(new { error = result.Message });
    }
}
```

### Blazor Server - Full Example
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register BeepServices for Blazor Server
builder.Services.AddBeepForBlazorServer(opts => {
    opts.AppRepoName = "CustomerPortal";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
    opts.EnableAutoMapping = true;
    opts.EnableAssemblyLoading = true;
    opts.EnableSignalRSupport = true;
    opts.SignalRMaxBufferSize = 64 * 1024;
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Pages/Products.razor
@page "/products"
@using TheTechIdea.Beep.Container.Services
@using TheTechIdea.Beep.DataBase
@inject IBeepService BeepService
@inject ILogger<Products> Logger

<PageTitle>Products</PageTitle>

<h3>Product Catalog</h3>

@if (loading)
{
    <p><em>Loading products...</em></p>
}
else if (error != null)
{
    <div class="alert alert-danger">@error</div>
}
else
{
    <table class="table">
        <thead>
            <tr>
                <th>Name</th>
                <th>Price</th>
                <th>Stock</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var product in products)
            {
                <tr>
                    <td>@product.Name</td>
                    <td>@product.Price.ToString("C")</td>
                    <td>@product.Stock</td>
                </tr>
            }
        </tbody>
    </table>
}

@code {
    private List<Product> products = new();
    private bool loading = true;
    private string? error;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var ds = BeepService.DMEEditor.GetDataSource("ProductsDB");
            var result = await ds.GetEntityAsync("Products", new List<AppFilter>());
            
            products = result.Select(p => 
                JsonSerializer.Deserialize<Product>(JsonSerializer.Serialize(p)))
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading products");
            error = ex.Message;
        }
        finally
        {
            loading = false;
        }
    }

    private class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }
}
```

### Blazor WASM - Full Example
```csharp
// Program.cs
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TheTechIdea.Beep.Container.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => 
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register BeepServices for WASM
builder.Services.AddBeepForBlazorWasm(opts => {
    opts.AppRepoName = "InventoryWasm";
    opts.EnableBrowserStorage = true;
    opts.BrowserStorageQuota = 50 * 1024 * 1024; // 50MB
    opts.EnableAutoMapping = true;
    // EnableAssemblyLoading defaults to false for WASM
});

await builder.Build().RunAsync();

// Components/Inventory.razor
@page "/inventory"
@using TheTechIdea.Beep.Container.Services
@inject IBeepService BeepService
@inject IJSRuntime JS

<h3>Inventory (Offline-Capable)</h3>

@if (items == null)
{
    <p>Loading from browser storage...</p>
}
else
{
    <button @onclick="SyncWithServer">Sync with Server</button>
    
    <ul>
        @foreach (var item in items)
        {
            <li>@item.Name - @item.Quantity units</li>
        }
    </ul>
}

@code {
    private List<InventoryItem> items;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Data is persisted in browser's IndexedDB
            var ds = BeepService.DMEEditor.GetDataSource("LocalInventory");
            var result = await ds.GetEntityAsync("Items", new List<AppFilter>());
            items = result.Cast<InventoryItem>().ToList();
        }
        catch
        {
            items = new List<InventoryItem>();
        }
    }

    private async Task SyncWithServer()
    {
        // Sync logic with Web API
        await JS.InvokeVoidAsync("alert", "Syncing with server...");
    }

    private class InventoryItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
    }
}
```

## Property Migration

### Old (Deprecated)
```csharp
// ❌ Generates obsolete warnings
options.Containername = "MyApp";
options.ContainerName = "MyApp";
var name = beepService.Containername; // Obsolete
```

### New (Preferred)
```csharp
// ✅ Modern property name
options.AppRepoName = "MyApp";
var name = beepService.AppRepoName;
```

## Method Migration

### Old Registration
```csharp
// ❌ Generic registration (no environment optimization)
services.AddBeepServices(opts => {
    opts.Containername = "MyApp";
    opts.DirectoryPath = baseDir;
    opts.ServiceLifetime = ServiceLifetime.Singleton; // Manual
});
```

### New Registration
```csharp
// ✅ Desktop (Singleton preset)
services.AddBeepForDesktop(opts => {
    opts.AppRepoName = "MyApp";
    opts.DirectoryPath = baseDir;
    // Lifetime = Singleton automatically
});

// ✅ Web (Scoped preset)
services.AddBeepForWeb(opts => {
    opts.AppRepoName = "MyApp";
    opts.DirectoryPath = baseDir;
    // Lifetime = Scoped automatically
});
```

## Environment-Specific Options Reference

### Base Options (All Environments)
```csharp
public class BeepServiceOptions
{
    public string AppRepoName { get; set; }           // Required - app name
    public string DirectoryPath { get; set; }         // Required - base directory
    public BeepConfigType ConfigType { get; set; }    // Configuration type
    public bool EnableAssemblyLoading { get; set; }   // Auto-load plugins
    public bool EnableAutoMapping { get; set; }       // Auto-map entities
    public ServiceLifetime ServiceLifetime { get; set; } // DI lifetime
}
```

### Desktop Options
```csharp
public class DesktopBeepOptions : BeepServiceOptions
{
    public bool EnableProgressReporting { get; set; }  // IProgress<PassedArgs>
    public bool EnableDesignTimeSupport { get; set; }  // Visual Studio integration
    public bool EnableWindowsFormsSupport { get; set; } // WinForms features
}
```

### Web Options
```csharp
public class WebBeepOptions : BeepServiceOptions
{
    public int MaxConnectionPoolSize { get; set; }     // Connection pool limit
    public TimeSpan ConnectionIdleTimeout { get; set; } // Idle cleanup timeout
}
```

### Blazor Options
```csharp
public class BlazorBeepOptions : BeepServiceOptions
{
    public BlazorHostingModel HostingModel { get; set; } // Server or WASM
    public bool EnableSignalRSupport { get; set; }      // SignalR (Server only)
    public int SignalRMaxBufferSize { get; set; }       // SignalR buffer
    public bool EnableBrowserStorage { get; set; }      // IndexedDB (WASM)
    public long BrowserStorageQuota { get; set; }       // Storage quota
}
```

## Exception Handling

### Validation Exceptions
```csharp
try
{
    builder.Services.AddBeepForDesktop(opts => {
        opts.AppRepoName = ""; // Invalid!
    });
}
catch (BeepServiceValidationException ex)
{
    // ex.PropertyName = "AppRepoName"
    // ex.Message = "BeepService validation failed for property 'AppRepoName': AppRepoName cannot be null or empty"
    Console.WriteLine($"Validation error in {ex.PropertyName}: {ex.Message}");
}
```

### State Exceptions
```csharp
try
{
    beepService.Configure(/* ... */); // First call - OK
    beepService.Configure(/* ... */); // Second call - Error!
}
catch (BeepServiceStateException ex)
{
    // ex.Message = "BeepService has already been configured..."
    Console.WriteLine($"State error: {ex.Message}");
    
    // Fix: Dispose and reconfigure
    beepService.Dispose();
    beepService.Configure(/* ... */); // Now OK
}
```

## Key Namespaces

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.ConfigUtil;
```

## Service Lifetime Matrix

| Environment | Method | Lifetime | DI Registration |
|-------------|--------|----------|-----------------|
| Desktop WinForms | `AddBeepForDesktop()` | Singleton | `services.AddSingleton<IBeepService>()` |
| Desktop WPF | `AddBeepForDesktop()` | Singleton | `services.AddSingleton<IBeepService>()` |
| Web API | `AddBeepForWeb()` | Scoped | `services.AddScoped<IBeepService>()` |
| Blazor Server | `AddBeepForBlazorServer()` | Scoped | `services.AddScoped<IBeepService>()` |
| Blazor WASM | `AddBeepForBlazorWasm()` | Singleton | `services.AddSingleton<IBeepService>()` |

## Common Tasks

### Task: Add Connection at Runtime
```csharp
var connProps = new ConnectionProperties {
    ConnectionName = "NewDB",
    ConnectionString = "Data Source=./data.db",
    DatabaseType = DataSourceType.SqlLite,
    DriverName = "SQLite"
};

beepService.DMEEditor.ConfigEditor.AddDataConnection(connProps);
var ds = beepService.DMEEditor.GetDataSource("NewDB");
```

### Task: Load Assemblies with Progress
```csharp
var progress = new Progress<PassedArgs>(args =>
{
    Console.WriteLine($"[{args.EventType}] {args.Messege}");
    if (args.ParameterInt1 > 0) // Progress percentage
    {
        Console.WriteLine($"Progress: {args.ParameterInt1}%");
    }
});

beepService.LoadAssemblies(progress);
```

### Task: Query Data
```csharp
var ds = beepService.DMEEditor.GetDataSource("MyDB");
var filters = new List<AppFilter> {
    new AppFilter { 
        FieldName = "Status", 
        Operator = "=", 
        FilterValue = "Active" 
    }
};
var results = await ds.GetEntityAsync("Customers", filters);
```

### Task: Use UnitOfWork
```csharp
var uow = beepService.DMEEditor.CreateUnitOfWork<Customer>();

// Add new
uow.AddNew(new Customer { Name = "John Doe", Email = "john@example.com" });

// Update existing
var existing = uow.Get(c => c.Id == 123);
existing.Email = "newemail@example.com";
uow.Modify(existing);

// Delete
var toDelete = uow.Get(c => c.Id == 456);
uow.Delete(toDelete);

// Commit all changes
uow.Commit();
```

### Task: Handle Connection Errors
```csharp
var ds = beepService.DMEEditor.GetDataSource("MyDB");

try
{
    var state = ds.Openconnection();
    if (state != ConnectionState.Open)
    {
        var error = ds.ErrorObject;
        _logger.LogError($"Connection failed: {error.Message}");
        // Handle error...
    }
}
catch (Exception ex)
{
    _logger.LogError(ex, "Connection exception");
}
```

## Version Information

- **Enhanced API Version:** 2.0
- **Release Date:** 2026-02-17
- **.NET Version:** 8.0+
- **BeepDM Version:** 2.0+

## Related Documentation

- Main Guide: `DataManagementEngineStandard/Services/README.md`
- Migration Guide: `DataManagementEngineStandard/Services/MIGRATION.md`
- Implementation: `DataManagementEngineStandard/Services/IMPLEMENTATION_SUMMARY.md`
- Examples: `DataManagementEngineStandard/Services/Examples/`
