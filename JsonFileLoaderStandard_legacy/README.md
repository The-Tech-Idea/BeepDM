# JsonLoader - Enhanced JSON Utility Library

A comprehensive JSON serialization and deserialization library for .NET applications with modern features, async support, and robust error handling.

## Features

- ? **Dual Interface Support**: Both basic `IJsonLoader` and enhanced `IEnhancedJsonLoader`
- ? **Async/Await Support**: Asynchronous file operations for better performance
- ? **Structured Logging**: Integration with Microsoft.Extensions.Logging
- ? **Type Safety**: Proper type handling and conversion
- ? **Error Handling**: Comprehensive exception handling with logging
- ? **DataTable/DataSet Support**: Convert JSON to DataTable/DataSet with proper type mapping
- ? **Validation**: JSON string validation
- ? **Dependency Injection**: Full DI container support
- ? **UTF-8 Encoding**: Proper encoding support for international characters

## Installation

Add the JsonFileLoader package to your project:

```xml
<PackageReference Include="TheTechIdea.Beep.JsonLoader" Version="2.0.3" />
```

## Basic Usage

### Simple Serialization/Deserialization

```csharp
var jsonLoader = new JsonLoader();

// Serialize object to JSON string
var person = new Person { Name = "John", Age = 30 };
string json = jsonLoader.SerializeObject(person);

// Deserialize JSON string to object
var deserializedPerson = jsonLoader.DeserializeSingleObjectFromjsonString<Person>(json);
```

### File Operations

```csharp
// Serialize to file
jsonLoader.Serialize("data.json", myObject);

// Deserialize from file
var myObject = jsonLoader.DeserializeSingleObject<MyClass>("data.json");

// Deserialize list from file
var myList = jsonLoader.DeserializeObject<MyClass>("list.json");
```

### Async Operations

```csharp
// Async file operations for better performance
await jsonLoader.SerializeAsync("data.json", myObject);
var myObject = await jsonLoader.DeserializeSingleObjectAsync<MyClass>("data.json");
var myList = await jsonLoader.DeserializeObjectAsync<MyClass>("list.json");
```

## Advanced Usage

### With Dependency Injection

```csharp
// In Program.cs (.NET 6+) or Startup.cs
using TheTechIdea.Beep.Utilities.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register JsonLoader services
builder.Services.AddJsonLoader();
// or
builder.Services.AddJsonLoaderScoped();
// or
builder.Services.AddJsonLoaderTransient();

var app = builder.Build();
```

### In Controllers/Services

```csharp
[ApiController]
[Route("[controller]")]
public class DataController : ControllerBase
{
    private readonly IEnhancedJsonLoader _jsonLoader;
    private readonly ILogger<DataController> _logger;

    public DataController(IEnhancedJsonLoader jsonLoader, ILogger<DataController> logger)
    {
        _jsonLoader = jsonLoader;
        _logger = logger;
    }

    [HttpPost("save")]
    public async Task<IActionResult> SaveData([FromBody] MyData data)
    {
        try
        {
            await _jsonLoader.SerializeAsync($"data_{DateTime.Now:yyyyMMdd}.json", data);
            return Ok("Data saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save data");
            return StatusCode(500, "Error saving data");
        }
    }
}
```

### Custom Serialization Settings

```csharp
var customSettings = new JsonSerializerSettings
{
    NullValueHandling = NullValueHandling.Ignore,
    DateFormatHandling = DateFormatHandling.IsoDateFormat,
    Formatting = Formatting.Indented
};

string json = jsonLoader.SerializeObject(myObject, customSettings);
```

### DataTable/DataSet Conversion

```csharp
// Convert JSON array to DataTable
string jsonArray = @"[
    {""Name"": ""John"", ""Age"": 30, ""Active"": true},
    {""Name"": ""Jane"", ""Age"": 25, ""Active"": false}
]";

DataTable dataTable = jsonLoader.JsonToDataTable(jsonArray);
DataSet dataSet = jsonLoader.ConverttoDataset(jsonArray);
```

### JSON Validation

```csharp
string jsonString = @"{""name"": ""John"", ""age"": 30}";
bool isValid = jsonLoader.IsValidJson(jsonString); // Returns true

string invalidJson = @"{name: John}"; // Missing quotes
bool isValidInvalid = jsonLoader.IsValidJson(invalidJson); // Returns false
```

## Error Handling

The enhanced JsonLoader provides comprehensive error handling:

```csharp
// All methods handle exceptions gracefully and log errors
try
{
    var result = jsonLoader.DeserializeSingleObject<MyClass>("nonexistent.json");
    // Returns default(MyClass) instead of throwing exception
}
catch (Exception ex)
{
    // Only critical errors that need immediate attention are thrown
    logger.LogError(ex, "Critical error occurred");
}
```

## Type Safety Features

### Proper Type Mapping for DataTable

The library properly maps JSON types to .NET types:

| JSON Type | .NET Type |
|-----------|-----------|
| number (integer) | `long` |
| number (float) | `double` |
| boolean | `bool` |
| string | `string` |
| date | `DateTime` |
| array/object | `string` (JSON representation) |
| null | `DBNull.Value` |

### Smart Type Conversion

```csharp
// Handles various data types intelligently
var json = @"{
    ""id"": 123,
    ""price"": 45.99,
    ""active"": true,
    ""created"": ""2023-12-01T10:30:00Z"",
    ""tags"": [""new"", ""featured""],
    ""metadata"": {""category"": ""electronics""}
}";

DataTable table = jsonLoader.JsonToDataTable($"[{json}]");
// Each column will have the appropriate .NET type
```

## Performance Features

- **Async Operations**: Non-blocking file I/O operations
- **UTF-8 Encoding**: Efficient encoding for international characters  
- **Memory Efficient**: Streaming operations where possible
- **Lazy Loading**: Objects created only when needed

## Thread Safety

JsonLoader is thread-safe and can be safely used in concurrent scenarios:

```csharp
// Safe to use in parallel operations
var tasks = files.Select(async file => 
    await jsonLoader.DeserializeObjectAsync<MyClass>(file));
    
var results = await Task.WhenAll(tasks);
```

## Best Practices

1. **Use Dependency Injection**: Register through DI for better testability
2. **Use Async Methods**: For file operations to avoid blocking
3. **Handle Null Values**: Check for null returns from deserialization
4. **Validate JSON**: Use `IsValidJson()` before processing untrusted input
5. **Use Structured Logging**: The built-in logging provides valuable insights
6. **Configure Settings**: Use custom settings for specific requirements

## Migration from Old Version

If upgrading from the previous version:

```csharp
// Old way
var jsonLoader = new JsonLoader();
var result = jsonLoader.DeserializeSingleObject<MyClass>("file.json");

// New way (same syntax, enhanced features)
var jsonLoader = new JsonLoader(logger); // Optional logger
var result = jsonLoader.DeserializeSingleObject<MyClass>("file.json");

// Or use async for better performance
var result = await jsonLoader.DeserializeSingleObjectAsync<MyClass>("file.json");
```

All existing method signatures remain compatible, so no breaking changes are required.