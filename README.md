# BeepDM: Beep Data Management Engine

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
**Current Status: Alpha** - Actively developed, expect bugs, contributions welcome!

BeepDM is a modular, extensible data management engine providing a full-stack backend for .NET applications — from configuration bootstrapping and plugin discovery, through data access and entity CRUD, to synchronization, ETL, and workflows.

## Architecture: The Five Core Services

BeepDM's runtime is composed of five core services wired by `BeepService`. Understanding these is understanding the engine:

```
BeepService (Services)
  ├── ConfigEditor (ConfigUtil)       ← configuration persistence, JSON files, paths
  ├── AssemblyHandler (AssemblyHandler) ← plugin/DLL/NuGet discovery and loading
  ├── Util                              ← data conversion, driver linking
  └── DMEEditor (Editor/DM)            ← central hub: data sources, logging, ETL
        └── UnitofWork<T> (Editor/UOW) ← consumer-facing CRUD, validation, export
```

### 1. BeepService — Bootstrapper
**Path:** `DataManagementEngineStandard/Services/`  |  **Doc:** [`Help/services-registration-lifetimes.html`](Help/services-registration-lifetimes.html)

Creates and wires the entire object graph. Supports Desktop, Web API, Blazor Server, Blazor WASM, and MAUI.

```csharp
// Minimal startup — all platforms
services.AddBeepServices(options =>
{
    options.AppPath = "C:\\MyApp";
    options.AppRepoName = "MyAppRepo";
    options.ConfigType = BeepConfigType.DataConnector;
});

// Or use platform shortcuts:
services.AddBeepForDesktop("C:\\MyApp", "MyAppRepo");   // WinForms/WPF
services.AddBeepForWeb("C:\\MyApp", "MyAppRepo");       // ASP.NET Core
services.AddBeepForBlazorServer("C:\\MyApp", "MyAppRepo");
services.AddBeepForBlazorWasm("C:\\MyApp", "MyAppRepo");
```

---

### 2. ConfigEditor — Configuration Hub
**Path:** `DataManagementEngineStandard/ConfigUtil/`  |  **Doc:** [`Help/configeditor.html`](Help/configeditor.html)

Central configuration store persisting to JSON files. Delegates to specialized managers:

| Manager | Responsibility | JSON File |
|---|---|---|
| `DataConnectionManager` | Connection strings, connection properties | `DataConnections.json` |
| `QueryManager` | SQL / query repository | `QueryList.json` |
| `ComponentConfigManager` | Drivers, workflows, reports, projects | `ConnectionConfig.json` |
| `EntityMappingManager` | Entity structures, field mappings | entity files |
| `MigrationHistoryManager` | Per-datasource migration history | migration files |

```csharp
var config = beepService.Config_editor;

// Add a data source connection
config.AddDataConnection(new ConnectionProperties
{
    ConnectionString = "Data Source=./northwind.db",
    ConnectionName = "northwind.db",
    DatabaseType = DataSourceType.SqlLite,
    Category = DatasourceCategory.RDBMS
});

// Discovered type registries (populated by AssemblyHandler)
config.DataSourcesClasses   // All IDataSource implementations found
config.DataDriversClasses    // All driver configs found
config.DefaultResolverClasses // Default value resolvers
config.WorkFlowActions       // Workflow action types
```

---

### 3. AssemblyHandler — Plugin & Driver Discovery
**Path:** `DataManagementEngineStandard/AssemblyHandler/`  |  **Doc:** [`Help/assemblyhandler-loading-nuget-extensions.html`](Help/assemblyhandler-loading-nuget-extensions.html)

Scans DLLs, discovers plugins, and manages NuGet packages. Two implementations:

| Implementation | When to Use |
|---|---|
| `AssemblyHandler` (default) | Single app, few plugins — direct `Assembly.LoadFrom` |
| `SharedContextAssemblyHandler` | Multi-tenant, plugin-heavy — `AssemblyLoadContext` isolation, plugin lifecycle, health monitoring |

```csharp
var handler = beepService.DMEEditor.assemblyHandler;

// Full scan: built-in + drivers + plugins + extensions + NuGet packages
handler.LoadAllAssembly(progress, CancellationToken.None);

// Search & install NuGet drivers
var packages = await handler.SearchNuGetPackagesAsync("postgresql", take: 10);
await handler.InstallAndLoadNuGetPackageAsync(packages[0].PackageId);

// Switch to SharedContext for plugin-heavy apps
beepService.AssemblyHandlerType = AssemblyHandlerType.SharedContext;
```

**Scanned contracts:** `IDataSource`, `IDM_Addin`, `ILoaderExtention`, `IWorkFlowAction`, `IRuleParser`, `IPipelinePlugin`, `IDefaultValueResolver`, `IFileFormatReader`, and 8 more.

---

### 4. DMEEditor — Central Hub
**Path:** `DataManagementEngineStandard/Editor/DM/`  |  **Doc:** [`Help/dmeeditor.html`](Help/dmeeditor.html)

The mother class. Orchestrates all other services. Every consumer operation flows through `DMEEditor`.

```csharp
var dm = beepService.DMEEditor;

// Open a data source (discovers from config, creates IDataSource via AssemblyHandler)
var ds = dm.GetDataSource("northwind.db");
ds.Openconnection();

// Query entity data
var customers = ds.GetEntity("Customers", new List<AppFilter>
{
    new AppFilter { FieldName = "Country", Operator = "=", FilterValue = "UK" }
});

// Insert
ds.InsertEntity("Customers", newRow);

// Access sub-services
dm.ConfigEditor       // IConfigEditor — configuration
dm.assemblyHandler    // IAssemblyHandler — plugins
dm.typesHelper        // IDataTypesHelper — type mapping
dm.ETL                // IETL — data transformation
dm.classCreator       // IClassCreator — dynamic type generation
dm.WorkFlowEditor     // IWorkFlowEditor — workflow management

// Schema operations
var structure = dm.GetEntityStructure("Customers", "northwind.db");
dm.CreateEntityAs(structure);
```

---

### 5. UnitofWork<T> — Consumer-Facing CRUD
**Path:** `DataManagementEngineStandard/Editor/UOW/`  |  **Doc:** [`Help/unitofwork.html`](Help/unitofwork.html)

Full-featured entity CRUD with change tracking, validation, undo/redo, import/export, virtual paging, and computed columns.

```csharp
var uow = new UnitofWork<Product>(dmEditor, "northwind.db", "Products");

// Query with filters and paging
uow.PageSize = 50;
uow.PageIndex = 1;
uow.Get(new List<AppFilter> { new AppFilter { FieldName = "CategoryId", FilterValue = "1" } });

// Create / Update / Delete
var product = uow.New();
product.ProductName = "Widget";
product.UnitPrice = 19.99m;
uow.Add(product);
uow.Commit();

// Change tracking
var changes = uow.GetChangeSummary(); // Inserted: 1, Updated: 0, Deleted: 0

// Export
using var stream = File.Create("products.json");
await uow.ToJsonAsync(stream);

// Undo / Redo
uow.EnableUndo(enable: true, maxDepth: 50);
uow.Undo(); // Reverts last operation

// Validation
uow.IsAutoValidateEnabled = true;
uow.BlockCommitOnValidationError = true;
var errors = uow.GetInvalidItems();

// Aggregates
var totalRevenue = uow.Sum("Revenue");
var avgPrice = uow.Average("UnitPrice");
var customersByCountry = uow.GroupBy("Country");
```

`UnitofWork<T>` also provides: soft delete, virtual/lazy loading, computed columns, bookmarks, batch freeze, concurrency control, and 12 lifecycle events (`PreInsert`, `PostInsert`, `PreUpdate`, `PostUpdate`, etc.).

---

## Core Interfaces

| Interface | Purpose |
|---|---|
| `IBeepService` | Bootstrapper: creates and wires the entire object graph |
| `IDMEEditor` | Central orchestrator: data sources, logging, ETL, configuration |
| `IConfigEditor` | Configuration persistence with specialized per-concern managers |
| `IAssemblyHandler` | DLL discovery, NuGet lifecycle, plugin registration |
| `IDataSource` | All data source implementations (SQL, file, REST, in-memory) |
| `IUnitofWork<T>` | Consumer CRUD with validation, export, change tracking |
| `IDataImportManager` | Batch import pipeline with validation, quality rules, staging |
| `IMappingManager` | Entity mapping with auto-matching, conventions, versioning |
| `IDefaultsManager` | Column defaults with 10 built-in resolvers (DateTime, GUID, User, etc.) |
| `IBeepSyncManager` | Data synchronization with CDC, conflict resolution, SLO |
| `IMigrationManager` | Schema migration with preflight, dry-run, 2PC/Saga transactions |
| `IProxyDataSource` | Failover, circuit breaker, load-balanced data sources |
| `IDistributedDataSource` | Distributed queries with sharding, partition routing, resharding |
| `ISetupWizard` | Wizard-based application initialization with checkpoints |

## Getting Started

### Prerequisites
- .NET 8, 9, or 10
- Visual Studio 2022+

### Installation

```bash
git clone https://github.com/The-Tech-Idea/BeepDM.git
cd BeepDM
dotnet restore
dotnet build
```

### Quick Start — ASP.NET Core

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register BeepDM
builder.Services.AddBeepForWeb(
    appPath: builder.Environment.ContentRootPath,
    appName: "MyApp");

var app = builder.Build();

// Access DMEEditor
var dm = app.Services.GetRequiredService<IDMEEditor>();

// Connect to a data source
var props = new ConnectionProperties
{
    ConnectionString = "Data Source=./Data/northwind.db",
    ConnectionName = "northwind.db",
    DatabaseType = DataSourceType.SqlLite,
    Category = DatasourceCategory.RDBMS
};
dm.ConfigEditor.AddDataConnection(props);

var ds = dm.GetDataSource("northwind.db");
ds.Openconnection();

// Use UnitofWork
var uow = new UnitofWork<Customer>(dm, "northwind.db", "Customers");
uow.Get();
var customers = uow.Units;
```

### Quick Start — Desktop (WinForms/WPF)

```csharp
var services = new ServiceCollection();
services.AddBeepForDesktop("C:\\MyApp", "MyAppRepo");
var sp = services.BuildServiceProvider();
var beepService = sp.GetRequiredService<IBeepService>();

// Initialize defaults
beepService.LoadAssemblies();

var dm = beepService.DMEEditor;
// ... same data source access pattern
```

### Quick Start — Blazor Server

```csharp
builder.Services.AddBeepForBlazorServer(
    builder.Environment.ContentRootPath, "MyApp");
```

### Quick Start — Blazor WASM

```csharp
builder.Services.AddBeepForBlazorWasm(
    builder.HostEnvironment.BaseAddress, "MyApp");
// Uses BlazorIndexedDbSink for telemetry
```

## Directory Structure

Every BeepDM project follows this directory structure:

| Directory | Purpose |
|---|---|
| `Addin/` | DLLs implementing `IDM_Addin` (forms, controls, classes) |
| `Config/` | `DataConnections.json`, `ConnectionConfig.json`, `QueryList.json`, `DataTypeMapping.json` |
| `ConnectionDrivers/` | Database driver DLLs (SQLite, SQL Server, Oracle, etc.) |
| `DataFiles/` | Project data files (CSV, JSON, XLS, etc.) |
| `DataViews/` | Federated view definitions (JSON) |
| `LoadingExtensions/` | `ILoaderExtention` implementations |
| `Mapping/` | Entity mapping definitions |
| `ProjectClasses/` | Custom `IDataSource`, add-in, and extension DLLs |
| `Scripts/` | ETL scripts and logs |
| `WorkFlow/` | Workflow definitions |

## Extending BeepDM

### Creating a Data Source

```csharp
[AddinAttribute(Category = DatasourceCategory.CLOUD,
    DatasourceType = DataSourceType.WebService)]
public class MyDataSource : IDataSource
{
    // Implement IDataSource methods
    public object GetEntity(string entityName, List<AppFilter> filter) { ... }
    public IErrorsInfo InsertEntity(string entityName, object inserted) { ... }
    // ...
}
```

Place the compiled DLL in `ProjectClasses/`. AssemblyHandler discovers it on next `LoadAllAssembly()`.

### Creating an Add-in

```csharp
[AddinAttribute(Caption = "My Addin", Name = "MyAddin", addinType = AddinType.Class)]
public class MyAddin : IDM_Addin
{
    public void Run(IPassedArgs args) { ... }
    public void SetConfig(IDMEEditor editor, IDMLogger logger, ...) { ... }
}
```

Place in `Addin/` or `ProjectClasses/`.

## Documentation

### Help/ Directory (HTML — 70+ pages)
Open `Help/index.html` in a browser for comprehensive documentation covering:
- **Getting Started**: BeepService, Registration, Setup Framework
- **Core Concepts**: UnitOfWork, Connections, Data Sources
- **Data Management**: WebAPI, JSON, CSV, DataView, Proxy, Distributed, Caching
- **Editor Classes**: DMEEditor, ConfigEditor, DataSync, Import, Mapping, Defaults, Migration
- **Advanced**: AssemblyHandler, Rules Engine, ETL Workflow, BeepSync, Services, Helpers, Utils

### Docs/ Directory (Markdown — 25+ guides)
- [Getting Started](Docs/GettingStarted.md)
- [Core Architecture](Docs/CoreArchitecture.md)
- [Service Registration](Docs/ServiceRegistration.md)
- [Unit of Work Pattern](Docs/UnitOfWork.md)
- [Assembly Handler](Docs/AssemblyHandler.md)
- [Configuration Management](Docs/Configuration.md)
- [Creating Custom Data Sources](Docs/HowToCreateNewDataSource.md)
- [ETL Operations](Docs/ETL.md)
- [WebAPI DataSource](Docs/WebAPI.md)
- [And 15+ more...](Docs/)

## Project Status
- **Alpha Phase**: Core features functional, APIs may evolve.
- **Contributions**: Welcome! See [CONTRIBUTING.md](CONTRIBUTING.md).

## License
BeepDM is licensed under the [MIT License](LICENSE).

## Links
- [Wiki](https://github.com/The-Tech-Idea/BeepDM/wiki)
- [Issues](https://github.com/The-Tech-Idea/BeepDM/issues)
- [Beep Data Sources](https://github.com/The-Tech-Idea/BeepDataSources)
- [Beep Enterprize Winform](https://github.com/The-Tech-Idea/BeepEnterprize.winform)
