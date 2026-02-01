---
name: unitofwork
description: Expert guidance for using UnitOfWork pattern in BeepDM for CRUD operations, change tracking, transaction management, and default value integration. Use when working with UnitofWork<T>, entity operations, or implementing service layers with BeepDM.
---

# UnitOfWork Pattern Guide

Expert guidance for using the UnitOfWork pattern in Beep Data Management Engine (BeepDM) for CRUD operations, change tracking, transaction management, and default value integration.

## Core Concept

**UnitOfWork<T>** is a generic repository pattern that provides:
- **Change Tracking**: Automatically tracks inserts, updates, and deletes
- **Transaction Management**: Handles transactions automatically
- **Default Values**: Integrates with DefaultsManager for automatic value assignment
- **Event System**: Pre/post events for all operations
- **Validation**: Built-in validation support
- **Datasource-Agnostic**: Works with any IDataSource implementation

## Basic Usage

### Initialization

```csharp
// Standard initialization
using var uow = new UnitofWork<Customer>(
    editor,                    // IDMEEditor instance
    "MyDatabase",             // Datasource name
    "Customers",              // Entity name
    "Id"                      // Primary key field name
);

// With EntityStructure
using var uow = new UnitofWork<Customer>(
    editor,
    "MyDatabase",
    "Customers",
    entityStructure,          // Pre-defined EntityStructure
    "Id"
);

// List mode (in-memory operations)
var initialData = new ObservableBindingList<Customer> { /* ... */ };
using var uow = new UnitofWork<Customer>(
    editor,
    isInListMode: true,
    initialData,
    "Id"
);
```

## CRUD Operations

### Create Operations

```csharp
using var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers", "Id");

// Method 1: New() - Creates new entity and sets as CurrentItem
uow.New();
var newCustomer = uow.CurrentItem;
newCustomer.Name = "John Doe";
newCustomer.Email = "john@example.com";
// DefaultsManager will automatically set CreatedAt, CreatedBy, etc.

// Method 2: Add() - Adds existing entity
var customer = new Customer 
{ 
    Name = "Jane Smith", 
    Email = "jane@example.com" 
};
uow.Add(customer);
// DefaultsManager will apply insert defaults

// Commit changes
var result = await uow.Commit();
if (result.Flag == Errors.Ok)
{
    Console.WriteLine("Customer created successfully");
}
```

### Read Operations

```csharp
using var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers", "Id");

// Get all entities
var allCustomers = await uow.Get();

// Get with filters
var filters = new List<AppFilter>
{
    new AppFilter { FieldName = "Status", Operator = "=", FilterValue = "Active" },
    new AppFilter { FieldName = "City", Operator = "=", FilterValue = "New York" }
};
var filteredCustomers = await uow.Get(filters);

// Get by ID
var customer = uow.Get("123");  // Primary key value as string
var customer2 = uow.Get(123);   // Primary key value as int (if supported)

// Get by predicate
var customer = uow.Read(c => c.Email == "john@example.com");

// Get multiple by predicate
var customers = await uow.MultiRead(c => c.Status == "Active");
```

### Update Operations

```csharp
using var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers", "Id");

// Load entity first
var customers = await uow.Get();
var customer = customers.FirstOrDefault(c => c.Id == 123);

if (customer != null)
{
    // Modify entity
    customer.Name = "Updated Name";
    customer.Email = "updated@example.com";
    
    // Update - DefaultsManager will apply update defaults (ModifiedAt, ModifiedBy, etc.)
    var updateResult = uow.Update(customer);
    
    if (updateResult.Flag == Errors.Ok)
    {
        // Commit changes
        var commitResult = await uow.Commit();
    }
}

// Update by ID
var updateResult = uow.Update("123", updatedCustomer);

// Update by predicate
var updateResult = uow.Update(
    c => c.Id == 123, 
    updatedCustomer
);

// Async update
var updateResult = await uow.UpdateAsync(customer);
```

### Delete Operations

```csharp
using var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers", "Id");

// Load entity first
var customers = await uow.Get();
var customer = customers.FirstOrDefault(c => c.Id == 123);

if (customer != null)
{
    // Delete entity
    var deleteResult = uow.Delete(customer);
    
    if (deleteResult.Flag == Errors.Ok)
    {
        // Commit changes
        var commitResult = await uow.Commit();
    }
}

// Delete by ID
var deleteResult = uow.Delete("123");

// Delete current item
uow.CurrentItem = customer;
var deleteResult = uow.Delete();

// Delete by predicate
var deleteResult = uow.Delete(c => c.Status == "Inactive");

// Async delete
var deleteResult = await uow.DeleteAsync(customer);
```

## Transaction Management

### Automatic Transaction Handling

```csharp
using var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers", "Id");

// All operations are tracked
uow.Add(customer1);
uow.Add(customer2);
uow.Update(customer3);
uow.Delete(customer4);

// Commit automatically handles transaction
var result = await uow.Commit();
// - Begins transaction
// - Applies all changes (inserts, updates, deletes)
// - Commits transaction
// - Clears change tracking
```

### Manual Transaction Control

```csharp
using var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers", "Id");

// Make changes
uow.Add(customer1);
uow.Update(customer2);

// Check if dirty before committing
if (uow.IsDirty)
{
    var result = await uow.Commit();
}

// Rollback changes
var rollbackResult = await uow.Rollback();
```

### Commit with Progress

```csharp
var progress = new Progress<PassedArgs>(args =>
{
    Console.WriteLine($"Progress: {args.Message}");
});

var cancellationToken = new CancellationTokenSource().Token;

var result = await uow.Commit(progress, cancellationToken);
```

## Change Tracking

### Tracking Properties

```csharp
// Check if there are uncommitted changes
bool hasChanges = uow.IsDirty;

// Get tracked changes
var insertedKeys = uow.InsertedKeys;      // Dictionary<int, string>
var updatedKeys = uow.UpdatedKeys;        // Dictionary<int, string>
var deletedKeys = uow.DeletedKeys;        // Dictionary<int, string>
var deletedUnits = uow.DeletedUnits;      // List<T>

// Get deleted entities
var deleted = uow.GetDeletedEntities();
```

### Entity States

```csharp
// UnitOfWork automatically tracks entity states:
// - EntityState.Added
// - EntityState.Modified
// - EntityState.Deleted
// - EntityState.Unchanged
```

## DefaultsManager Integration

### Automatic Default Application

```csharp
// DefaultsManager must be initialized first
DefaultsManager.Initialize(editor);

// Configure defaults for Customer entity
DefaultsManager.SetColumnDefault(editor, "MyDatabase", "Customers", 
    "CreatedAt", "NOW", isRule: true);
DefaultsManager.SetColumnDefault(editor, "MyDatabase", "Customers", 
    "CreatedBy", "USERNAME", isRule: true);
DefaultsManager.SetColumnDefault(editor, "MyDatabase", "Customers", 
    "Status", "Active", isRule: false);

// UnitOfWork automatically applies defaults
using var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers", "Id");

uow.New();
var customer = uow.CurrentItem;
customer.Name = "John Doe";
// CreatedAt, CreatedBy, Status will be automatically set by DefaultsManager

await uow.Commit();
```

## Event System

### Available Events

```csharp
using var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers", "Id");

// Pre-events (can cancel operation)
uow.PreCreate += (sender, args) =>
{
    Console.WriteLine("Before creating entity");
    // args.Cancel = true; // Cancel operation
};

uow.PreUpdate += (sender, args) =>
{
    Console.WriteLine("Before updating entity");
};

uow.PreDelete += (sender, args) =>
{
    Console.WriteLine("Before deleting entity");
};

uow.PreCommit += (sender, args) =>
{
    Console.WriteLine("Before committing changes");
};

// Post-events
uow.PostCreate += (sender, args) =>
{
    Console.WriteLine("After creating entity");
};

uow.PostUpdate += (sender, args) =>
{
    Console.WriteLine("After updating entity");
};

uow.PostDelete += (sender, args) =>
{
    Console.WriteLine("After deleting entity");
};

uow.PostCommit += (sender, args) =>
{
    Console.WriteLine("After committing changes");
};

// Query events
uow.PreQuery += (sender, args) =>
{
    Console.WriteLine("Before query");
};

uow.PostQuery += (sender, args) =>
{
    Console.WriteLine("After query");
};
```

## Filtering and Paging

### Filtering

```csharp
using var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers", "Id");

// Simple filter
var filters = new List<AppFilter>
{
    new AppFilter 
    { 
        FieldName = "Status", 
        Operator = "=", 
        FilterValue = "Active" 
    }
};
var activeCustomers = await uow.Get(filters);

// Multiple filters (AND logic)
var filters = new List<AppFilter>
{
    new AppFilter { FieldName = "Status", Operator = "=", FilterValue = "Active" },
    new AppFilter { FieldName = "City", Operator = "=", FilterValue = "New York" }
};
var filtered = await uow.Get(filters);
```

### Paging

```csharp
using var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers", "Id");

uow.PageIndex = 0;
uow.PageSize = 10;

var filters = new List<AppFilter>
{
    new AppFilter { FieldName = "PageIndex", FilterValue = uow.PageIndex.ToString() },
    new AppFilter { FieldName = "PageSize", FilterValue = uow.PageSize.ToString() }
};

var pagedCustomers = await uow.Get(filters);
int totalCount = uow.TotalItemCount;
```

## Service Layer Pattern

### Example: CustomerService

```csharp
public class CustomerService
{
    private readonly IDMEEditor _editor;
    
    public CustomerService(IDMEEditor editor)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
    }

    public async Task<List<Customer>> GetAllCustomersAsync()
    {
        using var uow = new UnitofWork<Customer>(
            _editor, 
            AppDbContext.DataSourceName, 
            "Customers", 
            "Id"
        );
        var customers = await uow.Get();
        return customers.OrderBy(c => c.Name).ToList();
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        using var uow = new UnitofWork<Customer>(
            _editor, 
            AppDbContext.DataSourceName, 
            "Customers", 
            "Id"
        );
        var filters = new List<AppFilter>
        {
            new AppFilter { FieldName = "Id", Operator = "=", FilterValue = id.ToString() }
        };
        var customers = await uow.Get(filters);
        return customers.FirstOrDefault();
    }

    public async Task<IErrorsInfo> CreateCustomerAsync(Customer customer)
    {
        // Validate
        var validationResult = ValidateCustomer(customer);
        if (validationResult.Flag != Errors.Ok)
            return validationResult;

        using var uow = new UnitofWork<Customer>(
            _editor, 
            AppDbContext.DataSourceName, 
            "Customers", 
            "Id"
        );
        
        uow.Add(customer);
        return await uow.Commit();
    }

    public async Task<IErrorsInfo> UpdateCustomerAsync(Customer customer)
    {
        using var uow = new UnitofWork<Customer>(
            _editor, 
            AppDbContext.DataSourceName, 
            "Customers", 
            "Id"
        );
        
        var existing = await GetCustomerByIdAsync(customer.Id);
        if (existing == null)
        {
            return new ErrorsInfo 
            { 
                Flag = Errors.Failed, 
                Message = $"Customer {customer.Id} not found" 
            };
        }

        var updateResult = uow.Update(customer);
        if (updateResult.Flag != Errors.Ok)
            return updateResult;

        return await uow.Commit();
    }

    public async Task<IErrorsInfo> DeleteCustomerAsync(int id)
    {
        using var uow = new UnitofWork<Customer>(
            _editor, 
            AppDbContext.DataSourceName, 
            "Customers", 
            "Id"
        );
        
        var customer = await GetCustomerByIdAsync(id);
        if (customer == null)
        {
            return new ErrorsInfo 
            { 
                Flag = Errors.Failed, 
                Message = $"Customer {id} not found" 
            };
        }

        var deleteResult = uow.Delete(customer);
        if (deleteResult.Flag != Errors.Ok)
            return deleteResult;

        return await uow.Commit();
    }
}
```

## Best Practices

### 1. Always Use `using` Statement
**✅ Correct**: Automatic disposal
```csharp
using var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers", "Id");
```

**❌ Wrong**: Manual disposal required
```csharp
var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers", "Id");
// Must call uow.Dispose() manually
```

### 2. Commit After Operations
**✅ Correct**: Commit changes
```csharp
uow.Add(customer);
var result = await uow.Commit();
```

**❌ Wrong**: Changes not persisted
```csharp
uow.Add(customer);
// Changes are tracked but not persisted until Commit()
```

### 3. Check Errors After Operations
```csharp
var result = await uow.Commit();
if (result.Flag != Errors.Ok)
{
    _logger.LogError($"Commit failed: {result.Message}");
    // Handle error
}
```

### 4. Use Async Methods
**✅ Preferred**: Async operations
```csharp
var customers = await uow.Get();
var result = await uow.Commit();
```

**⚠️ Acceptable**: Synchronous wrappers (for backward compatibility)
```csharp
var customers = uow.Get().GetAwaiter().GetResult();
```

### 5. Validate Before Operations
```csharp
// Validate entity before adding
var validationResult = ValidateCustomer(customer);
if (validationResult.Flag != Errors.Ok)
{
    return validationResult;
}

uow.Add(customer);
await uow.Commit();
```

### 6. Handle Transactions Properly
```csharp
// UnitOfWork handles transactions automatically
// But you can check IsDirty before committing
if (uow.IsDirty)
{
    var result = await uow.Commit();
}
```

## Common Patterns

### Pattern 1: Batch Operations
```csharp
using var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers", "Id");

// Add multiple entities
foreach (var customer in newCustomers)
{
    uow.Add(customer);
}

// Commit all at once (single transaction)
var result = await uow.Commit();
```

### Pattern 2: Conditional Updates
```csharp
using var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers", "Id");

var customers = await uow.Get();
foreach (var customer in customers.Where(c => c.Status == "Pending"))
{
    customer.Status = "Active";
    uow.Update(customer);
}

if (uow.IsDirty)
{
    await uow.Commit();
}
```

### Pattern 3: Service Method with Error Handling
```csharp
public async Task<IErrorsInfo> UpdateCustomerStatusAsync(int id, string newStatus)
{
    try
    {
        using var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers", "Id");
        
        var customer = await GetCustomerByIdAsync(id);
        if (customer == null)
        {
            return new ErrorsInfo 
            { 
                Flag = Errors.Failed, 
                Message = $"Customer {id} not found" 
            };
        }

        customer.Status = newStatus;
        var updateResult = uow.Update(customer);
        if (updateResult.Flag != Errors.Ok)
            return updateResult;

        return await uow.Commit();
    }
    catch (Exception ex)
    {
        return new ErrorsInfo 
        { 
            Flag = Errors.Failed, 
            Message = ex.Message, 
            Ex = ex 
        };
    }
}
```

## Related Skills

- **@beepdm** - Core BeepDM architecture and IDataSource usage
- **@connection** - Connection management and driver configuration
- **@defaults** - DefaultsManager for automatic value assignment

## Key Files

- `UnitofWork.Core.cs` - Core implementation
- `UnitofWork.CRUD.cs` - CRUD operations
- `UnitofWork.Core.Extensions.cs` - Extended functionality
- `UnitofWork.Core.Utilities.cs` - Utility methods
- `UnitOfWorkFactory.cs` - Factory for dynamic type creation
- `Examples/UnitofWorkExamples.cs` - Usage examples
