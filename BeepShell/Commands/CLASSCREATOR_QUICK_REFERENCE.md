# ClassCreator & DataSource Commands - Quick Reference

## DataSource Commands

### List & Info
```bash
datasource list                        # List all data sources
datasource info mydb                   # Show datasource details
datasource entities mydb               # List all tables/entities
datasource schema mydb Customers       # Show detailed table schema
```

### Connection Management
```bash
datasource open mydb                   # Open connection
datasource close mydb                  # Close connection
datasource test mydb                   # Test connection
datasource refresh mydb                # Refresh entity list
```

### Query & Create
```bash
datasource query mydb "SELECT * FROM Customers"  # Execute query
datasource create mydb NewTable                  # Create entity (future)
```

## Class Generator Commands

### Basic Class Generation
```bash
# Generate POCO class
class generate mydb Customers --output ./Models

# With custom namespace
class generate mydb Customers --output ./Models --namespace MyApp.Models
```

### Database Layer
```bash
# Generate Data Access Layer
class dal mydb Customers --output ./DataAccess

# Generate EF DbContext (all tables)
class dbcontext mydb --output ./Data --all

# Generate DbContext (specific tables)
class dbcontext mydb --output ./Data --tables Customers Orders

# Generate Repository pattern
class repository mydb Customers --output ./Repositories

# Generate Repository interface only
class repository mydb Customers --output ./Repositories --interface

# Generate EF migration
class migration mydb Customers --output ./Migrations
```

### Web API
```bash
# Generate Web API controller
class api mydb Customers --output ./Controllers

# Generate API model/DTO
class model mydb Customers --output ./Models

# Generate Swagger/OpenAPI documentation
class swagger mydb --output ./Docs --all

# Generate GraphQL schema
class graphql mydb Customers --output ./GraphQL

# Generate gRPC service
class grpc mydb Customers --output ./Services
```

### DLL Creation
```bash
# Create compiled DLL from all tables
class dll mydb --output ./bin --name MyDataLib --all

# Create DLL from specific tables
class dll mydb --output ./bin --name MyLib --tables Customers Orders

# Create assembly with version
class assembly mydb --output ./bin --version 1.0.0
```

### Testing
```bash
# Generate unit tests
class test mydb Customers --output ./Tests

# Generate mock data
class mock mydb Customers --output ./Tests/Mocks

# Generate validators
class validator mydb Customers --output ./Validators
```

### Advanced Features
```bash
# Generate XML documentation
class docs mydb Customers --output ./Docs

# Generate Blazor components
class blazor mydb Customers --output ./Components

# Generate Razor pages
class razor mydb Customers --output ./Pages
```

## Common Workflows

### Setup New Project with EF Core
```bash
# 1. List available data sources
datasource list

# 2. Open connection
datasource open northwind

# 3. Check entities
datasource entities northwind

# 4. Generate DbContext
class dbcontext northwind --output ./Data --all --namespace MyApp.Data

# 5. Generate repositories for key tables
class repository northwind Customers --output ./Repositories
class repository northwind Orders --output ./Repositories
class repository northwind Products --output ./Repositories
```

### Create Complete Web API
```bash
# 1. Check table schema
datasource schema mydb Customers

# 2. Generate models
class model mydb Customers --output ./Models
class model mydb Orders --output ./Models

# 3. Generate controllers
class api mydb Customers --output ./Controllers
class api mydb Orders --output ./Controllers

# 4. Generate Swagger docs
class swagger mydb --output ./Docs --all
```

### Generate Test Project
```bash
# 1. Generate unit tests
class test mydb Customers --output ./Tests

# 2. Generate mock data
class mock mydb Customers --output ./Tests/TestData

# 3. Generate validators
class validator mydb Customers --output ./Validators
```

### Create DLL Library
```bash
# 1. Verify tables
datasource entities mydb

# 2. Generate DLL with all entities
class dll mydb --output ./dist --name MyDataModels --all --version 1.0.0

# Or for specific tables
class dll mydb --output ./dist --name MyDataModels --tables Customers Orders Products
```

## Tips & Tricks

### Using Aliases
```bash
# 'ds' is short for 'datasource'
ds list
ds open mydb
ds schema mydb Customers
```

### Check Before Generate
```bash
# Always check schema first
datasource schema mydb TableName

# This shows:
# - Field names and types
# - Nullable fields
# - Primary keys
# - Relations
```

### Batch Operations
```bash
# Generate for all tables
class dbcontext mydb --output ./Data --all

# Generate for specific tables
class dbcontext mydb --output ./Data --tables Table1 Table2 Table3
```

### Organize Output
```bash
# Separate folders for different layers
class generate mydb Customers --output ./Core/Models
class dal mydb Customers --output ./Data/DAL
class repository mydb Customers --output ./Data/Repositories
class api mydb Customers --output ./WebAPI/Controllers
class test mydb Customers --output ./Tests/Unit
```

### Connection Management
```bash
# Test before use
datasource test mydb

# Refresh if schema changed
datasource refresh mydb

# Close when done
datasource close mydb
```

## Error Handling

### Data Source Not Found
```bash
# Error: Data source 'mydb' not found
# Solution: List available sources
datasource list
```

### Entity Not Found
```bash
# Error: Entity 'TableName' not found
# Solution: List entities
datasource entities mydb
```

### Connection Failed
```bash
# Error: Failed to open connection
# Solution: Test connection
datasource test mydb
```

### No Tables Found
```bash
# Error: No tables found
# Solution: Refresh entity list
datasource refresh mydb
```

## Examples by Use Case

### ASP.NET Core Project
```bash
# Models
class generate northwind Customers --output ./Models --namespace MyApp.Models
class generate northwind Orders --output ./Models --namespace MyApp.Models

# Data layer
class dbcontext northwind --output ./Data --all --namespace MyApp.Data
class repository northwind Customers --output ./Repositories --namespace MyApp.Repositories

# API
class api northwind Customers --output ./Controllers --namespace MyApp.Controllers
class swagger northwind --output ./wwwroot --all
```

### Blazor Application
```bash
# Models
class generate mydb Products --output ./Models

# Blazor components
class blazor mydb Products --output ./Components

# Data access
class repository mydb Products --output ./Data
```

### Microservice
```bash
# gRPC services
class grpc mydb Customers --output ./Services

# Repository
class repository mydb Customers --output ./Repositories

# Tests
class test mydb Customers --output ./Tests
```

### Class Library
```bash
# Create DLL with models only
class dll mydb --output ./bin --name MyCompany.Models --all

# Or with DAL
class dal mydb Customers --output ./DataAccess
class dll mydb --output ./bin --name MyCompany.Data --all
```

## Advanced Options

### Custom Namespaces
```bash
--namespace MyCompany.ProjectName.Layer
```

### Output Directories
```bash
--output ./relative/path
--output C:/absolute/path
```

### Selective Generation
```bash
--all                           # All tables
--tables Table1 Table2 Table3   # Specific tables
--interface                     # Interface only (repository)
```

### Version Control
```bash
--version 1.0.0                 # Assembly version
```

## Performance Tips

1. **Cache connections**: Keep frequently used datasources open
2. **Batch generate**: Use `--all` for multiple tables
3. **Check schema first**: Use `datasource schema` before generating
4. **Refresh when needed**: Use `datasource refresh` after schema changes

## Integration with Other Commands

### With Connection Commands
```bash
# Create connection, then use it
connection create --name mydb --type SqlServer --server localhost

# Use in class generation
class generate mydb Customers --output ./Models
```

### With Query Commands
```bash
# Query data
query run mydb "SELECT * FROM Customers WHERE Country='USA'"

# Generate class for that table
class generate mydb Customers --output ./Models
```

## Next Steps

After generating code:
1. Review generated files
2. Customize as needed
3. Add business logic
4. Write additional tests
5. Deploy to your project

For more details, see `CLASS_CREATOR_REFACTORING_SUMMARY.md`
