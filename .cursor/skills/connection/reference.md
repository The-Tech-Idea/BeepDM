# Connection Management Quick Reference

## Driver Linking

```csharp
// Get best matching driver
var driver = ConnectionHelper.GetBestMatchingDriver(props, editor.ConfigEditor);
props.DriverName = driver.PackageName;
props.DriverVersion = driver.version;

// Link connection to drivers
var driverConfig = ConnectionHelper.LinkConnection2Drivers(props, configEditor);

// Get drivers by type
var drivers = ConnectionHelper.GetDriversForDataSourceType(DataSourceType.SqlServer, configEditor);
```

## Connection String Processing

```csharp
// Replace placeholders
var processed = ConnectionHelper.ReplaceValueFromConnectionString(driver, props, editor);

// Normalize paths
var absolutePath = ConnectionHelper.NormalizePath(relativePath, basePath);
ConnectionHelper.NormalizeFilePath(props, basePath);

// Validate placeholders
var missing = ConnectionHelper.ValidateRequiredPlaceholders(template, props);
```

## Validation

```csharp
// General validation
bool isValid = ConnectionHelper.IsConnectionStringValid(connectionString, DataSourceType.SqlServer);

// Datasource-specific
bool isValid = ConnectionHelper.ValidateSqlServerConnectionString(connectionString);
bool isValid = ConnectionHelper.ValidateMySqlConnectionString(connectionString);
bool isValid = ConnectionHelper.ValidateSQLiteConnectionString(connectionString);

// Structure validation
bool isValid = ConnectionHelper.ValidateConnectionStringStructure(connectionString);

// Get requirements
string requirements = ConnectionHelper.GetValidationRequirements(DataSourceType.SqlServer);
```

## Security

```csharp
// Secure connection string
string secured = ConnectionHelper.SecureConnectionString(connectionString);

// Check for sensitive info
bool hasSensitive = ConnectionHelper.ContainsSensitiveInformation(connectionString);

// Selective masking
string masked = ConnectionHelper.SelectiveMask(connectionString, '*', 2);

// Get sensitive parameters
string[] sensitive = ConnectionHelper.GetSensitiveParameterNames();

// Check if secured
bool isSecured = ConnectionHelper.IsConnectionStringSecured(connectionString);
```

## Common Patterns

### Create Connection Properties (Datasource-Agnostic)
```csharp
var props = new ConnectionProperties
{
    ConnectionName = "MyDatabase",
    DatabaseType = DataSourceType.SqlServer,
    Category = DatasourceCategory.RDBMS,
    ConnectionString = $"Server={server};Database={database};..."
};

// Always get driver dynamically
var driver = ConnectionHelper.GetBestMatchingDriver(props, editor.ConfigEditor);
if (driver != null)
{
    props.DriverName = driver.PackageName;
    props.DriverVersion = driver.version;
}
```

### Validate Before Creation
```csharp
// Validate structure
if (!ConnectionHelper.ValidateConnectionStringStructure(props.ConnectionString))
    throw new ArgumentException("Invalid format");

// Validate for datasource
if (!ConnectionHelper.IsConnectionStringValid(props.ConnectionString, props.DatabaseType))
    throw new ArgumentException("Invalid for datasource type");

// Get driver
var driver = ConnectionHelper.GetBestMatchingDriver(props, editor.ConfigEditor);
if (driver == null)
    throw new InvalidOperationException("No driver found");
```
