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

## Core Components
- **`IDMEEditor`**: Central hub for managing data sources, logging, ETL, and configurations.
- **`IConfigEditor` (`ConfigEditor`)**: Manages framework configurations, including `DataDriversClasses` (connection configs), `DataTypesMap` (type mappings), `QueryList` (SQL queries), and `DataConnections` (connection properties). Persists settings to JSON files.
- **`IDataSource`**: Interface defining the contract for all data source implementations (e.g., SQLite, XLS). Provides methods for connecting, querying, and managing entities (e.g., `GetEntity`, `CreateEntityAs`, `UpdateEntities`), relying on `ConfigEditor` for configuration.
- **`DataSyncManager`**: Manages data synchronization with `DataSyncSchema`.
- **`UnitofWork<T>`**: Generic entity management with observable collections and async CRUD.
- **`DataImportManager`**: Handles data import with field mapping and batch processing.
- **`IBeepService`**: Service layer for initializing and managing BeepDM components.

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
After bootstrapping the BeepDM framework with `IBeepService`, perform these three essential steps to populate `ConfigEditor` with defaults, enabling `IDataSource` operations:

#### 1. Add Connection Configurations
Populates `ConfigEditor.DataDriversClasses` with default connection drivers (e.g., SQLite, SQL Server, XLS).
```csharp
using TheTechIdea.Beep.Container;
using TheTechIdea.Beep.Helpers;

beepService.AddAllConnectionConfigurations(); // Uses ConnectionHelper.GetAllConnectionConfigs()
```

#### 2. Add Data Type Mappings
Populates `ConfigEditor.DataTypesMap` with default type mappings for `IDataSource` type translation.
```csharp
using TheTechIdea.Beep.Container;
using TheTechIdea.Beep.Helpers;

beepService.AddAllDataSourceMappings(); // Uses DataTypeFieldMappingHelper.GetMappings()
```

#### 3. Add Query Configurations
Populates `ConfigEditor.QueryList` with default SQL query repositories for RDBMS `IDataSource` implementations.
```csharp
using TheTechIdea.Beep.Container;

beepService.AddAllDataSourceQueryConfigurations(); // Uses RDBMSHelper.CreateQuerySqlRepos()
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

    // Perform initialization for IDataSource support
    beepService.AddAllConnectionConfigurations();
    beepService.AddAllDataSourceMappings();
    beepService.AddAllDataSourceQueryConfigurations();

    // ConfigEditor is now populated, enabling IDataSource operations
}
```

### Requirements
Before using BeepDM with a specific data source, ensure its configuration is registered in `ConfigEditor`. The initialization steps above add defaults for common `IDataSource` implementations (e.g., SQLite, XLS), but custom data sources require additional setup (see "Extending BeepDM").

### Basic Usage
#### Bootstrapping with Autofac
```csharp
using Autofac;
using TheTechIdea.Beep.Container.Services;

static void Main()
{
    var builder = new ContainerBuilder();
    BeepServicesRegisterAutFac.RegisterServices(builder);
    var container = builder.Build();

    BeepServicesRegisterAutFac.ConfigureServices(container);
    var beepService = BeepServicesRegisterAutFac.beepService;

    // Initialize framework (required for IDataSource)
    beepService.AddAllConnectionConfigurations();
    beepService.AddAllDataSourceMappings();
    beepService.AddAllDataSourceQueryConfigurations();

    // Examples below assume this setup
    return;
}
```

#### Connecting to SQLite
```csharp
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;

// Assumes SQLite config/mappings added via initialization to ConfigEditor
var config = beepService.DMEEditor.ConfigEditor.DataDriversClasses
    .FirstOrDefault(p => p.DatasourceType == DataSourceType.SqlLite);
if (config == null)
    throw new Exception("SQLite connection config not found in ConfigEditor.DataDriversClasses.");

// Create connection properties
var connectionProperties = new ConnectionProperties
{
    ConnectionString = "Data Source=./Beep/dbfiles/northwind.db",
    ConnectionName = "northwind.db",
    DriverName = config.PackageName,
    DriverVersion = config.version,
    DatabaseType = DataSourceType.SqlLite,
    Category = DatasourceCategory.RDBMS
};

// Add to ConfigEditor.DataConnections and create datasource
beepService.DMEEditor.ConfigEditor.AddDataConnection(connectionProperties);
var sqliteDB = (SQLiteDataSource)beepService.DMEEditor.GetDataSource("northwind.db");
sqliteDB.Openconnection();

if (sqliteDB.ConnectionStatus == ConnectionState.Open)
    Console.WriteLine("SQLite connection opened successfully");
else
    Console.WriteLine("Connection failed");
```

#### Connecting to an XLS File
```csharp
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.FileManager;

// Assumes XLS config/mappings added via initialization to ConfigEditor
var config = beepService.DMEEditor.ConfigEditor.DataDriversClasses
    .FirstOrDefault(p => p.DatasourceType == DataSourceType.Xls);
if (config == null)
    throw new Exception("XLS connection config not found in ConfigEditor.DataDriversClasses.");

// Create connection properties
var connectionProperties = new ConnectionProperties
{
    FileName = "country.xls",
    FilePath = "./dbfiles",
    ConnectionName = "country.xls",
    DriverName = config.PackageName,
    DriverVersion = config.version,
    DatabaseType = DataSourceType.Xls,
    Ext = "xls",
    Category = DatasourceCategory.FILE
};

// Add to ConfigEditor.DataConnections and create datasource
beepService.DMEEditor.ConfigEditor.AddDataConnection(connectionProperties);
var xlsFile = (TxtXlsCSVFileSource)beepService.DMEEditor.GetDataSource("country.xls");
xlsFile.Openconnection();

if (xlsFile.ConnectionStatus == ConnectionState.Open)
    Console.WriteLine("XLS connection opened successfully");
else
    Console.WriteLine("Connection failed");
```

#### Moving Data Between Sources
```csharp
using TheTechIdea.Beep.DataBase;

// Assumes SQLite and XLS configs/mappings added via initialization to ConfigEditor
var xlsConfig = beepService.DMEEditor.ConfigEditor.DataDriversClasses
    .FirstOrDefault(p => p.DatasourceType == DataSourceType.Xls);
var xlsConn = new ConnectionProperties
{
    FileName = "country.xls",
    FilePath = "./dbfiles",
    ConnectionName = "country.xls",
    DriverName = xlsConfig.PackageName,
    DriverVersion = xlsConfig.version,
    DatabaseType = DataSourceType.Xls,
    Category = DatasourceCategory.FILE
};
beepService.DMEEditor.ConfigEditor.AddDataConnection(xlsConn);
var sourceDS = beepService.DMEEditor.GetDataSource("country.xls");

var sqliteConfig = beepService.DMEEditor.ConfigEditor.DataDriversClasses
    .FirstOrDefault(p => p.DatasourceType == DataSourceType.SqlLite);
var sqliteConn = new ConnectionProperties
{
    ConnectionString = "Data Source=./Beep/dbfiles/northwind.db",
    ConnectionName = "northwind.db",
    DriverName = sqliteConfig.PackageName,
    DriverVersion = sqliteConfig.version,
    DatabaseType = DataSourceType.SqlLite,
    Category = DatasourceCategory.RDBMS
};
beepService.DMEEditor.ConfigEditor.AddDataConnection(sqliteConn);
var destDS = beepService.DMEEditor.GetDataSource("northwind.db");

// Open connections
sourceDS.Openconnection();
destDS.Openconnection();

if (sourceDS.ConnectionStatus == ConnectionState.Open && destDS.ConnectionStatus == ConnectionState.Open)
{
    string entityName = "Countries";
    var progress = new Progress<PassedArgs>(args => Console.WriteLine(args.Messege));
    
    var srcEntity = sourceDS.GetEntityStructure(entityName, true);
    var destEntity = destDS.GetEntityStructure(entityName, true);
    if (srcEntity != null && destEntity != null)
    {
        var data = sourceDS.GetEntity(entityName, null);
        if (!destDS.CheckEntityExist(entityName))
            destDS.CreateEntityAs(srcEntity);
        destDS.UpdateEntities(entityName, data, progress);
        Console.WriteLine($"Entity {entityName} moved successfully");
    }
    else
        Console.WriteLine("Entity not found");
}
else
    Console.WriteLine("Connection failed");
```

#### Using UnitofWork for Entity Management
```csharp
using TheTechIdea.Beep.Editor;

// Assumes SQLite config/mappings added via initialization to ConfigEditor
var ds = beepService.DMEEditor.GetDataSource("northwind.db");
ds.Openconnection();

// Define entity class
public class Customer : Entity { public int Id { get; set; } public string Name { get; set; } }

var uow = new UnitofWork<Customer>(beepService.DMEEditor, "northwind.db", "Customers", "Id");
await uow.Get();

// Add a new customer
var customer = new Customer { Name = "John Doe" };
uow.Add(customer);

// Commit changes
await uow.Commit(new Progress<PassedArgs>(args => Console.WriteLine(args.Messege)));
Console.WriteLine("Customer added successfully");
```

## Extending BeepDM
BeepDM supports extending its functionality by adding custom connection records and data type mappings to `ConfigEditor` at startup using `ConnectionHelper` and `DataTypeFieldMappingHelper`. This extends the `IDataSource` ecosystem beyond defaults.

### Adding a Custom Connection Record
To support a new data source, define a `ConnectionDriversConfig` and register it with `ConnectionHelper.GetAllConnectionConfigs()`.
```csharp
using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Helpers;

static void Main()
{
    var builder = new ContainerBuilder();
    BeepServicesRegisterAutFac.RegisterServices(builder);
    var container = builder.Build();
    BeepServicesRegisterAutFac.ConfigureServices(container);
    var beepService = BeepServicesRegisterAutFac.beepService;

    // Define a custom connection (e.g., PostgreSQL)
    var customConfig = new ConnectionDriversConfig
    {
        GuidID = "custom-postgre-guid",
        PackageName = "Npgsql",
        DriverClass = "Npgsql",
        version = "4.1.3.0",
        dllname = "Npgsql.dll",
        AdapterType = "Npgsql.NpgsqlDataAdapter",
        DbConnectionType = "Npgsql.NpgsqlConnection",
        ConnectionString = "Host={Host};Port={Port};Database={DataBase};Username={UserID};Password={Password};",
        iconname = "postgre.svg",
        classHandler = "CustomPostgreDataSource",
        ADOType = true,
        DatasourceCategory = DatasourceCategory.RDBMS,
        DatasourceType = DataSourceType.Postgre
    };

    // Add to ConfigEditor.DataDriversClasses
    var configs = ConnectionHelper.GetAllConnectionConfigs();
    configs.Add(customConfig);
    beepService.DMEEditor.ConfigEditor.DataDriversClasses = configs;

    // Use the custom connection
    var connProps = new ConnectionProperties
    {
        ConnectionName = "CustomPostgreDB",
        DatabaseType = DataSourceType.Postgre,
        Host = "localhost",
        Port = 5432,
        Database = "mydb",
        UserID = "postgres",
        Password = "password"
    };
    beepService.DMEEditor.ConfigEditor.AddDataConnection(connProps);
    var dataSource = beepService.DMEEditor.GetDataSource("CustomPostgreDB");
    dataSource.Openconnection();
    if (dataSource.ConnectionStatus == ConnectionState.Open)
        Console.WriteLine("Custom PostgreSQL connection opened");
}
```

### Adding Data Type Mappings
To map data types for a custom data source, define `DatatypeMapping` entries and register them with `DataTypeFieldMappingHelper`.
```csharp
using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Helpers;

static void Main()
{
    var builder = new ContainerBuilder();
    BeepServicesRegisterAutFac.RegisterServices(builder);
    var container = builder.Build();
    BeepServicesRegisterAutFac.ConfigureServices(container);
    var beepService = BeepServicesRegisterAutFac.beepService;

    // Define custom data type mappings (e.g., for PostgreSQL)
    var customMappings = new List<DatatypeMapping>
    {
        new DatatypeMapping
        {
            GuidID = Guid.NewGuid().ToString(),
            DataType = "boolean",
            DataSourceName = "CustomPostgreDataSource",
            NetDataType = "System.Boolean",
            Fav = true
        },
        new DatatypeMapping
        {
            GuidID = Guid.NewGuid().ToString(),
            DataType = "varchar",
            DataSourceName = "CustomPostgreDataSource",
            NetDataType = "System.String",
            Fav = true
        },
        new DatatypeMapping
        {
            GuidID = Guid.NewGuid().ToString(),
            DataType = "integer",
            DataSourceName = "CustomPostgreDataSource",
            NetDataType = "System.Int32",
            Fav = false
        }
    };

    // Add to ConfigEditor.DataTypesMap
    var mappings = DataTypeFieldMappingHelper.GetMappings();
    mappings.AddRange(customMappings);
    beepService.DMEEditor.ConfigEditor.DataTypesMap = mappings;

    // Test mapping
    var field = new EntityField { fieldtype = "System.Boolean" };
    var mappedType = DataTypeFieldMappingHelper.GetDataTypeFromDataSourceClassName("CustomPostgreDataSource", field, beepService.DMEEditor);
    Console.WriteLine($"Mapped type: {mappedType}"); // Should output "boolean"
}
```

## Project Status
- **Alpha Phase**: Core features are functional, but APIs may evolve. Testing and documentation are in progress.
- **Contributions**: Welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) (TBD) for details.

## License
BeepDM is licensed under the [MIT License](LICENSE). Use, modify, and distribute it freely!

## Learn More
- [Wiki](https://github.com/The-Tech-Idea/BeepDM/wiki) - Detailed docs and tutorials.
- [Issues](https://github.com/The-Tech-Idea/BeepDM/issues) - Report bugs or suggest features.

Simplify your data management with BeepDM—connect, sync, and extend with ease!
