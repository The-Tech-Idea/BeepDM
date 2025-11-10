# BeepShell - Interactive REPL for BeepDM

An interactive shell (REPL - Read-Eval-Print Loop) for BeepDM that maintains persistent connections and state across commands, providing a faster and more efficient way to work with your data sources.

## 🚀 Why BeepShell?

Unlike the standard BeepDM CLI which creates new instances for each command, BeepShell:

✅ **Persistent Connections** - Open connections once, use them multiple times  
✅ **Faster Execution** - No overhead from recreating services per command  
✅ **Session State** - Track your work with command history and statistics  
✅ **Interactive Workflow** - Perfect for exploration and development  
✅ **Same Commands** - All CLI commands work the same way  
✅ **Extensible** - Create custom commands and workflows as plugins  

## 🔌 Extensibility

BeepShell supports a powerful plugin system that allows you to create custom commands and workflows:

- **IShellCommand** - Create new CLI commands
- **IShellWorkflow** - Build complex multi-step operations
- **IShellExtension** - Package multiple commands/workflows together
- **Automatic Discovery** - Just implement the interface and drop the DLL in a BeepDM folder

Extensions are automatically discovered and loaded using AssemblyHandler's `ScanExtensions()` mechanism.

📖 **[Read the Extension Development Guide](EXTENSION_DEVELOPMENT.md)** to start building custom commands!  

## 📦 Installation

### Build from Source

```bash
cd BeepShell
dotnet build
dotnet run
```

### Install as Tool (Coming Soon)

```bash
dotnet tool install --global TheTechIdea.Beep.Shell
beepshell
```

## 🎯 Quick Start

### Launch BeepShell

```bash
# Default profile
dotnet run

# Specific profile
dotnet run --profile production

# Or set environment variable
$env:BEEP_PROFILE="dev"
dotnet run
```

### Your First Session

```
BeepShell v1.0.0
Type 'help' for commands | Type 'exit' to quit

beep> help                           # Show available commands
beep> config connection list         # List all connections  
beep> ds test MyDatabase            # Test and open connection
beep> ds entities MyDatabase        # List tables (connection stays open!)
beep> status                        # Check session stats
beep> exit                          # Close shell
```

## 📚 Shell Commands

### Shell-Specific Commands

These commands only work in BeepShell (not in CLI):

```bash
help, ?                    # Show help
clear, cls                 # Clear screen
exit, quit, q             # Exit shell
status                    # Show session statistics
connections               # Show all open connections
datasources               # Show active data sources
history                   # Show command history
extensions                # List loaded extensions
workflows                 # List available workflows
profile                   # Show current profile
profile switch <name>     # Switch to different profile
reload                    # Reload configuration from disk
close <datasource>        # Close specific connection
```

### All CLI Commands Available

Every command from BeepDM CLI works in BeepShell, plus any custom commands from loaded extensions:

```bash
# Configuration
config show
config connection add
config connection list

# Data Sources
ds test MyDatabase
ds info MyDatabase
ds entities MyDatabase

# Query
query exec MyDatabase "SELECT * FROM Users"
query entity MyDatabase Users --limit 50

# ETL
etl copy-structure SourceDB DestDB Users
etl copy-data SourceDB DestDB Users

# Class Generation
class generate-poco MyDB Users --output ./Models

# Custom Extension Commands (examples)
export --source MyDB --table Users --output users.csv
import --file data.csv --target MyDB --table ImportedData
analyze --source MyDB --table Orders

class generate-webapi MyDB --output ./Controllers

# And all other commands...
```

## 🔄 Persistent State

### What Persists in a Session?

✅ **DMEEditor Instance** - Reused across all commands  
✅ **Open Connections** - Stay open until you close them  
✅ **Loaded Assemblies** - No need to reload drivers  
✅ **Configuration** - Loaded once at startup  
✅ **Data Sources List** - Accumulates as you work  

### Example Workflow

```bash
beep> config connection list
[Shows 5 connections]

beep> ds test Database1
✓ Connection successful

beep> ds test Database2  
✓ Connection successful

beep> connections        # Both connections still open!
Open Connections (2)
  Database1    SqlServer    Open
  Database2    PostgreSQL   Open

beep> datasources        # Both in memory!
Active Data Sources (2)
  Database1    SqlServer    ●
  Database2    PostgreSQL   ●

beep> etl copy-data Database1 Database2 Users
[Copies data - uses existing connections, no reconnection needed!]

beep> close Database1    # Close when done
✓ Closed connection to 'Database1'
```

## 📊 Session Statistics

The `status` command shows comprehensive session information:

```bash
beep> status

Session Status
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Profile:             production
Session Time:        15 min 32 sec
Start Time:          2025-11-10 10:30:15

Command Statistics
  Total Commands:    47
  Successful:        45
  Failed:            2
  Execution Time:    2.3 sec

Connections
  Active Data Sources: 3
  Open Connections:    2
  Configured:          12

Configuration
  Drivers Loaded:      8
  Assemblies:          15
```

## 🔧 Profile Management

### Switch Profiles During Session

```bash
beep> profile
Current profile: dev

beep> profile switch production
✓ Switched to profile: production

beep> config show
[Shows production configuration]
```

### Profile Locations

Profiles are stored in:
```
%AppData%\TheTechIdea\BeepShell\Profiles\
├── default/
├── dev/
├── staging/
└── production/
```

## 💡 Use Cases

### 1. Data Exploration

```bash
beep> ds test MyDatabase
beep> ds entities MyDatabase
beep> query entity MyDatabase Users --limit 10
beep> query entity MyDatabase Orders --filter "Status=Pending"
```

### 2. Batch Operations

```bash
beep> ds test SourceDB
beep> ds test DestDB
beep> etl copy-structure SourceDB DestDB Users
beep> etl copy-structure SourceDB DestDB Orders
beep> etl copy-structure SourceDB DestDB Products
beep> etl copy-data SourceDB DestDB Users
beep> etl copy-data SourceDB DestDB Orders
```

### 3. Code Generation

```bash
beep> ds test MyDatabase
beep> class generate-poco MyDatabase Users --output ./Models
beep> class generate-poco MyDatabase Orders --output ./Models
beep> class generate-poco MyDatabase Products --output ./Models
beep> class generate-webapi MyDatabase --output ./Controllers
```

### 4. Configuration Management

```bash
beep> config connection list
beep> config connection add
beep> config connection update MyConnection
beep> reload                    # Reload if changed externally
beep> config connection list
```

## 🆚 BeepShell vs BeepDM CLI

| Feature | BeepShell | CLI |
|---------|-----------|-----|
| **Execution Model** | Interactive | One-shot |
| **State** | Persistent | Stateless |
| **Connections** | Kept open | Closed per command |
| **Startup Time** | Once | Per command |
| **Best For** | Development, exploration | Automation, scripts |
| **Memory** | ~10MB base | ~5MB per invocation |
| **Commands** | Same + shell commands | All standard commands |

## 🎨 Features

### Command History

```bash
beep> history
Command History (Last 10)
  1   config connection list
  2   ds test MyDatabase
  3   ds entities MyDatabase
  4   query entity MyDatabase Users
  5   status
```

### Connection Management

```bash
beep> connections
Open Connections (3)
  Database1    SqlServer     Open    45 entities
  Database2    PostgreSQL    Open    32 entities
  FileDB       CSV           Open    5 entities

beep> close Database1
✓ Closed connection to 'Database1'
```

### Auto-completion (Future)

- Tab completion for commands
- Data source name completion
- Entity name completion

## 🔒 Best Practices

### 1. Close Connections When Done

```bash
beep> close MyDatabase
# Or exit shell to close all
beep> exit
```

### 2. Use Profiles for Different Environments

```bash
# Development
beepshell --profile dev

# Production (read-only recommended)
beepshell --profile production
```

### 3. Check Status Regularly

```bash
beep> status          # Monitor open connections
beep> connections     # See what's open
```

### 4. Reload After External Changes

```bash
# If configuration files changed externally
beep> reload
```

## ⚠️ Important Notes

### Memory Usage

BeepShell maintains state, so memory usage grows with:
- Number of open connections
- Number of loaded entities
- Amount of cached data

Monitor with `status` command and close unused connections.

### Configuration Changes

- Changes made in BeepShell persist to disk (same as CLI)
- Use `reload` if configuration changed externally
- Switching profiles closes all connections

### Threading

BeepShell is single-threaded. Long-running operations will block the prompt.

## 🛠️ Development

### Project Structure

```
BeepShell/
├── Program.cs                        # Entry point
├── BeepShell.csproj                  # Project file
├── Infrastructure/
│   ├── ShellServiceProvider.cs      # Persistent service provider
│   └── InteractiveShell.cs          # REPL implementation
└── Commands/
    └── ShellCommands.cs             # Shell-specific commands
```

### Adding New Shell Commands

Edit `InteractiveShell.cs` and add to `HandleShellCommand()`:

```csharp
case "mynewcommand":
    ShellCommands.MyNewCommand(_editor, _sessionState);
    return true;
```

## 📄 License

Same as BeepDM main project.

## 🆘 Getting Help

```bash
beep> help                    # Show all commands
beep> config --help          # Help for specific command group
beep> status                 # Check session status
```

## 🚀 Coming Soon

- [ ] Tab auto-completion
- [ ] Command aliases
- [ ] Script execution from files
- [ ] Session save/restore
- [ ] Multi-line command support
- [ ] Connection pooling
- [ ] Background task execution

---

**BeepShell** - Your persistent companion for BeepDM data management!
