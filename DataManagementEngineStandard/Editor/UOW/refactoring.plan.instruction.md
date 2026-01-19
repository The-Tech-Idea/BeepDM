# UnitofWork Refactoring Plan

## Overview
The current `UnitofWork<T>` class is a large monolithic class (over 2000 lines) that handles multiple responsibilities. This refactoring will break it down into manageable partial classes and helper components while integrating the `DefaultsManager` for insert and update operations.

## Current Analysis
The existing `UnitofWork<T>` class handles:
- Entity state management and tracking
- CRUD operations (Create, Read, Update, Delete)
- Change logging and undo functionality
- Collection management and filtering
- Data validation and error handling
- Paging and navigation
- Commit/rollback operations
- Event handling

## Refactoring Strategy

### 1. Partial Class Structure
Split the main class into logical partial classes:

#### UnitofWork.Core.cs (Main partial class)
- Core properties and fields
- Constructors
- Basic initialization
- IDisposable implementation

#### UnitofWork.CRUD.cs
- Insert, Update, Delete operations
- Read and query methods
- Integration with DefaultsManager

#### UnitofWork.EntityManagement.cs
- Entity state tracking
- Collection change handling
- Entity validation
- Property change notifications

#### UnitofWork.Navigation.cs
- Movement operations (First, Next, Previous, Last)
- Paging functionality
- Filtering and search

#### UnitofWork.ChangeTracking.cs
- Change log management
- Undo/redo operations
- Entity state monitoring

#### UnitofWork.Events.cs
- Event declarations and handling
- Event parameter management
- Custom event implementations

#### UnitofWork.Commit.cs
- Commit and rollback operations
- Transaction management
- Batch processing

### 2. Helper Classes

#### UnitofWorkValidationHelper.cs
- Entity validation logic
- Data integrity checks
- Business rule validation
- Primary key validation

#### UnitofWorkDataHelper.cs
- Data conversion utilities
- Collection filtering
- Entity cloning
- Type conversion

#### UnitofWorkEventHelper.cs
- Event management
- Event parameter creation
- Event lifecycle handling

#### UnitofWorkDefaultsHelper.cs
- Integration with DefaultsManager
- Default value application
- Rule-based default processing
- Entity-specific defaults

#### UnitofWorkStateHelper.cs
- Entity state management
- Change tracking utilities
- State transition logic

#### UnitofWorkCollectionHelper.cs
- ObservableBindingList management
- Collection synchronization
- Tracking item management

### 3. DefaultsManager Integration Points

#### Insert Operations
- Apply default values before entity insertion
- Rule-based default value resolution
- Entity-specific default configurations
- Validation of default value applications

#### Update Operations
- Apply conditional defaults on updates
- Modified date/user tracking
- Version control defaults
- Audit trail defaults

#### Validation Integration
- Validate default value rules before application
- Ensure default values meet entity constraints
- Handle default value conflicts

### 4. Interface Enhancements

#### IUnitofWorkDefaults<T>
```csharp
public interface IUnitofWorkDefaults<T> where T : Entity, new()
{
    void ApplyDefaults(T entity, DefaultValueContext context);
    Task<T> ApplyDefaultsAsync(T entity, DefaultValueContext context);
    bool HasDefaults(string FieldName);
    DefaultValue GetDefaultForField(string FieldName);
}
```

#### IUnitofWorkValidation<T>
```csharp
public interface IUnitofWorkValidation<T> where T : Entity, new()
{
    IErrorsInfo ValidateEntity(T entity);
    IErrorsInfo ValidateForInsert(T entity);
    IErrorsInfo ValidateForUpdate(T entity);
    IErrorsInfo ValidateForDelete(T entity);
}
```

## Implementation Steps

### Phase 1: Backup and Structure Setup
1. Create backup copy of original UnitofWork.cs as UnitofWork.Original.cs
2. Create folder structure for helpers
3. Create base interfaces and enums

### Phase 2: Helper Classes Creation
1. Create validation helper with entity validation logic
2. Create data helper with utility methods
3. Create defaults helper with DefaultsManager integration
4. Create state helper with change tracking logic
5. Create collection helper with list management
6. Create event helper with event management

### Phase 3: Partial Classes Creation
1. Create UnitofWork.Core.cs with core structure
2. Create UnitofWork.CRUD.cs with CRUD operations
3. Create UnitofWork.EntityManagement.cs with entity handling
4. Create UnitofWork.Navigation.cs with navigation features
5. Create UnitofWork.ChangeTracking.cs with change management
6. Create UnitofWork.Events.cs with event handling
7. Create UnitofWork.Commit.cs with transaction management

### Phase 4: Integration and Testing
1. Integrate DefaultsManager into CRUD operations
2. Update interfaces to support new functionality
3. Create comprehensive unit tests
4. Performance testing and optimization

### Phase 5: Documentation and Examples
1. Create usage documentation
2. Create example implementations
3. Create migration guide from old to new structure

## Benefits of This Refactoring

### Maintainability
- Smaller, focused code files
- Clear separation of concerns
- Easier to understand and modify

### Testability
- Individual components can be unit tested
- Mock dependencies easily
- Better test coverage

### Extensibility
- Easy to add new features
- Plugin architecture for helpers
- Interface-based design

### Performance
- Lazy loading of helpers
- Optimized for specific operations
- Reduced memory footprint

### Integration
- Seamless DefaultsManager integration
- Better error handling
- Enhanced validation capabilities

## DefaultsManager Integration Details

### Insert Operations Integration
1. **Pre-Insert Defaults**: Apply static and rule-based defaults before validation
2. **Entity-Specific Defaults**: Support entity.column specific default configurations
3. **Context-Aware Defaults**: Pass entity context to default resolvers
4. **Validation Integration**: Validate applied defaults meet entity constraints

### Update Operations Integration
1. **Conditional Defaults**: Apply defaults only when fields are null/empty
2. **Audit Defaults**: Automatically set modified date, user, version
3. **Computed Defaults**: Recalculate computed fields on updates
4. **Change-Based Defaults**: Apply defaults based on what changed

### Configuration Management
1. **Entity-Level Configuration**: Configure defaults per entity type
2. **Field-Level Configuration**: Granular control over field defaults
3. **Rule Precedence**: Define rule execution order and precedence
4. **Performance Optimization**: Cache frequently used default configurations

## File Structure After Refactoring

```
DataManagementEngineStandard/Editor/UOW/
??? UnitofWork.Original.cs                    # Backup of original
??? UnitofWork.Core.cs                        # Main partial class
??? UnitofWork.CRUD.cs                        # CRUD operations
??? UnitofWork.EntityManagement.cs            # Entity management
??? UnitofWork.Navigation.cs                  # Navigation and paging
??? UnitofWork.ChangeTracking.cs             # Change tracking
??? UnitofWork.Events.cs                     # Event handling
??? UnitofWork.Commit.cs                     # Commit/rollback
??? Helpers/
?   ??? UnitofWorkValidationHelper.cs        # Validation logic
?   ??? UnitofWorkDataHelper.cs              # Data utilities
?   ??? UnitofWorkEventHelper.cs             # Event management
?   ??? UnitofWorkDefaultsHelper.cs          # Defaults integration
?   ??? UnitofWorkStateHelper.cs             # State management
?   ??? UnitofWorkCollectionHelper.cs        # Collection management
??? Interfaces/
?   ??? IUnitofWorkDefaults.cs               # Defaults interface
?   ??? IUnitofWorkValidation.cs             # Validation interface
?   ??? IUnitofWorkHelpers.cs                # Helper interfaces
??? Examples/
    ??? UnitofWorkExamples.cs                # Usage examples
```

## Success Criteria
1. All existing functionality preserved
2. Comprehensive test coverage (>90%)
3. Performance maintained or improved
4. DefaultsManager fully integrated
5. Clear documentation and examples
6. Backward compatibility maintained
7. Helper classes independently testable
8. Partial classes logically organized