# BeepDM Skills Reference

This directory contains AI-assisted coding skills for working with the BeepDM framework. Each skill provides detailed guidance, examples, and best practices for specific aspects of the framework.

## 📚 Available Skills

### 🆕 Service Registration & Configuration

#### [`beepserviceregistration`](beepserviceregistration/) - **NEW Enhanced API**
Modern fluent API for BeepService registration with environment-specific optimizations.

**Use when:**
- Setting up BeepDM in a new application (Desktop, Web, Blazor)
- Migrating from legacy registration patterns
- Configuring environment-specific features (progress reporting, connection pooling, SignalR)

**Key Features:**
- Fluent builder API with method chaining
- Desktop-optimized: `AddBeepForDesktop()` (Singleton, progress UI, design-time support)
- Web-optimized: `AddBeepForWeb()` (Scoped, connection pooling, cleanup middleware)
- Blazor Server: `AddBeepForBlazorServer()` (Scoped, SignalR support)
- Blazor WASM: `AddBeepForBlazorWasm()` (Singleton, browser storage)
- Enhanced validation with descriptive errors
- Standardized naming (`AppRepoName` vs `Containername`)

**Quick Start:**
```csharp
// Desktop
builder.Services.AddBeepForDesktop(opts => {
    opts.AppRepoName = "MyApp";
    opts.DirectoryPath = AppContext.BaseDirectory;
});

// Web API
builder.Services.AddBeepForWeb(opts => {
    opts.AppRepoName = "MyAPI";
    opts.DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Beep");
});
```

---

#### [`beepservice`](beepservice/) - Legacy Initialization
Traditional DMEEditor initialization patterns for desktop applications.

**Use when:**
- Working with existing code using direct `DMEEditor` instantiation
- Understanding legacy patterns during migration
- Simple scenarios without dependency injection

**Quick Start:**
```csharp
var editor = new DMEEditor();
var props = new ConnectionProperties { /* ... */ };
editor.ConfigEditor.AddDataConnection(props);
var state = editor.OpenDataSource(props.ConnectionName);
```

---

### 🔌 Connection Management

#### [`connection`](connection/)
Managing database connections and connection lifecycle.

**Use when:**
- Creating and configuring data connections
- Managing connection state (open/close)
- Handling connection errors and retries

**Quick Start:**
```csharp
var connProps = new ConnectionProperties {
    ConnectionName = "MyDB",
    ConnectionString = "Data Source=./data.db",
    DatabaseType = DataSourceType.SqlLite
};
beepService.DMEEditor.ConfigEditor.AddDataConnection(connProps);
```

---

#### [`connectionproperties`](connectionproperties/)
Building and configuring connection properties for different database types.

**Use when:**
- Creating connection strings for SQL Server, MySQL, PostgreSQL, SQLite, etc.
- Understanding connection property structure
- Debugging connection issues

---

### ⚙️ Configuration

#### [`configeditor`](configeditor/)
Managing BeepDM configuration files and runtime configuration.

**Use when:**
- Adding/updating/removing connections
- Managing data type mappings
- Reading/writing configuration files

**Quick Start:**
```csharp
var editor = beepService.DMEEditor.ConfigEditor;
editor.AddDataConnection(connProps);
editor.SaveDataconnectionsValues(); // Persist to DataConnections.json
```

---

#### [`environmentservice`](environmentservice/)
Managing environment-specific settings and multi-environment configurations.

**Use when:**
- Supporting Dev/Staging/Production environments
- Managing environment variables
- Loading environment-specific configurations

---

### 💾 Data Access

#### [`idatasource`](idatasource/)
Working with IDataSource interface for CRUD operations.

**Use when:**
- Querying data from any datasource
- Inserting, updating, deleting records
- Getting entity metadata and structure

**Quick Start:**
```csharp
var ds = beepService.DMEEditor.GetDataSource("MyDB");
var data = await ds.GetEntityAsync("Customers", filters);
var result = ds.InsertEntity("Customers", newCustomer);
```

---

#### [`unitofwork`](unitofwork/)
Transactional operations with UnitOfWork pattern.

**Use when:**
- Performing multiple operations as a transaction
- Implementing batch inserts/updates/deletes
- Need rollback capability

**Quick Start:**
```csharp
var uow = beepService.DMEEditor.CreateUnitOfWork<Customer>();
uow.AddNew(customer1);
uow.Modify(customer2);
uow.Delete(customer3);
uow.Commit(); // All or nothing
```

---

### 📊 Data Processing

#### [`etl`](etl/)
Extract, Transform, Load operations for data migration and synchronization.

**Use when:**
- Copying data between datasources
- Transforming data during migration
- Bulk data operations

**Quick Start:**
```csharp
var sourceDs = beepService.DMEEditor.GetDataSource("SourceDB");
var targetDs = beepService.DMEEditor.GetDataSource("TargetDB");
await beepService.DMEEditor.ETL.CopyEntityDataAsync(sourceDs, targetDs, "Customers");
```

---

#### [`beepsync`](beepsync/)
Real-time data synchronization between datasources.

**Use when:**
- Keeping multiple databases in sync
- Implementing offline-first patterns
- Building data replication pipelines

---

#### [`mapping`](mapping/)
Entity type mapping and data transformation.

**Use when:**
- Mapping database entities to C# classes
- Auto-mapping configurations
- Custom type conversions

---

#### [`importing`](importing/)
Importing data from files (CSV, Excel, JSON, XML).

**Use when:**
- Bulk importing from files
- Initial data loading
- Data migration from external sources

---

### 🧩 Advanced Features

#### [`forms`](forms/)
Dynamic form generation and master-detail relationships.

**Use when:**
- Building data-driven forms
- Implementing master-detail patterns
- Creating dynamic UI from entity metadata

---

#### [`migration`](migration/)
Database schema migrations and versioning.

**Use when:**
- Creating/updating database schema
- Running migrations on startup
- Managing schema versions

**Quick Start:**
```csharp
var migrationManager = new MigrationManager(editor, dataSource);
migrationManager.EnsureDatabaseCreated("MyApp.Entities", createIfNotExists: true);
```

---

### 💾 In-Memory & Local Storage

#### [`inmemorydb`](inmemorydb/)
In-memory database operations and caching.

**Use when:**
- Testing without database setup
- Fast temporary data storage
- Caching frequently accessed data

---

#### [`localdb`](localdb/)
Local database operations (SQLite, LiteDB).

**Use when:**
- Desktop apps with local storage
- Offline-capable applications
- Lightweight embedded databases

---

#### [`observablebindinglist`](observablebindinglist/)
Observable collections for data binding in UI applications.

**Use when:**
- Binding data to WinForms/WPF controls
- Auto-updating UI on data changes
- Implementing MVVM patterns

---

## 🚀 Getting Started Paths

### New Desktop Application
1. Start with [`beepserviceregistration`](beepserviceregistration/) - Set up services with `AddBeepForDesktop()`
2. Use [`connection`](connection/) - Add database connections
3. Try [`unitofwork`](unitofwork/) - Implement CRUD operations
4. Explore [`forms`](forms/) - Build dynamic UI

### New Web API
1. Start with [`beepserviceregistration`](beepserviceregistration/) - Set up services with `AddBeepForWeb()`
2. Use [`connection`](connection/) - Configure connections
3. Try [`idatasource`](idatasource/) - Build API endpoints
4. Explore [`etl`](etl/) - Data synchronization endpoints

### New Blazor Application
1. Start with [`beepserviceregistration`](beepserviceregistration/) - Set up services with `AddBeepForBlazorServer()` or `AddBeepForBlazorWasm()`
2. Use [`connection`](connection/) - Add connections
3. Try [`idatasource`](idatasource/) - Query data in components
4. Explore [`beepsync`](beepsync/) - Real-time updates

### Migrating Existing Application
1. Read [`beepserviceregistration`](beepserviceregistration/) - Migration guide section
2. Update to environment-specific registration
3. Replace `Containername` with `AppRepoName`
4. Test thoroughly (breaking changes documented)

## 📖 Skill Structure

Each skill directory contains:

- **`SKILL.md`** - Comprehensive guide with:
  - Scope and when to use
  - Core steps and patterns
  - Validation and error handling
  - Pitfalls and best practices
  - File locations
  - Detailed examples

- **`reference.md`** - Quick reference with:
  - Code snippets
  - Common patterns
  - Key methods and classes
  - Troubleshooting

## 🔗 Cross-Skill Workflows

### Workflow: Complete CRUD Application

```csharp
// 1. Service Registration (beepserviceregistration)
builder.Services.AddBeepForDesktop(opts => {
    opts.AppRepoName = "CRUDApp";
    opts.DirectoryPath = AppContext.BaseDirectory;
});

// 2. Connection Setup (connection)
var connProps = new ConnectionProperties {
    ConnectionName = "AppDB",
    ConnectionString = "Data Source=./app.db",
    DatabaseType = DataSourceType.SqlLite
};
beepService.DMEEditor.ConfigEditor.AddDataConnection(connProps);

// 3. Migrations (migration)
var ds = beepService.DMEEditor.GetDataSource("AppDB");
var migrationManager = new MigrationManager(beepService.DMEEditor, ds);
migrationManager.EnsureDatabaseCreated("MyApp.Entities", true);

// 4. CRUD Operations (unitofwork)
var uow = beepService.DMEEditor.CreateUnitOfWork<Customer>();
uow.AddNew(new Customer { Name = "John" });
uow.Commit();

// 5. Query Data (idatasource)
var customers = await ds.GetEntityAsync("Customers", new List<AppFilter>());
```

### Workflow: Data Synchronization

```csharp
// 1. Service Registration (beepserviceregistration)
builder.Services.AddBeepForWeb(opts => {
    opts.AppRepoName = "SyncService";
    opts.MaxConnectionPoolSize = 100;
});

// 2. Setup Connections (connection)
beepService.DMEEditor.ConfigEditor.AddDataConnection(sourceProps);
beepService.DMEEditor.ConfigEditor.AddDataConnection(targetProps);

// 3. ETL Operations (etl)
var sourceDs = beepService.DMEEditor.GetDataSource("SourceDB");
var targetDs = beepService.DMEEditor.GetDataSource("TargetDB");
await beepService.DMEEditor.ETL.CopyEntityDataAsync(sourceDs, targetDs, "Products");

// 4. Sync Service (beepsync)
var syncConfig = new SyncConfiguration { /* ... */ };
await beepService.DMEEditor.SyncManager.ExecuteSyncAsync(syncConfig);
```

### Workflow: Offline-First Blazor App

```csharp
// 1. Service Registration (beepserviceregistration)
builder.Services.AddBeepForBlazorWasm(opts => {
    opts.AppRepoName = "OfflineApp";
    opts.EnableBrowserStorage = true;
    opts.BrowserStorageQuota = 100 * 1024 * 1024; // 100MB
});

// 2. Local Storage (localdb / inmemorydb)
var localDs = beepService.DMEEditor.GetDataSource("LocalStorage");
var cachedData = await localDs.GetEntityAsync("Products", filters);

// 3. Sync When Online (beepsync)
if (await IsOnlineAsync())
{
    var serverDs = beepService.DMEEditor.GetDataSource("ServerAPI");
    await beepService.DMEEditor.SyncManager.SyncToServerAsync(localDs, serverDs);
}
```

## 🎯 Best Practices

### 1. Always Use Environment-Specific Registration
```csharp
// ✅ GOOD - Desktop
services.AddBeepForDesktop(opts => { /* ... */ });

// ✅ GOOD - Web
services.AddBeepForWeb(opts => { /* ... */ });

// ❌ BAD - Generic (no optimizations)
services.AddBeepServices(opts => { /* ... */ });
```

### 2. Use Standardized Property Names
```csharp
// ✅ GOOD
opts.AppRepoName = "MyApp";

// ❌ BAD (Deprecated)
opts.Containername = "MyApp";
```

### 3. Prefer UnitOfWork for Transactions
```csharp
// ✅ GOOD - Transactional
var uow = editor.CreateUnitOfWork<Customer>();
uow.AddNew(customer);
uow.Commit(); // All or nothing

// ❌ BAD - No transaction
ds.InsertEntity("Customers", customer); // Individual operation
```

### 4. Always Check Connection State
```csharp
// ✅ GOOD
var state = ds.Openconnection();
if (state != ConnectionState.Open)
{
    // Handle error
    logger.LogError(ds.ErrorObject.Message);
}

// ❌ BAD
ds.Openconnection(); // Ignoring result
```

### 5. Use Async Methods When Available
```csharp
// ✅ GOOD
var data = await ds.GetEntityAsync("Customers", filters);

// ⚠️ OK but blocks thread
var data = ds.GetEntity("Customers", filters);
```

## 📚 Additional Resources

### Documentation Files
- **Services README**: `DataManagementEngineStandard/Services/README.md`
- **Migration Guide**: `DataManagementEngineStandard/Services/MIGRATION.md`
- **Implementation Summary**: `DataManagementEngineStandard/Services/IMPLEMENTATION_SUMMARY.md`
- **Code Examples**: `DataManagementEngineStandard/Services/Examples/`

### BeepDM Core Documentation
- **Main README**: `BeepDM/README.md`
- **Architecture**: `DataManagementEngineStandard/Docs/`
- **Integration Tests**: `tests/IntegrationTests/`

### Related Repositories
- **[BeepDM](https://github.com/The-Tech-Idea/BeepDM)** - Core data management engine
- **[BeepDataSources](https://github.com/The-Tech-Idea/BeepDataSources)** - 287+ datasource implementations
- **[Beep.Desktop](https://github.com/The-Tech-Idea/Beep.Desktop)** - Desktop UI controls and AppManager
- **[Beep.Winform](https://github.com/The-Tech-Idea/Beep.Winform)** - WinForms controls library

## 🆘 Getting Help

### Common Issues by Skill

| Issue | Related Skill | Quick Fix |
|-------|---------------|-----------|
| "AppRepoName cannot be null" | [`beepserviceregistration`](beepserviceregistration/) | Set `opts.AppRepoName = "YourAppName"` |
| Connection fails to open | [`connection`](connection/) | Check connection string, verify database exists |
| "Already configured" error | [`beepserviceregistration`](beepserviceregistration/) | Call `Dispose()` before reconfiguring |
| UnitOfWork not committing | [`unitofwork`](unitofwork/) | Ensure `Commit()` is called after changes |
| Entity not found | [`idatasource`](idatasource/) | Verify entity name matches database table |
| Migration fails | [`migration`](migration/) | Check namespace contains entities, verify permissions |
| Assembly loading errors | [`beepserviceregistration`](beepserviceregistration/) | Ensure plugins in `ProjectClasses` or `ConnectionDrivers` folder |

### Debugging Steps
1. Check logs for detailed error messages
2. Verify connection strings in `DataConnections.json`
3. Ensure `DirectoryPath` points to valid folder with configs
4. Use try-catch to capture `IErrorsInfo` details
5. Consult skill-specific troubleshooting section

## 📊 Version Information

- **Skills Version**: 2.0
- **Last Updated**: 2026-02-17
- **BeepDM Version**: 2.0+
- **.NET Version**: 8.0+

## 🤝 Contributing

When creating new skills:
1. Follow the established structure (SKILL.md + reference.md)
2. Include comprehensive examples
3. Document pitfalls and best practices
4. Cross-reference related skills
5. Update this README with skill information

---

**Need help choosing a skill?** Start with [`beepserviceregistration`](beepserviceregistration/) for new projects, or [`beepservice`](beepservice/) for understanding existing code.
