# DefaultsManager — Comprehensive Guide & Examples

## Table of Contents

1. [Overview & Core Concept](#1-overview--core-concept)
2. [Rule-String Convention](#2-rule-string-convention)
3. [Quick-Start](#3-quick-start)
4. [EntityDefaultsProfile — Fluent Builder](#4-entitydefaultsprofile--fluent-builder)
5. [Built-in Templates](#5-built-in-templates)
6. [Applying Defaults](#6-applying-defaults)
   - [Dictionary record](#61-dictionary-record)
   - [POCO / strongly-typed](#62-poco--strongly-typed)
   - [DataRow](#63-datarow)
7. [Static helpers vs. DI instance](#7-static-helpers-vs-di-instance)
8. [Built-in Resolvers Reference](#8-built-in-resolvers-reference)
   - [DateTime](#81-datetime-resolver)
   - [UserContext](#82-usercontext-resolver)
   - [Guid](#83-guid-resolver)
   - [Environment](#84-environment-resolver)
   - [SystemInfo](#85-systeminfo-resolver)
   - [Configuration](#86-configuration-resolver)
9. [Dot-style rule DSL](#9-dot-style-rule-dsl)
10. [Testing & validating rules](#10-testing--validating-rules)
11. [Custom resolvers](#11-custom-resolvers)
12. [Column-level defaults (legacy API)](#12-column-level-defaults-legacy-api)
13. [Common scenarios & recipes](#13-common-scenarios--recipes)
14. [Architecture notes](#14-architecture-notes)

---

## 1. Overview & Core Concept

`DefaultsManager` automatically populates entity-field values when a new record is created.

```
Developer defines rules  →  Rules stored in EntityDefaultsProfile
                             ↓
Application calls Apply()  →  DefaultsManager resolves each rule
                             ↓
Fields are populated in  Dictionary / POCO / DataRow
```

Two responsibilities are deliberately separated:

| Concern | Component | When it runs |
|---------|-----------|-------------|
| **Parse** (syntax → structure) | `RuleNormalizer` + `DotStyleRuleParser` | Parse time |
| **Resolve** (token + context → value) | `DefaultValueResolverManager` + 11 built-in resolvers | Apply time |

---

## 2. Rule-String Convention

A single character prefix tells the system how to treat a rule string:

| Prefix | Meaning | Example | What happens |
|--------|---------|---------|-------------|
| `:` | **Expression** — parse and resolve at runtime | `:NOW` | Calls the DateTime resolver → `DateTime.Now` |
| *(none)* | **Literal** — used as-is, no resolver invoked | `Active` | Field is set to the string `"Active"` |

```csharp
// Expression (resolved at runtime)
":NOW"          // → DateTime.Now
":USERNAME"     // → Environment.UserName
":NEWGUID"      // → Guid.NewGuid().ToString()
":ADDDAYS(NOW,7)"  // → DateTime.Now.AddDays(7)

// Literals (used verbatim)
"Active"        // → "Active"
"1"             // → "1"
"pending"       // → "pending"
"true"          // → "true"
```

> **Backward compatibility**: bare tokens without `:` (e.g. `NOW`, `USERNAME`) still work
> but emit a deprecation warning (`NRM002`).  Always use `:` in new code.

---

## 3. Quick-Start

```csharp
using TheTechIdea.Beep.Editor.Defaults;

// 1. Initialize (once per application lifetime)
DefaultsManager.Initialize(editor);   // editor is your IDMEEditor

// 2. Define defaults for an entity
var profile = EntityDefaultsProfile.For("Orders")
    .Set("Id",         ":NEWGUID")    // auto-generate a GUID key
    .Set("CreatedAt",  ":NOW")        // timestamp
    .Set("CreatedBy",  ":USERNAME")   // current user
    .Set("Status",     "pending")     // literal default
    .Set("Priority",   1);            // int literal

// 3. Register the profile against a data source + entity name
DefaultsManager.RegisterProfile("mydb", "Orders", profile);

// 4. Apply when creating a new record
var record = new Dictionary<string, object>();
var result = DefaultsManager.ApplyToRecord(editor, "mydb", "Orders", record);

// record["Id"]        = "550e8400-e29b-41d4-a716-446655440000"
// record["CreatedAt"] = DateTime.Now
// record["CreatedBy"] = "jsmith"
// record["Status"]    = "pending"
// record["Priority"]  = "1"
```

---

## 4. EntityDefaultsProfile — Fluent Builder

### Creating a profile

```csharp
var profile = EntityDefaultsProfile.For("Customers")
    .Set("Id",          ":NEWGUID")
    .Set("CreatedAt",   ":NOW")
    .Set("CreatedBy",   ":USERNAME")
    .Set("Status",      "active")
    .Set("IsDeleted",   false)          // bool overload → "false"
    .Set("RetryCount",  0)              // int overload  → "0"
    .Set("Region",      "EMEA");
```

### `Set` overloads

```csharp
// String rule (expression or literal)
profile.Set("FieldName", ":NOW");
profile.Set("FieldName", "SomeValue");

// Int literal
profile.Set("FieldName", 42);

// Bool literal
profile.Set("FieldName", true);

// applyOnlyIfNull = false → always overwrite, even if the field already has a value
profile.Set("ModifiedAt", ":NOW", applyOnlyIfNull: false);
```

### Removing a rule

```csharp
profile.Remove("Region");   // field no longer gets a default
```

### Chaining edits after creation

```csharp
// Profiles are mutable — add/replace/remove at any time before registering
profile
    .Set("Category", "General")
    .Remove("Region")
    .Set("ModifiedAt", ":NOW", applyOnlyIfNull: false);
```

---

## 5. Built-in Templates

Five ready-made templates cover the most common audit-field patterns:

```csharp
// 5a. AuditTemplate — CreatedBy, CreatedAt, ModifiedBy, ModifiedAt, IsActive, Version, RowGuid
var profile = EntityDefaultsProfile.AuditTemplate("Orders");

// 5b. TimestampTemplate — CreatedAt, ModifiedAt
var profile = EntityDefaultsProfile.TimestampTemplate("Documents");

// 5c. UserStampTemplate — CreatedBy, ModifiedBy
var profile = EntityDefaultsProfile.UserStampTemplate("Notes");

// 5d. IdentityTemplate — Id = :NEWGUID
var profile = EntityDefaultsProfile.IdentityTemplate("Products");

// 5e. FullTemplate — Id + full audit fields (no RowGuid)
var profile = EntityDefaultsProfile.FullTemplate("Invoices");
```

#### Extending a template

```csharp
// Start from a template, then add domain-specific fields
var profile = EntityDefaultsProfile.FullTemplate("Orders")
    .Set("Status",      "new")
    .Set("Priority",    1)
    .Set("TenantId",    ":CONFIG(DefaultTenant)");  // from app.config
```

#### Registering templates for multiple entities at once

```csharp
foreach (var entityName in new[] { "Orders", "Invoices", "Quotes" })
{
        DefaultsManager.RegisterProfile("salesdb", entityName,
        EntityDefaultsProfile.FullTemplate(entityName));
}

---

## 6. Applying Defaults

### 6.1 Dictionary record

Ideal for untyped data, dynamic forms, or JSON-sourced payloads.

```csharp
var record = new Dictionary<string, object>
{
    // Pre-populated values are left untouched by default (ApplyOnlyIfNull = true)
    ["CustomerName"] = "Acme Corp"
};

var result = DefaultsManager.Apply(editor, "salesdb", "Orders", record);

if (result.Flag == Errors.Failed)
    Console.WriteLine(result.Message);

// Fields added automatically:
// record["Id"]         → new GUID string
// record["CreatedAt"]  → DateTime.Now
// record["CreatedBy"]  → current Windows username
// record["Status"]     → "new"
// record["CustomerName"] → "Acme Corp"  ← NOT overwritten
```

### 6.2 POCO / strongly-typed

Defaults are set via reflection.  Property names are matched case-insensitively.

```csharp
public class Order
{
    public string Id         { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy   { get; set; }
    public string Status      { get; set; }
    public int    Priority    { get; set; }
}

var order = new Order { Status = "priority" };  // already set

var result = DefaultsManager.Apply(editor, "salesdb", "Orders", order);

// order.Id         → "550e8400-..."    (was null  → filled)
// order.CreatedAt  → DateTime.Now      (was 0001  → filled)
// order.CreatedBy  → "jsmith"          (was null  → filled)
// order.Status     → "priority"        ← NOT overwritten (ApplyOnlyIfNull = true)
// order.Priority   → 1                 (was 0     → filled)
```

### 6.3 DataRow

```csharp
DataTable table = BuildYourSchema();
DataRow row = table.NewRow();

var result = DefaultsManager.Apply(editor, "salesdb", "Orders", row);

table.Rows.Add(row);
```

---

## 7. Static helpers vs. DI instance

### Static API (no DI, call-site convenience)

```csharp
// Initialize once (e.g. in Program.cs or AppStartup)
DefaultsManager.Initialize(editor);

// Use anywhere with the available IDMEEditor
DefaultsManager.RegisterProfile("db", profile);
var value = DefaultsManager.Resolve(editor, ":NOW");
```

### Instance via IDefaultsManager (Dependency Injection)

```csharp
// Registration (e.g. Autofac / IServiceCollection)
services.AddSingleton<IDefaultsManager, DefaultsManager>();

// Consumer
public class OrderService
{
    private readonly IDefaultsManager _defaults;
    private readonly IDMEEditor       _editor;

    public OrderService(IDefaultsManager defaults, IDMEEditor editor)
    {
        _defaults = defaults;
        _editor   = editor;
        _defaults.Initialize(editor);
    }

    public Order NewOrder()
    {
        var order = new Order();
        _defaults.Apply(_editor, "salesdb", "Orders", order);
        return order;
    }
}
```

---

## 8. Built-in Resolvers Reference

All expressions are written with the `:` prefix when used in rule strings.

### 8.1 DateTime Resolver

| Rule string | Example result |
|------------|---------------|
| `:NOW` | `2026-03-18 14:35:00` |
| `:TODAY` | `2026-03-18 00:00:00` |
| `:YESTERDAY` | `2026-03-17 00:00:00` |
| `:TOMORROW` | `2026-03-19 00:00:00` |
| `:CURRENTTIME` | `14:35:00.123` (TimeSpan) |
| `:STARTOFMONTH` | `2026-03-01 00:00:00` |
| `:ENDOFMONTH` | `2026-03-31 00:00:00` |
| `:STARTOFYEAR` | `2026-01-01 00:00:00` |
| `:ENDOFYEAR` | `2026-12-31 00:00:00` |
| `:STARTOFWEEK` | Monday of current week |
| `:ENDOFWEEK` | Sunday of current week |
| `:ADDDAYS(TODAY,7)` | 7 days from today |
| `:ADDDAYS(NOW,-30)` | 30 days ago |
| `:ADDHOURS(NOW,2)` | 2 hours from now |
| `:ADDMINUTES(NOW,30)` | 30 minutes from now |
| `:ADDMONTHS(TODAY,1)` | 1 month ahead |
| `:ADDYEARS(TODAY,-1)` | 1 year ago |
| `:FORMAT(NOW,'yyyy-MM-dd')` | `"2026-03-18"` |
| `:FORMAT(NOW,'HH:mm:ss')` | `"14:35:00"` |
| `:DATEFORMAT(TODAY,'dd/MM/yyyy')` | `"18/03/2026"` |

**Profile examples**:
```csharp
EntityDefaultsProfile.For("Jobs")
    .Set("ScheduledAt",  ":ADDDAYS(TODAY,1)")       // tomorrow
    .Set("ExpiresAt",    ":ADDMONTHS(TODAY,3)")      // 3 months ahead
    .Set("SnapshotDate", ":TODAY")
    .Set("DisplayDate",  ":FORMAT(NOW,'dd MMM yyyy')")
    .Set("LogAt",        ":NOW", applyOnlyIfNull: false);  // always refresh
```

---

### 8.2 UserContext Resolver

| Rule string | Example result |
|------------|---------------|
| `:USERNAME` | `"DOMAIN\jsmith"` or `"jsmith"` |
| `:CURRENTUSER` | Same as USERNAME |
| `:USERID` | Windows SID string |
| `:USEREMAIL` | `"jsmith@company.com"` (if available) |
| `:USERDOMAIN` | `"COMPANY"` |
| `:USERPROFILE` | `"C:\Users\jsmith"` |
| `:USERGROUP` | Primary user group |
| `:USERROLE` | Application role |
| `:USERPRINCIPAL` | UPN (`jsmith@company.com`) |
| `:USERPROFILE(Documents)` | Documents folder path |
| `:USERPROFILE(Desktop)` | Desktop folder path |
| `:USERROLE(Application)` | Role for a specific application |

**Profile examples**:
```csharp
EntityDefaultsProfile.For("AuditLog")
    .Set("Actor",      ":USERNAME")
    .Set("Domain",     ":USERDOMAIN")
    .Set("LoggedAt",   ":NOW")
    .Set("EventType",  "login");
```

---

### 8.3 Guid Resolver

| Rule string | Example result |
|------------|---------------|
| `:NEWGUID` | `"550e8400-e29b-41d4-a716-446655440000"` |
| `:GUID` | Same as NEWGUID |
| `:UUID` | Same as NEWGUID |
| `:GENERATEUNIQUEID` | Same as NEWGUID |
| `:GUID(N)` | `"550e8400e29b41d4a716446655440000"` (no hyphens) |
| `:GUID(D)` | `"550e8400-e29b-41d4-a716-446655440000"` (with hyphens) |
| `:GUID(B)` | `"{550e8400-e29b-41d4-a716-446655440000}"` (braces) |
| `:GUID(P)` | `"(550e8400-e29b-41d4-a716-446655440000)"` (parentheses) |

**Profile examples**:
```csharp
EntityDefaultsProfile.For("Files")
    .Set("FileId",      ":NEWGUID")
    .Set("TrackingRef", ":GUID(N)")       // compact, no hyphens
    .Set("CorrelationId", ":GUID(B)");    // braced form for legacy systems
```

---

### 8.4 Environment Resolver

| Rule string | Example result |
|------------|---------------|
| `:ENV(USERNAME)` | `"jsmith"` |
| `:ENV(APPDATA)` | `"C:\Users\jsmith\AppData\Roaming"` |
| `:ENVIRONMENT(COMPUTERNAME)` | `"WORKSTATION01"` |
| `:ENVVAR(TEMP)` | `"C:\Users\jsmith\AppData\Local\Temp"` |
| `:ENV:PATH` | `PATH` env var (key-value syntax) |
| `:TEMP` | Temp folder path |
| `:TEMPPATH` | Same as TEMP |
| `:SYSTEMPATH` | System PATH variable |
| `:USERPATH` | User PATH variable |
| `:ENVIRONMENTVARIABLE(MY_VAR)` | Value of `MY_VAR` |

**Profile examples**:
```csharp
EntityDefaultsProfile.For("Reports")
    .Set("OutputPath",  ":ENV(USERPROFILE)")
    .Set("TempPath",    ":TEMP")
    .Set("ServerName",  ":ENVIRONMENT(COMPUTERNAME)");
```

---

### 8.5 SystemInfo Resolver

| Rule string | Example result |
|------------|---------------|
| `:MACHINENAME` | `"WORKSTATION01"` |
| `:HOSTNAME` | Same as MACHINENAME |
| `:VERSION` | `"8.0.1"` (.NET runtime) |
| `:APPVERSION` | `"2.1.400.0"` (entry assembly) |
| `:OSVERSION` | `"Microsoft Windows NT 10.0.22621.0"` |
| `:PLATFORM` | `"Win32NT"` |
| `:PROCESSORCOUNT` | `8` (int) |
| `:WORKINGSET` | Memory bytes (long) |
| `:TIMESTAMP` | Unix seconds (long) |
| `:TICKS` | `DateTime.Now.Ticks` (long) |

**Profile examples**:
```csharp
EntityDefaultsProfile.For("TelemetryEvent")
    .Set("Machine",     ":MACHINENAME")
    .Set("OsVersion",   ":OSVERSION")
    .Set("AppVersion",  ":APPVERSION")
    .Set("EventAt",     ":NOW")
    .Set("UnixTime",    ":TIMESTAMP")
    .Set("SessionId",   ":NEWGUID");
```

---

### 8.6 Configuration Resolver

| Rule string | Example result |
|------------|---------------|
| `:CONFIG(LogLevel)` | Value of `LogLevel` in app.config |
| `:CONFIGURATIONVALUE(MaxRetries)` | Value of `MaxRetries` in app.config |
| `:APPSETTING(DefaultTheme)` | App settings value |
| `:SETTING(MaxRecords)` | App settings value |
| `:CONNECTIONSTRING(DefaultConnection)` | Connection string value |
| `:CONFIG:DatabaseTimeout` | Key-value format |
| `:APPSETTING:DefaultPageSize` | Key-value format |

**Profile examples**:
```csharp
EntityDefaultsProfile.For("Job")
    .Set("Source",       ":CONFIG(DataSourceName)")
    .Set("MaxRows",      ":APPSETTING(ImportBatchSize)")
    .Set("Environment",  ":CONFIG(Environment)")
    .Set("CreatedAt",    ":NOW");
```

---

## 9. Dot-style rule DSL

The rule parser also accepts a dot-style DSL as an alternative to function syntax.
Both forms are equivalent — the normalizer converts dot-style to function-style before resolving.

```
:NOW.ADD.7          →  ADDDAYS(NOW,7)
:NOW.ADD.HOURS.2    →  ADDHOURS(NOW,2)
:NOW.FORMAT.yyyy-MM-dd   →  FORMAT(NOW,'yyyy-MM-dd')
```

You can use either form in your profiles:

```csharp
profile.Set("ExpiresAt",  ":NOW.ADD.30");           // dot-style
profile.Set("ExpiresAt",  ":ADDDAYS(NOW,30)");      // function-style — equivalent
```

---

## 10. Testing & Validating Rules

Use `DefaultsManager.TestRule` to test any rule from application code
or an admin/configurator UI:

```csharp
// Validate syntax
var validation = DefaultsManager.ValidateRule(editor, ":ADDDAYS(TODAY,7)");
Console.WriteLine(validation.Flag);     // Errors.Ok
Console.WriteLine(validation.Message);  // "Rule is valid"

// Resolve and inspect the actual value
var (result, value) = DefaultsManager.TestRule(editor, ":NOW");
Console.WriteLine(value);  // 2026-03-18 14:35:00

// Test a literal
var (result2, value2) = DefaultsManager.TestRule(editor, "pending");
Console.WriteLine(value2);  // "pending"   (literal — no resolver invoked)

// Test a bad rule
var (result3, _) = DefaultsManager.TestRule(editor, ":ADDDAYS(BROKEN");
Console.WriteLine(result3.Flag);    // Errors.Failed
Console.WriteLine(result3.Message); // parse error text
```

---

## 11. Custom Resolvers

Implement `IDefaultValueResolver` (or extend `BaseDefaultValueResolver`) to add new tokens.

```csharp
using TheTechIdea.Beep.Editor.Defaults.Interfaces;
using TheTechIdea.Beep.Editor.Defaults.Resolvers;

/// <summary>Resolves tenant-scoped values from your application context.</summary>
public class TenantContextResolver : BaseDefaultValueResolver
{
    private readonly ITenantService _tenantService;

    public TenantContextResolver(IDMEEditor editor, ITenantService tenantService)
        : base(editor)
    {
        _tenantService = tenantService;
    }

    public override string ResolverName => "TenantContext";

    public override IEnumerable<string> SupportedRuleTypes => new[]
    {
        "TENANTID", "TENANTNAME", "TENANTREGION", "TENANTPLAN"
    };

    public override object ResolveValue(string rule, IPassedArgs parameters)
    {
        var tenant = _tenantService.GetCurrentTenant();
        return rule.ToUpperInvariant() switch
        {
            "TENANTID"     => tenant?.Id.ToString(),
            "TENANTNAME"   => tenant?.Name,
            "TENANTREGION" => tenant?.Region,
            "TENANTPLAN"   => tenant?.PlanName,
            _              => null
        };
    }

    public override bool CanHandle(string rule) =>
        SupportedRuleTypes.Contains(rule.ToUpperInvariant().Trim());

    public override IEnumerable<string> GetExamples() => new[]
    {
        "TENANTID — Current tenant identifier",
        "TENANTNAME — Current tenant display name",
        "TENANTREGION — Tenant's assigned region",
        "TENANTPLAN — Tenant's subscription plan"
    };
}

// Registration
DefaultsManager.RegisterCustomResolver(editor, new TenantContextResolver(editor, tenantService));

// Now usable in profiles:
EntityDefaultsProfile.For("Orders")
    .Set("TenantId",    ":TENANTID")
    .Set("TenantName",  ":TENANTNAME")
    .Set("CreatedAt",   ":NOW")
    .Set("CreatedBy",   ":USERNAME");
```

---

## 12. Column-level defaults (legacy API)

The flat `DefaultValue`-based API is still fully supported for backward compatibility.

```csharp
// Set a default for a single column
DefaultsManager.SetColumnDefault(editor, "salesdb", "Orders", "Status",
    "pending",         // value
    isRule: false);    // false = literal, true = expression

DefaultsManager.SetColumnDefault(editor, "salesdb", "Orders", "CreatedAt",
    "NOW",             // expression token (no colon needed in legacy API)
    isRule: true);

// Read back
var status = DefaultsManager.GetColumnDefault(editor, "salesdb", "Orders", "Status");
// → "pending"

// Set multiple columns at once
DefaultsManager.SetMultipleColumnDefaults(editor, "salesdb", "Orders",
    new Dictionary<string, (string value, bool isRule)>
    {
        ["Status"]    = ("pending", false),
        ["CreatedAt"] = ("NOW",     true),
        ["CreatedBy"] = ("USERNAME",true),
        ["Priority"]  = ("1",       false),
    });

// Remove a default
DefaultsManager.RemoveColumnDefault(editor, "salesdb", "Orders", "Status");

// Get all defaults for an entity
var allDefaults = DefaultsManager.GetEntityDefaults(editor, "salesdb", "Orders");
// Returns Dictionary<string, DefaultValue>  (key = column name)
```

---

## 13. Common Scenarios & Recipes

### Recipe 1 — Multi-tenant SaaS record isolation

```csharp
var profile = EntityDefaultsProfile.For("Cases")
    .Set("Id",          ":NEWGUID")
    .Set("TenantId",    ":TENANTID")          // custom resolver
    .Set("CreatedAt",   ":NOW")
    .Set("CreatedBy",   ":USERNAME")
    .Set("Status",      "open")
    .Set("Priority",    2)
    .Set("Source",      ":CONFIG(DefaultSource)");

DefaultsManager.RegisterProfile("support_db", "Cases", profile);
```

---

### Recipe 2 — Soft-delete audit trail

```csharp
// Profile for "delete" events — ApplyOnlyIfNull = false so ModifiedAt always updates
var deleteProfile = EntityDefaultsProfile.For("Orders")
    .Set("DeletedAt",  ":NOW",      applyOnlyIfNull: false)
    .Set("DeletedBy",  ":USERNAME", applyOnlyIfNull: false)
    .Set("IsDeleted",  true,        applyOnlyIfNull: false);

// Reuse profile only when deleting
DefaultsManager.Apply(editor, "salesdb", "DeleteEvent_Orders", record);
```

---

### Recipe 3 — IoT device-state snapshot

```csharp
var deviceProfile = EntityDefaultsProfile.For("DeviceState")
    .Set("RecordId",   ":NEWGUID")
    .Set("SnapshotAt", ":NOW",       applyOnlyIfNull: false)
    .Set("Machine",    ":MACHINENAME")
    .Set("UnixTime",   ":TIMESTAMP", applyOnlyIfNull: false)
    .Set("SessionId",  ":NEWGUID");
```

---

### Recipe 4 — Scheduled-job metadata

```csharp
var jobProfile = EntityDefaultsProfile.FullTemplate("BackgroundJob")
    .Set("QueuedAt",    ":NOW")
    .Set("RunAt",       ":ADDMINUTES(NOW,5)")    // queued to run in 5 minutes
    .Set("ExpiresAt",   ":ADDDAYS(TODAY,1)")     // expires tomorrow
    .Set("MaxRetries",  ":APPSETTING(JobMaxRetries)")
    .Set("Status",      "queued");
```

---

### Recipe 5 — Resolve a single value inline

```csharp
// You do not need a profile to resolve a one-off value
var expiry     = (DateTime)DefaultsManager.Resolve(editor, ":ADDDAYS(TODAY,30)");
var correlId   = (string)DefaultsManager.Resolve(editor, ":NEWGUID");
var serverName = (string)DefaultsManager.Resolve(editor, ":MACHINENAME");
var literal    = (string)DefaultsManager.Resolve(editor, "FixedCategory"); // → "FixedCategory"
```

---

### Recipe 6 — Validate all rules in a profile before saving

```csharp
var profile = EntityDefaultsProfile.For("Invoices")
    .Set("Id",         ":NEWGUID")
    .Set("IssuedAt",   ":BADTOKEN")     // intentionally wrong
    .Set("DueDate",    ":ADDDAYS(TODAY,30)");

var failedRules = new List<string>();
foreach (var rule in profile.Rules)
{
    var validation = DefaultsManager.ValidateRule(editor, rule.RuleString);
    if (validation.Flag == Errors.Failed)
        failedRules.Add($"  {rule.FieldName}: {validation.Message}");
}

if (failedRules.Any())
    throw new InvalidOperationException("Invalid rules:\n" + string.Join("\n", failedRules));
```

---

### Recipe 7 — Inspect all registered resolvers

```csharp
var resolvers = DefaultsManager.GetAvailableResolvers(editor);
// resolvers is Dictionary<resolverName, IEnumerable<supportedTokens>>

foreach (var (name, tokens) in resolvers)
    Console.WriteLine($"{name}: {string.Join(", ", tokens)}");

// Output:
// DateTime: NOW, TODAY, YESTERDAY, TOMORROW, CURRENTDATE, ...
// UserContext: USERNAME, USERID, USEREMAIL, ...
// Guid: NEWGUID, GUID, UUID, ...
// Environment: ENVIRONMENTVARIABLE, ENV, ...
// SystemInfo: MACHINENAME, HOSTNAME, VERSION, ...
// Configuration: CONFIGURATIONVALUE, CONFIG, ...
```

---

## 14. Architecture Notes

### Flow of a rule string through the system

```
Rule string (":ADDDAYS(TODAY,7)")
    │
    ▼
RuleNormalizer.Normalize()
    ├── Starts with ":"  → expression path
    │       │
    │       ▼
    │   DotStyleRuleParser  (dot DSL  → function form, if needed)
    │       │
    │       ▼
    │   ParsedRule  { IsLiteral=false, NormalizedRule="ADDDAYS(TODAY,7)", IsValid=true }
    │
    └── No ":"  → literal path
            │
            ▼
        ParsedRule  { IsLiteral=true, NormalizedRule="Active", IsValid=true }
                             │
                             ▼  (if literal → skip resolver, return as-is)

NormalizedRule → DefaultValueResolverManager.ResolveValue()
    │
    ├── GetResolverForRule("ADDDAYS(TODAY,7)")  → DateTimeResolver
    │
    └── DateTimeResolver.ResolveValue(...)  → DateTime.Now.AddDays(7)
```

### Key types

| Type | Responsibility |
|------|---------------|
| `EntityDefaultsProfile` | Fluent builder, stores ordered list of `FieldDefaultRule` |
| `FieldDefaultRule` | `FieldName`, `RuleString`, `ApplyOnlyIfNull` |
| `DefaultsManager` | Static surface + `IDefaultsManager` implementation |
| `DefaultsManager.Apply.cs` | The three `ApplyInstance` overloads (Dictionary / POCO / DataRow) |
| `RuleNormalizer` | `:` prefix detection, literal vs. expression routing |
| `ParsedRule` | `IsLiteral`, `IsValid`, `NormalizedRule`, `Diagnostics` |
| `DefaultValueResolverManager` | Routes normalized token to correct resolver |
| `BaseDefaultValueResolver` | Base class for all built-in and custom resolvers |
| `IDefaultValueResolver` | Interface for custom resolvers |

### Thread safety

`DefaultsManager` uses a `static readonly object _lockObject` for all mutations to
the profile registry.  Reads are inherently safe once initialized.  The resolver
manager has a built-in value cache bounded to 512 entries (FIFO eviction).

### Caching

Resolvers that return stable values (e.g. `SystemInfoResolver`, `ConfigurationResolver`)
set `IsDeterministic = true`, which enables the result cache.
Time-based resolvers (`DateTimeResolver`) are non-deterministic → not cached.
