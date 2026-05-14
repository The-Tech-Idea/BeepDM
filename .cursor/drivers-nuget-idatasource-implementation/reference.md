# Drivers, NuGet, and IDataSource Implementation Reference

Complete patterns for implementing a custom data source driver, packaging as NuGet, and integrating with Setup Framework.

## Scenario A: Implement a Custom IDataSource

```csharp
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;

namespace MyCompany.Beep.DataSources
{
    /// <summary>Custom data source for MySQL via Connector/NET.</summary>
    [AddinAttribute(
        Name = "MySQL Data Source",
        Path = "TheTechIdea.Beep.DataSources.MySQL",
        ObjectName = "MySQLDataSource",
        Version = "1.0.0",
        AddinType = AddinType.DataSource)]
    public class MySQLDataSource : IDataSource
    {
        private IConfigEditor _configEditor;
        private ConnectionProperties _connectionProperties;
        private IDMLogger _logger;
        
        public event EventHandler<PassedArgs> PassEvent;
        
        // ── IDataSource Properties ────────────────────────────────────────
        
        public ConnectionState ConnectionStatus { get; private set; } = ConnectionState.Closed;
        public string DatasourceName { get; set; } = "MySQL";
        public DataSourceType DatasourceType => DataSourceType.MySQL;
        
        // ── Initialize ────────────────────────────────────────────────────
        
        public void Init(IConfigEditor configEditor, ConnectionProperties connectionProperties, IDMLogger logger)
        {
            _configEditor = configEditor;
            _connectionProperties = connectionProperties;
            _logger = logger;
            DatasourceName = connectionProperties.ConnectionName;
        }
        
        // ── Connection Management ─────────────────────────────────────────
        
        public ConnectionState Openconnection()
        {
            if (ConnectionStatus == ConnectionState.Open) return ConnectionState.Open;
            
            try
            {
                var connStr = _connectionProperties.ConnectionString;
                var conn = new MySqlConnection(connStr);
                conn.Open();
                
                ConnectionStatus = ConnectionState.Open;
                RaisePassEvent("Connected to MySQL server");
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"MySQL connection failed: {ex.Message}");
                ConnectionStatus = ConnectionState.Broken;
                RaisePassEvent($"Connection error: {ex.Message}");
                return ConnectionStatus;
            }
        }
        
        public void Closeconnection()
        {
            try
            {
                // Close any open connections
                ConnectionStatus = ConnectionState.Closed;
                _logger?.WriteLog("MySQL connection closed");
            }
            catch { }
        }
        
        // ── Entity Operations ─────────────────────────────────────────────
        
        public IEnumerable<string> GetEntitesList()
        {
            try
            {
                var schema = GetSchema("Tables");
                return schema?.Rows.Cast<DataRow>()
                    .Select(r => r["TABLE_NAME"].ToString())
                    .ToList() ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"GetEntitesList failed: {ex.Message}");
                return new List<string>();
            }
        }
        
        public bool CheckEntityExist(string EntityName)
        {
            try
            {
                var entities = GetEntitesList();
                return entities.Contains(EntityName, StringComparer.OrdinalIgnoreCase);
            }
            catch { return false; }
        }
        
        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            try
            {
                var sql = $"SELECT * FROM `{EntityName}`";
                if (filter?.Count > 0)
                    sql += " WHERE " + string.Join(" AND ", 
                        filter.Select(f => $"`{f.FilterName}` = @p{filter.IndexOf(f)}"));
                
                using (var cmd = new MySqlCommand(sql, _connection))
                {
                    for (int i = 0; i < filter?.Count; i++)
                        cmd.Parameters.AddWithValue($"@p{i}", filter[i].FilterValue);
                    
                    var adapter = new MySqlDataAdapter(cmd);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    
                    return dt.Rows.Cast<DataRow>().Cast<object>();
                }
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"GetEntity failed: {ex.Message}");
                return new List<object>();
            }
        }
        
        public IErrorsInfo InsertEntity(string EntityName, object entity)
        {
            try
            {
                var props = entity.GetType().GetProperties();
                var columns = string.Join(", ", props.Select(p => $"`{p.Name}`"));
                var values = string.Join(", ", props.Select((p, i) => $"@p{i}"));
                
                var sql = $"INSERT INTO `{EntityName}` ({columns}) VALUES ({values})";
                
                using (var cmd = new MySqlCommand(sql, _connection))
                {
                    for (int i = 0; i < props.Length; i++)
                        cmd.Parameters.AddWithValue($"@p{i}", props[i].GetValue(entity));
                    
                    cmd.ExecuteNonQuery();
                }
                
                return new ErrorsInfo { Flag = Errors.Ok, Message = "Insert successful" };
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"InsertEntity failed: {ex.Message}");
                return new ErrorsInfo { Flag = Errors.Failed, Message = ex.Message, Ex = ex };
            }
        }
        
        // ── Schema Inspection ─────────────────────────────────────────────
        
        public EntityStructure GetEntityStructure(string EntityName)
        {
            var structure = new EntityStructure { EntityName = EntityName };
            
            try
            {
                var columns = GetSchema("Columns", new[] { _connectionProperties.Schema, EntityName });
                foreach (DataRow col in columns?.Rows)
                {
                    var colName = col["COLUMN_NAME"].ToString();
                    var dataType = col["DATA_TYPE"].ToString();
                    var isNullable = col["IS_NULLABLE"].ToString() == "YES";
                    
                    structure.Fields.Add(new EntityField
                    {
                        FieldName = colName,
                        FieldType = ParseDataType(dataType),
                        AllowNull = isNullable
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"GetEntityStructure failed: {ex.Message}");
            }
            
            return structure;
        }
        
        // ── Helpers ───────────────────────────────────────────────────────
        
        private DataTable GetSchema(string collectionName, string[] restrictions = null)
        {
            using (var conn = new MySqlConnection(_connectionProperties.ConnectionString))
            {
                conn.Open();
                return conn.GetSchema(collectionName, restrictions);
            }
        }
        
        private Type ParseDataType(string mySQLType)
        {
            return mySQLType.ToLower() switch
            {
                "int" or "bigint" or "smallint" => typeof(int),
                "decimal" or "float" or "double" => typeof(decimal),
                "varchar" or "char" or "text" => typeof(string),
                "datetime" or "date" => typeof(DateTime),
                "bool" => typeof(bool),
                _ => typeof(object)
            };
        }
        
        private void RaisePassEvent(string message)
            => PassEvent?.Invoke(this, new PassedArgs { Messege = message });
    }
}
```

## Scenario B: Package as NuGet

Create a `.csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    
    <!-- NuGet packaging -->
    <PackageId>TheTechIdea.Beep.DataSources.MySQL</PackageId>
    <Version>1.0.0</Version>
    <Title>BeepDM MySQL Data Source</Title>
    <Description>Custom IDataSource implementation for MySQL via Connector/NET</Description>
    <Authors>Your Company</Authors>
    <License>MIT</License>
    <ProjectUrl>https://github.com/your-org/beep-mysql-driver</ProjectUrl>
    <RepositoryUrl>https://github.com/your-org/beep-mysql-driver</RepositoryUrl>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Reference BeepDM interfaces -->
    <PackageReference Include="TheTechIdea.Beep.Models" Version="1.0.0" />
    <PackageReference Include="TheTechIdea.Beep.Core" Version="1.0.0" />
    
    <!-- MySQL driver -->
    <PackageReference Include="MySqlConnector" Version="2.3.0" />
  </ItemGroup>
</Project>
```

Pack and push:
```bash
dotnet pack MySQLDataSource.csproj -c Release -o ./nupkg
dotnet nuget push ./nupkg/TheTechIdea.Beep.DataSources.MySQL.1.0.0.nupkg \
  --api-key YOUR_NUGET_KEY --source https://api.nuget.org/v3/index.json
```

## Scenario C: Driver Registration via [AddinAttribute]

The `[AddinAttribute]` on your IDataSource class tells AssemblyHandler how to discover and register it:

```csharp
[AddinAttribute(
    Name = "MySQL Data Source",                              // Display name
    Path = "TheTechIdea.Beep.DataSources.MySQL",            // Fully qualified namespace
    ObjectName = "MySQLDataSource",                         // Class name
    Version = "1.0.0",                                      // Driver version
    AddinType = AddinType.DataSource,                       // Mark as data source
    ImageUrl = "https://mysql.com/logo.png",               // Optional icon
    Description = "MySQL via Connector/NET for BeepDM")]
public class MySQLDataSource : IDataSource
{
    // ...
}
```

When AssemblyHandler scans and loads the DLL:
1. It finds the `[AddinAttribute]` via reflection
2. Creates a `ConnectionDriversConfig` entry in `DataDriversClasses`
3. Makes the driver available for `GetBestMatchingDriver` and Setup Framework

## Scenario D: Integrate with Setup Framework

Once your driver NuGet is published, users can set up and use it:

```csharp
public class MyAppSetup
{
    public ISetupWizard CreateWizard(IDMEEditor editor)
    {
        var connProps = new ConnectionProperties
        {
            ConnectionName = "ProductDB",
            DatabaseType = DataSourceType.MySQL,  // or custom enum value
            ConnectionString = "Server=localhost;Database=products;User=root;Password=xxx"
        };

        return new SetupWizardBuilder()
            .WithId("app-setup")
            
            // Step 1: Ensure MySQL driver is loaded (from NuGet if needed)
            .AddStep(new DriverProvisionStep(new DriverProvisionStepOptions
            {
                PackageName = "TheTechIdea.Beep.DataSources.MySQL"
            }))
            
            // Step 2: Configure the connection
            .AddStep(new ConnectionConfigStep(new ConnectionConfigStepOptions
            {
                ConnectionProperties = connProps,
                OpenConnection = true
            }))
            
            // Step 3: Apply schema
            .AddStep(new SchemaSetupStep(new SchemaSetupStepOptions
            {
                EntityTypes = new[] { typeof(Product), typeof(Order) }
            }))
            
            .Build();
    }
}
```

## Scenario E: Discover and Register Multiple Drivers

Scan a folder for driver DLLs and register them all:

```csharp
var assemblyHandler = editor.assemblyHandler;

// Load all [AddinAttribute]-marked classes from a plugin folder
var result = assemblyHandler.LoadExtensionsFromPaths(
    new[] { "./Plugins" },
    new[] { "TheTechIdea" });  // Namespace filter

if (result.Flag == Errors.Ok)
{
    var drivers = editor.ConfigEditor.DataDriversClasses;
    Console.WriteLine($"Discovered {drivers.Count} drivers:");
    foreach (var driver in drivers)
        Console.WriteLine($"  - {driver.PackageName} (v{driver.Version})");
}
```

## Key IDataSource Members

```csharp
public interface IDataSource
{
    // State
    ConnectionState ConnectionStatus { get; }
    string DatasourceName { get; set; }
    DataSourceType DatasourceType { get; }
    
    // Initialization
    void Init(IConfigEditor configEditor, ConnectionProperties cp, IDMLogger logger);
    
    // Connection
    ConnectionState Openconnection();
    void Closeconnection();
    
    // Entity List & Existence
    IEnumerable<string> GetEntitesList();
    bool CheckEntityExist(string EntityName);
    
    // Query
    IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter);
    
    // Insert/Update/Delete
    IErrorsInfo InsertEntity(string EntityName, object entity);
    IErrorsInfo UpdateEntity(string EntityName, object entity);
    IErrorsInfo DeleteEntity(string EntityName, object entity);
    
    // Schema
    EntityStructure GetEntityStructure(string EntityName);
    IErrorsInfo CreateEntity(EntityStructure structure);
    IErrorsInfo DropEntity(string EntityName);
}
```

## NuGet Source Configuration

In `Config/ConnectionConfig.json`, users can specify custom NuGet sources:

```json
{
  "NuGetSources": [
    {
      "Name": "MyCompany Internal",
      "Url": "https://mycompany.com/nuget/v3/index.json",
      "IsPrivate": true,
      "ApiKey": "xyz123"
    },
    {
      "Name": "nuget.org",
      "Url": "https://api.nuget.org/v3/index.json",
      "IsPrivate": false
    }
  ]
}
```

DriverProvisionStep will check both sources when downloading.

## Testing Your Driver

```csharp
[TestClass]
public class MySQLDataSourceTests
{
    private MySQLDataSource _ds;
    private ConnectionProperties _props;
    
    [TestInitialize]
    public void Setup()
    {
        _ds = new MySQLDataSource();
        _props = new ConnectionProperties
        {
            ConnectionName = "TestDB",
            ConnectionString = "Server=localhost;Database=test;User=root"
        };
        _ds.Init(null, _props, null);
    }
    
    [TestMethod]
    public void OpenConnection_ShouldSucceed()
    {
        var state = _ds.Openconnection();
        Assert.AreEqual(ConnectionState.Open, state);
    }
    
    [TestMethod]
    public void GetEntitesList_ShouldReturnTables()
    {
        _ds.Openconnection();
        var entities = _ds.GetEntitesList();
        Assert.IsTrue(entities.Count() > 0);
    }
}
```
