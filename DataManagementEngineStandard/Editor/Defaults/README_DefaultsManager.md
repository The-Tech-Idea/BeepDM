# Enhanced DefaultsManager Documentation

## Overview

The Enhanced DefaultsManager provides a modern, helper-based architecture for setting default values for entity columns in any data source. It supports both static values and dynamic rules with extensible resolvers.

## Key Features

- **Helper-based Architecture**: Modular design with separation of concerns
- **Extensible Resolvers**: Easy to add custom rule resolvers
- **Entity Column Defaults**: Set defaults at the column level for any entity
- **Dynamic Rules**: Support for formulas and logic expressions
- **Validation**: Comprehensive validation for rules and configurations
- **Templates**: Pre-built templates for common scenarios
- **Import/Export**: Portable configuration management

## Architecture

### Core Components

1. **DefaultsManager** - Main entry point (partial class)
2. **IDefaultValueHelper** - Manages CRUD operations for default values
3. **IDefaultValueResolverManager** - Manages rule resolvers
4. **IDefaultValueValidationHelper** - Validates configurations and rules
5. **Built-in Resolvers** - DateTime, UserContext, SystemInfo, Guid, Formula

### Helper Pattern

```csharp
// Initialize the manager
DefaultsManager.Initialize(dmeEditor);

// Access helpers directly if needed
var helper = DefaultsManager.DefaultValueHelper;
var resolverManager = DefaultsManager.ResolverManager;
var validator = DefaultsManager.ValidationHelper;
```

## Basic Usage

### Setting Column Defaults

```csharp
// Static value
DefaultsManager.SetColumnDefault(editor, "MyDatabase", "Users", "Status", "Active");

// Dynamic rule - current timestamp
DefaultsManager.SetColumnDefault(editor, "MyDatabase", "Users", "CreatedDate", "NOW", isRule: true);

// Dynamic rule - current user
DefaultsManager.SetColumnDefault(editor, "MyDatabase", "Users", "CreatedBy", "USERNAME", isRule: true);

// Dynamic rule - auto-generated ID
DefaultsManager.SetColumnDefault(editor, "MyDatabase", "Users", "UserID", "NEWGUID", isRule: true);
```

### Getting Column Defaults

```csharp
// Get resolved default value for a column
var defaultValue = DefaultsManager.GetColumnDefault(editor, "MyDatabase", "Users", "CreatedDate");

// Get all defaults for an entity
var entityDefaults = DefaultsManager.GetEntityDefaults(editor, "MyDatabase", "Users");
```

### Applying Defaults to Records

```csharp
// Apply defaults to a new record
var user = new User();
user = DefaultsManager.ApplyDefaultsToRecord(editor, "MyDatabase", "Users", user) as User;
```

## Built-in Rule Types

### DateTime Resolver

```csharp
"NOW"                    // Current date and time
"TODAY"                  // Current date (midnight)
"YESTERDAY"              // Yesterday's date
"TOMORROW"               // Tomorrow's date
"CURRENTDATE"            // Same as TODAY
"CURRENTTIME"            // Current time of day
"ADDDAYS(TODAY, 7)"      // Add 7 days to today
"FORMAT(NOW, 'yyyy-MM-dd')" // Formatted current date
```

### User Context Resolver

```csharp
"USERNAME"               // Current Windows username
"USERID"                 // Current user identifier
"USEREMAIL"              // Current user email (if available)
"CURRENTUSER"            // Same as USERNAME
```

### System Info Resolver

```csharp
"MACHINENAME"            // Current machine name
"HOSTNAME"               // Same as machine name
"VERSION"                // .NET Framework version
"APPVERSION"             // Application version
```

### GUID Resolver

```csharp
"NEWGUID"                // Generate new GUID
"GUID"                   // Same as NEWGUID
"UUID"                   // Same as NEWGUID
"GUID(N)"                // GUID without hyphens
"GUID(D)"                // GUID with hyphens (default)
```

### Formula Resolver

```csharp
"SEQUENCE(1000)"         // Start sequence from 1000
"INCREMENT(fieldname)"   // Increment based on existing field
"RANDOM(1, 100)"         // Random number between 1 and 100
"CALCULATE(field1 + field2)" // Simple calculation
```

## Advanced Features

### Custom Resolvers

Create custom resolvers by implementing `IDefaultValueResolver`:

```csharp
public class CustomBusinessResolver : BaseDefaultValueResolver
{
    public CustomBusinessResolver(IDMEEditor editor) : base(editor) { }

    public override string ResolverName => "BusinessLogic";

    public override IEnumerable<string> SupportedRuleTypes => new[]
    {
        "NEXTORDERID", "CUSTOMERCODE", "SALESREP"
    };

    public override object ResolveValue(string rule, IPassedArgs parameters)
    {
        return rule.ToUpperInvariant() switch
        {
            "NEXTORDERID" => GetNextOrderId(),
            "CUSTOMERCODE" => GenerateCustomerCode(),
            "SALESREP" => GetDefaultSalesRep(),
            _ => null
        };
    }

    public override bool CanHandle(string rule)
    {
        var upperRule = rule.ToUpperInvariant().Trim();
        return SupportedRuleTypes.Any(type => upperRule.Contains(type));
    }

    public override IEnumerable<string> GetExamples()
    {
        return new[]
        {
            "NEXTORDERID - Get next order ID from sequence",
            "CUSTOMERCODE - Generate customer code",
            "SALESREP - Get default sales representative"
        };
    }

    private string GetNextOrderId() => $"ORD-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
    private string GenerateCustomerCode() => $"CUST-{DateTime.Now:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    private string GetDefaultSalesRep() => "UNASSIGNED";
}

// Register the custom resolver
DefaultsManager.RegisterCustomResolver(editor, new CustomBusinessResolver(editor));
```

### Bulk Operations

```csharp
// Set multiple defaults at once
var columnDefaults = new Dictionary<string, (string value, bool isRule)>
{
    { "CreatedBy", ("USERNAME", true) },
    { "CreatedDate", ("NOW", true) },
    { "Status", ("Active", false) },
    { "IsDeleted", ("false", false) },
    { "Priority", ("Normal", false) }
};

var result = DefaultsManager.SetMultipleColumnDefaults(editor, "MyDatabase", "Orders", columnDefaults);
```

### Templates

```csharp
// Create audit field template
var auditFields = DefaultsManager.CreateDefaultValueTemplate(editor, DefaultValueTemplateType.AuditFields);

// Create system field template  
var systemFields = DefaultsManager.CreateDefaultValueTemplate(editor, DefaultValueTemplateType.SystemFields);

// Create common defaults template
var commonDefaults = DefaultsManager.CreateDefaultValueTemplate(editor, DefaultValueTemplateType.CommonDefaults);
```

### Validation

```csharp
// Validate a rule before using it
var (validation, testValue) = DefaultsManager.TestRule(editor, "NOW");
if (validation.Flag == Errors.Ok)
{
    Console.WriteLine($"Rule resolved to: {testValue}");
}

// Validate a default value configuration
var defaultValue = new DefaultValue { PropertyName = "CreatedDate", Rule = "NOW" };
var validationResult = DefaultsManager.ValidateDefaultValue(editor, defaultValue);
```

### Import/Export

```csharp
// Export defaults configuration
var exportedConfig = DefaultsManager.ExportDefaults(editor, "MyDatabase");
File.WriteAllText("defaults-backup.json", exportedConfig);

// Import defaults configuration
var importedConfig = File.ReadAllText("defaults-backup.json");
var importResult = DefaultsManager.ImportDefaults(editor, "AnotherDatabase", importedConfig, replaceExisting: false);
```

## Rule Syntax Examples

### Common Patterns

```csharp
// Login tracking
"CreatedBy" -> "USERNAME"
"LoginTime" -> "NOW"

// Unique identifiers
"OrderID" -> "SEQUENCE(10000)"
"TransactionID" -> "NEWGUID"

// Timestamps with formatting
"DateCreated" -> "FORMAT(NOW, 'yyyy-MM-dd HH:mm:ss')"
"FileDate" -> "FORMAT(TODAY, 'yyyyMMdd')"

// Calculated values
"ExpiryDate" -> "ADDDAYS(TODAY, 30)"
"NextReview" -> "ADDDAYS(NOW, 90)"

// Random values for testing
"TestScore" -> "RANDOM(70, 100)"
"Priority" -> "RANDOM(1, 5)"
```

### Complex Examples

```csharp
// Conditional logic (requires custom resolver)
"StatusCode" -> "IF(HOUR(NOW) > 17, 'CLOSED', 'OPEN')"

// Environment-based defaults
"Environment" -> "SWITCH(MACHINENAME, 'PROD-01', 'PRODUCTION', 'DEV')"

// User-based assignments
"Department" -> "LOOKUP(USERNAME, 'UserDepartments')"
```

## Best Practices

### 1. Initialization
Always initialize the DefaultsManager at application startup:

```csharp
// In your application startup
DefaultsManager.Initialize(dmeEditor);
```

### 2. Rule Validation
Validate rules during configuration, not at runtime:

```csharp
var (validation, _) = DefaultsManager.TestRule(editor, userInputRule);
if (validation.Flag == Errors.Failed)
{
    // Show validation error to user
    ShowError(validation.Message);
    return;
}
```

### 3. Error Handling
Always check operation results:

```csharp
var result = DefaultsManager.SetColumnDefault(editor, ds, entity, column, value, isRule);
if (result.Flag == Errors.Failed)
{
    Logger.LogError($"Failed to set default: {result.Message}");
}
```

### 4. Performance
Cache resolved values when appropriate:

```csharp
// For frequently accessed static rules like USERNAME
private static string _cachedUsername;
private static DateTime _lastUsernameFetch;

public string GetCachedUsername()
{
    if (_cachedUsername == null || DateTime.Now - _lastUsernameFetch > TimeSpan.FromMinutes(5))
    {
        _cachedUsername = DefaultsManager.ResolverManager.ResolveValue("USERNAME", null)?.ToString();
        _lastUsernameFetch = DateTime.Now;
    }
    return _cachedUsername;
}
```

### 5. Custom Resolvers
Design custom resolvers to be stateless and thread-safe:

```csharp
public class ThreadSafeResolver : BaseDefaultValueResolver
{
    private readonly object _lockObject = new object();
    
    public override object ResolveValue(string rule, IPassedArgs parameters)
    {
        lock (_lockObject)
        {
            // Thread-safe resolution logic
        }
    }
}
```

## Migration from Legacy DefaultsManager

The enhanced DefaultsManager maintains backward compatibility:

```csharp
// Old way (still works)
var defaults = DefaultsManager.GetDefaults(editor, dataSourceName);
var resolved = DefaultsManager.ResolveDefaultValue(editor, defaultValue, parameters);

// New way (enhanced features)
DefaultsManager.Initialize(editor); // Initialize once
var columnDefault = DefaultsManager.GetColumnDefault(editor, dataSourceName, entityName, columnName);
```

## Troubleshooting

### Common Issues

1. **Manager not initialized**: Call `Initialize()` before using any methods
2. **Rule not resolving**: Check if appropriate resolver is registered
3. **Validation failures**: Use `TestRule()` to debug rule syntax
4. **Performance issues**: Consider caching frequently resolved values

### Debugging

```csharp
// Get all available resolvers
var resolvers = DefaultsManager.GetAvailableResolvers(editor);
foreach (var resolver in resolvers)
{
    Console.WriteLine($"Resolver: {resolver.Key}");
    foreach (var ruleType in resolver.Value)
    {
        Console.WriteLine($"  - {ruleType}");
    }
}

// Get resolver examples
var examples = DefaultsManager.GetResolverExamples(editor);
foreach (var example in examples)
{
    Console.WriteLine($"Examples for {example.Key}:");
    foreach (var ex in example.Value)
    {
        Console.WriteLine($"  {ex}");
    }
}
```

## Conclusion

The Enhanced DefaultsManager provides a robust, extensible system for managing default values in any data source. Its helper-based architecture makes it easy to extend with custom resolvers while maintaining backward compatibility with existing code.