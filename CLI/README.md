# BeepDM CLI

Command-line interface for the BeepDM data management platform - A comprehensive tool for database operations, code generation, ETL, and data management.

## 🚀 Quick Start

```bash
# Install from source
dotnet pack
dotnet tool install --global --add-source ./nupkg TheTechIdea.Beep.CLI

# Get help
beep --help
beep <command> --help
```

## 📋 Table of Contents

- [Installation](#installation)
- [Profile Management](#profile-management)
- [Configuration](#configuration)
- [Data Source Operations](#data-source-operations)
- [Driver Management](#driver-management)
- [Class Generation](#class-generation)
  - [Basic POCO Classes](#basic-poco-classes)
  - [Batch Generation](#batch-generation)
  - [Web API & Services](#web-api--services)
  - [Database & Data Access](#database--data-access)
  - [UI Components](#ui-components)
  - [Testing & Validation](#testing--validation)
- [ETL Operations](#etl-operations)
- [Data Management](#data-management)
- [Field Mapping](#field-mapping)
- [Data Synchronization](#data-synchronization)
- [Data Import](#data-import)

---

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

---

## Profile Management

Profiles allow you to maintain separate configurations for different environments (dev, test, prod).

### Commands

```bash
# List all profiles
beep profile list
beep profile list --verbose

# Create a new profile
beep profile create staging
beep profile create prod --from staging --description "Production environment"

# Show profile details
beep profile show staging

# Rename a profile
beep profile rename old-name new-name

# Delete a profile
beep profile delete old-env
beep profile delete old-env --force

# Export/Import profiles
beep profile export prod prod-backup.zip
beep profile import prod-backup.zip restored-prod

# Clean empty profiles
beep profile clean
beep profile clean --dry-run
```

---

## Configuration

### Commands

```bash
# Show configuration
beep config show
beep config show --profile production

# Show config path
beep config path

# Validate configuration
beep config validate

# Connection management
beep config connection list
beep config connection list --filter RDBMS --details
beep config connection add
beep config connection update MyConnection
beep config connection delete MyConnection --force
```

---

## Data Source Operations

### Commands

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

---

## Driver Management

### Commands

```bash
# List installed drivers
beep driver list
beep driver list --category RDBMS
beep driver list --type SqlServer --details

# Scan and register drivers
beep driver scan C:\Drivers\SqlServer

# Show driver details
beep driver info SqlServerDriver

# Validate driver compatibility
beep driver validate MyConnection

# Find drivers for file extension
beep driver for-extension csv
```

---

## Class Generation

Generate classes, DTOs, Web APIs, repositories, and more from your data structures - **32 commands available!**

### Basic POCO Classes

#### Single Entity Generation

```bash
# Generate simple POCO class
beep class generate-poco MyDatabase Users --output ./Models --namespace MyApp.Models

# Generate INotifyPropertyChanged class (for WPF/MVVM)
beep class generate-inotify MyDatabase Users --output ./ViewModels --namespace MyApp.ViewModels

# Generate Entity class with change tracking
beep class generate-entity MyDatabase Users --output ./Entities --namespace MyApp.Entities

# Generate modern C# record (immutable)
beep class generate-record MyDatabase Users --output ./Models --namespace MyApp.Models

# Generate nullable-aware class (C# 8+)
beep class generate-nullable MyDatabase Users --output ./Models --namespace MyApp.Models

# Generate DDD aggregate root
beep class generate-ddd MyDatabase Users --output ./Domain --namespace MyApp.Domain
```

### Batch Generation

Generate multiple classes at once from multiple entities:

```bash
# Generate POCO classes for all entities
beep class generate-poco-batch MyDatabase --output ./Models --namespace MyApp.Models

# Generate POCO classes for specific entities
beep class generate-poco-batch MyDatabase --output ./Models --namespace MyApp.Models \
  --entities Users,Orders,Products --classname MyModels

# Generate INotifyPropertyChanged classes for all entities
beep class generate-inotify-batch MyDatabase --output ./ViewModels --namespace MyApp.ViewModels

# Generate INotifyPropertyChanged for specific entities
beep class generate-inotify-batch MyDatabase --output ./ViewModels --namespace MyApp.ViewModels \
  --entities Users,Orders

# Generate Entity classes for all entities
beep class generate-entity-batch MyDatabase --output ./Entities --namespace MyApp.Entities

# Generate Entity classes for specific entities
beep class generate-entity-batch MyDatabase --output ./Entities --namespace MyApp.Entities \
  --entities Users,Orders,Products
```

### Web API & Services

```bash
# Generate Web API controllers for all entities
beep class generate-webapi MyDatabase --output ./Controllers --namespace MyApp.Controllers

# Generate Web API controllers for specific entities
beep class generate-webapi MyDatabase --output ./Controllers --namespace MyApp.Controllers \
  --entities Users,Orders

# Generate Web API controller with parameter template
beep class generate-webapi-params UserController --output ./Controllers --namespace MyApp.Controllers

# Generate minimal Web API template
beep class generate-minimal-api --output ./API --namespace MyApp.API

# Generate gRPC service (proto + implementation)
beep class generate-grpc MyDatabase Users --output ./Services --namespace MyApp.Grpc

# Generate GraphQL schema for all entities
beep class generate-graphql MyDatabase --output ./GraphQL --namespace MyApp.GraphQL

# Generate serverless functions (Azure/AWS)
beep class generate-serverless MyDatabase Users --output ./Functions --provider Azure
```

### Database & Data Access

```bash
# Generate EF Core DbContext for all entities
beep class generate-dbcontext MyDatabase --output ./Data --namespace MyApp.Data

# Generate EF Core entity configuration
beep class generate-entity-config MyDatabase Users --output ./Data/Config --namespace MyApp.Data

# Generate EF Core migration
beep class generate-migration MyDatabase Users --output ./Migrations --namespace MyApp.Migrations

# Generate repository pattern implementation
beep class generate-repository MyDatabase Users --output ./Repositories --namespace MyApp.Repositories

# Generate repository interface only
beep class generate-repository MyDatabase Users --output ./Repositories --interface-only

# Generate data access layer
beep class generate-dal MyDatabase Users --output ./DataAccess

# Create compiled DLL from entities
beep class create-dll MyDatabase MyEntities --output ./bin

# Create DLL for specific entities
beep class create-dll MyDatabase MyEntities --output ./bin --entities Users,Orders,Products

# Create DLL from C# files
beep class create-dll-from-files MyAssembly ./SourceFiles --output ./bin --namespace MyApp.Models
```

### UI Components

```bash
# Generate Blazor component
beep class generate-blazor MyDatabase Users --output ./Components --namespace MyApp.Components
```

### Testing & Validation

```bash
# Validate entity structure
beep class validate-entity MyDatabase Users

# Generate unit test class (xUnit)
beep class generate-tests MyDatabase Users --output ./Tests

# Generate FluentValidation validator
beep class generate-validator MyDatabase Users --output ./Validators --namespace MyApp.Validators
```

### Documentation & Utilities

```bash
# Generate XML documentation
beep class generate-docs MyDatabase Users --output ./Docs

# Generate entity diff report
beep class generate-diff MyDatabase Users1 Users2 --output ./Reports
```

### Low-Level Compilation (Advanced)

```bash
# Compile C# code to assembly
beep class compile-assembly ./MyCode.cs

# Compile code to type
beep class compile-type ./MyCode.cs MyNamespace.MyClass

# Compile class to DLL from text
beep class compile-class-dll ./MyCode.cs ./bin/MyAssembly.dll

# Generate C# code from file
beep class generate-csharp ./template.txt
```

---

## ETL Operations

### Commands

```bash
# Copy entity structure
beep etl copy-structure SourceDB DestDB Users

# Copy data between entities
beep etl copy-data SourceDB DestDB Users --batch-size 1000

# Validate data consistency
beep etl validate SourceDB DestDB Users
```

---

## Data Management

Enhanced data management and schema operations:

### Commands

```bash
# Display detailed schema information
beep dm schema MyDatabase Users

# List all entities with details
beep dm list-entities MyDatabase
beep dm list-entities MyDatabase --count

# Export schema to JSON
beep dm export-schema MyDatabase schema.json
beep dm export-schema MyDatabase schema.json --entities Users,Orders

# Compare schemas between data sources
beep dm compare-schemas Database1 Database2

# Show statistics for a data source
beep dm stats MyDatabase
```

---

## Field Mapping

Create and manage field mappings between entities:

### Commands

```bash
# Create field mapping
beep mapping create SourceDB SourceTable DestDB DestTable
beep mapping create SourceDB SourceTable DestDB DestTable --auto-map

# List all mappings
beep mapping list
beep mapping list --datasource MyDatabase

# Show mapping details
beep mapping show Users MyDatabase

# Delete a mapping
beep mapping delete Users MyDatabase
```

---

## Data Synchronization

Bi-directional data synchronization between data sources:

### Commands

```bash
# Create sync schema
beep sync create mySyncSchema SourceDB SourceTable DestDB DestTable
beep sync create mySyncSchema SourceDB SourceTable DestDB DestTable --bidirectional

# Run synchronization
beep sync run mySyncSchema
beep sync run mySyncSchema --dry-run

# List sync schemas
beep sync list

# Show sync schema details
beep sync show mySyncSchema

# Delete sync schema
beep sync delete mySyncSchema
```

---

## Data Import

Bulk data import from files:

### Commands

```bash
# Import data from file
beep import file MyDatabase Users ./data.csv

# Validate import without executing
beep import validate MyDatabase Users ./data.csv
```

---

## Query Execution

### Commands

```bash
# Execute SQL query
beep query exec MyDatabase "SELECT * FROM Users WHERE Age > 25"

# Query an entity with filters
beep query entity MyDatabase Users --filter "Age>25" --limit 50
```

---

## Environment Variables

- `BEEP_CONFIG_PATH`: Override default config location
- `BEEP_PROFILE`: Default profile to use

---

## 💡 Advanced Examples

### Complete Code Generation Workflow

```bash
# 1. Validate entity structure
beep class validate-entity ProductionDB Users

# 2. Generate POCO classes for all entities
beep class generate-poco-batch ProductionDB --output ./Models --namespace MyApp.Models

# 3. Generate repositories
beep class generate-repository ProductionDB Users --output ./Repositories

# 4. Generate Web API controllers for all entities
beep class generate-webapi ProductionDB --output ./Controllers --namespace MyApp.Controllers

# 5. Generate unit tests
beep class generate-tests ProductionDB Users --output ./Tests

# 6. Create DLL with all entities
beep class create-dll ProductionDB MyDataModels --output ./bin
```

### Batch Class Generation for Multiple Entities

```bash
# Generate POCO classes for specific entities
beep class generate-poco-batch MyDB --output ./Models --namespace MyApp.Models \
  --entities Users,Orders,Products,Categories

# Generate INotifyPropertyChanged classes for all entities
beep class generate-inotify-batch MyDB --output ./ViewModels --namespace MyApp.ViewModels

# Generate Entity classes for data layer
beep class generate-entity-batch MyDB --output ./Entities --namespace MyApp.Data.Entities \
  --entities Users,Orders,Products
```

### Schema Migration Workflow

```bash
# 1. Compare schemas
beep dm compare-schemas DevDB ProdDB

# 2. Export production schema
beep dm export-schema ProdDB prod-schema.json

# 3. Copy entity structure
beep etl copy-structure DevDB ProdDB Users

# 4. Validate consistency
beep etl validate DevDB ProdDB Users

# 5. Copy data
beep etl copy-data DevDB ProdDB Users
```

### Data Analysis Workflow

```bash
# 1. Show database statistics
beep dm stats MyDatabase

# 2. List all entities with counts
beep dm list-entities MyDatabase --count

# 3. Display detailed schema for specific entity
beep dm schema MyDatabase Users

# 4. Export complete schema
beep dm export-schema MyDatabase complete-schema.json
```

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

### Data Synchronization Setup

```bash
# 1. Create field mapping
beep mapping create SourceDB Users DestDB Users --auto-map

# 2. Create sync schema
beep sync create userSync SourceDB Users DestDB Users --bidirectional

# 3. Test with dry run
beep sync run userSync --dry-run

# 4. Execute synchronization
beep sync run userSync
```

---

## 🏗️ Development

The CLI is built on:
- **System.CommandLine** - Modern command-line parsing
- **Spectre.Console** - Rich terminal UI
- **Microsoft.Extensions.DependencyInjection** - Dependency injection
- **BeepDM DataManagementEngine** - Core data management platform

### Project Structure

```
CLI/
├── Program.cs                          # Entry point
├── Commands/                           # Command implementations (10 command groups)
│   ├── ProfileCommands.cs              # Profile management (8 commands)
│   ├── ConfigCommands.cs               # Configuration management (7 commands)
│   ├── DriverCommands.cs               # Driver management (5 commands)
│   ├── DataSourceCommands.cs           # Data source operations (4 commands)
│   ├── QueryCommands.cs                # Query execution (2 commands)
│   ├── ETLCommands.cs                  # ETL operations (3 commands)
│   ├── MappingCommands.cs              # Field mapping (4 commands)
│   ├── SyncCommands.cs                 # Data synchronization (5 commands)
│   ├── ImportCommands.cs               # Data import (2 commands)
│   ├── ClassCreatorCommands.cs         # Class generation (32 commands)
│   └── DataManagementCommands.cs       # Data management (6 commands)
└── Infrastructure/                     # Core services
    ├── BeepServiceProvider.cs          # DI container
    ├── ProfileManager.cs               # Profile management
    └── CliHelper.cs                    # CLI utility helpers
```

### Command Groups Summary

| Group | Commands | Description |
|-------|----------|-------------|
| **profile** | 8 | Manage configuration profiles |
| **config** | 7 | Configuration and connection management |
| **driver** | 5 | Database driver management |
| **datasource (ds)** | 4 | Data source operations |
| **query** | 2 | Query execution |
| **etl** | 3 | ETL operations |
| **mapping** | 4 | Field mapping |
| **sync** | 5 | Data synchronization |
| **import** | 2 | Data import |
| **class** | 32 | Code generation (includes 3 new batch commands) |
| **dm** | 6 | Data management |
| **TOTAL** | **78** | **All commands** |

---

## 🆕 New Batch Generation Commands

Three new powerful batch commands for generating multiple classes at once:

### 1. `generate-poco-batch`
Generate POCO classes for multiple entities in one command.

```bash
# All entities
beep class generate-poco-batch MyDatabase --output ./Models

# Specific entities
beep class generate-poco-batch MyDatabase --output ./Models \
  --entities Users,Orders,Products --classname MyModels
```

### 2. `generate-inotify-batch`
Generate INotifyPropertyChanged classes for multiple entities.

```bash
# All entities
beep class generate-inotify-batch MyDatabase --output ./ViewModels

# Specific entities
beep class generate-inotify-batch MyDatabase --output ./ViewModels \
  --entities Users,Orders
```

### 3. `generate-entity-batch`
Generate Entity classes with change tracking for multiple entities.

```bash
# All entities
beep class generate-entity-batch MyDatabase --output ./Entities

# Specific entities
beep class generate-entity-batch MyDatabase --output ./Entities \
  --entities Users,Orders,Products
```

---

## 📚 Additional Resources

- **HTML Help Site**: See `help-site/index.html` for comprehensive interactive documentation
- **API Documentation**: Check the DataManagementEngine documentation
- **Examples**: See the `examples/` directory for more use cases

---

## 🤝 Contributing

Contributions are welcome! The CLI is designed to be extensible.

### To add a new command:

1. Create a new command class in `Commands/`
2. Implement the `Build()` method returning a `Command`
3. Use `CliHelper` for consistent messaging
4. Register it in `Program.cs`
5. Update this README and the HTML help site

---

## 📄 License

Same as BeepDM main project.

---

## 🆘 Getting Help

```bash
# General help
beep --help

# Command group help
beep class --help
beep dm --help

# Specific command help
beep class generate-poco --help
beep dm schema --help
```

For more detailed documentation, open the HTML help site at `help-site/index.html`.
