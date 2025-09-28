# CacheMemoryDataSource

## Overview

The `CacheMemoryDataSource` is a complete implementation of the `IDataSource` interface that uses in-memory caching as the primary data storage mechanism. It provides full CRUD operations, entity management, filtering, and querying capabilities entirely in memory, making it ideal for:

- **High-performance data access** where speed is critical
- **Temporary data storage** during application runtime
- **Testing scenarios** where you need a lightweight, fast data source
- **Session data management** for web applications
- **Cache-first architectures** with optional persistence

## Features

### Core Functionality
- ? **Full CRUD Operations**: Create, Read, Update, Delete
- ? **Entity Management**: Dynamic entity creation and structure discovery
- ? **Advanced Filtering**: Multiple filter operators and conditions
- ? **Paging Support**: Efficient data pagination
- ? **Async Operations**: Full async/await support
- ? **Thread-Safe**: Concurrent operations with ConcurrentDictionary
- ? **Auto-Discovery**: Automatic entity structure inference from data

### Cache Integration
- ? **CacheManager Integration**: Works seamlessly with the cache management system
- ? **Dual Storage**: Data stored both locally and in external cache providers
- ? **Fallback Support**: Graceful handling of cache provider failures
- ? **Statistics**: Performance monitoring and cache hit/miss tracking

### Data Types & Operations
- ? **Multiple Data Types**: Supports all .NET primitive types and complex objects
- ? **Type Inference**: Automatic field type detection from data
- ? **Primary Key Support**: Automatic and manual primary key management
- ? **Schema Persistence**: Entity structures saved to configuration

## Quick Start

### Basic Usage

```csharp
// Initialize the data source
var logger = new YourLoggerImplementation();
var dmeEditor = new YourDMEEditorImplementation();
var errorInfo = new ErrorsInfo();

var cacheDataSource = new CacheMemoryDataSource(
    "MyCacheSource", 
    logger, 
    dmeEditor, 
    DataSourceType.InMemoryCache, 
    errorInfo
);

// Open connection
var connectionResult = cacheDataSource.Openconnection();

// Insert data
var user = new { Id = 1, Name = "John Doe", Email = "john@example.com" };
var result = cacheDataSource.InsertEntity("Users", user);

// Query data
var users = cacheDataSource.GetEntity("Users", null);

// Filter data
var filters = new List<AppFilter>
{
    new AppFilter { FieldName = "Name", Operator = "contains", FilterValue = "John" }
};
var filteredUsers = cacheDataSource.GetEntity("Users", filters);

// Close connection
cacheDataSource.Closeconnection();
cacheDataSource.Dispose();
```

### Advanced Entity Management

```csharp
// Create custom entity structure
var userEntity = new EntityStructure
{
    EntityName = "Users",
    DatasourceEntityName = "Users",
    DatabaseType = DataSourceType.InMemoryCache,
    Fields = new List<EntityField>
    {
        new EntityField { fieldname = "Id", fieldtype = "System.Int32", IsKey = true },
        new EntityField { fieldname = "Name", fieldtype = "System.String" },
        new EntityField { fieldname = "Email", fieldtype = "System.String" },
        new EntityField { fieldname = "CreatedDate", fieldtype = "System.DateTime" }
    }
};

// Create the entity
bool created = cacheDataSource.CreateEntityAs(userEntity);
```

## Supported Filter Operations

The CacheMemoryDataSource supports comprehensive filtering with the following operations:

| Operator | Description | Example |
|----------|-------------|---------|
| `=`, `equals` | Exact match | `Name = "John"` |
| `!=`, `<>` | Not equal | `Status != "Inactive"` |
| `contains` | String contains | `Name contains "John"` |
| `startswith` | String starts with | `Email startswith "john"` |
| `endswith` | String ends with | `Email endswith "@company.com"` |
| `>` | Greater than | `Age > 18` |
| `>=` | Greater than or equal | `Price >= 100` |
| `<` | Less than | `Age < 65` |
| `<=` | Less than or equal | `Price <= 1000` |
| `isnull` | Is null/empty | `MiddleName isnull` |

### Multiple Filters

```csharp
var complexFilters = new List<AppFilter>
{
    new AppFilter { FieldName = "Department", Operator = "=", FilterValue = "Engineering" },
    new AppFilter { FieldName = "Salary", Operator = ">=", FilterValue = "50000" },
    new AppFilter { FieldName = "Status", Operator = "!=", FilterValue = "Terminated" }
};

var results = cacheDataSource.GetEntity("Employees", complexFilters);
```

## Paging Support

```csharp
// Get page 2 with 10 items per page
var pagedResult = cacheDataSource.GetEntity("Users", filters, pageNumber: 2, pageSize: 10);

// Access the data
foreach (var item in pagedResult.Data)
{
    // Process each item
}
```

## Performance Characteristics

### Strengths
- **Extremely Fast**: All operations happen in memory
- **Concurrent**: Thread-safe operations using ConcurrentDictionary
- **Scalable**: Handles thousands of entities and records efficiently
- **Low Latency**: Microsecond response times for simple queries

### Considerations
- **Memory Usage**: All data stored in RAM
- **Persistence**: Data lost on application restart (unless using cache provider persistence)
- **Capacity**: Limited by available system memory

## Configuration

### CacheManager Integration

```csharp
// Configure cache manager before using CacheMemoryDataSource
var config = new CacheConfiguration
{
    DefaultExpiry = TimeSpan.FromHours(1),
    MaxItems = 10000,
    EnableStatistics = true,
    CleanupInterval = TimeSpan.FromMinutes(5)
};

CacheManager.Initialize(config, CacheProviderType.InMemory);

// CacheMemoryDataSource will automatically use the configured cache manager
var dataSource = new CacheMemoryDataSource("MySource", logger, dmeEditor, DataSourceType.InMemoryCache, errorInfo);
```

### Connection Properties

The CacheMemoryDataSource automatically creates appropriate connection properties:

```csharp
// Connection properties are automatically set
var connectionProp = dataSource.Dataconnection.ConnectionProp;
// connectionProp.DatabaseType = DataSourceType.InMemoryCache
// connectionProp.Category = DatasourceCategory.INMEMORY
// connectionProp.DriverName = "CacheMemoryDataSource"
```

## Error Handling

The data source provides comprehensive error information:

```csharp
var result = cacheDataSource.InsertEntity("Users", user);
if (result.Flag != Errors.Ok)
{
    Console.WriteLine($"Error: {result.Message}");
    if (result.Ex != null)
    {
        Console.WriteLine($"Exception: {result.Ex.Message}");
    }
}
```

## Best Practices

### 1. Connection Management
```csharp
// Always open connection before operations
cacheDataSource.Openconnection();

// Properly dispose when done
using var dataSource = cacheDataSource;
// ... use data source
// Automatically disposed at end of using block
```

### 2. Entity Design
```csharp
// Always include an Id field for optimal performance
var entity = new { Id = Guid.NewGuid().ToString(), Name = "Example" };

// Use consistent field naming
var user = new { 
    Id = 1,
    FirstName = "John",
    LastName = "Doe",
    Email = "john.doe@example.com",
    CreatedAt = DateTime.UtcNow 
};
```

### 3. Filtering Performance
```csharp
// More specific filters first for better performance
var filters = new List<AppFilter>
{
    new AppFilter { FieldName = "Id", Operator = "=", FilterValue = "123" }, // Most specific
    new AppFilter { FieldName = "Status", Operator = "=", FilterValue = "Active" },
    new AppFilter { FieldName = "Name", Operator = "contains", FilterValue = "John" } // Least specific
};
```

### 4. Memory Management
```csharp
// For large datasets, use paging
var pageSize = 100;
var pageNumber = 1;
PagedResult results;

do
{
    results = cacheDataSource.GetEntity("LargeDataset", null, pageNumber++, pageSize);
    ProcessData(results.Data);
} while (results.Data.Any());
```

## Integration Examples

### With Entity Framework
```csharp
// Use CacheMemoryDataSource as a high-speed cache layer
var cachedData = cacheDataSource.GetEntity("Users", filters);
if (!cachedData.Any())
{
    // Fallback to database
    var dbData = await dbContext.Users.Where(u => u.Active).ToListAsync();
    
    // Cache the results
    foreach (var user in dbData)
    {
        cacheDataSource.InsertEntity("Users", user);
    }
    
    cachedData = cacheDataSource.GetEntity("Users", filters);
}
```

### With Web APIs
```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly CacheMemoryDataSource _cacheDataSource;
    
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string department = null)
    {
        var filters = new List<AppFilter>();
        if (!string.IsNullOrEmpty(department))
        {
            filters.Add(new AppFilter { FieldName = "Department", Operator = "=", FilterValue = department });
        }
        
        var users = _cacheDataSource.GetEntity("Users", filters);
        return Ok(users);
    }
}
```

## Troubleshooting

### Common Issues

1. **Connection Not Open**
   ```csharp
   // Always check connection status
   if (cacheDataSource.ConnectionStatus != ConnectionState.Open)
   {
       cacheDataSource.Openconnection();
   }
   ```

2. **Entity Not Found**
   ```csharp
   // Check if entity exists before operations
   if (!cacheDataSource.CheckEntityExist("Users"))
   {
       // Create entity or handle error
   }
   ```

3. **Memory Issues**
   ```csharp
   // Monitor memory usage in production
   var stats = CacheManager.GetStatistics();
   if (stats.PrimaryProvider?.MemoryUsage > maxMemoryThreshold)
   {
       // Clear old data or implement eviction strategy
       cacheDataSource.ClearAsync("old_*");
   }
   ```

## See Also

- [CacheManager Documentation](./README.md)
- [Cache Providers](./Providers/)
- [Examples](./Examples/)
- [Performance Guidelines](./Examples/CacheManagerExamples.cs)