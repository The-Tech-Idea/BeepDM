# Getting Started with BeepDM

## Overview

BeepDM (Beep Data Management) is a modular, extensible data management engine for .NET that provides unified access to 200+ data sources including RDBMS, NoSQL, APIs, files, and streaming platforms.

## Quick Start

### 1. Installation

```bash
# Clone the repository
git clone https://github.com/The-Tech-Idea/BeepDM.git

# Build the solution
dotnet build BeepDM.sln
```

### 2. Basic Setup (Desktop Application)

```csharp
using var host = Host.CreateApplicationBuilder();
host.Services.AddBeepForDesktop(opts =>
{
    opts.AppRepoName = "MyDesktopApp";
    opts.DirectoryPath = AppContext.BaseDirectory;
    opts.EnableAssemblyLoading = true;
    opts.EnableAutoMapping = true;
});

var app = host.Build();
var beepService = app.Services.GetRequiredService<IBeepService>();

// Load plugins and drivers
var progress = new Progress<PassedArgs>(args => 
    Console.WriteLine($"Loading: {args.Messege}"));
beepService.LoadAssemblies(progress);
```

### 3. Create a Connection

```csharp
var editor = beepService.DMEEditor;

// Add connection configuration
var props = new ConnectionProperties
{
    ConnectionName = "MyDatabase",
    DatabaseType = DataSourceType.SqlLite,
    Category = DatasourceCategory.RDBMS,
    ConnectionString = "Data Source=./Beep/dbfiles/app.db",
    DriverName = "SQLite"
};

editor.ConfigEditor.AddDataConnection(props);
editor.ConfigEditor.SaveDataconnectionsValues();

// Open the datasource
var state = editor.OpenDataSource("MyDatabase");
if (state != ConnectionState.Open)
{
    throw new InvalidOperationException("Failed to open datasource");
}

// Get the datasource
var ds = editor.GetDataSource("MyDatabase");
```

### 4. Basic CRUD Operations

```csharp
// Read data
var entities = ds.GetEntitesList();
var data = ds.GetEntity("Customers", new List<AppFilter>());

// Insert
var newRecord = new Dictionary<string, object>
{
    ["Name"] = "John Doe",
    ["Email"] = "john@example.com"
};
var result = ds.InsertEntity("Customers", newRecord);

// Update
var updateData = new Dictionary<string, object>
{
    ["Name"] = "Jane Doe"
};
var updateResult = ds.UpdateEntities("Customers", updateData, progress);

// Delete
var deleteResult = ds.DeleteEntity("Customers", 
    new List<AppFilter> { new AppFilter { FieldName = "Id", FilterValue = "1" } });
```

### 5. Using UnitOfWork

```csharp
using var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers", "Id");

// Create
uow.New();
uow.CurrentItem.Name = "New Customer";

// Read
var allCustomers = await uow.Get();
var customer = uow.Get("123");

// Update
customer.Name = "Updated Name";
uow.Update(customer);

// Commit
var result = await uow.Commit();
if (result.Flag != Errors.Ok)
    Console.WriteLine(result.Message);
```

## Next Steps

- [Core Architecture](CoreArchitecture.md) - Learn about IDMEEditor, IDataSource, and ConfigEditor
- [Service Registration](ServiceRegistration.md) - Desktop, Web API, and Blazor setup
- [Data Source Implementation](HowToCreateNewDataSource.md) - Build custom data source plugins
- [Unit of Work Pattern](UnitOfWork.md) - Advanced CRUD with change tracking
- [ETL Operations](ETL.md) - Data migration and transformation
- [Forms Manager](FormsManager.md) - Oracle Forms-style UI orchestration
- [Assembly Handler](AssemblyHandler.md) - Plugin loading and discovery
- [Configuration Management](Configuration.md) - ConfigEditor and managers

## Prerequisites

- .NET 8.0 or later
- Visual Studio 2022 or VS Code
- Supported databases: SQL Server, MySQL, PostgreSQL, Oracle, SQLite, MongoDB, Redis, and more

## Project Structure

```
BeepDM/
├── DataManagementEngineStandard/    # Core engine implementation
│   ├── Editor/                      # DMEEditor, ETL, UOW, Forms
│   ├── ConfigUtil/                  # ConfigEditor and managers
│   ├── Helpers/                     # Data source helpers
│   └── Services/                    # DI registration
├── DataManagementModelsStandard/    # Interfaces and contracts
│   ├── IDataSource.cs               # Main data source contract
│   └── Editor/                      # IDMEEditor, IUnitOfWork, etc.
├── tests/                           # Unit and integration tests
└── Docs/                            # Documentation
```

## Support

- [Issues](https://github.com/The-Tech-Idea/BeepDM/issues)
- [Wiki](https://github.com/The-Tech-Idea/BeepDM/wiki)
