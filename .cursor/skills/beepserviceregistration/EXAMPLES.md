# BeepService Registration - Complete Examples Collection

This document provides comprehensive, working examples for all BeepService registration patterns across Desktop, Web, and Blazor applications.

## Table of Contents

1. [Desktop Applications](#desktop-applications)
   - [Minimal Setup](#desktop-minimal-5-lines)
   - [With BeepDesktopServices](#desktop-with-beepdesktopservices)
   - [Advanced Fluent API](#desktop-advanced-fluent-api)
   - [Full WinForms Application](#desktop-full-winforms-application)
   - [With Progress Reporting](#desktop-with-progress-reporting)

2. [Web API Applications](#web-api-applications)
   - [Minimal Setup](#web-minimal-5-lines)
   - [With Middleware](#web-with-middleware)
   - [Full API with Controllers](#web-full-api-with-controllers)
   - [With Connection Pooling](#web-with-connection-pooling)
   - [RESTful CRUD API](#web-restful-crud-api)

3. [Blazor Server Applications](#blazor-server-applications)
   - [Minimal Setup](#blazor-server-minimal-5-lines)
   - [With SignalR](#blazor-server-with-signalr)
   - [Full Application](#blazor-server-full-application)
   - [Real-Time Dashboard](#blazor-server-real-time-dashboard)

4. [Blazor WASM Applications](#blazor-wasm-applications)
   - [Minimal Setup](#blazor-wasm-minimal-5-lines)
   - [With Browser Storage](#blazor-wasm-with-browser-storage)
   - [Offline-First App](#blazor-wasm-offline-first-app)
   - [Sync with Server](#blazor-wasm-sync-with-server)

5. [Migration Examples](#migration-examples)
   - [Legacy to Modern Desktop](#migration-legacy-to-modern-desktop)
   - [Legacy to Modern Web](#migration-legacy-to-modern-web)
   - [Property Name Updates](#migration-property-name-updates)

6. [Advanced Patterns](#advanced-patterns)
   - [Multi-Environment Configuration](#advanced-multi-environment)
   - [Custom Middleware](#advanced-custom-middleware)
   - [Health Checks Integration](#advanced-health-checks)
   - [Testing Patterns](#advanced-testing-patterns)

---

## Desktop Applications

### Desktop Minimal (5 Lines)

```csharp
// Program.cs - Absolute minimum setup
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TheTechIdea.Beep.Container.Services;

var builder = Host.CreateApplicationBuilder();
builder.Services.AddBeepForDesktop(opts => {
    opts.AppRepoName = "MyDesktopApp";
    opts.DirectoryPath = AppContext.BaseDirectory;
});
var host = builder.Build();
var beepService = host.Services.GetRequiredService<IBeepService>();

// Now use beepService...
Application.Run(new MainForm(beepService));
```

### Desktop with BeepDesktopServices

```csharp
// Program.cs - Using BeepDesktopServices abstraction
using Microsoft.Extensions.Hosting;
using System.Windows.Forms;
using TheTechIdea.Beep.Desktop.Common;

namespace MyWinFormsApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            StartApplication();
        }

        private static void StartApplication()
        {
            // 1. Create host builder
            var builder = Host.CreateApplicationBuilder();

            // 2. Register BeepServices (uses AddBeepForDesktop internally)
            BeepDesktopServices.RegisterServices(builder, options =>
            {
                options.ContainerName = "MyApp";
                options.DirectoryPath = AppContext.BaseDirectory;
                options.ShowWaitForm = true;
                options.EnableAssemblyLoading = true;
                options.EnableAutoMapping = true;
            });

            // 3. Build and configure
            var host = builder.Build();
            BeepDesktopServices.ConfigureServices(host);

            // 4. Load assemblies with wait form
            BeepDesktopServices.StartLoading(
                new[] { "MyCompany", "TheTechIdea" },
                showWaitForm: true
            );

            // 5. Access DMEEditor
            var editor = BeepDesktopServices.AppManager.DMEEditor;

            // 6. Setup routing and show main form
            BeepAppServices.RegisterRoutes();
            Application.Run(new MainForm(editor));
        }
    }
}
```

### Desktop Advanced Fluent API

```csharp
// Program.cs - Full fluent API with all features
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.Container.Services;

var builder = Host.CreateApplicationBuilder();

// Fluent configuration with all desktop features
builder.Services.AddBeepForDesktop()
    .WithAppRepoName("AdvancedDesktopApp")
    .WithDirectoryPath(Path.Combine(AppContext.BaseDirectory, "Beep"))
    .WithProgressReporting()          // IProgress<PassedArgs> support
    .WithDesignTimeSupport()          // Visual Studio integration
    .WithWindowsFormsSupport()        // WinForms-specific features
    .WithAssemblyLoading()            // Auto-discover plugins
    .WithAutoMapping()                // Entity auto-mapping
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
        logging.AddDebug();
        logging.SetMinimumLevel(LogLevel.Information);
    });

var host = builder.Build();
var beepService = host.Services.GetRequiredService<IBeepService>();

// Custom progress handler
var progress = new Progress<PassedArgs>(args =>
{
    Console.WriteLine($"[{args.EventType}] {args.Messege}");
    if (args.ParameterInt1 > 0)
    {
        Console.WriteLine($"Progress: {args.ParameterInt1}%");
    }
});

beepService.LoadAssemblies(progress);
Application.Run(new MainForm(beepService));
```

### Desktop Full WinForms Application

```csharp
// Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows.Forms;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.DataBase;

namespace ProductManagementApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var builder = Host.CreateApplicationBuilder();
            
            builder.Services.AddBeepForDesktop(opts =>
            {
                opts.AppRepoName = "ProductManager";
                opts.DirectoryPath = AppContext.BaseDirectory;
                opts.EnableProgressReporting = true;
                opts.EnableDesignTimeSupport = true;
                opts.EnableAssemblyLoading = true;
                opts.EnableAutoMapping = true;
            });

            var host = builder.Build();
            var beepService = host.Services.GetRequiredService<IBeepService>();

            // Setup database connection
            SetupDatabase(beepService);

            Application.Run(new MainForm(beepService));
        }

        private static void SetupDatabase(IBeepService beepService)
        {
            var connProps = new ConnectionProperties
            {
                ConnectionName = "ProductsDB",
                ConnectionString = "Data Source=./products.db",
                DatabaseType = DataSourceType.SqlLite,
                DriverName = "SQLite",
                Category = DatasourceCategory.RDBMS
            };

            beepService.DMEEditor.ConfigEditor.AddDataConnection(connProps);
            
            var ds = beepService.DMEEditor.GetDataSource("ProductsDB");
            var state = ds.Openconnection();
            
            if (state != ConnectionState.Open)
            {
                MessageBox.Show($"Failed to open database: {ds.ErrorObject.Message}",
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }
    }

    // MainForm.cs
    public partial class MainForm : Form
    {
        private readonly IBeepService _beepService;
        private readonly IDMEEditor _editor;

        public MainForm(IBeepService beepService)
        {
            _beepService = beepService;
            _editor = beepService.DMEEditor;
            InitializeComponent();
            LoadProducts();
        }

        private async void LoadProducts()
        {
            try
            {
                var ds = _editor.GetDataSource("ProductsDB");
                var products = await ds.GetEntityAsync("Products", new List<AppFilter>());
                
                dataGridView1.DataSource = products;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading products: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnAdd_Click(object sender, EventArgs e)
        {
            var uow = _editor.CreateUnitOfWork<Product>();
            
            uow.AddNew(new Product
            {
                Name = txtName.Text,
                Price = decimal.Parse(txtPrice.Text),
                Stock = int.Parse(txtStock.Text)
            });

            uow.Commit();
            LoadProducts(); // Refresh grid
        }
    }
}
```

### Desktop with Progress Reporting

```csharp
// Program.cs - Custom progress reporting with splash screen
using System.Windows.Forms;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.DataBase;

namespace MyApp
{
    public class SplashForm : Form
    {
        private Label lblStatus;
        private ProgressBar progressBar;

        public SplashForm()
        {
            Text = "Loading...";
            Size = new Size(400, 150);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;

            lblStatus = new Label { Dock = DockStyle.Top, Height = 60, TextAlign = ContentAlignment.MiddleCenter };
            progressBar = new ProgressBar { Dock = DockStyle.Bottom, Height = 30 };

            Controls.Add(lblStatus);
            Controls.Add(progressBar);
        }

        public void UpdateStatus(string message, int percent)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateStatus(message, percent)));
                return;
            }

            lblStatus.Text = message;
            progressBar.Value = Math.Min(percent, 100);
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();

            var splashForm = new SplashForm();
            splashForm.Show();
            Application.DoEvents();

            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddBeepForDesktop(opts =>
            {
                opts.AppRepoName = "MyApp";
                opts.DirectoryPath = AppContext.BaseDirectory;
                opts.EnableProgressReporting = true;
                opts.EnableAssemblyLoading = true;
            });

            var host = builder.Build();
            var beepService = host.Services.GetRequiredService<IBeepService>();

            // Progress reporting
            var progress = new Progress<PassedArgs>(args =>
            {
                splashForm.UpdateStatus(args.Messege ?? "Loading...", args.ParameterInt1);
            });

            beepService.LoadAssemblies(progress);

            splashForm.Close();
            Application.Run(new MainForm(beepService));
        }
    }
}
```

---

## Web API Applications

### Web Minimal (5 Lines)

```csharp
// Program.cs - Absolute minimum
using TheTechIdea.Beep.Container.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBeepForWeb(opts => {
    opts.AppRepoName = "MyWebApi";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
});
builder.Services.AddControllers();
var app = builder.Build();
app.UseBeepForWeb();
app.MapControllers();
app.Run();
```

### Web with Middleware

```csharp
// Program.cs - With connection cleanup middleware
using TheTechIdea.Beep.Container.Services;

var builder = WebApplication.CreateBuilder(args);

// Register BeepServices
builder.Services.AddBeepForWeb(opts =>
{
    opts.AppRepoName = "ProductAPI";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
    opts.MaxConnectionPoolSize = 100;
    opts.ConnectionIdleTimeout = TimeSpan.FromMinutes(5);
    opts.EnableAutoMapping = true;
    opts.EnableAssemblyLoading = true;
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

// CRITICAL: Add BeepForWeb middleware for connection cleanup
app.UseBeepForWeb();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

### Web Full API with Controllers

```csharp
// Program.cs
using Microsoft.AspNetCore.Builder;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.DataBase;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBeepForWeb(opts =>
{
    opts.AppRepoName = "CustomerAPI";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
    opts.MaxConnectionPoolSize = 100;
    opts.ConnectionIdleTimeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Setup database connection
SetupDatabase(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseBeepForWeb();
app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

void SetupDatabase(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var beepService = scope.ServiceProvider.GetRequiredService<IBeepService>();
    
    var connProps = new ConnectionProperties
    {
        ConnectionName = "CustomersDB",
        ConnectionString = "Data Source=./customers.db",
        DatabaseType = DataSourceType.SqlLite
    };
    
    beepService.DMEEditor.ConfigEditor.AddDataConnection(connProps);
}

// Controllers/CustomersController.cs
using Microsoft.AspNetCore.Mvc;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.DataBase;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly IBeepService _beepService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(IBeepService beepService, ILogger<CustomersController> logger)
    {
        _beepService = beepService; // Scoped - new instance per request
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var ds = _beepService.DMEEditor.GetDataSource("CustomersDB");
            var filters = new List<AppFilter>();
            
            var result = ds.GetEntity("Customers", filters, pageNumber, pageSize);
            
            return Ok(new
            {
                data = result.Data,
                totalRecords = result.TotalRecords,
                pageNumber = result.PageNumber,
                pageSize = result.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customers");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomer(int id)
    {
        var ds = _beepService.DMEEditor.GetDataSource("CustomersDB");
        var filters = new List<AppFilter>
        {
            new AppFilter { FieldName = "Id", Operator = "=", FilterValue = id.ToString() }
        };
        
        var customers = await ds.GetEntityAsync("Customers", filters);
        var customer = customers.FirstOrDefault();
        
        return customer != null ? Ok(customer) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] Customer customer)
    {
        var ds = _beepService.DMEEditor.GetDataSource("CustomersDB");
        var result = ds.InsertEntity("Customers", customer);
        
        if (result.Flag == Errors.Ok)
        {
            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
        }
        
        return BadRequest(new { error = result.Message });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] Customer customer)
    {
        customer.Id = id;
        
        var ds = _beepService.DMEEditor.GetDataSource("CustomersDB");
        var result = ds.UpdateEntity("Customers", customer);
        
        return result.Flag == Errors.Ok ? NoContent() : BadRequest(new { error = result.Message });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var ds = _beepService.DMEEditor.GetDataSource("CustomersDB");
        var filters = new List<AppFilter>
        {
            new AppFilter { FieldName = "Id", Operator = "=", FilterValue = id.ToString() }
        };
        
        var customers = await ds.GetEntityAsync("Customers", filters);
        var customer = customers.FirstOrDefault();
        
        if (customer == null) return NotFound();
        
        var result = ds.DeleteEntity("Customers", customer);
        return result.Flag == Errors.Ok ? NoContent() : BadRequest(new { error = result.Message });
    }
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
}
```

### Web with Connection Pooling

```csharp
// Program.cs - Optimized for high-traffic APIs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBeepForWeb(opts =>
{
    opts.AppRepoName = "HighTrafficAPI";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
    
    // Connection pooling configuration
    opts.MaxConnectionPoolSize = 200;              // Max concurrent connections
    opts.ConnectionIdleTimeout = TimeSpan.FromMinutes(10); // Cleanup idle connections
    
    opts.EnableAutoMapping = true;
    opts.EnableAssemblyLoading = true;
});

builder.Services.AddControllers();

var app = builder.Build();

// Middleware automatically manages connection pool
app.UseBeepForWeb();

app.MapControllers();
app.Run();
```

### Web RESTful CRUD API

```csharp
// Program.cs with minimal API endpoints
using Microsoft.AspNetCore.Mvc;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.DataBase;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBeepForWeb(opts =>
{
    opts.AppRepoName = "ProductsAPI";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
});

var app = builder.Build();

app.UseBeepForWeb();

// Setup connection
var connProps = new ConnectionProperties
{
    ConnectionName = "ProductsDB",
    ConnectionString = "Data Source=./products.db",
    DatabaseType = DataSourceType.SqlLite
};

using (var scope = app.Services.CreateScope())
{
    var beepService = scope.ServiceProvider.GetRequiredService<IBeepService>();
    beepService.DMEEditor.ConfigEditor.AddDataConnection(connProps);
}

// Minimal API endpoints
app.MapGet("/api/products", async (IBeepService beepService) =>
{
    var ds = beepService.DMEEditor.GetDataSource("ProductsDB");
    var products = await ds.GetEntityAsync("Products", new List<AppFilter>());
    return Results.Ok(products);
});

app.MapGet("/api/products/{id}", async (int id, IBeepService beepService) =>
{
    var ds = beepService.DMEEditor.GetDataSource("ProductsDB");
    var filters = new List<AppFilter> { new() { FieldName = "Id", Operator = "=", FilterValue = id.ToString() } };
    var products = await ds.GetEntityAsync("Products", filters);
    var product = products.FirstOrDefault();
    return product != null ? Results.Ok(product) : Results.NotFound();
});

app.MapPost("/api/products", async ([FromBody] Product product, IBeepService beepService) =>
{
    var ds = beepService.DMEEditor.GetDataSource("ProductsDB");
    var result = ds.InsertEntity("Products", product);
    return result.Flag == Errors.Ok 
        ? Results.Created($"/api/products/{product.Id}", product)
        : Results.BadRequest(result.Message);
});

app.MapPut("/api/products/{id}", async (int id, [FromBody] Product product, IBeepService beepService) =>
{
    product.Id = id;
    var ds = beepService.DMEEditor.GetDataSource("ProductsDB");
    var result = ds.UpdateEntity("Products", product);
    return result.Flag == Errors.Ok ? Results.NoContent() : Results.BadRequest(result.Message);
});

app.MapDelete("/api/products/{id}", async (int id, IBeepService beepService) =>
{
    var ds = beepService.DMEEditor.GetDataSource("ProductsDB");
    var filters = new List<AppFilter> { new() { FieldName = "Id", Operator = "=", FilterValue = id.ToString() } };
    var products = await ds.GetEntityAsync("Products", filters);
    var product = products.FirstOrDefault();
    if (product == null) return Results.NotFound();
    var result = ds.DeleteEntity("Products", product);
    return result.Flag == Errors.Ok ? Results.NoContent() : Results.BadRequest(result.Message);
});

app.Run();

record Product(int Id, string Name, decimal Price, int Stock);
```

---

## Blazor Server Applications

### Blazor Server Minimal (5 Lines)

```csharp
// Program.cs
using TheTechIdea.Beep.Container.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBeepForBlazorServer(opts => {
    opts.AppRepoName = "MyBlazorApp";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
});
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
var app = builder.Build();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
```

### Blazor Server with SignalR

```csharp
// Program.cs - Optimized for real-time updates
using TheTechIdea.Beep.Container.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBeepForBlazorServer(opts =>
{
    opts.AppRepoName = "RealtimeDashboard";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
    opts.EnableSignalRSupport = true;             // Enable SignalR
    opts.SignalRMaxBufferSize = 128 * 1024;       // 128KB for large messages
    opts.EnableAutoMapping = true;
    opts.EnableAssemblyLoading = true;
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
```

### Blazor Server Full Application

```csharp
// Program.cs
using Microsoft.AspNetCore.Components;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.DataBase;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBeepForBlazorServer(opts =>
{
    opts.AppRepoName = "CustomerPortal";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
    opts.EnableSignalRSupport = true;
    opts.EnableAutoMapping = true;
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Setup database
SetupDatabase(app.Services);

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

void SetupDatabase(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var beepService = scope.ServiceProvider.GetRequiredService<IBeepService>();
    
    var connProps = new ConnectionProperties
    {
        ConnectionName = "PortalDB",
        ConnectionString = "Data Source=./portal.db",
        DatabaseType = DataSourceType.SqlLite
    };
    
    beepService.DMEEditor.ConfigEditor.AddDataConnection(connProps);
}

// Pages/Customers.razor
@page "/customers"
@using TheTechIdea.Beep.Container.Services
@using TheTechIdea.Beep.DataBase
@using System.Text.Json
@inject IBeepService BeepService
@inject ILogger<Customers> Logger
@rendermode InteractiveServer

<PageTitle>Customers</PageTitle>

<h3>Customer Management</h3>

@if (loading)
{
    <div class="spinner-border" role="status">
        <span class="visually-hidden">Loading...</span>
    </div>
}
else if (error != null)
{
    <div class="alert alert-danger">@error</div>
}
else
{
    <button class="btn btn-primary mb-3" @onclick="ShowAddForm">Add Customer</button>

    @if (showAddForm)
    {
        <div class="card mb-3">
            <div class="card-body">
                <h5>Add New Customer</h5>
                <EditForm Model="@newCustomer" OnValidSubmit="HandleAddCustomer">
                    <div class="mb-3">
                        <label>Name:</label>
                        <InputText @bind-Value="newCustomer.Name" class="form-control" />
                    </div>
                    <div class="mb-3">
                        <label>Email:</label>
                        <InputText @bind-Value="newCustomer.Email" class="form-control" />
                    </div>
                    <div class="mb-3">
                        <label>Phone:</label>
                        <InputText @bind-Value="newCustomer.Phone" class="form-control" />
                    </div>
                    <button type="submit" class="btn btn-success">Save</button>
                    <button type="button" class="btn btn-secondary" @onclick="() => showAddForm = false">Cancel</button>
                </EditForm>
            </div>
        </div>
    }

    <table class="table table-striped">
        <thead>
            <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Phone</th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var customer in customers)
            {
                <tr>
                    <td>@customer.Name</td>
                    <td>@customer.Email</td>
                    <td>@customer.Phone</td>
                    <td>
                        <button class="btn btn-sm btn-danger" @onclick="() => DeleteCustomer(customer)">Delete</button>
                    </td>
                </tr>
            }
        </tbody>
    </table>
}

@code {
    private List<Customer> customers = new();
    private Customer newCustomer = new();
    private bool loading = true;
    private bool showAddForm = false;
    private string? error;

    protected override async Task OnInitializedAsync()
    {
        await LoadCustomers();
    }

    private async Task LoadCustomers()
    {
        loading = true;
        error = null;

        try
        {
            var ds = BeepService.DMEEditor.GetDataSource("PortalDB");
            var result = await ds.GetEntityAsync("Customers", new List<AppFilter>());
            
            customers = result.Select(c => 
                JsonSerializer.Deserialize<Customer>(JsonSerializer.Serialize(c)))
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading customers");
            error = ex.Message;
        }
        finally
        {
            loading = false;
        }
    }

    private void ShowAddForm()
    {
        showAddForm = true;
        newCustomer = new Customer();
    }

    private async Task HandleAddCustomer()
    {
        try
        {
            var ds = BeepService.DMEEditor.GetDataSource("PortalDB");
            var result = ds.InsertEntity("Customers", newCustomer);
            
            if (result.Flag == Errors.Ok)
            {
                showAddForm = false;
                await LoadCustomers();
            }
            else
            {
                error = result.Message;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding customer");
            error = ex.Message;
        }
    }

    private async Task DeleteCustomer(Customer customer)
    {
        try
        {
            var ds = BeepService.DMEEditor.GetDataSource("PortalDB");
            var result = ds.DeleteEntity("Customers", customer);
            
            if (result.Flag == Errors.Ok)
            {
                await LoadCustomers();
            }
            else
            {
                error = result.Message;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting customer");
            error = ex.Message;
        }
    }

    private class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
    }
}
```

### Blazor Server Real-Time Dashboard

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBeepForBlazorServer(opts =>
{
    opts.AppRepoName = "RealtimeDashboard";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
    opts.EnableSignalRSupport = true;
    opts.SignalRMaxBufferSize = 256 * 1024; // Large buffer for real-time data
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();

// Pages/Dashboard.razor
@page "/dashboard"
@using TheTechIdea.Beep.Container.Services
@inject IBeepService BeepService
@implements IDisposable
@rendermode InteractiveServer

<h3>Real-Time Dashboard</h3>

<div class="row">
    <div class="col-md-3">
        <div class="card">
            <div class="card-body">
                <h5>Total Orders</h5>
                <h2>@totalOrders</h2>
            </div>
        </div>
    </div>
    <div class="col-md-3">
        <div class="card">
            <div class="card-body">
                <h5>Revenue Today</h5>
                <h2>$@revenueToday.ToString("N2")</h2>
            </div>
        </div>
    </div>
</div>

@code {
    private int totalOrders;
    private decimal revenueToday;
    private Timer? refreshTimer;

    protected override void OnInitialized()
    {
        LoadDashboardData();
        
        // Refresh every 5 seconds
        refreshTimer = new Timer(async _ =>
        {
            LoadDashboardData();
            await InvokeAsync(StateHasChanged);
        }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    private async void LoadDashboardData()
    {
        var ds = BeepService.DMEEditor.GetDataSource("DashboardDB");
        
        var orders = await ds.GetEntityAsync("Orders", new List<AppFilter>());
        totalOrders = orders.Count();
        
        // Calculate revenue...
    }

    public void Dispose()
    {
        refreshTimer?.Dispose();
    }
}
```

---

## Blazor WASM Applications

### Blazor WASM Minimal (5 Lines)

```csharp
// Program.cs
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TheTechIdea.Beep.Container.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddBeepForBlazorWasm(opts => {
    opts.AppRepoName = "MyBlazorWasm";
    opts.EnableBrowserStorage = true;
});
builder.RootComponents.Add<App>("#app");
await builder.Build().RunAsync();
```

### Blazor WASM with Browser Storage

```csharp
// Program.cs - Full setup with IndexedDB
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TheTechIdea.Beep.Container.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => 
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddBeepForBlazorWasm(opts =>
{
    opts.AppRepoName = "InventoryWasm";
    opts.EnableBrowserStorage = true;              // Use browser's IndexedDB
    opts.BrowserStorageQuota = 100 * 1024 * 1024;  // 100MB quota
    opts.EnableAutoMapping = true;
    // EnableAssemblyLoading defaults to false for WASM
});

await builder.Build().RunAsync();
```

### Blazor WASM Offline-First App

```csharp
// Program.cs
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TheTechIdea.Beep.Container.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient 
    { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddBeepForBlazorWasm(opts =>
{
    opts.AppRepoName = "OfflineFirstApp";
    opts.EnableBrowserStorage = true;
    opts.BrowserStorageQuota = 50 * 1024 * 1024; // 50MB
});

await builder.Build().RunAsync();

// Pages/Inventory.razor
@page "/inventory"
@using TheTechIdea.Beep.Container.Services
@using TheTechIdea.Beep.DataBase
@inject IBeepService BeepService
@inject IJSRuntime JS

<h3>Inventory (Offline-Capable)</h3>

<p>Status: <strong>@(isOnline ? "Online" : "Offline")</strong></p>

@if (loading)
{
    <p>Loading from browser storage...</p>
}
else
{
    <button class="btn btn-primary" @onclick="SyncWithServer" disabled="@(!isOnline)">
        Sync with Server
    </button>

    <button class="btn btn-success" @onclick="ShowAddForm">Add Item</button>

    @if (showAddForm)
    {
        <div class="card mt-3">
            <div class="card-body">
                <EditForm Model="@newItem" OnValidSubmit="HandleAddItem">
                    <div class="mb-3">
                        <label>Name:</label>
                        <InputText @bind-Value="newItem.Name" class="form-control" />
                    </div>
                    <div class="mb-3">
                        <label>Quantity:</label>
                        <InputNumber @bind-Value="newItem.Quantity" class="form-control" />
                    </div>
                    <button type="submit" class="btn btn-success">Save</button>
                    <button type="button" class="btn btn-secondary" @onclick="() => showAddForm = false">Cancel</button>
                </EditForm>
            </div>
        </div>
    }

    <table class="table mt-3">
        <thead>
            <tr>
                <th>Name</th>
                <th>Quantity</th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var item in items)
            {
                <tr>
                    <td>@item.Name</td>
                    <td>@item.Quantity</td>
                    <td>
                        <button class="btn btn-sm btn-danger" @onclick="() => DeleteItem(item)">Delete</button>
                    </td>
                </tr>
            }
        </tbody>
    </table>
}

@code {
    private List<InventoryItem> items = new();
    private InventoryItem newItem = new();
    private bool loading = true;
    private bool showAddForm = false;
    private bool isOnline = true;

    protected override async Task OnInitializedAsync()
    {
        await CheckOnlineStatus();
        await LoadItems();
    }

    private async Task CheckOnlineStatus()
    {
        isOnline = await JS.InvokeAsync<bool>("navigator.onLine");
    }

    private async Task LoadItems()
    {
        loading = true;

        try
        {
            // Data persisted in browser's IndexedDB
            var ds = BeepService.DMEEditor.GetDataSource("LocalInventory");
            var result = await ds.GetEntityAsync("Items", new List<AppFilter>());
            items = result.Cast<InventoryItem>().ToList();
        }
        catch
        {
            items = new List<InventoryItem>();
        }
        finally
        {
            loading = false;
        }
    }

    private void ShowAddForm()
    {
        showAddForm = true;
        newItem = new InventoryItem();
    }

    private async Task HandleAddItem()
    {
        var ds = BeepService.DMEEditor.GetDataSource("LocalInventory");
        var result = ds.InsertEntity("Items", newItem);
        
        if (result.Flag == Errors.Ok)
        {
            showAddForm = false;
            await LoadItems();
        }
    }

    private async Task DeleteItem(InventoryItem item)
    {
        var ds = BeepService.DMEEditor.GetDataSource("LocalInventory");
        var result = ds.DeleteEntity("Items", item);
        
        if (result.Flag == Errors.Ok)
        {
            await LoadItems();
        }
    }

    private async Task SyncWithServer()
    {
        if (!isOnline) return;

        await JS.InvokeVoidAsync("alert", "Syncing with server...");
        // Implement sync logic with Web API
    }

    private class InventoryItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
    }
}
```

### Blazor WASM Sync with Server

```csharp
// Program.cs
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient 
    { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddBeepForBlazorWasm(opts =>
{
    opts.AppRepoName = "SyncApp";
    opts.EnableBrowserStorage = true;
});

// Add sync service
builder.Services.AddScoped<SyncService>();

await builder.Build().RunAsync();

// Services/SyncService.cs
public class SyncService
{
    private readonly HttpClient _httpClient;
    private readonly IBeepService _beepService;

    public SyncService(HttpClient httpClient, IBeepService beepService)
    {
        _httpClient = httpClient;
        _beepService = beepService;
    }

    public async Task<bool> SyncToServer()
    {
        try
        {
            // Get local data
            var localDs = _beepService.DMEEditor.GetDataSource("LocalStorage");
            var localItems = await localDs.GetEntityAsync("Items", new List<AppFilter>());

            // Send to server
            var response = await _httpClient.PostAsJsonAsync("/api/sync", localItems);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SyncFromServer()
    {
        try
        {
            // Get server data
            var serverItems = await _httpClient.GetFromJsonAsync<List<object>>("/api/items");

            // Save locally
            var localDs = _beepService.DMEEditor.GetDataSource("LocalStorage");
            foreach (var item in serverItems)
            {
                localDs.InsertEntity("Items", item);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

---

## Migration Examples

### Migration: Legacy to Modern Desktop

```csharp
// ❌ BEFORE - Legacy Pattern
using TheTechIdea.Beep.Container.Services;

var beepService = new BeepService();
beepService.Configure(
    directorypath: AppContext.BaseDirectory,
    containername: "MyApp",
    configType: BeepConfigType.DataConnector,
    AddasSingleton: true
);

// ✅ AFTER - Modern Pattern with DI
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TheTechIdea.Beep.Container.Services;

var builder = Host.CreateApplicationBuilder();
builder.Services.AddBeepForDesktop(opts =>
{
    opts.AppRepoName = "MyApp";  // ← Changed from Containername
    opts.DirectoryPath = AppContext.BaseDirectory;
    opts.ConfigType = BeepConfigType.DataConnector;
    // Singleton lifetime is automatic for desktop
});

var host = builder.Build();
var beepService = host.Services.GetRequiredService<IBeepService>();
```

### Migration: Legacy to Modern Web

```csharp
// ❌ BEFORE - Legacy Pattern (NOT THREAD-SAFE!)
using TheTechIdea.Beep.Container.Services;

var beepService = new BeepService();
beepService.Configure(
    directorypath: baseDir,
    containername: "MyAPI",
    configType: BeepConfigType.DataConnector,
    AddasSingleton: false  // Attempted scoped
);

// ✅ AFTER - Modern Pattern (THREAD-SAFE)
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBeepForWeb(opts =>
{
    opts.AppRepoName = "MyAPI";
    opts.DirectoryPath = baseDir;
    opts.ConfigType = BeepConfigType.DataConnector;
    opts.MaxConnectionPoolSize = 100;
    opts.ConnectionIdleTimeout = TimeSpan.FromMinutes(5);
    // Scoped lifetime is automatic for web
});

var app = builder.Build();
app.UseBeepForWeb(); // Middleware for connection cleanup
app.MapControllers();
app.Run();
```

### Migration: Property Name Updates

```csharp
// ❌ BEFORE - Deprecated Property Names
options.Containername = "MyApp";     // Obsolete
options.ContainerName = "MyApp";     // Obsolete
var name = beepService.Containername; // Obsolete

// ✅ AFTER - Standardized Property Name
options.AppRepoName = "MyApp";        // Preferred
var name = beepService.AppRepoName;   // Preferred
```

---

## Advanced Patterns

### Advanced: Multi-Environment Configuration

```csharp
// Program.cs - Environment-specific settings
var builder = WebApplication.CreateBuilder(args);

var environment = builder.Environment.EnvironmentName;
var beepDirectory = environment switch
{
    "Development" => Path.Combine(AppContext.BaseDirectory, "Beep", "Dev"),
    "Staging" => Path.Combine(AppContext.BaseDirectory, "Beep", "Staging"),
    "Production" => "/var/beep/prod",
    _ => AppContext.BaseDirectory
};

builder.Services.AddBeepForWeb(opts =>
{
    opts.AppRepoName = $"API-{environment}";
    opts.DirectoryPath = beepDirectory;
    opts.MaxConnectionPoolSize = environment == "Production" ? 200 : 50;
    opts.ConnectionIdleTimeout = environment == "Production" 
        ? TimeSpan.FromMinutes(10) 
        : TimeSpan.FromMinutes(2);
});

var app = builder.Build();
app.UseBeepForWeb();
app.MapControllers();
app.Run();
```

### Advanced: Custom Middleware

```csharp
// Middleware/BeepLoggingMiddleware.cs
public class BeepLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BeepLoggingMiddleware> _logger;

    public BeepLoggingMiddleware(RequestDelegate next, ILogger<BeepLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IBeepService beepService)
    {
        var connectionsBefore = beepService.DMEEditor.GetDataSources().Count;
        
        await _next(context);
        
        var connectionsAfter = beepService.DMEEditor.GetDataSources().Count;
        
        if (connectionsAfter > connectionsBefore)
        {
            _logger.LogWarning($"Connection leak detected! Before: {connectionsBefore}, After: {connectionsAfter}");
        }
    }
}

// Program.cs
app.UseBeepForWeb();
app.UseMiddleware<BeepLoggingMiddleware>();
```

### Advanced: Health Checks Integration

```csharp
// HealthChecks/BeepHealthCheck.cs
using Microsoft.Extensions.Diagnostics.HealthChecks;

public class BeepHealthCheck : IHealthCheck
{
    private readonly IBeepService _beepService;

    public BeepHealthCheck(IBeepService beepService)
    {
        _beepService = beepService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dataSources = _beepService.DMEEditor.GetDataSources();
            var healthyConnections = 0;

            foreach (var ds in dataSources)
            {
                if (ds.ConnectionStatus == ConnectionState.Open)
                {
                    healthyConnections++;
                }
            }

            var data = new Dictionary<string, object>
            {
                ["TotalDataSources"] = dataSources.Count,
                ["HealthyConnections"] = healthyConnections
            };

            return healthyConnections == dataSources.Count
                ? HealthCheckResult.Healthy("All connections healthy", data)
                : HealthCheckResult.Degraded($"Only {healthyConnections}/{dataSources.Count} connections healthy", data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("BeepService health check failed", ex);
        }
    }
}

// Program.cs
builder.Services.AddBeepForWeb(opts => { /* ... */ });
builder.Services.AddHealthChecks()
    .AddCheck<BeepHealthCheck>("beep_service");

var app = builder.Build();
app.MapHealthChecks("/health");
```

### Advanced: Testing Patterns

```csharp
// Tests/BeepServiceTests.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

public class BeepServiceTests
{
    [Fact]
    public void AddBeepForDesktop_ShouldRegisterAsSingleton()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddBeepForDesktop(opts =>
        {
            opts.AppRepoName = "TestApp";
            opts.DirectoryPath = Path.GetTempPath();
        });

        // Act
        var host = builder.Build();
        var service1 = host.Services.GetRequiredService<IBeepService>();
        var service2 = host.Services.GetRequiredService<IBeepService>();

        // Assert
        Assert.Same(service1, service2); // Should be same instance (Singleton)
    }

    [Fact]
    public void AddBeepForWeb_ShouldRegisterAsScoped()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddBeepForWeb(opts =>
        {
            opts.AppRepoName = "TestAPI";
            opts.DirectoryPath = Path.GetTempPath();
        });

        // Act
        var app = builder.Build();
        
        using var scope1 = app.Services.CreateScope();
        var service1a = scope1.ServiceProvider.GetRequiredService<IBeepService>();
        var service1b = scope1.ServiceProvider.GetRequiredService<IBeepService>();
        
        using var scope2 = app.Services.CreateScope();
        var service2 = scope2.ServiceProvider.GetRequiredService<IBeepService>();

        // Assert
        Assert.Same(service1a, service1b);      // Same within scope
        Assert.NotSame(service1a, service2);    // Different across scopes
    }

    [Fact]
    public void InvalidAppRepoName_ShouldThrowValidationException()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        // Act & Assert
        var exception = Assert.Throws<BeepServiceValidationException>(() =>
        {
            builder.Services.AddBeepForDesktop(opts =>
            {
                opts.AppRepoName = ""; // Invalid!
                opts.DirectoryPath = Path.GetTempPath();
            });
        });

        Assert.Contains("AppRepoName", exception.Message);
    }
}
```

---

## Summary

This document provides complete, working examples for all BeepService registration patterns. Key takeaways:

### Desktop Applications
- Use `AddBeepForDesktop()` for Singleton lifetime
- Enable progress reporting for better UX
- Use `BeepDesktopServices` for abstraction layer

### Web API Applications
-  Use `AddBeepForWeb()` for Scoped lifetime (thread-safe)
- Always add `UseBeepForWeb()` middleware for connection cleanup
- Configure connection pooling for high-traffic APIs

### Blazor Server Applications
- Use `AddBeepForBlazorServer()` for Scoped lifetime
- Enable SignalR for real-time updates
- Use `@rendermode InteractiveServer` directive

### Blazor WASM Applications
- Use `AddBeepForBlazorWasm()` for Singleton lifetime
- Enable browser storage for offline-first apps
- Implement sync logic for online/offline scenarios

### Migration
- Replace `Containername` with `AppRepoName`
- Use environment-specific methods instead of generic `AddBeepServices()`
- Leverage DI container instead of direct instantiation

All examples are production-ready and follow best practices for BeepDM 2.0+.
