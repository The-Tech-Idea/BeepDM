# BeepShell Extension Development Guide

## Overview

BeepShell is designed to be highly extensible. You can create custom commands and workflows that are automatically discovered and loaded when BeepShell starts. This is achieved by implementing specific interfaces and leveraging the existing `AssemblyHandler.ScanExtensions()` mechanism from BeepDM.

## Extension Architecture

BeepShell supports three types of extensions:

1. **IShellCommand** - Individual commands (like `export`, `import`, `analyze`)
2. **IShellWorkflow** - Complex multi-step operations (like ETL pipelines, migrations)
3. **IShellExtension** - Extension providers that bundle multiple commands and workflows

All extensions are discovered automatically through reflection when BeepShell initializes.

## How Extension Discovery Works

1. **On Startup**: BeepShell creates a `ShellExtensionScanner` that implements `ILoaderExtention`
2. **Assembly Scanning**: The scanner iterates through all loaded assemblies in `AssemblyHandler.LoadedAssemblies`
3. **Type Discovery**: It searches for classes implementing `IShellCommand`, `IShellWorkflow`, or `IShellExtension`
4. **Instantiation**: Found types are instantiated and initialized with the persistent `DMEEditor` instance
5. **Registration**: Commands are added to the System.CommandLine `RootCommand` for execution

## Creating a Shell Command Extension

### 1. Create a New Class Library Project

```bash
dotnet new classlib -n MyBeepShellExtension
dotnet add reference ../BeepShell/BeepShell.csproj
dotnet add reference ../DataManagementEngineStandard/DataManagementEngine.csproj
dotnet add package System.CommandLine -v 2.0.0-beta4.22272.1
dotnet add package Spectre.Console -v 0.49.1
```

### 2. Implement IShellCommand

```csharp
using System.CommandLine;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.Editor;

namespace MyBeepShellExtension
{
    public class AnalyzeCommand : IShellCommand
    {
        private IDMEEditor _editor;

        // Required properties
        public string CommandName => "analyze";
        public string Description => "Analyze data source statistics";
        public string Category => "Analysis";
        public string Version => "1.0.0";
        public string Author => "Your Name";

        // Called when shell starts
        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
        }

        // Build the System.CommandLine Command
        public Command BuildCommand()
        {
            var command = new Command("analyze", Description);

            var sourceOption = new Option<string>(
                aliases: new[] { "--source", "-s" },
                description: "Data source to analyze"
            ) { IsRequired = true };

            command.AddOption(sourceOption);

            command.SetHandler(async (source) =>
            {
                await ExecuteAnalysis(source);
            }, sourceOption);

            return command;
        }

        private async Task ExecuteAnalysis(string sourceName)
        {
            var ds = _editor.GetDataSource(sourceName);
            if (ds == null)
            {
                AnsiConsole.MarkupLine($"[red]Data source not found[/]");
                return;
            }

            // Your analysis logic here
            AnsiConsole.MarkupLine($"[green]Analyzing {sourceName}...[/]");
            // ...
        }

        public bool CanExecute() => _editor != null;

        public string[] GetExamples()
        {
            return new[] 
            { 
                "analyze --source mydb",
                "analyze -s production" 
            };
        }
    }
}
```

### 3. Build and Deploy

```bash
dotnet build
# Copy DLL to one of these BeepDM folders:
# - ProjectClass
# - Addin
# - OtherDLL
```

The extension will be automatically discovered on next BeepShell launch!

## Creating a Workflow Extension

Workflows are for complex multi-step operations with progress tracking:

```csharp
using BeepShell.Infrastructure;
using Spectre.Console;

public class BackupWorkflow : IShellWorkflow
{
    private IDMEEditor _editor;

    public string WorkflowName => "backup";
    public string Description => "Backup database to file";
    public string Category => "Admin";

    public void Initialize(IDMEEditor editor)
    {
        _editor = editor;
    }

    public async Task<WorkflowResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var result = new WorkflowResult();
        
        try
        {
            var source = parameters["source"].ToString();
            var outputPath = parameters["outputPath"].ToString();

            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Backing up...[/]");
                    
                    // Step 1
                    task.Description = "Connecting...";
                    // ... your code
                    task.Increment(25);

                    // Step 2
                    task.Description = "Exporting data...";
                    // ... your code
                    task.Increment(50);

                    // Step 3
                    task.Description = "Compressing...";
                    // ... your code
                    task.Increment(25);
                });

            result.Success = true;
            result.Message = "Backup completed";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    public bool ValidateParameters(Dictionary<string, object> parameters)
    {
        return parameters.ContainsKey("source") && 
               parameters.ContainsKey("outputPath");
    }

    public List<WorkflowParameter> GetRequiredParameters()
    {
        return new List<WorkflowParameter>
        {
            new() { Name = "source", ParameterType = typeof(string), Required = true },
            new() { Name = "outputPath", ParameterType = typeof(string), Required = true }
        };
    }
}
```

## Creating an Extension Provider

Bundle multiple commands and workflows together:

```csharp
using BeepShell.Infrastructure;

public class MyToolsExtension : IShellExtension
{
    private readonly List<IShellCommand> _commands = new();
    private readonly List<IShellWorkflow> _workflows = new();

    public string ExtensionName => "My Tools";
    public string Version => "1.0.0";
    public string Author => "Your Name";

    public void Initialize(IDMEEditor editor)
    {
        // Register your commands
        var analyzeCmd = new AnalyzeCommand();
        analyzeCmd.Initialize(editor);
        _commands.Add(analyzeCmd);

        // Register your workflows
        var backupWf = new BackupWorkflow();
        backupWf.Initialize(editor);
        _workflows.Add(backupWf);
    }

    public IEnumerable<IShellCommand> GetCommands() => _commands;
    public IEnumerable<IShellWorkflow> GetWorkflows() => _workflows;

    public void Cleanup()
    {
        _commands.Clear();
        _workflows.Clear();
    }
}
```

## Extension Benefits

### ✅ Automatic Discovery
- No manual registration required
- Just implement interface and drop DLL in folder
- Works with BeepDM's existing folder scanning

### ✅ Persistent DMEEditor Access
- Extensions receive the same `DMEEditor` instance used throughout shell session
- Access to open connections, loaded assemblies, configuration
- No recreation overhead

### ✅ Full System.CommandLine Integration
- Build rich CLI commands with options, arguments, subcommands
- Automatic help generation
- Tab completion support (future)

### ✅ Spectre.Console UI
- Beautiful terminal output with tables, progress bars, spinners
- Markup for colored text
- Interactive prompts

## Example Extension Project

See the `BeepShell.Extensions.Example` project for complete working examples:

- **ExportCommand** - Export table data to CSV
- **ImportCommand** - Import CSV data to table
- **DataSyncWorkflow** - ETL workflow to sync between sources
- **DataToolsExtension** - Extension provider bundling all of the above

## Shell Commands for Extensions

Once extensions are loaded, use these shell commands:

```bash
# List all loaded extensions
beep> extensions

# List all workflows
beep> workflows

# Get help for your custom command
beep> help export

# Execute your command
beep> export --source mydb --table customers --output data.csv

# Check loaded commands count
beep> status
```

## Deployment

### Option 1: BeepDM Folders
Copy your extension DLL to any BeepDM folder configured in `Beep.config.json`:
- `ProjectClass`
- `Addin`
- `OtherDLL`

### Option 2: Project Reference
Add your extension project as a reference to BeepShell:

```xml
<ProjectReference Include="..\MyBeepShellExtension\MyBeepShellExtension.csproj" />
```

### Option 3: NuGet Package
Package your extension as NuGet and reference it in BeepShell.csproj.

## Best Practices

1. **Error Handling**: Always wrap operations in try-catch and provide clear error messages
2. **Validation**: Implement `CanExecute()` to check prerequisites
3. **Examples**: Provide usage examples in `GetExamples()`
4. **Progress Feedback**: Use `AnsiConsole.Status()` or `AnsiConsole.Progress()` for long operations
5. **Resource Cleanup**: Implement proper disposal in `IShellExtension.Cleanup()`
6. **Naming**: Use clear, verb-based command names (export, import, analyze, sync)
7. **Categories**: Group related commands with consistent Category values

## Advanced Topics

### Accessing BeepDM Services

Your extension has full access to BeepDM infrastructure:

```csharp
public void Initialize(IDMEEditor editor)
{
    _editor = editor;
    
    // Access logger
    _editor.Logger.WriteLog("Extension initialized");
    
    // Access configuration
    var config = _editor.ConfigEditor.Config;
    
    // Access assembly handler
    var assemblies = _editor.AssemblyHandler.LoadedAssemblies;
    
    // Access error handling
    _editor.ErrorObject.Flag = Errors.Ok;
}
```

### Working with Data Sources

```csharp
// Get data source
var ds = _editor.GetDataSource("mydb");

// Open connection (if needed)
if (ds.ConnectionStatus != ConnectionState.Open)
    ds.Openconnection();

// Query data
var data = ds.GetEntity("customers", null);

// Update data
ds.UpdateEntity("customers", modifiedData);

// Execute custom query
var result = ds.RunQuery("SELECT * FROM orders WHERE status = 'pending'");

// Close connection
ds.Closeconnection();
```

### Persisting Extension Configuration

Store extension-specific settings in BeepDM config:

```csharp
public void Initialize(IDMEEditor editor)
{
    _editor = editor;
    
    // Load custom settings
    var settings = _editor.ConfigEditor.ReadAppValues("MyExtension");
    
    // Save settings
    _editor.ConfigEditor.WriteAppValue("MyExtension", "LastExportPath", "/data/exports");
    _editor.ConfigEditor.SaveConfigValues();
}
```

## Troubleshooting

### Extension Not Discovered
1. Verify DLL is in a scanned folder (check `Beep.config.json`)
2. Ensure class implements interface correctly (not abstract, has public constructor)
3. Check BeepShell startup output for load errors
4. Verify assembly references match BeepShell version

### Command Not Appearing
1. Ensure `BuildCommand()` returns valid Command object
2. Check command name doesn't conflict with built-in commands
3. Verify no exceptions in `Initialize()` method

### Runtime Errors
1. Check `_editor` is not null in command execution
2. Validate data sources exist before accessing
3. Handle connection state properly
4. Use `CanExecute()` to prevent invalid execution

## Summary

BeepShell's extension system provides:
- **Zero-friction plugin development** - Implement interface, drop DLL, done
- **Full BeepDM integration** - Access all services, connections, and configuration
- **Modern CLI experience** - System.CommandLine + Spectre.Console
- **Production-ready** - Error handling, validation, progress tracking built-in

Start building your extensions today and share them with the BeepDM community!
