# DMLogger ASP.NET Core Integration

DMLogger now supports both Serilog and Microsoft.Extensions.Logging interfaces, making it fully compatible with ASP.NET Core applications.

## Features

- **Dual Interface Support**: Implements both `Serilog.ILogger` and `Microsoft.Extensions.Logging.ILogger`
- **ASP.NET Core Integration**: Full dependency injection support
- **Category-based Logging**: Supports categorized logging for better organization
- **Custom Log States**: Supports Active, Paused, and Stopped states
- **Log Filtering**: Built-in filtering capabilities
- **Event Notifications**: Raises events when logs are written

## Installation

Add the DMLogger package to your project:

```xml
<PackageReference Include="TheTechIdea.Beep.DMLogger" Version="2.0.2" />
```

## Usage

### Basic Registration

Register DMLogger in your ASP.NET Core application:

```csharp
// Program.cs (.NET 6+)
using TheTechIdea.Beep.Logger.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Method 1: Register as service
builder.Services.AddDMLogger();

// Method 2: Register as logging provider
builder.Logging.AddDMLogger();

var app = builder.Build();
```

### Advanced Configuration

```csharp
// Configure DMLogger with custom settings
builder.Services.AddDMLogger(logger =>
{
    // Configure custom settings
    logger.ConfigureLogger(config =>
    {
        var serilogConfig = (LoggerConfiguration)config;
        serilogConfig
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Hour);
    });
    
    // Add custom filters
    logger.AddLogFilter(message => !message.Contains("sensitive"));
});
```

### Using in Controllers

```csharp
[ApiController]
[Route("[controller]")]
public class WeatherController : ControllerBase
{
    private readonly ILogger<WeatherController> _logger;
    private readonly IDMLogger _dmLogger;

    public WeatherController(ILogger<WeatherController> logger, IDMLogger dmLogger)
    {
        _logger = logger;
        _dmLogger = dmLogger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        // Using Microsoft.Extensions.Logging interface
        _logger.LogInformation("Weather data requested");
        
        // Using DMLogger specific methods
        _dmLogger.LogWithContext("Weather request", new { UserId = 123, Timestamp = DateTime.Now });
        
        return Ok();
    }
}
```

### Log State Management

```csharp
public class LoggingService
{
    private readonly DMLogger _dmLogger;

    public LoggingService(DMLogger dmLogger)
    {
        _dmLogger = dmLogger;
    }

    public void ManageLogging()
    {
        // Pause logging temporarily
        _dmLogger.PauseLog();
        
        // Resume logging
        _dmLogger.StartLog();
        
        // Stop logging completely
        _dmLogger.StopLog();
        
        // Check current state
        var currentState = _dmLogger.LogState;
    }
}
```

### Event Handling

```csharp
public void ConfigureEventHandling(DMLogger logger)
{
    // Subscribe to log events
    logger.Onevent += (sender, message) =>
    {
        // Handle log events (e.g., send to external system)
        Console.WriteLine($"Log Event: {message}");
    };
}
```

## Supported Log Levels

DMLogger supports all standard log levels from both logging frameworks:

| Microsoft.Extensions.Logging | Serilog | DMLogger Method |
|------------------------------|---------|-----------------|
| Trace | Verbose | LogTrace |
| Debug | Debug | LogDebug |
| Information | Information | LogInfo |
| Warning | Warning | LogWarning |
| Error | Error | LogError |
| Critical | Fatal | LogCritical |

## Best Practices

1. **Use Dependency Injection**: Register DMLogger through DI for better testability
2. **Configure Early**: Set up logging configuration in `Program.cs` or `Startup.cs`
3. **Use Structured Logging**: Take advantage of `LogStructured` and `LogWithContext` methods
4. **Handle Log States**: Use pause/resume functionality for performance-sensitive operations
5. **Filter Appropriately**: Use log filters to reduce noise and improve performance
6. **Monitor Events**: Subscribe to `Onevent` for real-time log monitoring

## Thread Safety

DMLogger is thread-safe and can be safely used in concurrent scenarios. All logging operations are synchronized using internal locking mechanisms.