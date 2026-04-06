---
name: defaults
description: Reference for the BeepDM Defaults system that automatically fills entity fields with computed or literal values before a record is saved.
---
# Defaults System — Skill Reference

## Purpose

The **Defaults system** automatically fills entity fields with computed or literal values (e.g. `NEWGUID`, `NOW`, `USERNAME`, a literal `"Active"`) before a record is saved.  
Use it to inject audit fields, identity columns, timestamps, and context-aware values without scattering resolution logic across forms, services, or data sources.

---

## Architecture

```
DefaultsManager (static + IDefaultsManager)
    │
    ├── EntityDefaultsProfile registry  ("datasource::entity" key)
    │       └── FieldDefaultRule[]  (FieldName, RuleString, ApplyOnlyIfNull)
    │
    ├── RuleNormalizer  ──►  ParsedRule  (IsLiteral vs. expression)
    │
    └── DefaultValueResolverManager
            └── IDefaultValueResolver[] (built-in + plug-in via [DefaultResolverAttribute])
```

### Dual Surface

| Surface | When to use |
|---------|-------------|
| `DefaultsManager.Method(editor, ...)` **static helpers** | Scripts, simple apps, legacy code |
| `IDefaultsManager` injected via DI | Services, controllers, testable code |

### Rule-String Convention

| Prefix | Meaning | Example |
|--------|---------|---------|
| `:TOKEN` | Expression — routed to resolver pipeline | `:NOW`, `:USERNAME`, `:NEWGUID` |
| no prefix | Literal — used as-is | `"Active"`, `"1"`, `"pending"` |

Legacy bare tokens (`NOW`, `USERNAME`, `NEWGUID`, …) are still accepted for backward compatibility but emit a deprecation warning.

---

## Core API — `DefaultsManager` Static Surface

All static methods share the same signatures as `IDefaultsManager` but accept `IDMEEditor editor` as a first parameter.

### Initialization

```csharp
// Call once at startup (thread-safe; second call is a no-op)
DefaultsManager.Initialize(editor);

// Or let it auto-initialize on first use:
DefaultsManager.EnsureInitialized(editor);   // called internally by every method
```

### Profile Registration

```csharp
// Register a profile for a specific datasource + entity
DefaultsManager.RegisterProfile(editor, datasource: "OrdersDB", entity: "Orders", profile);

// Retrieve (returns null if not registered)
var p = DefaultsManager.GetProfile(datasource, entity);

// Remove
DefaultsManager.RemoveProfile(editor, datasource, entity);
```

Profile key format: `"datasource::entity"` (OrdinalIgnoreCase).

### Apply — Three Overloads

```csharp
// Dictionary<string, object>
IErrorsInfo r1 = DefaultsManager.Apply(editor, datasource, entity, dict, context: null);

// POCO (reflection-based, property names matched OrdinalIgnoreCase)
IErrorsInfo r2 = DefaultsManager.Apply<Order>(editor, datasource, entity, order, context: null);

// DataRow
IErrorsInfo r3 = DefaultsManager.Apply(editor, datasource, entity, dataRow, context: null);
```

`context` (`IPassedArgs`) is optional; pass `null` unless a resolver needs request-scope data.  
`ApplyOnlyIfNull = true` (default) — fields that already have a non-null, non-empty value are left unchanged.

### Resolve a Single Rule

```csharp
object value = DefaultsManager.Resolve(editor, ":NOW", context: null);
// → DateTime.UtcNow (approximately)
```

### Diagnostics

```csharp
// Test a rule against sample parameters
IErrorsInfo test = DefaultsManager.TestRule(editor, ":USERNAME", new Dictionary<string,string>());

// Validate syntax only (no side effects)
IErrorsInfo valid = DefaultsManager.ValidateRule(editor, ":NEWGUID");
```

### Resolver Management

```csharp
// Register a custom resolver at runtime (no assembly scan needed)
DefaultsManager.RegisterCustomResolver(editor, new MyCustomResolver(editor));

// Enumerate all registered resolvers
IReadOnlyList<string> names = DefaultsManager.GetAvailableResolvers(editor);
```

---

## `EntityDefaultsProfile` + `FieldDefaultRule`

### Creating a Profile

```csharp
var profile = EntityDefaultsProfile
    .For("Orders")
    .Set("Id",          ":NEWGUID")
    .Set("Status",      "pending")          // literal
    .Set("CreatedAt",   ":NOW")
    .Set("CreatedBy",   ":USERNAME")
    .Set("Version",     1)                  // int literal overload
    .Set("IsActive",    true);              // bool literal overload
```

### `FieldDefaultRule` Properties

| Property | Type | Notes |
|----------|------|-------|
| `FieldName` | `string` | Matched case-insensitively against record keys / POCO properties |
| `RuleString` | `string` | Raw rule, e.g. `":NOW"` or `"pending"` |
| `ApplyOnlyIfNull` | `bool` | `true` = skip if field already has a value (default) |
| `IsExpression` | `bool` | `true` when `RuleString` starts with `:` |
| `ExpressionBody` | `string` | Token after the colon, e.g. `"NOW"` |

### Removing a Rule

```csharp
profile.Remove("Version");
```

---

## Template Factories

Pre-built profiles for common patterns — call as a factory, then add custom rules.

```csharp
// Full audit template (7 fields)
var p = EntityDefaultsProfile.FullTemplate("Orders");

// Then register:
DefaultsManager.RegisterProfile(editor, "OrdersDB", "Orders", p);
```

| Factory | Fields Populated |
|---------|-----------------|
| `IdentityTemplate(entity)` | `Id` = `:NEWGUID` |
| `TimestampTemplate(entity)` | `CreatedAt` = `:NOW`, `ModifiedAt` = `:NOW` |
| `UserStampTemplate(entity)` | `CreatedBy` = `:USERNAME`, `ModifiedBy` = `:USERNAME` |
| `AuditTemplate(entity)` | `CreatedBy/At`, `ModifiedBy/At`, `IsActive=true`, `Version=1`, `RowGuid` = `:NEWGUID` |
| `FullTemplate(entity)` | `Id` = `:NEWGUID` **+** all AuditTemplate fields |

---

## Built-in Resolvers

| ResolverName | Key Tokens | Notes |
|---|---|---|
| `"Guid"` | `NEWGUID`, `GUID`, `UUID` | Returns `Guid.NewGuid().ToString()` |
| `"DateTime"` | `NOW`, `TODAY`, `ADDDAYS`, `FORMAT`, `STARTOFMONTH` | UTC-aware |
| `"UserContext"` | `USERNAME`, `USERID`, `USERDOMAIN`, `USERROLE` | Current OS / thread principal |
| `"Environment"` | `ENV`, `ENVIRONMENTVARIABLE`, `TEMP`, `SYSTEMPATH` | `Environment.GetEnvironmentVariable` |
| `"SystemInfo"` | `MACHINENAME`, `OSVERSION`, `PROCESSORCOUNT` | `Environment.*` system facts |
| `"Configuration"` | `CONFIG`, `APPSETTING`, `CONNECTIONSTRING` | Reads from `ConfigEditor` |
| `"DataSource"` | `GETENTITY`, `LOOKUP`, `QUERY`, `COUNT`, `MAX`, `MIN`, `SUM`, `AVG` | Live query via `IDMEEditor` |
| `"Expression"` | `IF`, `CASE`, `ISNULL`, `COALESCE`, `EQ`, `GT`, `LT` | Conditional logic |
| `"Formula"` | `SEQUENCE`, `RANDOM`, `ADD`, `SUBTRACT`, `MULTIPLY`, `DIVIDE` | Arithmetic / sequences |
| `"ObjectProperty"` | `PROPERTY`, `FIELD`, `OBJECTVALUE`, `NESTED` | Reads from context object |

Rule examples: `:NOW`, `:NEWGUID`, `:USERNAME`, `:ENV.PATH`, `:CONFIG.MyKey`

---

## Rule-Parsing Pipeline (internal, rarely touched)

```
Raw rule string
    └─► RuleNormalizer.Normalize(rule)
            └─► ParsedRule  { IsLiteral, RuleSyntaxVersion, NormalizedRule }
                    └─► DefaultValueResolverManager
                            └─► foreach resolver: CanHandle(token) → ResolveValue(rule, context)
```

**`RuleSyntaxVersion`**: `V1Legacy=0`, `V1Dot=1`, `Literal=2`, `Unknown=3`  
**`DotStyleRuleParser`**: handles dot-segment DSL (`ADD.10.5`); used internally, not a plugin contract.

---

## Extensibility — Adding a Custom Resolver

### Step 1 — Implement `IDefaultValueResolver`

```csharp
using TheTechIdea.Beep.Editor.Defaults.Attributes;
using TheTechIdea.Beep.Editor;

[DefaultResolver(
    resolverName:   "Tenant",
    displayName:    "Tenant Context Resolver",
    Description =   "Resolves current tenant ID and name from ambient context.",
    SupportedTokens = "TENANTID,TENANTNAME,TENANTCODE",
    Version =       "1.0")]
public class TenantContextResolver : BaseDefaultValueResolver
{
    private readonly IDMEEditor _editor;

    // Preferred ctor — receives IDMEEditor via reflection in DefaultResolverRegistry
    public TenantContextResolver(IDMEEditor editor) { _editor = editor; }

    // Fallback ctor — required when no IDMEEditor is available at discovery time
    public TenantContextResolver() { }

    public override bool CanHandle(string token) =>
        token.Equals("TENANTID", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("TENANTNAME", StringComparison.OrdinalIgnoreCase);

    public override object ResolveValue(string rule, IPassedArgs context)
    {
        var token = rule.TrimStart(':').ToUpperInvariant();
        return token switch
        {
            "TENANTID"   => TenantContext.Current?.Id ?? Guid.Empty.ToString(),
            "TENANTNAME" => TenantContext.Current?.Name ?? "",
            _ => null
        };
    }
}
```

### Step 2 — Deploy

Drop the assembly into the drivers/plugins folder. `AssemblyHandler` scans it, detects `IDefaultValueResolver` + `[DefaultResolverAttribute]`, and populates `ConfigEditor.DefaultResolverClasses`.

### Step 3 — Discover (once at startup)

```csharp
var registry = new DefaultResolverRegistry(editor);
registry.Discover();   // instantiates + registers all decorated resolvers
```

Or register a single resolver without the registry:

```csharp
DefaultsManager.RegisterCustomResolver(editor, new TenantContextResolver(editor));
```

### `[DefaultResolverAttribute]` Properties

| Property | Required | Purpose |
|----------|----------|---------|
| `ResolverName` | ✅ | Unique key, also used by `DefaultResolverRegistry.TryGet(name)` |
| `DisplayName` | ✅ | Human-readable label |
| `Description` | — | Tooltip / docs |
| `Author` | — | Optional metadata |
| `Version` | — | Optional metadata |
| `IconPath` | — | Optional icon |
| `SupportedTokens` | — | Comma-separated token list for discovery UI |

### `DefaultResolverRegistry` API

```csharp
registry.Discover();                                  // scan + register all
registry.TryGet("Tenant", out var descriptor);        // → DefaultResolverDescriptor
registry.GetDescriptors();                            // all discovered descriptors
IDefaultValueResolver r = registry.Create("Tenant"); // instantiate by name
```

**Constructor selection (reflection)**: `DefaultResolverRegistry.Discover()` tries `new T(IDMEEditor)` first, falls back to `new T()`. Always provide both ctors.

---

## DI / `IDefaultsManager`

```csharp
// Registration (e.g. Autofac or MS.Extensions.DI)
services.AddSingleton<IDefaultsManager, DefaultsManager>();

// Usage in a service
public class OrderService
{
    private readonly IDefaultsManager _defaults;
    private readonly IDMEEditor _editor;

    public OrderService(IDefaultsManager defaults, IDMEEditor editor)
    {
        _defaults = defaults;
        _editor = editor;
    }

    public void CreateOrder(Order order)
    {
        _defaults.Apply(_editor, "OrdersDB", "Orders", order, context: null);
        // order.Id, CreatedAt, CreatedBy etc. are now populated
    }
}
```

---

## Complete Usage Example

```csharp
// 1. Initialize (once at app startup)
DefaultsManager.Initialize(editor);

// 2. Build a profile using the FullTemplate + custom fields
var profile = EntityDefaultsProfile
    .FullTemplate("Orders")          // Id, CreatedBy/At, ModifiedBy/At, IsActive, Version, RowGuid
    .Set("Status", "pending")        // literal
    .Set("Region", ":ENV.REGION");   // environment variable

// 3. Register the profile
DefaultsManager.RegisterProfile(editor, datasource: "OrdersDB", entity: "Orders", profile);

// 4a. Apply to a Dictionary
var record = new Dictionary<string, object> { ["CustomerName"] = "Acme" };
DefaultsManager.Apply(editor, "OrdersDB", "Orders", record, context: null);
// record["Id"]        → new Guid string
// record["CreatedAt"] → DateTime.UtcNow
// record["Status"]    → "pending"

// 4b. Apply to a POCO
var order = new Order { CustomerName = "Acme" };
DefaultsManager.Apply(editor, "OrdersDB", "Orders", order, context: null);

// 4c. Apply to a DataRow
DefaultsManager.Apply(editor, "OrdersDB", "Orders", dataRow, context: null);

// 5. Resolve a single rule ad-hoc
string userId = (string)DefaultsManager.Resolve(editor, ":USERNAME", null);
```

---

## File Locations

| Purpose | Path |
|---------|------|
| Static manager + profiles | `DataManagementEngineStandard/Editor/Defaults/DefaultsManager.cs` |
| Apply overloads (3) | `DataManagementEngineStandard/Editor/Defaults/DefaultsManager.Apply.cs` |
| Profile + templates + `FieldDefaultRule` | `DataManagementEngineStandard/Editor/Defaults/EntityDefaultsProfile.cs` |
| Interface | `DataManagementEngineStandard/Editor/Defaults/IDefaultsManager.cs` |
| Attribute | `DataManagementModelsStandard/Editor/Defaults/Attributes/DefaultResolverAttribute.cs` |
| Registry descriptor | `DataManagementEngineStandard/Editor/Defaults/Registry/DefaultResolverDescriptor.cs` |
| Registry | `DataManagementEngineStandard/Editor/Defaults/Registry/DefaultResolverRegistry.cs` |
| Resolver base | `DataManagementEngineStandard/Editor/Defaults/Resolvers/BaseDefaultValueResolver.cs` |
| Resolver manager | `DataManagementEngineStandard/Editor/Defaults/Resolvers/DefaultValueResolverManager.cs` |
| Built-in resolvers | `DataManagementEngineStandard/Editor/Defaults/Resolvers/*.cs` (10 files) |
| Rule parser entry | `DataManagementEngineStandard/Editor/Defaults/RuleParsing/RuleNormalizer.cs` |
| Parsed result | `DataManagementEngineStandard/Editor/Defaults/RuleParsing/ParsedRule.cs` |
| AssemblyHandler scan hook | `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Scanning.cs` |
| AssemblyHandler attr hook | `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Helpers.cs` |
| `AssemblyClassDefinition` | `DataManagementModelsStandard/ConfigUtil/AssemblyClassDefinition.cs` |
| `IConfigEditor` + `ConfigEditor` | `DefaultResolverClasses` property (3 locations in ConfigEditor.cs) |
| Resolver README | `DataManagementEngineStandard/Editor/Defaults/Resolvers/README.md` |
