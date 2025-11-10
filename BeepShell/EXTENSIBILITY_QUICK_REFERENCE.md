# BeepShell Extensibility Quick Reference

## Extension Types

### Basic Extension
```csharp
public class MyExtension : IShellExtension
{
    public string ExtensionName => "My Extension";
    public string Version => "1.0.0";
    public string Author => "Author Name";
    public string Description => "Description";
    public string[] Dependencies => Array.Empty<string>();
    
    public void Initialize(IDMEEditor editor) { }
    public IEnumerable<IShellCommand> GetCommands() => _commands;
    public IEnumerable<IShellWorkflow> GetWorkflows() => _workflows;
    public void Cleanup() { }
}
```

### Hot-Reloadable Plugin
```csharp
public class MyPlugin : IShellPlugin
{
    public string PluginId => "myplugin-id";
    public string AssemblyPath { get; set; }
    public bool SupportsHotReload => true;
    
    public async Task<bool> PrepareUnloadAsync() { /* save state */ return true; }
    public async Task OnReloadAsync() { /* restore state */ }
    public PluginHealthStatus GetHealthStatus() { /* return status */ }
    
    // ... IShellExtension members
}
```

### Shell Command
```csharp
[ShellCommand(Name = "export", Category = "Data", Aliases = new[] { "exp" })]
public class ExportCommand : IShellCommand
{
    public string CommandName => "export";
    public string Description => "Export data";
    public string Category => "Data";
    public string Version => "1.0.0";
    public string Author => "Author";
    public string[] Aliases => new[] { "exp" };
    
    public void Initialize(IDMEEditor editor) { _editor = editor; }
    public Command BuildCommand() { /* build System.CommandLine.Command */ }
    public bool CanExecute() => true;
    public string[] GetExamples() => new[] { "export --source db --table users" };
    public bool OnBeforeExecute() => true;
    public void OnAfterExecute() { }
}
```

## Configuration

### Dictionary Config
```csharp
var config = new ExtensionConfig("config.json");
config.Load();
config.SetValue("key", value);
var value = config.GetValue<T>("key", defaultValue);
config.Save();
```

### Typed Config
```csharp
public class MySettings { public int BatchSize { get; set; } }
var config = new ExtensionConfig<MySettings>("config.json");
config.Load();
var size = config.Config.BatchSize;
config.Update(s => s.BatchSize = 2000);
```

## Events

### Subscribe
```csharp
eventBus.Subscribe(ShellEventType.CommandExecuted, args => { });
eventBus.OnCommandExecuted((cmd, duration) => { });
eventBus.OnConnectionOpened(name => { });
eventBus.OnConfigurationChanged(() => { });
```

### Publish
```csharp
await eventBus.PublishAsync(ShellEventType.ExtensionLoaded, new ShellEventArgs
{
    Data = { ["key"] = value }
});
```

## Prompts

```csharp
// Text
var text = ShellPrompts.PromptText("Enter value", "default");

// Password
var password = ShellPrompts.PromptSecret("Password");

// Confirm
var confirmed = ShellPrompts.PromptConfirm("Continue?");

// Selection
var choice = ShellPrompts.PromptSelection("Select", choices);
var multiple = ShellPrompts.PromptMultiSelection("Select multiple", choices);

// Data source
var ds = ShellPrompts.PromptDataSource(editor);
var entity = ShellPrompts.PromptEntity(editor, dsName);

// Files
var file = ShellPrompts.PromptFilePath("File", mustExist: true);
var dir = ShellPrompts.PromptDirectoryPath("Directory");

// Progress
var result = await ShellPrompts.WithProgressAsync("Working", async ctx => { });

// Status
var result = await ShellPrompts.WithStatusAsync("Processing", async ctx => { });

// Display
ShellPrompts.DisplayTable("Title", columns, rows);
ShellPrompts.DisplayPanel("Title", "Content", Color.Green);
ShellPrompts.DisplayRule("Separator");
```

## Shell Commands

```bash
# Assembly Management (Base Commands)
assembly list [--verbose]           # List assemblies
assembly load <path>                # Load assembly
assembly unload <name>              # Unload assembly
assembly scan [--all]               # Scan assemblies
assembly types [--interface <name>] # List types
assembly drivers                    # List data drivers
assembly extensions                 # List loader extensions
assembly create <typename>          # Create instance
assembly nugget load <path>         # Load nugget package
asm                                 # Alias for 'assembly'

# Extensions
extensions              # List extensions
workflows              # List workflows

# Plugins
plugin list            # List plugins
plugin load <path>     # Load plugin
plugin unload <id>     # Unload plugin
plugin reload <id>     # Reload plugin
plugin health [id]     # Health status

# Events
events                 # Show subscribers

# Aliases
alias                  # List aliases
alias name command     # Create alias
alias clear           # Reset aliases

# Standard
status                 # Session stats
connections           # Open connections
datasources           # Data sources
history               # Command history
profile               # Current profile
reload                # Reload config
```

## Manifest Template

```json
{
  "id": "extension.id",
  "name": "Extension Name",
  "version": "1.0.0",
  "author": "Author",
  "description": "Description",
  "minShellVersion": "1.0.0",
  "assembly": {
    "fileName": "Extension.dll",
    "targetFramework": "net8.0"
  },
  "commands": [],
  "workflows": [],
  "configuration": {
    "fileName": "config.json"
  }
}
```

## Lifecycle Hooks

```csharp
Initialize(editor)         // First - set up
OnLoad()                   // After init success
OnConfigurationChanged()   // Config reload
OnUnload()                 // Before unload
Cleanup()                  // Final cleanup
```

## Event Types

- `ShellStarted` - Shell ready
- `ShellStopping` - Shutting down
- `CommandExecuting` - Before command
- `CommandExecuted` - After command
- `CommandFailed` - Command error
- `ConnectionOpened` - Connected
- `ConnectionClosed` - Disconnected
- `ConfigurationChanged` - Config reload
- `ProfileSwitched` - Profile changed
- `ExtensionLoaded` - Extension loaded
- `ExtensionUnloaded` - Extension unloaded
- `PluginReloaded` - Plugin reloaded

## Attributes

```csharp
[ShellExtension(
    Name = "Name",
    Version = "1.0.0",
    Author = "Author",
    Description = "Desc",
    Dependencies = new[] { "Other" },
    MinShellVersion = "1.0.0",
    ConfigFileName = "config.json"
)]

[ShellCommand(
    Name = "cmd",
    Description = "Desc",
    Category = "Category",
    Aliases = new[] { "alias" },
    RequiresConnection = true
)]
```

## Best Practices

✅ Use configuration files  
✅ Subscribe to events for reactivity  
✅ Implement health checks  
✅ Clean up in OnUnload/Cleanup  
✅ Create manifests for distribution  
✅ Use prompts for interactivity  
✅ Support hot-reload when possible  
✅ Document with examples  
✅ Handle errors gracefully  
✅ Log important operations  

## Common Patterns

### Command with Config
```csharp
public class MyCommand : IShellCommand
{
    private IExtensionConfig _config;
    
    public void Initialize(IDMEEditor editor)
    {
        _config = new ExtensionConfig("config.json");
        _config.Load();
    }
    
    public Command BuildCommand()
    {
        var cmd = new Command("mycommand");
        cmd.SetHandler(() =>
        {
            var setting = _config.GetValue<int>("setting", 100);
            // use setting
        });
        return cmd;
    }
}
```

### Extension with Events
```csharp
public class MyExtension : IShellExtension
{
    public void Initialize(IDMEEditor editor)
    {
        var eventBus = /* get event bus */;
        eventBus.OnCommandExecuted((cmd, duration) =>
        {
            _stats.RecordCommand(cmd, duration);
        });
    }
}
```

### Plugin with Health
```csharp
public class MyPlugin : IShellPlugin
{
    private int _errorCount = 0;
    private DateTime _startTime;
    
    public PluginHealthStatus GetHealthStatus()
    {
        return new PluginHealthStatus
        {
            IsHealthy = _errorCount < 10,
            Status = _errorCount == 0 ? "Healthy" : "Degraded",
            Warnings = _errorCount > 0 ? new List<string> { $"{_errorCount} errors" } : new(),
            Metrics = new Dictionary<string, object>
            {
                ["uptime"] = DateTime.Now - _startTime,
                ["errorCount"] = _errorCount
            }
        };
    }
}
```

## Deployment

1. Build extension DLL
2. Create `extension.manifest.json`
3. Copy to BeepDM folder (ProjectClass/Addin/OtherDLL) or
4. Use `plugin load <path>` for hot-reload

## Resources

- `BeepShell.Extensions.Example` - Working examples
- `EXTENSIBILITY_ENHANCEMENTS.md` - Complete guide
- `EXTENSION_DEVELOPMENT.md` - Original documentation
- Shell help: `help`, `extensions`, `workflows`, `plugin`
