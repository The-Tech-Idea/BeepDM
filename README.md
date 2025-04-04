# BeepDM: Beep Data Management Engine

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)  
**Current Status: Alpha** - Actively developed, expect bugs, contributions welcome!

BeepDM is a modular, extensible data management engine designed to streamline connecting, managing, and synchronizing data across diverse sources. It provides a robust framework for developers, supporting databases, files, and in-memory stores with programmatic control over connections, data movement, and entity management.

## Key Features
- **Modular Architecture**: Flexible components for data sources, ETL, workflows, and add-ins.
- **Wide Data Source Support**: Connect to databases (e.g., SQLite, SQL Server), files (e.g., CSV, XLS), APIs, and in-memory stores via `IDataConnection` and `IRDBSource`.
- **Data Synchronization**: `DataSyncManager` for real-time or scheduled sync with metrics and logging.
- **Entity Management**: `UnitofWork<T>` for CRUD operations, change tracking, and transactional commits.
- **ETL & Import**: `DataImportManager` for transforming and importing data with batch processing.
- **Dependency Injection**: Supports **Microsoft.Extensions.DependencyInjection** and **Autofac**.
- **Configuration Management**: Centralized settings via `IConfigEditor`.
- **Extensibility**: Add custom functionality with `IDM_Addin` and extend connections/data types.

## Core Components (Main Interfaces)
These are the primary interfaces driving BeepDM’s functionality:
1. **`IDMEEditor`**: The mother class, orchestrating all components below. Acts as the central hub for data management operations.
2. **`IConfigEditor` (`ConfigEditor`)**: Manages framework configurations (e.g., `DataDriversClasses`, `DataTypesMap`, `QueryList`, `DataConnections`), persisting them to JSON files.
3. **`IDataSource`**: Defines the contract for all data source implementations (e.g., SQLite, XLS), providing methods like `GetEntity`, `CreateEntityAs`, and `UpdateEntities`.
4. **`IETL`**: Handles Extract, Transform, and Load operations for data integration.
5. **`IDataTypesHelper`**: Manages data type mappings and configurations, supporting `IDataSource` type translation.
6. **`IUtil`**: Provides common utility functions used across the engine.
7. **`IAssemblyHandler`**: Loads assemblies and extracts implementations (e.g., `IDataSource`, drivers, `IDM_Addin`, extensions).
8. **`IErrorsInfo`**: Handles error reporting and management.
9. **`IDMLogger`**: Manages logging across the framework.
10. **`IJsonLoader`**: Handles loading and saving JSON configuration files.
11. **`IClassCreator`**: Generates classes/types for data source entities.
12. **`IWorkFlowEditor`**: Manages data workflows.
13. **`IWorkFlowStepEditor`**: Manages individual steps/stages within workflows.
14. **`IRuleParser`**: Parses data rules used in workflows.
15. **`IRulesEditor`**: Manages data rules configuration.

## Directory Structure
Every BeepDM project follows this directory structure:
1. **Addin**: Stores DLLs implementing the `IDM_Addin` interface (e.g., user controls, forms, classes).
2. **AI**: Stores AI scripts (for future use).
3. **Config**: Contains configuration files:
   - `QueryList.json`: Defines query types for retrieving metadata from data sources.
   - `ConnectionConfig.json`: Defines drivers, data source classes, and metadata (e.g., icons).
   - `DataTypeMapping.json`: Maps data types between data sources.
   - `DataConnections.json`: Stores data source connection details.
4. **ConnectionDrivers**: Holds data source driver DLLs (e.g., Oracle, SQLite, SQL Server).
5. **DataFiles**: Primary storage for project data files.
6. **DataViews**: Stores JSON files for federated views of data source entities.
7. **Entities**: Temporary storage for data source entity descriptions.
8. **GFX**: Stores graphics and icons used by the application.
9. **LoadingExtensions**: Contains classes implementing `ILoaderExtention` to dynamically load additional functionality.
10. **Mapping**: Stores mapping definitions between data sources.
11. **OtherDLL**: Holds miscellaneous DLLs required by the application.
12. **ProjectClasses**: Primary folder for loading custom implementations (e.g., `IDataSource`, add-ins).
13. **ProjectData**: Stores project-specific files.
14. **Scripts**: Stores scripts and logs.
15. **WorkFlow**: Stores workflow definitions.

## Getting Started
BeepDM is in alpha and offers programmatic control over data operations. Below are examples using **Autofac** to demonstrate core functionality with `IDataSource`-based data sources.

### Prerequisites
- .NET Framework or .NET Core (specific version TBD).
- NuGet packages: `Autofac`.
- Database drivers (e.g., SQLite) or file access for your data sources.

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/The-Tech-Idea/BeepDM.git
   ```
2. Open the solution in Visual Studio.
3. Restore NuGet packages.
4. Build the project.

### Initialization
After bootstrapping with `IBeepService`, perform these steps to populate `ConfigEditor` with defaults, enabling `IDataSource` operations:

#### 1. Add Connection Configurations
Populates `ConfigEditor.DataDriversClasses` with default drivers.
```csharp
using TheTechIdea.Beep.Container;
using TheTechIdea.Beep.Helpers;

beepService.AddAllConnectionConfigurations();
```

#### 2. Add Data Type Mappings
Populates `ConfigEditor.DataTypesMap` with default type mappings.
```csharp
using TheTechIdea.Beep.Container;
using TheTechIdea.Beep.Helpers;

beepService.AddAllDataSourceMappings();
```

#### 3. Add Query Configurations
Populates `ConfigEditor.QueryList` with default SQL queries for RDBMS.
```csharp
using TheTechIdea.Beep.Container;

beepService.AddAllDataSourceQueryConfigurations();
```

#### Example Initialization
```csharp
using Autofac;
using TheTechIdea.Beep.Container;
using TheTechIdea.Beep.Container.Services;

static void Main()
{
    var builder = new ContainerBuilder();
    BeepServicesRegisterAutFac.RegisterServices(builder);
    var container = builder.Build();
    BeepServicesRegisterAutFac.ConfigureServices(container);
    var beepService = BeepServicesRegisterAutFac.beepService;

    // Initialize for IDataSource support
    beepService.AddAllConnectionConfigurations();
    beepService.AddAllDataSourceMappings();
    beepService.AddAllDataSourceQueryConfigurations();
}
```

### Requirements
Ensure data source configurations are registered in `ConfigEditor`. The initialization steps add defaults for common `IDataSource` implementations (e.g., SQLite, XLS). Custom data sources require additional setup (see "Extending BeepDM").

### Basic Usage
#### Bootstrapping with Autofac (Mother Class: `DMEEditor`)
`IDMEEditor` must be initialized to use BeepDM. Here’s an implementation using Autofac:
```csharp
using Autofac;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Logger;
using TheTechIdea.Util;

static void Main()
{
    var builder = new ContainerBuilder();
    builder.RegisterType<DMEEditor>().As<IDMEEditor>().SingleInstance();
    builder.RegisterType<ConfigEditor>().As<IConfigEditor>().SingleInstance();
    builder.RegisterType<DMLogger>().As<IDMLogger>().SingleInstance();
    builder.RegisterType<Util>().As<IUtil>().SingleInstance();
    builder.RegisterType<ErrorsInfo>().As<IErrorsInfo>().SingleInstance();
    builder.RegisterType<JsonLoader>().As<IJsonLoader>().SingleInstance();
    builder.RegisterType<AssemblyHandler>().As<IAssemblyHandler>().SingleInstance();

    var container = builder.Build();
    BeepServicesRegisterAutFac.ConfigureServices(container);
    var beepService = BeepServicesRegisterAutFac.beepService;

    // Initialize framework
    beepService.AddAllConnectionConfigurations();
    beepService.AddAllDataSourceMappings();
    beepService.AddAllDataSourceQueryConfigurations();
}
```
- All features are pluggable; replace implementations (e.g., `DMLogger` with `YourLogger`) as needed.

#### Connecting to SQLite
```csharp
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;

var config = beepService.DMEEditor.ConfigEditor.DataDriversClasses
    .FirstOrDefault(p => p.DatasourceType == DataSourceType.SqlLite);
if (config == null)
    throw new Exception("SQLite config not found in ConfigEditor.DataDriversClasses.");

var connProps = new ConnectionProperties
{
    ConnectionString = "Data Source=./Beep/dbfiles/northwind.db",
    ConnectionName = "northwind.db",
    DriverName = config.PackageName,
    DriverVersion = config.version,
    DatabaseType = DataSourceType.SqlLite,
    Category = DatasourceCategory.RDBMS
};

beepService.DMEEditor.ConfigEditor.AddDataConnection(connProps);
var sqliteDB = (SQLiteDataSource)beepService.DMEEditor.GetDataSource("northwind.db");
sqliteDB.Openconnection();

if (sqliteDB.ConnectionStatus == ConnectionState.Open)
    Console.WriteLine("SQLite connection opened successfully");
```

## Extending BeepDM
### Creating a New Data Source
1. **Implement `IDataSource`**:
   ```csharp
   using System;
   using System.Collections.Generic;
   using System.Data;
   using TheTechIdea.Beep;
   using TheTechIdea.Beep.DataBase;

   [AddinAttribute(Category = DatasourceCategory.CLOUD, DatasourceType = DataSourceType.WebService)]
   public class AzureCosmosDataSource : IDataSource
   {
       public string GuidID { get; set; } = Guid.NewGuid().ToString();
       public event EventHandler<PassedArgs> PassEvent;
       public DataSourceType DatasourceType { get; set; } = DataSourceType.WebService;
       public DatasourceCategory Category { get; set; } = DatasourceCategory.CLOUD;
       public IDataConnection Dataconnection { get; set; }
       public string DatasourceName { get; set; }
       public IErrorsInfo ErrorObject { get; set; }
       public string Id { get; set; }
       public IDMLogger Logger { get; set; }
       public List<string> EntitiesNames { get; set; }
       public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
       public IDMEEditor DMEEditor { get; set; }
       public ConnectionState ConnectionStatus { get; set; }
       public string ColumnDelimiter { get; set; } = "''";
       public string ParameterDelimiter { get; set; } = ":";

       public AzureCosmosDataSource(string name, IDMEEditor editor)
       {
           DatasourceName = name;
           DMEEditor = editor;
       }

       public ConnectionState Openconnection() { /* Implement */ return ConnectionState.Open; }
       public ConnectionState Closeconnection() { /* Implement */ return ConnectionState.Closed; }
       public bool CheckEntityExist(string EntityName) { /* Implement */ return false; }
       public bool CreateEntityAs(EntityStructure entity) { /* Implement */ return false; }
       public object GetEntity(string EntityName, List<AppFilter> filter) { /* Implement */ return null; }
       public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress) { /* Implement */ return null; }
       // Implement other IDataSource methods...

       public void Dispose() { /* Implement cleanup */ }
   }
   ```

2. **Add to `ConfigEditor`**:
   - Place driver DLLs (if needed) in `ConnectionDrivers`.
   - Update `ConnectionConfig.json` or use the Beep Enterprize Winform app:
     ```csharp
     var driver = new ConnectionDriversConfig
     {
         GuidID = "azure-cosmos-guid",
         PackageName = "AzureCosmos",
         DriverClass = "AzureCosmos",
         version = "1.0.0",
         DbConnectionType = "AzureCosmosConnection",
         ConnectionString = "AccountEndpoint={Host};AccountKey={Password};Database={Database};",
         classHandler = "AzureCosmosDataSource",
         DatasourceCategory = DatasourceCategory.CLOUD,
         DatasourceType = DataSourceType.WebService,
         ADOType = false
     };
     beepService.DMEEditor.ConfigEditor.DataDriversClasses.Add(driver);
     ```
   - Place the DLL in `ProjectClasses`.

### Creating a New Add-in
1. **Implement `IDM_Addin`**:
   ```csharp
   using TheTechIdea.Beep;
   using TheTechIdea.Beep.Addin;

   [AddinAttribute(Caption = "Copy Entity Manager", Name = "CopyEntityManager", misc = "ImportDataManager", addinType = AddinType.Class)]
   public class CopyEntityManager : IDM_Addin
   {
       public string AddinName => "CopyEntityManager";
       public IDMEEditor DMEEditor { get; set; }
       public IPassedArgs Passedarg { get; set; }
       public IDMLogger Logger { get; set; }
       public IErrorsInfo ErrorObject { get; set; }

       public void Run(IPassedArgs pPassedarg)
       {
           var ds = DMEEditor.GetDataSource(Passedarg.DatasourceName);
           if (ds != null) ds.Openconnection();
           // Implement logic
       }

       public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
       {
           DMEEditor = pbl;
           Passedarg = e;
           Logger = plogger;
           ErrorObject = per;
       }
   }
   ```

2. **Deploy**: Place the DLL in `Addin` or `ProjectClasses`. It will appear in the add-in tree.

### Creating an Extension
1. **Implement `ILoaderExtention`**:
   ```csharp
   using TheTechIdea.Beep;
   using TheTechIdea.Util;

   public class CustomExtension : ILoaderExtention
   {
       public IAssemblyHandler Loader { get; set; }

       public CustomExtension(IAssemblyHandler ploader) { Loader = ploader; }

       public IErrorsInfo LoadAllAssembly()
       {
           var er = new ErrorsInfo();
           // Custom loading logic
           return er;
       }

       public IErrorsInfo Scan()
       {
           var er = new ErrorsInfo();
           LoadAllAssembly();
           er.Flag = Errors.Ok;
           return er;
       }
   }
   ```

2. **Deploy**: Place the DLL in `LoadingExtensions`.

## Project Status
- **Alpha Phase**: Core features functional, APIs may evolve.
- **Contributions**: Welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) (TBD).

## License
BeepDM is licensed under the [MIT License](LICENSE).

## Learn More
- [Wiki](https://github.com/The-Tech-Idea/BeepDM/wiki/Beep-Data-Management-Engine-(BeepDM))
- [Issues](https://github.com/The-Tech-Idea/BeepDM/issues)
- [Beep Data Sources](https://github.com/The-Tech-Idea/BeepDataSources)
- [Beep Enterprize Winform](https://github.com/The-Tech-Idea/BeepEnterprize.winform)
