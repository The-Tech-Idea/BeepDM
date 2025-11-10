# BeepShell Extensions Example

This project demonstrates how to create custom commands and workflows for BeepShell using the plugin extensibility system.

## What's Included

### Commands

1. **ExportCommand** (`export`)
   - Exports data from a data source to CSV file
   - Options: --source, --table, --output, --limit
   - Example: `export --source mydb --table customers --output customers.csv`

2. **ImportCommand** (`import`)
   - Imports data from CSV file to a data source
   - Options: --file, --target, --table, --truncate
   - Example: `import --file data.csv --target mydb --table users`

### Workflows

1. **DataSyncWorkflow** (`data-sync`)
   - Synchronizes data between two data sources with progress tracking
   - Parameters: source, sourceTable, target, targetTable, batchSize
   - Example workflow execution from shell

### Extension Provider

1. **DataToolsExtension**
   - Bundles all commands and workflows together
   - Demonstrates how to create an extension provider
   - Single point of initialization and cleanup

## How It Works

### 1. Automatic Discovery

When BeepShell starts, it:
- Creates a `ShellExtensionScanner` instance
- Scans all loaded assemblies through `AssemblyHandler`
- Discovers classes implementing `IShellCommand`, `IShellWorkflow`, or `IShellExtension`
- Instantiates and initializes each extension with the persistent `DMEEditor`

### 2. Command Registration

Each command:
- Implements `IShellCommand` interface
- Receives `DMEEditor` instance in `Initialize()`
- Builds a `System.CommandLine.Command` in `BuildCommand()`
- Gets added to the shell's `RootCommand` automatically

### 3. Execution

When you type a command in BeepShell:
- Input is parsed using `System.CommandLine`
- Command handler is invoked with typed arguments
- Command has access to persistent `DMEEditor` and open connections
- Results displayed using `Spectre.Console`

## Building and Deploying

### Build the Project

```bash
cd BeepShell.Extensions.Example
dotnet build
```

### Deploy to BeepDM

Copy the compiled DLL to any BeepDM scanned folder:

```bash
# Example - copy to ProjectClass folder
copy bin\Debug\net8.0\BeepShell.Extensions.Example.dll C:\BeepDM\ProjectClass\
```

Or update your `Beep.config.json` to add a custom folder:

```json
{
  "Folders": [
    {
      "FolderPath": "C:\\MyExtensions",
      "FolderFilesType": "ProjectClass"
    }
  ]
}
```

### Launch BeepShell

```bash
cd ..\BeepShell
dotnet run
```

You should see:
```
✓ Loaded 2 custom commands, 1 workflows, 1 extensions
```

### Use Your Commands

```bash
beep> help export
beep> export --source mydb --table customers --output customers.csv
beep> import --file customers.csv --target testdb --table imported_customers
beep> extensions
beep> workflows
```

## Key Concepts

### Persistent State

Your extensions benefit from BeepShell's persistent architecture:

- **Same DMEEditor instance** across all commands
- **Open connections** remain open
- **Loaded assemblies** stay in memory
- **No recreation overhead** between commands

### Integration with BeepDM

Extensions have full access to:

```csharp
_editor.DataSources          // All data sources
_editor.ConfigEditor         // Configuration
_editor.Logger              // Logging
_editor.ErrorObject         // Error handling
_editor.AssemblyHandler     // Assembly management
```

### Error Handling

All example commands demonstrate:
- Try-catch blocks for robust error handling
- `AnsiConsole.MarkupLine()` for user-friendly error messages
- Validation before execution
- Graceful cleanup on failure

### Progress Feedback

Workflows show how to use:
- `AnsiConsole.Status()` for spinner/status text
- `AnsiConsole.Progress()` for progress bars
- Task descriptions and incremental updates

## Extending This Example

### Add More Commands

Create new files implementing `IShellCommand`:

```csharp
public class BackupCommand : IShellCommand
{
    // ... implementation
}
```

Then add to `DataToolsExtension.Initialize()`:

```csharp
var backupCmd = new BackupCommand();
backupCmd.Initialize(editor);
_commands.Add(backupCmd);
```

### Add More Workflows

Create new files implementing `IShellWorkflow`:

```csharp
public class MigrationWorkflow : IShellWorkflow
{
    // ... implementation
}
```

### Create Multiple Extensions

You can have multiple `IShellExtension` implementations in the same assembly:

```csharp
public class AdminToolsExtension : IShellExtension { }
public class AnalyticsToolsExtension : IShellExtension { }
public class ETLToolsExtension : IShellExtension { }
```

Each will be discovered and loaded independently.

## Best Practices Demonstrated

1. **Clear Command Naming** - Use verb-based names (export, import, analyze)
2. **Rich Options** - Provide both short (-s) and long (--source) option names
3. **Required vs Optional** - Mark required options explicitly
4. **Validation** - Implement `CanExecute()` for prerequisites
5. **Examples** - Provide usage examples in `GetExamples()`
6. **Categories** - Group related commands with consistent categories
7. **Error Messages** - Clear, actionable error messages
8. **Progress Feedback** - Keep users informed during long operations
9. **Resource Cleanup** - Proper disposal in extension cleanup

## Testing Your Extension

### Manual Testing

```bash
# Test discovery
beep> extensions
# Should show "Data Tools Extension"

beep> workflows  
# Should show "data-sync"

# Test command help
beep> help export
# Should show options and description

# Test execution
beep> config connection list
beep> export -s mydb -t customers -o test.csv
# Should export data

# Test error handling
beep> export -s nonexistent -t table -o file.csv
# Should show clear error message
```

### Integration Testing

Test interaction with BeepShell features:

```bash
# Test with persistent connections
beep> ds test mydb
beep> connections
beep> export -s mydb -t users -o users.csv
beep> connections  # Connection should still be open

# Test with profile switching
beep> profile switch dev
beep> export -s mydb -t users -o users_dev.csv

# Test session tracking
beep> export -s mydb -t users -o users.csv
beep> status  # Should show command in statistics
beep> history  # Should show export in history
```

## Troubleshooting

### Extension Not Loading

1. Check DLL is in correct folder
2. Verify folder is configured in `Beep.config.json`
3. Ensure namespace and class names are correct
4. Check for compilation errors

### Command Not Appearing

1. Verify class implements `IShellCommand` correctly
2. Check `BuildCommand()` returns valid command
3. Ensure no exceptions in `Initialize()`
4. Verify extension is listed in `beep> extensions`

### Runtime Errors

1. Check `_editor` is not null
2. Validate data sources exist before accessing
3. Ensure connections are open before operations
4. Handle missing or invalid parameters

## Next Steps

1. **Customize the examples** to fit your needs
2. **Create domain-specific commands** for your workflows
3. **Package as NuGet** for easy distribution
4. **Share with the community** on GitHub

## Resources

- [Extension Development Guide](../BeepShell/EXTENSION_DEVELOPMENT.md)
- [BeepShell README](../BeepShell/README.md)
- [System.CommandLine Documentation](https://github.com/dotnet/command-line-api)
- [Spectre.Console Documentation](https://spectreconsole.net/)

## License

This example is part of BeepDM and follows the same license.
