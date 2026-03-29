# DefaultsManager — Developer Guide

A practical guide for using `DefaultsManager` to apply field defaults at record-creation time.

---

## Quick Start

```csharp
// 1. Initialize (call once per application session; safe to call multiple times)
DefaultsManager.Initialize(editor);

// 2. Register a profile for an entity
var profile = EntityDefaultsProfile.For("Users")
    .Set("CreatedBy", ":USERNAME")   // expression — resolves to current user
    .Set("CreatedAt", ":NOW")        // expression — resolves to DateTime.Now
    .Set("Id",        ":NEWGUID")    // expression — resolves to new Guid string
    .Set("Status",    "Active")      // literal — used as-is, no resolver invoked
    .Set("Version",   "1");          // literal int string

DefaultsManager.RegisterProfile("mydb", "Users", profile);

// 3. Apply defaults to a new record
var record = new Dictionary<string, object>();
DefaultsManager.Apply(editor, "mydb", "Users", record);
// record["CreatedAt"] == DateTime.Now, record["Status"] == "Active", …
```

---

## Rule String Convention

| Starts with `:` | Treated as | Handled by |
|--|--|--|
| `:NOW` | Expression — parsed + resolved | `RuleNormalizer` → `DefaultValueResolverManager` |
| `Active` | **Literal** — returned unchanged | Passed through directly |

**All expression tokens are case-insensitive.**

### Common expression tokens

| Token | Result |
|-------|--------|
| `:NOW` | `DateTime.Now` |
| `:TODAY` | `DateTime.Today` |
| `:USERNAME` | `Environment.UserName` (or injected context value) |
| `:NEWGUID` | New `Guid.NewGuid().ToString()` |
| `:GUID(N)` | Guid without dashes |
| `:MACHINENAME` | `Environment.MachineName` |
| `:ENV.MY_VAR` | OS environment variable `MY_VAR` |
| `:CONFIG.MyKey` | Config key `MyKey` from `ConfigEditor` |
| `:SEQUENCE` | Next integer from an auto-increment sequence |
| `:ADDDAYS(NOW,7)` | `DateTime.Now.AddDays(7)` |

---

## Built-in Profile Templates

Pre-built templates save boilerplate for common patterns:

```csharp
// Adds: CreatedBy, CreatedAt, ModifiedBy, ModifiedAt, IsActive=true, Version=1, RowGuid
DefaultsManager.RegisterProfile("mydb", "Orders",
    EntityDefaultsProfile.AuditTemplate("Orders"));

// Adds: CreatedAt, ModifiedAt only
DefaultsManager.RegisterProfile("mydb", "Logs",
    EntityDefaultsProfile.TimestampTemplate("Logs"));

// Adds: Id = :NEWGUID
DefaultsManager.RegisterProfile("mydb", "Items",
    EntityDefaultsProfile.IdentityTemplate("Items"));

// Adds: Id + full audit fields (IdentityTemplate + AuditTemplate combined)
DefaultsManager.RegisterProfile("mydb", "Products",
    EntityDefaultsProfile.FullTemplate("Products"));
```

---

## Apply Overloads

```csharp
// Dictionary<string, object>
DefaultsManager.Apply(editor, "mydb", "Users", record);

// POCO (via reflection — matches property names case-insensitively)
DefaultsManager.Apply<UserDto>(editor, "mydb", "Users", dto);

// DataRow
DefaultsManager.Apply(editor, "mydb", "Users", dataRow);

// Create a fresh POCO with defaults already applied
var newUser = DefaultsManager.ApplyToNew<UserDto>(editor, "mydb", "Users");
```

By default, `Apply` skips fields that already have a value
(`FieldDefaultRule.ApplyOnlyIfNull = true`).  Pass `.Set(field, rule, applyOnlyIfNull: false)`
to always overwrite.

---

## Resolve a Single Rule

```csharp
// Returns the resolved value for any rule string
object value = DefaultsManager.Resolve(editor, ":NOW");         // DateTime
object lit   = DefaultsManager.Resolve(editor, "Active");       // "Active"

// Test a rule — returns both a value and validation IErrorsInfo
var (result, value) = DefaultsManager.TestRule(editor, ":USERNAME");
```

---

## Persistence (legacy DefaultValue entries)

When defaults are stored on `ConnectionProperties.DatasourceDefaults` (the original
column-level persistence model), use:

```csharp
List<DefaultValue> saved = DefaultsManager.GetDefaults(editor, "mydb");
DefaultsManager.SaveDefaults(editor, saved, "mydb");

// Column-level helpers
DefaultsManager.SetColumnDefault(editor, "mydb", "Users", "Status", "Active");
object val = DefaultsManager.GetColumnDefault(editor, "mydb", "Users", "Status", args);

// Resolve a persisted DefaultValue entry
object resolved = DefaultsManager.ResolveDefaultValue(editor, defaultValueEntry, args);
```

---

## Custom Resolver

Implement `IDefaultValueResolver` and register it:

```csharp
public class TenantResolver : BaseDefaultValueResolver
{
    public override bool CanHandle(string normalizedRule) =>
        normalizedRule.StartsWith("TENANT", StringComparison.OrdinalIgnoreCase);

    public override object Resolve(string normalizedRule, ResolverContext context) =>
        context.PassedArgs?.CustomProperties?["TenantId"] ?? "default";
}

// Register before first Apply call
DefaultsManager.RegisterCustomResolver(editor, new TenantResolver());
```

The custom resolver is automatically used by `Apply` and `Resolve` for any rule
starting with `:TENANT...`.

---

## Expression Fields on EntityField

To store a default rule on an individual field definition (for schema-driven scenarios):

```csharp
field.CustomProperty["DefaultRule"] = ":NOW";

// At record creation time:
if (field.CustomProperty.TryGetValue("DefaultRule", out var rule))
    record[field.FieldName] = DefaultsManager.Resolve(editor, rule, args);
```

---

## Enhancement Plans (future)

The numbered files in this directory describe planned future enhancements.
See [00-overview-defaultsmanager-gap-matrix.md](./00-overview-defaultsmanager-gap-matrix.md)
for the capability roadmap.
