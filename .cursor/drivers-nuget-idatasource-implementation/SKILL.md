---
name: drivers-nuget-idatasource-implementation
description: Complete guidance for implementing custom IDataSource drivers, packaging as NuGet, integrating with AssemblyHandler discovery, and using with Setup Framework. Use when creating new data source drivers (MySQL, PostgreSQL, custom databases), handling [AddinAttribute] registration, or troubleshooting driver loading.
---

# Drivers, NuGet, and IDataSource Implementation Skill

Entry point for custom data source driver development in BeepDM.

## File Locations

**Core Interfaces:**
- `DataManagementModelsStandard/IDataSource.cs` — IDataSource contract
- `DataManagementModelsStandard/ConfigUtil/ConnectionDriversConfig.cs` — Driver metadata

**Example Implementations:**
- `DataSourcesPluginsCore/` — Reference implementations for various databases
- `DataSourcesPluginsCore/SQLite/` — SQLite driver example
- `DataSourcesPluginsCore/MSSQL/` — SQL Server driver example
- `DataSourcesPluginsCore/JSON/` — JSON file driver example

**AssemblyHandler (for discovery):**
- `Assembly_helpersStandard/AssemblyHandler.Core.cs` — Discovery mechanism
- `DataManagementModelsStandard/Addin/AddinAttribute.cs` — Registration attribute

**Setup Framework Integration:**
- `DataManagementEngineStandard/SetUp/Steps/DriverProvisionStep.cs` — Load drivers
- `DataManagementEngineStandard/ConfigUtil/ConnectionHelper.cs` — Driver matching

## Key Responsibilities

**IDataSource Implementation:**
- Manage connection lifecycle (Open, Close, ConnectionStatus)
- Query operations (GetEntity, CheckEntityExist, GetEntitesList)
- CRUD (InsertEntity, UpdateEntity, DeleteEntity)
- Schema inspection (GetEntityStructure, CreateEntity, DropEntity)

**[AddinAttribute] Registration:**
- Mark class with namespace, name, version, type
- AssemblyHandler discovers on DLL load
- Entry automatically added to DataDriversClasses

**NuGet Packaging:**
- Reference BeepDM models/core packages
- Include underlying database client library
- Publish to nuget.org or private feed
- DriverProvisionStep downloads on demand

## Common Tasks

### Create a Simple IDataSource

```csharp
[AddinAttribute(
    Name = "PostgreSQL Data Source",
    Path = "MyCompany.Beep.DataSources.PostgreSQL",
    ObjectName = "PostgreSQLDataSource",
    Version = "1.0.0",
    AddinType = AddinType.DataSource)]
public class PostgreSQLDataSource : IDataSource
{
    private ConnectionState _status = ConnectionState.Closed;
    private NpgsqlConnection _connection;
    
    public ConnectionState ConnectionStatus => _status;
    public DataSourceType DatasourceType => DataSourceType.PostgreSQL;
    public string DatasourceName { get; set; }
    
    public void Init(IConfigEditor ce, ConnectionProperties cp, IDMLogger logger)
    {
        DatasourceName = cp.ConnectionName;
    }
    
    public ConnectionState Openconnection()
    {
        try
        {
            _connection = new NpgsqlConnection(_props.ConnectionString);
            _connection.Open();
            _status = ConnectionState.Open;
            return _status;
        }
        catch { _status = ConnectionState.Broken; return _status; }
    }
    
    public void Closeconnection()
    {
        _connection?.Close();
        _status = ConnectionState.Closed;
    }
    
    // Implement remaining IDataSource methods...
}
```

### Package as NuGet

**1. Create project:**
```bash
dotnet new classlib -n TheTechIdea.Beep.DataSources.PostgreSQL
cd TheTechIdea.Beep.DataSources.PostgreSQL
```

**2. Add dependencies to .csproj:**
```xml
<PackageReference Include="TheTechIdea.Beep.Models" Version="1.0.0" />
<PackageReference Include="Npgsql" Version="7.0.0" />
```

**3. Configure NuGet metadata in .csproj:**
```xml
<PropertyGroup>
  <PackageId>TheTechIdea.Beep.DataSources.PostgreSQL</PackageId>
  <Version>1.0.0</Version>
  <Title>BeepDM PostgreSQL Driver</Title>
  <Description>IDataSource implementation for PostgreSQL</Description>
  <Authors>Your Company</Authors>
  <License>MIT</License>
</PropertyGroup>
```

**4. Pack and push:**
```bash
dotnet pack -c Release
dotnet nuget push bin/Release/*.nupkg --api-key KEY --source https://api.nuget.org/v3/index.json
```

### Register with Setup Framework

```csharp
var wizard = new SetupWizardBuilder()
    // Provision the PostgreSQL driver (downloads NuGet if needed)
    .AddStep(new DriverProvisionStep(new DriverProvisionStepOptions
    {
        PackageName = "TheTechIdea.Beep.DataSources.PostgreSQL",
        Version = "1.0.0"  // optional; uses latest if omitted
    }))
    
    // Configure connection
    .AddStep(new ConnectionConfigStep(new ConnectionConfigStepOptions
    {
        ConnectionProperties = new ConnectionProperties
        {
            ConnectionName = "ProductDB",
            DatabaseType = DataSourceType.PostgreSQL,
            ConnectionString = "Host=localhost;Database=products;User=postgres;Password=xxx"
        },
        OpenConnection = true
    }))
    
    .Build();
```

### Handle AddinAttribute Discovery

**Automatic (via AssemblyHandler):**
```csharp
// When DriverProvisionStep loads your NuGet, AssemblyHandler scans it
// and creates ConnectionDriversConfig entries for all [AddinAttribute] classes
var drivers = editor.ConfigEditor.DataDriversClasses;
// ... your PostgreSQLDataSource is now available
```

**Manual:**
```csharp
var asm = Assembly.LoadFrom("./TheTechIdea.Beep.DataSources.PostgreSQL.dll");
var types = asm.GetTypes()
    .Where(t => t.GetCustomAttribute<AddinAttribute>() != null);

foreach (var type in types)
{
    var attr = type.GetCustomAttribute<AddinAttribute>();
    var config = new ConnectionDriversConfig
    {
        PackageName = attr.Path,
        DatasourceType = DataSourceType.PostgreSQL,
        Version = attr.Version
    };
    // Register config...
}
```

### Debug Driver Loading Issues

**Problem: Driver not discovered**
- Verify: [AddinAttribute] applied to class
- Check: AddinType = AddinType.DataSource
- Ensure: Fully qualified Path = "Namespace.ClassName"

**Problem: "No matching driver found" in ConnectionConfigStep**
- Verify: ConnectionProperties.DatabaseType matches driver's DatasourceType
- Check: GetBestMatchingDriver can find the driver config
- Inspect: ConfigEditor.DataDriversClasses for your driver

**Problem: NuGet download fails**
- Check: Package exists on nuget.org or custom source
- Verify: AssemblyHandler has source URLs configured
- Inspect: App/plugin folders for cached package

## ConnectionDriversConfig Structure

```csharp
public class ConnectionDriversConfig
{
    public string PackageName { get; set; }  // e.g. "TheTechIdea.Beep.DataSources.PostgreSQL"
    public DataSourceType DatasourceType { get; set; }  // DataSourceType.PostgreSQL
    public DatasourceCategory DatasourceCategory { get; set; }  // RDBMS, FileBase, etc.
    public string Version { get; set; }  // e.g. "1.0.0"
    public string NuggetSource { get; set; }  // Custom NuGet URL (optional)
    public bool IsMissing { get; set; }  // Not loaded in-process yet
    public bool NuggetMissing { get; set; }  // Not downloaded from NuGet yet
    public string Description { get; set; }
}
```

## IDataSource Contract

```csharp
public interface IDataSource
{
    // Properties
    ConnectionState ConnectionStatus { get; }
    string DatasourceName { get; set; }
    DataSourceType DatasourceType { get; }
    event EventHandler<PassedArgs> PassEvent;
    
    // Lifecycle
    void Init(IConfigEditor ce, ConnectionProperties cp, IDMLogger logger);
    ConnectionState Openconnection();
    void Closeconnection();
    
    // Entity Discovery
    IEnumerable<string> GetEntitesList();
    bool CheckEntityExist(string EntityName);
    EntityStructure GetEntityStructure(string EntityName);
    
    // CRUD
    IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter);
    IErrorsInfo InsertEntity(string EntityName, object entity);
    IErrorsInfo UpdateEntity(string EntityName, object entity);
    IErrorsInfo DeleteEntity(string EntityName, object entity);
    
    // Schema
    IErrorsInfo CreateEntity(EntityStructure structure);
    IErrorsInfo DropEntity(string EntityName);
}
```

## Detailed Reference

Use [`reference.md`](./reference.md) for complete implementation examples.

## See Also

- [Setup Framework SKILL](../setup/SKILL.md) — Integration with wizard
- [AssemblyHandler SKILL](../shared-context-assemblyhandler/SKILL.md) — Driver discovery/loading
- [IDatasource SKILL](../idatasource/SKILL.md) — IDataSource interface details
