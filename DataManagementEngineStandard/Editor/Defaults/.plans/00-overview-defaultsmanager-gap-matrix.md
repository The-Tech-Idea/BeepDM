# DefaultsManager — Current Architecture Overview

_Last updated: reflects the rewritten defaults system (EntityDefaultsProfile, instance
IDefaultsManager, unified rule convention).  The old partials `DefaultsManager.Extended.cs`
and `DefaultsManager.Templates.cs` have been folded into the main file and profile
templates respectively._

---

## Architecture

### File map

| File | Role |
|------|------|
| `DefaultsManager.cs` | Central static class + `IDefaultsManager` instance bridge. Owns profile registry and resolver delegation. Previously split across `Extended.cs` + `Templates.cs` — both are gone. |
| `DefaultsManager.Apply.cs` | Partial — `Apply<T>` (POCO), `Apply(DataRow)`, `ApplyToNew<T>`. |
| `EntityDefaultsProfile.cs` | Fluent profile builder + `FieldDefaultRule`. Built-in template factories: `AuditTemplate`, `TimestampTemplate`, `UserStampTemplate`, `IdentityTemplate`, `FullTemplate`. |
| `IDefaultsManager.cs` | Instance interface (no `static abstract`). DI-friendly. |
| `Resolvers/DefaultValueResolverManager.cs` | Dispatches canonical token strings to 11 built-in resolvers. |
| `RuleParsing/RuleNormalizer.cs` | Converts DSL variants → canonical function form before dispatching. |
| `RuleParsing/ParsedRule.cs` | AST-like result with `IsLiteral` flag, `NormalizedRule`, `SyntaxVersion`. |
| `Helpers/DefaultValueHelper.cs` | Persistence bridge to `ConnectionProperties.DatasourceDefaults`. |
| `Helpers/DefaultValueValidationHelper.cs` | Syntax validation without side effects. |
| `Registry/DefaultResolverRegistry.cs` | Describes available resolvers (self-registration). |

### Rule-string convention

| Rule string | Meaning | Example resolved value |
|--|--|--|
| `:NOW` | Expression: current datetime | `DateTime.Now` |
| `:TODAY` | Expression: current date (time = 00:00) | `DateTime.Today` |
| `:USERNAME` | Expression: current Windows/OS user | `"alice.doe"` |
| `:NEWGUID` | Expression: new UUID v4 with dashes | `"d7a0-..."` |
| `:GUID(N)` | Expression: UUID no dashes | `"d7a0..."` |
| `:MACHINENAME` | Expression: machine hostname | `"DEVLAP01"` |
| `:ENV.MY_VAR` | Expression: OS environment variable | `"/usr/local"` |
| `:CONFIG.MyKey` | Expression: config key value | `"prod"` |
| `:SEQUENCE` | Expression: next integer from sequence | `42` |
| `:ADDDAYS(NOW,7)` | Expression: compound datetime formula | `DateTime.Now+7d` |
| `Active` | **Literal** — no colon, no resolver | `"Active"` |
| `1` | **Literal** | integer `1` |
| *(empty)* | No rule — field left as-is | — |

Rule strings starting with `:` are stripped of the prefix and handed to
`RuleNormalizer.Normalize` → `DefaultValueResolverManager.ResolveValue`.
Rule strings **without** `:` are passed through as-is (literal).

### Data flow

```
Caller sets rule string on EntityDefaultsProfile or DefaultValue.Rule
    │
    ▼  DefaultsManager.Apply / Resolve
ParsedRule ← RuleNormalizer.Normalize(ruleString)
    │  IsLiteral? → return ruleString unchanged
    │  else
    ▼  DefaultValueResolverManager.ResolveValue(normalizedRule, context)
Dispatched to matching IDefaultValueResolver.Resolve()
    │
    ▼  Actual runtime value
```

---

## Built-in template factories (`EntityDefaultsProfile`)

| Factory | Fields set |
|---------|-----------|
| `AuditTemplate(entity)` | CreatedBy=:USERNAME, CreatedAt=:NOW, ModifiedBy=:USERNAME, ModifiedAt=:NOW, IsActive=true, Version=1, RowGuid=:NEWGUID |
| `TimestampTemplate(entity)` | CreatedAt=:NOW, ModifiedAt=:NOW |
| `UserStampTemplate(entity)` | CreatedBy=:USERNAME, ModifiedBy=:USERNAME |
| `IdentityTemplate(entity)` | Id=:NEWGUID |
| `FullTemplate(entity)` | IdentityTemplate + AuditTemplate combined |

All templates use canonical resolver tokens (`:NOW`, `:USERNAME`, `:NEWGUID`) — not the
legacy aliases *CurrentDateTime*, *CurrentUser*, *GenerateUniqueId* which are no longer used.

---

## Enhancement roadmap (future phases)

The files `01–07-phase*.md` in this directory describe planned enhancements:
dot-style DSL, query-driven defaults, expression unification, caching, and rollout.
These are not yet implemented; the gap matrix below was their starting point.

| Capability | Current state | Planned target |
|------------|--------------|----------------|
| Rule DSL | `:TOKEN` + function syntax | + dot-style DSL (`ADDDAYS.NOW.7`) |
| Query Defaults | `DataSourceResolver` (basic) | Secure templates, parameter binding, timeout |
| Expression engine | `ExpressionResolver` + `FormulaResolver` | Unified engine + typed operators |
| Validation | Syntax check (DefaultValueValidationHelper) | Grammar-aware, diagnostics catalog |
| Caching | None | Resolver-level cache + pre-compilation |
| Observability | Logger | Per-resolver latency/error counters |
