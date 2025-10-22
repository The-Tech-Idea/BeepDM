# BeepDM CLI

Command-line interface for the BeepDM data management platform.

## Installation

### As a .NET Tool

```bash
# Install from source
dotnet pack
dotnet tool install --global --add-source ./nupkg TheTechIdea.Beep.CLI

# Or install from NuGet (once published)
dotnet tool install --global TheTechIdea.Beep.CLI
```

### Build from Source

```bash
cd CLI
dotnet build
dotnet run -- --help
```

## Usage

### Profile Management

Profiles allow you to maintain separate configurations for different environments (dev, test, prod).

```bash
# List all profiles
beep profile list

# Create a new profile
beep profile create staging

# Create a profile by copying from another
beep profile create prod --from staging

# Delete a profile
beep profile delete old-env

# Show profile details
beep profile show staging
```

### Configuration

```bash
# Show configuration for default profile
beep config show

# Show configuration for specific profile
beep config show --profile production

# Show config path
beep config path

# Validate configuration
beep config validate
```

### Data Source Operations

```bash
# List all data sources
beep datasource list
beep ds list  # short alias

# Test a connection
beep ds test MyDatabase

# Show data source details
beep ds info MyDatabase

# List entities in a data source
beep ds entities MyDatabase
```

### Driver Management

```bash
# List installed drivers
beep driver list

# Scan and register drivers from a directory
beep driver scan C:\Drivers\SqlServer

# Show driver details
beep driver info SqlServerDriver
```

### Query Execution

```bash
# Execute SQL query
beep query exec MyDatabase "SELECT * FROM Users WHERE Age > 25"

# Query an entity with filters
beep query entity MyDatabase Users --filter "Age>25" --limit 50
```

### Workflows

```bash
# List workflows
beep workflow list
beep wf list  # short alias

# Run a workflow (coming soon)
beep wf run DataSync
```

## Environment Variables

- `BEEP_CONFIG_PATH`: Override default config location
- `BEEP_PROFILE`: Default profile to use

## Examples

### Working with Multiple Environments

```bash
# Set up development environment
beep profile create dev
beep --profile dev ds list

# Set up production environment
beep profile create prod
beep --profile prod config show

# Use environment variable
export BEEP_PROFILE=prod
beep ds list
```

### Testing Connections

```bash
# Test all connections in a profile
for ds in $(beep ds list --profile dev | tail -n +2 | awk '{print $1}'); do
    beep ds test $ds --profile dev
done
```

### Scanning New Drivers

```bash
# Scan and register all drivers from a directory
beep driver scan ./ConnectionDrivers --profile dev
beep driver list --profile dev
```

## Development

The CLI is built on:
- **System.CommandLine** - Modern command-line parsing
- **Spectre.Console** - Rich terminal UI
- **Microsoft.Extensions.DependencyInjection** - Dependency injection

### Project Structure

```
CLI/
├── Program.cs                          # Entry point
├── Commands/                           # Command implementations
│   ├── ConfigCommands.cs
│   ├── DataSourceCommands.cs
│   ├── DriverCommands.cs
│   ├── QueryCommands.cs
│   ├── ETLCommands.cs
│   ├── WorkflowCommands.cs
│   └── ProfileCommands.cs
└── Infrastructure/                     # Core services
    ├── BeepServiceProvider.cs         # DI container
    └── ProfileManager.cs              # Profile management
```

## Contributing

Contributions are welcome! The CLI is designed to be extensible. To add a new command:

1. Create a new command class in `Commands/`
2. Implement the `Build()` method returning a `Command`
3. Register it in `Program.cs`

## License

Same as BeepDM main project.
