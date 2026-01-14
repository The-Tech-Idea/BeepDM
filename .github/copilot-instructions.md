# BeepDM — Copilot instructions (concise)

Purpose: give AI coding agents the minimum, high-value knowledge to be productive in this repo.

Quick orientation
- Core orchestrator: `DMEEditor` (`DataManagementEngineStandard/Editor/DM/DMEEditor.cs`).
- Core modules: `DataManagementEngineStandard` (implementation), `DataManagementModelsStandard` (interfaces), `Assembly_helpersStandard` (plugin/assembly loader), `Beep.Shell` (CLI).
- Configs: JSON files live in `Config/` (notably `DataConnections.json`, `ConnectionConfig.json`, `DataTypeMapping.json`).

Key patterns and conventions (do not change lightly)
- Dependency Injection: built with Autofac / `IHost` patterns. Many services are registered as **Singleton** (desktop) or **Scoped** (web).
- UnitOfWork: transactional API `UnitofWork<T>` (see `DataManagementEngineStandard/Editor/UOW/`). Use `Commit()` to persist.
- Plugin model: drivers/addins discovered by `AssemblyHandler` (`Assembly_helpersStandard/AssemblyHandler.Core.cs`) and flagged by `[AddinAttribute]`.
- Partial classes: `DMEEditor` is split across partials — prefer editing helper partials for feature-local changes.
- Progress & errors: long ops use `IProgress<PassedArgs>` and return `IErrorsInfo` instead of throwing.

Important files to consult (examples)
- `DataManagementEngineStandard/Editor/DM/DMEEditor.cs` — central orchestration
- `DataManagementEngineStandard/ConfigUtil/ConfigEditor.cs` — connection + mapping APIs
- `Assembly_helpersStandard/AssemblyHandler.Core.cs` — plugin discovery and loaders
- `DataSourcesPluginsCore/` — many example datasource implementations

Build / run / test (common commands)
- Build solution: `dotnet build BeepDM.sln`
- Run shell (dev): `dotnet run --project Beep.Shell/BeepShell.csproj`
- Run unit/integration tests: `dotnet test` (from repo root targets `tests/` projects)

Developer rules for code changes
- Preserve DI registrations and service lifetimes; prefer adding registrations rather than changing existing lifetimes.
- When adding a data source plugin: implement `IDataSource`, annotate with `[AddinAttribute(...)]`, place DLL or project under `ConnectionDrivers/` or `ProjectClasses/` and ensure `AssemblyHandler` can discover it.
- Use existing JSON config files in `Config/` and `ConfigEditor` helpers to add or modify connections — avoid ad-hoc connection strings.
- Keep public API shape stable: many consumers rely on `IDMEEditor`, `IConfigEditor`, `UnitofWork<T>`.

Quick search hints for exploration
- Find registration points: search for `RegisterType<` and `AddBeepServices(`
- Find plugin markers: search for `AddinAttribute` and `LoadExtensionsFromPaths`
- Find UOW usage: search for `CreateUnitOfWork<` and `Commit()`

If you modify this file
- Keep this document short. Update only with new, verifiable repository conventions.

Next step: tell me which areas you want expanded (build, testing, a specific module).

### Connecting to SQLite
```csharp
// From README.md examples
var config = beepService.DMEEditor.ConfigEditor.DataDriversClasses
    .FirstOrDefault(p => p.DatasourceType == DataSourceType.SqlLite);

beepService.DMEEditor.ConfigEditor.AddDataConnection(new ConnectionProperties {
    ConnectionString = "Data Source=./Beep/dbfiles/northwind.db",
    ConnectionName = "northwind.db",
    DriverName = config.PackageName,
    DatabaseType = DataSourceType.SqlLite
});

var sqliteDB = (SQLiteDataSource)beepService.DMEEditor.GetDataSource("northwind.db");
sqliteDB.Openconnection();
```

### Entity Operations with UnitofWork
```csharp
// Create UOW for transactional CRUD
var uow = beepService.DMEEditor.CreateUnitOfWork<Product>();
uow.AddNew(new Product { Name = "Widget", Price = 29.99 });
uow.Modify(existingProduct);
uow.Delete(productToRemove);
uow.Commit(); // Persists all changes
```

### Data Sync Flow
```csharp
// From DataManagementEngineStandard/Docs/datasyncmanager.html
var syncResult = await syncManager.ExecuteSync(config);
Console.WriteLine($"Success: {syncResult.Success}, Records: {syncResult.RecordsProcessed}");
```

## Cross-Component Communication

- **Logging**: All components call `IDMLogger.WriteLog(message)` or `Logger?.WriteLog(msg)`
- **Errors**: Populate `IErrorsInfo` (Flag, Message, Ex) and raise `PassEvent` with `PassedArgs`
- **Progress**: Components report progress via `IProgress<PassedArgs>` (DMEEditor.progress)
- **Async Patterns**: ETL, sync operations are async-first; use `await` for long-running data operations

## Critical File Locations

| Purpose | Path |
|---------|------|
| Central orchestrator | [DataManagementEngineStandard/Editor/DM/DMEEditor.cs](DataManagementEngineStandard/Editor/DM/DMEEditor.cs) |
| Config management | [DataManagementEngineStandard/ConfigUtil/ConfigEditor.cs](DataManagementEngineStandard/ConfigUtil/ConfigEditor.cs) |
| Main interfaces | [DataManagementModelsStandard/Editor/IDMEEditor.cs](DataManagementModelsStandard/Editor/IDMEEditor.cs), [DataManagementModelsStandard/IDataSource.cs](DataManagementModelsStandard/IDataSource.cs) |
| UnitofWork pattern | [DataManagementEngineStandard/Editor/UOW/](DataManagementEngineStandard/Editor/UOW/) |
| Forms/Master-Detail | [DataManagementEngineStandard/Editor/Forms/FormsManager.cs](DataManagementEngineStandard/Editor/Forms/FormsManager.cs) |
| Assembly scanning | [Assembly_helpersStandard/AssemblyHandler.Core.cs](Assembly_helpersStandard/AssemblyHandler.Core.cs) |
| Integration tests | [tests/IntegrationTests/](tests/IntegrationTests/) |

## Integration Patterns by Application Type

### Desktop Applications (WinForms)
```csharp
// From WinFormsApp.UI.Test/Program.cs - Modern DI setup
static void Main(string[] args)
{
    // Initialize configuration system early
    var config = UserSettingsManager.Configuration;
    
    // Register services using IHost pattern
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
            // Add BeepDM services as singleton
            services.AddBeepServices(options => 
            {
                options.DirectoryPath = AppContext.BaseDirectory;
                options.ContainerName = "DesktopApp";
                options.EnableAutoMapping = true;
                options.EnableAssemblyLoading = true;
            });
            
            // Register visualization manager
            services.AddSingleton<IAppManager, AppManager>();
            services.AddSingleton<IBeepService>();
        })
        .Build();

    var beepService = host.Services.GetRequiredService<IBeepService>();
    var visManager = host.Services.GetRequiredService<IAppManager>();
    
    // Start loading with progress reporting
    var progress = new Progress<PassedArgs>(args => visManager.PasstoWaitForm(args));
    beepService.LoadAssemblies(progress);
    
    // Initialize forms and routing
    BeepAppServices.RegisterRoutes();
    Application.Run(mainForm);
}
```

**Key Patterns**:
- Use `IHost` with MS.Extensions.DependencyInjection for modern desktop apps
- Register BeepService as **Singleton** for persistent connections across forms
- Use `IProgress<PassedArgs>` to report loading status to UI
- Load assemblies asynchronously before showing main form
- Inject `IAppManager` for visualization/routing

### CLI Applications (BeepShell)
```csharp
// From BeepShell/Program.cs - Interactive shell with persistent state
static void Main(string[] args)
{
    // Clean up any pending driver operations
    DriverShellCommands.ExecutePendingCleanup(appPath);
    
    // Get profile from args or environment
    string profile = GetProfileFromArgs(args);
    
    // Create shell with persistent DMEEditor
    var shell = new InteractiveShell(profile);
    return shell.Run();
}

// InteractiveShell maintains state across commands
public class InteractiveShell : IDisposable
{
    private ShellServiceProvider _services;
    private IDMEEditor _editor;  // Persistent across all commands
    private SessionState _sessionState;
    
    while (_isRunning)
    {
        var input = AnsiConsole.Prompt(new TextPrompt<string>("[cyan]beep>[/]"));
        ExecuteCommand(input.Trim());  // All commands access same _editor instance
    }
}
```

**Command Pattern** (from ConfigShellCommands):
```csharp
public partial class ConfigShellCommands
{
    private void AddConnectionCommands(Command parent)
    {
        var addCommand = new Command("add", "Add a new data connection");
        addCommand.Options.Add(new Option<string>("--name"));
        addCommand.Options.Add(new Option<string>("--driver"));
        addCommand.Options.Add(new Option<string>("--host"));
        addCommand.SetAction(parseResult =>
        {
            var name = parseResult.GetValue(nameOpt);
            var driver = parseResult.GetValue(driverOpt);
            // Create connection using _editor
            var connProps = new ConnectionProperties { ConnectionName = name, DriverName = driver };
            _editor.ConfigEditor.AddDataConnection(connProps);
        });
    }
}
```

**Key Patterns**:
- Use `ShellServiceProvider` to manage persistent DMEEditor across session
- Use System.CommandLine for command parsing
- Each command gets access to shared `_editor` instance
- Profile-based configuration for multi-environment support

### Web Applications (Blazor/ASP.NET Core)
```csharp
// From Beep.Container/RegisterBeepinServiceCollection.cs - Web DI setup
public void ConfigureServices(IServiceCollection services)
{
    // Register as SCOPED for web applications (one per HTTP request)
    services.AddBeepServices(options =>
    {
        options.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
        options.ContainerName = "WebApp";
        options.ServiceLifetime = ServiceLifetime.Scoped;  // NOT Singleton!
        options.EnableAutoMapping = true;
    });
    
    // OR use convenience method for web:
    services.RegisterScoped();
    
    // Add Blazor services
    services.AddRazorComponents();
    services.AddScoped<AuthService>();
}

public void Configure(IApplicationBuilder app)
{
    app.UseRouting();
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapRazorComponents<App>();
        // Map API endpoints for data operations
        endpoints.MapPost("/api/connections/add", HandleAddConnection);
        endpoints.MapPost("/api/data/sync", HandleDataSync);
    });
}

// Blazor component example
@inject IBeepService BeepService
@inject AuthService AuthService

@code {
    private List<EntityStructure> entities;
    
    protected override async Task OnInitializedAsync()
    {
        var ds = await BeepService.DMEEditor.GetDataSource("mydb");
        entities = await ds.GetEntitiesAsync();
    }
    
    private async Task SyncData()
    {
        var result = await BeepService.DMEEditor.ETL.SyncDataAsync(sourceDs, targetDs);
    }
}
```

**Web API Pattern**:
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
    
    [HttpPost("create-connection")]
    public IActionResult CreateConnection([FromBody] ConnectionRequest req)
    {
        try
        {
            var connProps = new ConnectionProperties
            {
                ConnectionName = req.Name,
                DriverName = req.Driver,
                ConnectionString = req.ConnectionString,
                DatabaseType = req.DatabaseType
            };
            
            _beepService.DMEEditor.ConfigEditor.AddDataConnection(connProps);
            var ds = _beepService.DMEEditor.GetDataSource(req.Name);
            
            return Ok(new { success = true, message = "Connection created" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpGet("entities/{connectionName}")]
    public async Task<IActionResult> GetEntities(string connectionName)
    {
        var ds = _beepService.DMEEditor.GetDataSource(connectionName);
        var entities = await ds.GetEntitiesAsync();
        return Ok(entities);
    }
    
    [HttpPost("sync")]
    public async Task<IActionResult> SyncData([FromBody] SyncRequest req)
    {
        var sourceDs = _beepService.DMEEditor.GetDataSource(req.SourceConnection);
        var targetDs = _beepService.DMEEditor.GetDataSource(req.TargetConnection);
        
        var result = await _beepService.DMEEditor.ETL.SyncDataAsync(sourceDs, targetDs);
        return Ok(result);
    }
}
```

**Key Patterns for Web**:
- Register as **Scoped** (not Singleton) for thread safety per HTTP request
- Use `IBeepService` dependency injection in controllers/components
- Create connections per-request or cache in distributed cache (Redis)
- Handle async operations properly (async/await)
- Validate user permissions before data operations
- Use `IProgress<PassedArgs>` for long-running operations with SignalR updates

## Beep.Containers Integration

### BeepService Registration (Microsoft.Extensions.DependencyInjection)
```csharp
// Beep.Container.Services.BeepService implements IBeepService
var beepService = new BeepService();
beepService.Configure(
    directorypath: AppContext.BaseDirectory,
    containername: "MyApp",
    configType: BeepConfigType.DataConnector,
    AddasSingleton: true
);

// OR use IServiceCollection extension methods
services.AddBeepServices(options =>
{
    options.DirectoryPath = AppContext.BaseDirectory;
    options.ContainerName = "MyApp";
    options.ConfigType = BeepConfigType.DataConnector;
    options.ServiceLifetime = ServiceLifetime.Singleton;  // Desktop
    // options.ServiceLifetime = ServiceLifetime.Scoped;  // Web
});

// Create mappings (must be called after registration)
services.CreateMapping(beepService);
```

### Creating and Managing DataSources
```csharp
// 1. Add connection configuration
var connProps = new ConnectionProperties
{
    ConnectionName = "ProductsDB",
    DriverName = "SQLite",
    ConnectionString = "Data Source=./products.db",
    DatabaseType = DataSourceType.SqlLite,
    Category = DatasourceCategory.RDBMS
};

beepService.DMEEditor.ConfigEditor.AddDataConnection(connProps);

// 2. Get and open data source
var dataSource = beepService.DMEEditor.GetDataSource("ProductsDB");
await dataSource.OpenConnectionAsync();

// 3. Perform operations
var entities = await dataSource.GetEntitiesAsync();
var table = await dataSource.GetDataAsync("Products", new EntityStructure { EntityName = "Products" });

// 4. Create UnitOfWork for transactions
var uow = beepService.DMEEditor.CreateUnitOfWork<Product>();
uow.AddNew(new Product { Name = "Widget", Price = 29.99 });
uow.Commit();

// 5. Close when done
dataSource.Closeconnection();
```

### Loading Custom Assemblies
```csharp
// Automatic loading from standard directories
var progress = new Progress<PassedArgs>(args => Console.WriteLine(args.Messege));
beepService.LoadAssemblies(progress);

// OR load from specific folder
beepService.DMEEditor.assemblyHandler.LoadExtensionsFromPaths(
    new[] { "./Plugins", "./CustomDrivers" },
    new[] { "TheTechIdea", "MyCompany" }
);

// Access loaded data sources
foreach (var dsClass in beepService.DMEEditor.ConfigEditor.DataDriversClasses)
{
    Console.WriteLine($"Driver: {dsClass.PackageName} - {dsClass.DatasourceType}");
}
```

## Project-Specific Conventions

1. **Namespace Structure**: `TheTechIdea.Beep.*` (e.g., `TheTechIdea.Beep.Editor`, `TheTechIdea.Beep.DataBase`)
2. **Disposal Pattern**: Use `IDisposable` with try-catch guards; DMEEditor.Dispose() cleans up all child resources
3. **Thread Safety**: Use `ConcurrentDictionary` in AssemblyHandler; `lock` objects in critical sections
4. **Event Handling**: `PassEvent` (EventHandler<PassedArgs>) for notifications; check for null before invoking
5. **Configuration Immutability**: Once loaded, config files should not be modified in-memory without re-saving via ConfigEditor
6. **Error Propagation**: Return `IErrorsInfo` from methods rather than throwing; use ErrorObject.Flag = Errors.Ok/Failed
7. **Service Lifetime**:
   - **Desktop/CLI**: Singleton (persistent across application lifetime)
   - **Web/Blazor**: Scoped (isolated per HTTP request)
8. **Async Operations**: Always use async/await for long-running operations (ETL, Sync, data loading)

---

**For detailed examples and architecture docs**, see [README.md](README.md), component-specific READMEs in each project folder, and HTML docs in [DataManagementEngineStandard/Docs/](DataManagementEngineStandard/Docs/).

**Related Repositories**:
- [Beep.Containers](https://github.com/The-Tech-Idea/Beep.Containers) - DI/Service registration
- [Beep.Desktop](https://github.com/The-Tech-Idea/Beep.Desktop) - Desktop UI controls and patterns
- [BeepShell](https://github.com/The-Tech-Idea/Beep.Shell) - CLI implementation reference
