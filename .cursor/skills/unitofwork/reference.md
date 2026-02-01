# UnitOfWork Quick Reference

## Initialization

```csharp
// Standard
using var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers", "Id");

// With EntityStructure
using var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers", entityStructure, "Id");

// List mode
using var uow = new UnitofWork<Customer>(editor, isInListMode: true, initialData, "Id");
```

## CRUD Operations

### Create
```csharp
// Method 1: New()
uow.New();
uow.CurrentItem.Name = "John Doe";
await uow.Commit();

// Method 2: Add()
uow.Add(customer);
await uow.Commit();
```

### Read
```csharp
// Get all
var all = await uow.Get();

// Get with filters
var filters = new List<AppFilter> { 
    new AppFilter { FieldName = "Status", Operator = "=", FilterValue = "Active" } 
};
var filtered = await uow.Get(filters);

// Get by ID
var item = uow.Get("123");

// Get by predicate
var item = uow.Read(c => c.Email == "john@example.com");
```

### Update
```csharp
// Update entity
customer.Name = "Updated";
var result = uow.Update(customer);
await uow.Commit();

// Update by ID
uow.Update("123", updatedCustomer);

// Async update
await uow.UpdateAsync(customer);
```

### Delete
```csharp
// Delete entity
var result = uow.Delete(customer);
await uow.Commit();

// Delete by ID
uow.Delete("123");

// Delete current
uow.Delete();

// Delete by predicate
uow.Delete(c => c.Status == "Inactive");

// Async delete
await uow.DeleteAsync(customer);
```

## Transaction Management

```csharp
// Automatic transaction
uow.Add(item1);
uow.Update(item2);
uow.Delete(item3);
var result = await uow.Commit(); // Handles transaction automatically

// Check if dirty
if (uow.IsDirty)
    await uow.Commit();

// Rollback
await uow.Rollback();

// Commit with progress
await uow.Commit(progress, cancellationToken);
```

## Change Tracking

```csharp
// Check for changes
bool hasChanges = uow.IsDirty;

// Get tracked changes
var inserted = uow.InsertedKeys;
var updated = uow.UpdatedKeys;
var deleted = uow.DeletedKeys;
var deletedUnits = uow.DeletedUnits;

// Get deleted entities
var deleted = uow.GetDeletedEntities();
```

## Events

```csharp
// Pre-events (can cancel)
uow.PreCreate += (s, e) => { /* e.Cancel = true; */ };
uow.PreUpdate += (s, e) => { };
uow.PreDelete += (s, e) => { };
uow.PreCommit += (s, e) => { };

// Post-events
uow.PostCreate += (s, e) => { };
uow.PostUpdate += (s, e) => { };
uow.PostDelete += (s, e) => { };
uow.PostCommit += (s, e) => { };

// Query events
uow.PreQuery += (s, e) => { };
uow.PostQuery += (s, e) => { };
```

## Filtering and Paging

```csharp
// Filtering
var filters = new List<AppFilter>
{
    new AppFilter { FieldName = "Status", Operator = "=", FilterValue = "Active" },
    new AppFilter { FieldName = "City", Operator = "=", FilterValue = "NYC" }
};
var filtered = await uow.Get(filters);

// Paging
uow.PageIndex = 0;
uow.PageSize = 10;
var paged = await uow.Get(filters);
int total = uow.TotalItemCount;
```

## Service Pattern

```csharp
public class CustomerService
{
    private readonly IDMEEditor _editor;
    
    public async Task<List<Customer>> GetAllAsync()
    {
        using var uow = new UnitofWork<Customer>(_editor, "MyDatabase", "Customers", "Id");
        return (await uow.Get()).ToList();
    }
    
    public async Task<IErrorsInfo> CreateAsync(Customer customer)
    {
        using var uow = new UnitofWork<Customer>(_editor, "MyDatabase", "Customers", "Id");
        uow.Add(customer);
        return await uow.Commit();
    }
    
    public async Task<IErrorsInfo> UpdateAsync(Customer customer)
    {
        using var uow = new UnitofWork<Customer>(_editor, "MyDatabase", "Customers", "Id");
        uow.Update(customer);
        return await uow.Commit();
    }
    
    public async Task<IErrorsInfo> DeleteAsync(int id)
    {
        using var uow = new UnitofWork<Customer>(_editor, "MyDatabase", "Customers", "Id");
        var customer = await GetByIdAsync(id);
        if (customer == null) return new ErrorsInfo { Flag = Errors.Failed, Message = "Not found" };
        uow.Delete(customer);
        return await uow.Commit();
    }
}
```

## Best Practices

1. **Always use `using`** - Automatic disposal
2. **Commit after operations** - Changes not persisted until Commit()
3. **Check errors** - Always check `result.Flag == Errors.Ok`
4. **Use async methods** - Prefer `await uow.Get()` over sync wrappers
5. **Validate before operations** - Validate entities before Add/Update
6. **Check IsDirty** - Before committing, check if there are changes
