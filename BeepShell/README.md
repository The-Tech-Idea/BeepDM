# BeepShell

**BeepShell** is an interactive REPL (Read-Eval-Print Loop) shell for BeepDM, providing a persistent session environment for database management, driver installation, data operations, and workflow execution.

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

---

## 🎯 Features

### Core Capabilities
- **Persistent State Management**: Maintains connections and context across commands
- **Hot-Reloadable Plugin System**: Load, unload, and reload plugins without restarting
- **Database Driver Management**: Install, update, and manage database drivers from NuGet
- **Interactive Data Operations**: Query, import, export, and transform data
- **Workflow Orchestration**: Define and execute complex ETL/data workflows
- **Profile Management**: Multiple configuration profiles for different environments

### Command Categories
- **Connection Management** (`conn`): Create, test, and manage database connections
- **Driver Management** (`driver`): Install, update, and browse NuGet packages
- **Data Source Operations** (`ds`): Work with data sources and entities
- **Query Execution** (`query`): Execute SQL and inspect results
- **Import/Export** (`import`, `export`): Data migration and transformation
- **Workflow Execution** (`workflow`): Run predefined data workflows
- **Plugin Management** (`plugin`): Load and manage shell extensions

---

## 🚀 Quick Start

### Running BeepShell

```powershell
# Run with default profile
dotnet run --project BeepShell

# Run with specific profile
dotnet run --project BeepShell -- --profile production

# Or use the compiled executable
.\BeepShell.exe --profile dev
```

### Basic Commands

```bash
# Show available commands
help

# List configured drivers
driver list

# Install a database driver
driver install

# Create a connection
conn create

# List connections
conn list

# Exit BeepShell
exit
```

---

## 📦 Driver Management

BeepShell provides advanced driver management with NuGet integration:

### Installing Drivers

```bash
# Interactive installation (recommended)
driver install

# Search NuGet.org for packages
> Search NuGet.org (recommended)
> Enter search term: Oracle

# Select package from results:
  ❯ Oracle.ManagedDataAccess.Core (12.5M downloads)
    Oracle.ManagedDataAccess (45.2M downloads)
    
# Select version:
  ❯ Latest (recommended)
    23.4.0
    21.12.0
    19.20.0
```

### Checking for Updates

```bash
# Check specific driver for updates
driver update --name Oracle --check

# Update driver to latest version
driver update --name Oracle

# List all installed drivers with versions
driver list
```

### Cleaning Drivers

```bash
# Remove all driver DLLs (marks for deletion on restart)
driver clean

# Force clean without confirmation
driver clean --force

# Drivers are deleted on next BeepShell startup
```

---

## 🔌 Plugin System

BeepShell supports a powerful plugin system for extending functionality with custom commands and workflows.

### Plugin Architecture

```
BeepShell/                    # Main shell application
├── Commands/                 # Built-in command implementations
├── Infrastructure/           # Core shell services
│   ├── PluginManager.cs     # Plugin lifecycle management
│   ├── PluginLoadContext.cs # Isolated assembly loading
│   └── ShellEventBus.cs     # Event communication
└── README.md

BeepShell.Shared/            # Shared plugin interfaces
├── Interfaces/
│   ├── IShellPlugin.cs      # Main plugin interface
│   ├── IShellCommand.cs     # Command interface
│   └── IShellWorkflow.cs    # Workflow interface
└── Models/
    └── PluginLoadResult.cs  # Plugin load status
```

---

## 🛠️ Creating Your Own Plugin

### Step 1: Create Plugin Project

```powershell
# Create a new class library targeting .NET 8.0
dotnet new classlib -n MyCustomPlugin -f net8.0

# Add reference to BeepShell.Shared
cd MyCustomPlugin
dotnet add reference ../BeepShell.Shared/BeepShell.Shared.csproj
```

### Step 2: Implement IShellPlugin

```csharp
using BeepShell.Shared.Interfaces;
using BeepShell.Shared.Models;
using System.CommandLine;
using TheTechIdea.Beep.Editor;

namespace MyCustomPlugin
{
    public class MyPlugin : IShellPlugin
    {
        private IDMEEditor? _editor;

        // Plugin metadata
        public string PluginId => "my-custom-plugin";
        public string ExtensionName => "My Custom Plugin";
        public string Version => "1.0.0";
        public bool SupportsHotReload => true;

        // Lifecycle methods
        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
        }

        public void OnLoad()
        {
            Console.WriteLine($"[MyPlugin] Loaded v{Version}");
        }

        public void OnUnload()
        {
            Console.WriteLine("[MyPlugin] Unloading...");
        }

        public void Cleanup()
        {
            // Release resources
            _editor = null;
        }

        // Hot-reload support
        public Task<bool> PrepareUnloadAsync()
        {
            // Check if safe to unload
            return Task.FromResult(true);
        }

        public Task OnReloadAsync()
        {
            Console.WriteLine("[MyPlugin] Reloaded!");
            return Task.CompletedTask;
        }

        // Provide commands
        public IEnumerable<IShellCommand> GetCommands()
        {
            return new[] { new MyCustomCommand(_editor!) };
        }

        // Provide workflows (optional)
        public IEnumerable<IShellWorkflow> GetWorkflows()
        {
            return Enumerable.Empty<IShellWorkflow>();
        }
    }
}
```

### Step 3: Implement Custom Command

```csharp
using BeepShell.Shared.Interfaces;
using System.CommandLine;
using TheTechIdea.Beep.Editor;
using Spectre.Console;

namespace MyCustomPlugin
{
    public class MyCustomCommand : IShellCommand
    {
        private readonly IDMEEditor _editor;

        public string CommandName => "mycmd";

        public MyCustomCommand(IDMEEditor editor)
        {
            _editor = editor;
        }

        public Command BuildCommand()
        {
            var command = new Command("mycmd", "My custom command");
            
            // Add options
            var nameOption = new Option<string>(
                new[] { "--name", "-n" }, 
                "Your name");
            command.AddOption(nameOption);

            // Set handler
            command.SetHandler((name) => Execute(name), nameOption);

            return command;
        }

        private void Execute(string name)
        {
            AnsiConsole.MarkupLine($"[green]Hello, {name}![/]");
            AnsiConsole.MarkupLine($"[cyan]Connected to {_editor.DataSources.Count} data sources[/]");
        }

        public string[] GetExamples()
        {
            return new[]
            {
                "mycmd --name John",
                "mycmd -n BeepShell"
            };
        }
    }
}
```

### Step 4: Build and Load Plugin

```powershell
# Build the plugin
dotnet build MyCustomPlugin

# Copy to plugins directory
mkdir BeepShell/bin/Debug/net8.0/Plugins
cp MyCustomPlugin/bin/Debug/net8.0/MyCustomPlugin.dll BeepShell/bin/Debug/net8.0/Plugins/

# Load in BeepShell
plugin load --path Plugins/MyCustomPlugin.dll
```

### Step 5: Use Your Plugin

```bash
# Your command is now available!
mycmd --name World
# Output: Hello, World!
#         Connected to 3 data sources

# Get help for your command
help mycmd

# Hot-reload after making changes
plugin reload --id my-custom-plugin
```

---

## 🔥 Advanced Plugin Features

### Hot-Reload Support

Plugins can be reloaded without restarting BeepShell:

```csharp
public class HotReloadablePlugin : IShellPlugin
{
    public bool SupportsHotReload => true;

    public async Task<bool> PrepareUnloadAsync()
    {
        // Save state before unload
        await SaveStateAsync();
        
        // Return true if safe to unload
        return true;
    }

    public async Task OnReloadAsync()
    {
        // Restore state after reload
        await RestoreStateAsync();
    }
}
```

### Event Communication

Plugins can communicate via the event bus:

```csharp
public class EventAwarePlugin : IShellPlugin
{
    private ShellEventBus? _eventBus;

    public void Initialize(IDMEEditor editor)
    {
        _eventBus = new ShellEventBus();
        
        // Subscribe to events
        _eventBus.Subscribe<PluginEventArgs>("PluginLoaded", OnPluginLoaded);
    }

    private void OnPluginLoaded(PluginEventArgs args)
    {
        Console.WriteLine($"Another plugin loaded: {args.Plugin.ExtensionName}");
    }

    public void OnLoad()
    {
        // Publish event
        _eventBus?.Publish("PluginLoaded", new PluginEventArgs 
        { 
            Plugin = this 
        });
    }
}
```

### Accessing Shell Services

Plugins have full access to BeepDM services:

```csharp
public class DataOperationPlugin : IShellPlugin
{
    private IDMEEditor? _editor;

    public void Initialize(IDMEEditor editor)
    {
        _editor = editor;
    }

    private void PerformOperation()
    {
        // Access configuration
        var config = _editor.ConfigEditor;
        
        // Access data sources
        var dataSources = _editor.DataSources;
        
        // Execute queries
        var result = _editor.RunQuery("SELECT * FROM Users");
        
        // Load assemblies
        var assemblies = _editor.assemblyHandler.GetAllAssemblies();
        
        // Log messages
        _editor.Logger?.WriteLog("Operation completed");
    }
}
```

---

## 📋 Plugin Best Practices

### 1. Resource Management

```csharp
public void Cleanup()
{
    // Dispose of resources
    _connections?.Clear();
    _cache?.Clear();
    
    // Unsubscribe from events
    _eventBus?.UnsubscribeAll();
    
    // Release references
    _editor = null;
}
```

### 2. Error Handling

```csharp
public void OnLoad()
{
    try
    {
        InitializeResources();
    }
    catch (Exception ex)
    {
        _editor?.Logger?.WriteLog($"[{PluginId}] Load error: {ex.Message}");
        throw; // Re-throw to signal load failure
    }
}
```

### 3. Versioning

```csharp
public class VersionedPlugin : IShellPlugin
{
    public string Version => "1.2.0";
    
    public void OnLoad()
    {
        // Check compatibility
        var shellVersion = Assembly.GetEntryAssembly()?.GetName().Version;
        if (shellVersion?.Major < 1)
        {
            throw new InvalidOperationException(
                $"Plugin requires BeepShell v1.0+, found v{shellVersion}");
        }
    }
}
```

### 4. Configuration

```csharp
public class ConfigurablePlugin : IShellPlugin
{
    private PluginConfig? _config;

    public void OnLoad()
    {
        // Load plugin-specific configuration
        var configPath = Path.Combine(
            _editor.ConfigEditor.ExePath, 
            "Plugins", 
            $"{PluginId}.json");
            
        if (File.Exists(configPath))
        {
            _config = JsonSerializer.Deserialize<PluginConfig>(
                File.ReadAllText(configPath));
        }
    }
}
```

---

## 🧪 Testing Your Plugin

### Unit Testing

```csharp
[Fact]
public void Plugin_ShouldLoadSuccessfully()
{
    // Arrange
    var mockEditor = new Mock<IDMEEditor>();
    var plugin = new MyPlugin();
    
    // Act
    plugin.Initialize(mockEditor.Object);
    plugin.OnLoad();
    
    // Assert
    Assert.Equal("my-custom-plugin", plugin.PluginId);
    Assert.True(plugin.SupportsHotReload);
}

[Fact]
public void Command_ShouldExecuteWithoutError()
{
    // Arrange
    var mockEditor = new Mock<IDMEEditor>();
    mockEditor.Setup(e => e.DataSources).Returns(new List<IDataSource>());
    
    var command = new MyCustomCommand(mockEditor.Object);
    
    // Act & Assert
    var cmd = command.BuildCommand();
    Assert.NotNull(cmd);
    Assert.Equal("mycmd", cmd.Name);
}
```

### Integration Testing

```bash
# Test plugin loading
plugin load --path MyPlugin.dll

# Verify command registration
help mycmd

# Test command execution
mycmd --name Test

# Test hot-reload
# (Make changes to plugin)
plugin reload --id my-custom-plugin

# Test unload
plugin unload --id my-custom-plugin
```

---

## 📚 Plugin Examples

### Example 1: Database Backup Plugin

```csharp
public class BackupCommand : IShellCommand
{
    public string CommandName => "backup";

    public Command BuildCommand()
    {
        var cmd = new Command("backup", "Backup database");
        var dsOption = new Option<string>("--datasource", "Data source name");
        cmd.AddOption(dsOption);
        cmd.SetHandler((ds) => BackupDatabase(ds), dsOption);
        return cmd;
    }

    private void BackupDatabase(string dataSourceName)
    {
        var ds = _editor.GetDataSource(dataSourceName);
        // Implement backup logic
    }
}
```

### Example 2: Data Validation Workflow

```csharp
public class ValidationWorkflow : IShellWorkflow
{
    public string WorkflowId => "data-validation";
    public string Name => "Data Validation";
    public string Description => "Validates data quality";

    public async Task<WorkflowResult> ExecuteAsync(
        WorkflowContext context)
    {
        // Implement validation logic
        var errors = await ValidateDataAsync(context);
        
        return new WorkflowResult
        {
            Success = errors.Count == 0,
            Message = $"Validation complete: {errors.Count} errors found"
        };
    }
}
```

---

## 🔧 Configuration

### Profile Configuration

BeepShell supports multiple profiles stored in `Profiles/` directory:

```
BeepShell/bin/Debug/net8.0/
├── Config.json              # Default profile
├── installed_drivers.json   # Driver tracker
├── ConnectionDrivers/       # Installed drivers
└── Profiles/
    ├── dev/
    │   └── Config.json
    ├── staging/
    │   └── Config.json
    └── production/
        └── Config.json
```

### Environment Variables

```powershell
# Set custom config path
$env:BEEP_CONFIG_PATH = "C:\MyConfigs\BeepDM"

# Set default profile
$env:BEEP_PROFILE = "production"

# Run with environment settings
.\BeepShell.exe
```

---

## 🐛 Troubleshooting

### Plugin Won't Load

```bash
# Check plugin path
plugin list

# Check for errors in logs
cat BeepDM.log

# Verify plugin implements IShellPlugin
# Ensure .NET 8.0 target framework
# Check assembly dependencies
```

### Driver Installation Issues

```bash
# Clean driver cache
driver clean

# Restart BeepShell
exit

# Reinstall driver with specific version
driver install
> Search NuGet.org
> Select package and version
```

### Hot-Reload Not Working

```csharp
// Ensure plugin supports hot-reload
public bool SupportsHotReload => true;

// Implement PrepareUnloadAsync
public async Task<bool> PrepareUnloadAsync()
{
    // Clean up resources
    return true; // Must return true to allow unload
}
```

---

## 📖 API Reference

### IShellPlugin Interface

```csharp
public interface IShellPlugin
{
    string PluginId { get; }
    string ExtensionName { get; }
    string Version { get; }
    bool SupportsHotReload { get; }
    
    void Initialize(IDMEEditor editor);
    void OnLoad();
    void OnUnload();
    void Cleanup();
    
    Task<bool> PrepareUnloadAsync();
    Task OnReloadAsync();
    
    IEnumerable<IShellCommand> GetCommands();
    IEnumerable<IShellWorkflow> GetWorkflows();
}
```

### IShellCommand Interface

```csharp
public interface IShellCommand
{
    string CommandName { get; }
    Command BuildCommand();
    string[] GetExamples();
}
```

### IShellWorkflow Interface

```csharp
public interface IShellWorkflow
{
    string WorkflowId { get; }
    string Name { get; }
    string Description { get; }
    
    Task<WorkflowResult> ExecuteAsync(WorkflowContext context);
}
```

---

## 🤝 Contributing

We welcome contributions! To create a plugin:

1. Fork the repository
2. Create a feature branch
3. Add your plugin to `Plugins/` directory
4. Update documentation
5. Submit a pull request

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.

---

## 🔗 Resources

- **BeepDM Documentation**: [GitHub Repository](https://github.com/The-Tech-Idea/BeepDM)
- **NuGet Package Gallery**: [nuget.org](https://www.nuget.org/)
- **System.CommandLine**: [Microsoft Docs](https://learn.microsoft.com/en-us/dotnet/standard/commandline/)
- **Spectre.Console**: [spectreconsole.net](https://spectreconsole.net/)

---

## 💡 Support

For questions and support:
- Open an issue on GitHub
- Check existing documentation
- Review plugin examples in `BeepShell.Shared/`

**Happy Coding! 🚀**
