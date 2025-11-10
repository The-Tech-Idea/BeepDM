# BeepShell Extensibility Enhancements

## Overview

BeepShell's extensibility system has been significantly enhanced with advanced features for plugin management, configuration, events, and developer experience. This document outlines all the new capabilities added to make BeepShell a powerful, flexible platform for building custom data management tools.

## What's New

### 🔌 Hot-Reloadable Plugins

Extensions can now be loaded, unloaded, and reloaded **without restarting BeepShell** using `AssemblyLoadContext` isolation.

**Key Features:**
- True memory cleanup when unloading
- Isolated plugin contexts prevent conflicts
- Real-time plugin updates during development
- Plugin health monitoring

**Usage:**
```bash
# Load a plugin dynamically
beep> plugin load C:\MyExtensions\MyPlugin.dll

# Reload after making changes
beep> plugin reload myplugin-id

# Unload when done
beep> plugin unload myplugin-id

# Check health status
beep> plugin health myplugin-id
```

**Implementing a Hot-Reloadable Plugin:**
```csharp
using BeepShell.Infrastructure;

public class MyHotReloadablePlugin : IShellPlugin
{
    public string PluginId => "myplugin-id";
    public string AssemblyPath { get; set; }
    public bool SupportsHotReload => true;
    
    public async Task<bool> PrepareUnloadAsync()
    {
        // Save state before unload
        await SaveStateAsync();
        return true;
    }
    
    public async Task OnReloadAsync()
    {
        // Restore state after reload
        await RestoreStateAsync();
    }
    
    public PluginHealthStatus GetHealthStatus()
    {
        return new PluginHealthStatus
        {
            IsHealthy = true,
            Status = "Running",
            Metrics = new Dictionary<string, object>
            {
                ["commandsExecuted"] = _commandCount,
                ["uptime"] = DateTime.Now - _startTime
            }
        };
    }
    
    // ... other IShellExtension members
}
```

### 🎯 Extension Lifecycle Hooks

Extensions now have fine-grained lifecycle management with multiple hooks:

```csharp
public interface IShellExtension
{
    void Initialize(IDMEEditor editor);      // Called first
    void OnLoad();                           // After successful init
    void OnUnload();                         // Before unload
    void OnConfigurationChanged();           // When config changes
    void Cleanup();                          // Final cleanup
}
```

**Example:**
```csharp
[ShellExtension(
    Name = "My Extension",
    Version = "1.0.0",
    ConfigFileName = "myext.config.json"
)]
public class MyExtension : IShellExtension
{
    private IExtensionConfig _config;
    
    public void Initialize(IDMEEditor editor)
    {
        // Load configuration
        var configPath = Path.Combine(/* extension config dir */, "myext.config.json");
        _config = new ExtensionConfig(configPath);
        _config.Load();
    }
    
    public void OnLoad()
    {
        // Extension loaded successfully
        Console.WriteLine("Extension loaded!");
    }
    
    public void OnConfigurationChanged()
    {
        // Reload settings when BeepShell config changes
        _config.Load();
    }
    
    public void OnUnload()
    {
        // Save pending work before unload
        _config.Save();
    }
}
```

### ⚙️ Extension Configuration System

Extensions can now have their own JSON configuration files with type-safe access:

**Dictionary-Based Config:**
```csharp
var config = new ExtensionConfig("path/to/config.json");
config.Load();

var batchSize = config.GetValue<int>("batchSize", 1000);
config.SetValue("lastRun", DateTime.Now);
config.Save();
```

**Strongly-Typed Config:**
```csharp
public class MyExtensionSettings
{
    public int BatchSize { get; set; } = 1000;
    public string DefaultFormat { get; set; } = "csv";
    public bool Enabled { get; set; } = true;
}

var config = new ExtensionConfig<MyExtensionSettings>("config.json");
config.Load();

// Access typed properties
var batchSize = config.Config.BatchSize;

// Update with lambda
config.Update(settings => {
    settings.BatchSize = 2000;
    settings.LastRun = DateTime.Now;
});
```

### 📡 Event Bus System

Extensions can now communicate via a pub/sub event bus:

**Subscribe to Events:**
```csharp
public class MyExtension : IShellExtension
{
    public void Initialize(IDMEEditor editor)
    {
        var eventBus = /* get from shell */;
        
        // Subscribe to command execution
        eventBus.Subscribe(ShellEventType.CommandExecuted, args =>
        {
            var command = args.GetData<string>("command");
            var duration = args.GetData<TimeSpan>("duration");
            Console.WriteLine($"Command {command} took {duration}");
        });
        
        // Subscribe to connection events
        eventBus.OnConnectionOpened(connectionName =>
        {
            Console.WriteLine($"Connected to {connectionName}");
        });
        
        // Subscribe to config changes
        eventBus.OnConfigurationChanged(() =>
        {
            this.OnConfigurationChanged();
        });
    }
}
```

**Publish Custom Events:**
```csharp
await eventBus.PublishAsync(ShellEventType.ExtensionLoaded, new ShellEventArgs
{
    Data = 
    {
        ["extensionName"] = "MyExtension",
        ["version"] = "1.0.0"
    }
});
```

**Available Event Types:**
- `ShellStarted` - Shell initialization complete
- `ShellStopping` - Shell shutting down
- `CommandExecuting` - Before command runs
- `CommandExecuted` - After successful command
- `CommandFailed` - Command failed
- `ConnectionOpened` - Data source connected
- `ConnectionClosed` - Data source disconnected
- `ConfigurationChanged` - Configuration reloaded
- `ProfileSwitched` - Profile changed
- `ExtensionLoaded` - Extension loaded
- `ExtensionUnloaded` - Extension unloaded
- `PluginReloaded` - Plugin reloaded

### 🎨 Interactive Prompt Helpers

Reusable Spectre.Console utilities for building interactive commands:

```csharp
using BeepShell.Infrastructure;

public class MyCommand : IShellCommand
{
    public Command BuildCommand()
    {
        var cmd = new Command("mycommand");
        
        cmd.SetHandler(async () =>
        {
            // Text input with validation
            var name = ShellPrompts.PromptText("Enter name", "default");
            
            // Password input
            var password = ShellPrompts.PromptSecret("Enter password");
            
            // Confirmation
            if (!ShellPrompts.PromptConfirm("Continue?", true))
                return;
            
            // Select from list
            var datasource = ShellPrompts.PromptDataSource(_editor);
            
            // Select table/entity
            var table = ShellPrompts.PromptEntity(_editor, datasource);
            
            // File path with validation
            var outputFile = ShellPrompts.PromptFilePath("Output file");
            
            // Progress bar
            var result = await ShellPrompts.WithProgressAsync("Exporting data", async ctx =>
            {
                var task = ctx.AddTask("Exporting", maxValue: 100);
                // ... do work, update task.Increment()
                return result;
            });
            
            // Display table
            ShellPrompts.DisplayTable("Results", 
                columns: new[] { "ID", "Name", "Status" },
                rows: data
            );
            
            // Display panel
            ShellPrompts.DisplayPanel("Success", "Operation completed!", Color.Green);
        });
        
        return cmd;
    }
}
```

### 🏷️ Extension Metadata Attributes

Use attributes for cleaner extension discovery and documentation:

```csharp
[ShellExtension(
    Name = "Data Tools Extension",
    Version = "1.0.0",
    Author = "BeepDM Community",
    Description = "Import/export and sync utilities",
    Dependencies = new[] { "RequiredExtension" },
    MinShellVersion = "1.0.0",
    ConfigFileName = "datatools.config.json"
)]
public class DataToolsExtension : IShellExtension
{
    // ...
}

[ShellCommand(
    Name = "export",
    Description = "Export table data",
    Category = "Data",
    Aliases = new[] { "exp", "e" },
    RequiresConnection = true
)]
public class ExportCommand : IShellCommand
{
    public string[] Aliases => new[] { "exp", "e" };
    // ...
}
```

### 🔗 Command Aliases

Users and extensions can create command shortcuts:

```bash
# Create alias
beep> alias exp export
beep> alias ls datasources

# Use alias
beep> exp --source mydb --table users --output users.csv

# View all aliases
beep> alias

# Clear custom aliases
beep> alias clear
```

**Built-in Aliases:**
- `ls` → `datasources`
- `quit`, `q` → `exit`
- `h` → `help`
- `stat` → `status`

### 📦 Extension Manifest

Package extensions with a manifest for marketplace distribution:

**extension.manifest.json:**
```json
{
  "id": "beepshell.extensions.myextension",
  "name": "My Extension",
  "version": "1.0.0",
  "author": "Your Name",
  "description": "Extension description",
  "homepage": "https://github.com/yourname/myextension",
  "license": "MIT",
  "keywords": ["data", "export", "import"],
  "category": "Data Management",
  "minShellVersion": "1.0.0",
  "dependencies": {
    "OtherExtension": "^2.0.0"
  },
  "assembly": {
    "fileName": "MyExtension.dll",
    "targetFramework": "net8.0",
    "runtimeDependencies": [
      "CsvHelper",
      "Newtonsoft.Json"
    ]
  },
  "commands": [
    {
      "name": "export",
      "description": "Export data to files",
      "category": "Data",
      "aliases": ["exp"],
      "examples": [
        "export --source db --table users --output users.csv"
      ]
    }
  ],
  "workflows": [
    {
      "name": "sync",
      "description": "Synchronize data",
      "category": "ETL",
      "parameters": [
        {
          "name": "source",
          "type": "string",
          "required": true,
          "description": "Source connection"
        }
      ]
    }
  ],
  "configuration": {
    "fileName": "myext.config.json",
    "schema": {
      "batchSize": {
        "type": "integer",
        "default": 1000
      }
    }
  },
  "changelog": [
    {
      "version": "1.0.0",
      "date": "2025-11-10",
      "changes": [
        "Initial release"
      ]
    }
  ]
}
```

**Validate Manifest:**
```csharp
var manifest = ExtensionManifest.Load("extension.manifest.json");
var validator = new ExtensionValidator();

if (validator.Validate(manifest))
{
    Console.WriteLine("Manifest is valid!");
}
else
{
    foreach (var error in validator.Errors)
        Console.WriteLine($"Error: {error}");
}
```

## Shell Commands

### Plugin Management

```bash
# List loaded plugins
plugin list

# Load plugin
plugin load C:\Extensions\MyPlugin.dll

# Unload plugin
plugin unload plugin-id

# Reload plugin (hot-reload)
plugin reload plugin-id

# Check health
plugin health
plugin health plugin-id
```

### Event Monitoring

```bash
# Show event subscribers
events
```

### Alias Management

```bash
# Show aliases
alias

# Create alias
alias myalias mycommand

# Clear aliases
alias clear
```

## Migration Guide

### From Basic Extension to Full-Featured Extension

**Before:**
```csharp
public class MyExtension : IShellExtension
{
    public void Initialize(IDMEEditor editor) { }
    public void Cleanup() { }
}
```

**After:**
```csharp
[ShellExtension(
    Name = "My Extension",
    Version = "2.0.0",
    ConfigFileName = "myext.config.json"
)]
public class MyExtension : IShellExtension, IShellPlugin
{
    private IExtensionConfig _config;
    private IShellEventBus _eventBus;
    
    public string PluginId => "myext-v2";
    public bool SupportsHotReload => true;
    
    public void Initialize(IDMEEditor editor)
    {
        // Load config
        _config = new ExtensionConfig("myext.config.json");
        _config.Load();
        
        // Subscribe to events
        _eventBus = /* get from shell context */;
        _eventBus.OnCommandExecuted((cmd, duration) => 
        {
            Log($"{cmd} executed in {duration}");
        });
    }
    
    public void OnLoad()
    {
        Console.WriteLine("Extension loaded!");
    }
    
    public async Task<bool> PrepareUnloadAsync()
    {
        _config.Save();
        return true;
    }
    
    public PluginHealthStatus GetHealthStatus()
    {
        return new PluginHealthStatus { IsHealthy = true };
    }
}
```

## Best Practices

### 1. Use Configuration Files
Store settings in JSON files instead of hardcoding:
```csharp
_config.SetValue("apiKey", apiKey);
_config.Save();
```

### 2. Subscribe to Events
React to shell events for better integration:
```csharp
eventBus.OnConnectionOpened(name => InitializeForConnection(name));
```

### 3. Implement Health Checks
Provide health status for monitoring:
```csharp
public PluginHealthStatus GetHealthStatus()
{
    return new PluginHealthStatus
    {
        IsHealthy = _isConnected && _errorCount == 0,
        Warnings = _warnings,
        Metrics = { ["requestCount"] = _requestCount }
    };
}
```

### 4. Use Lifecycle Hooks
Clean up resources properly:
```csharp
public void OnUnload()
{
    _httpClient?.Dispose();
    _config.Save();
}
```

### 5. Create Manifests
Document your extension for distribution:
```bash
Create extension.manifest.json with metadata
```

## Examples

See `BeepShell.Extensions.Example` for complete working examples demonstrating:
- Extension with configuration
- Lifecycle hooks usage
- Event subscriptions
- Interactive prompts
- Hot-reload support
- Extension manifest

## Troubleshooting

### Plugin Won't Load
- Check manifest validity
- Verify target framework compatibility
- Check logs for detailed errors

### Hot-Reload Not Working
- Ensure `SupportsHotReload` returns `true`
- Implement `PrepareUnloadAsync` properly
- Check for static state or cached references

### Configuration Not Persisting
- Call `config.Save()` after changes
- Check file permissions
- Verify config path is correct

## Summary of Enhancements

✅ Hot-reloadable plugins with `IShellPlugin`  
✅ Extension lifecycle hooks (`OnLoad`, `OnUnload`, `OnConfigurationChanged`)  
✅ Extension configuration system (`IExtensionConfig`)  
✅ Event bus for extension communication (`IShellEventBus`)  
✅ Interactive prompt helpers (`ShellPrompts`)  
✅ Extension metadata attributes (`[ShellExtension]`, `[ShellCommand]`)  
✅ Command aliases and shortcuts  
✅ Extension manifest schema for packaging  
✅ Plugin health monitoring  
✅ Comprehensive documentation and examples  

BeepShell is now a fully extensible, enterprise-ready platform for building custom data management tools!
