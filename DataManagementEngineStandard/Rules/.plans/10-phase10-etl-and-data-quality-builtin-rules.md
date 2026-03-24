# Phase 10 — ETL & Data Quality Built-in Rules

## Purpose
Provide a rich library of built-in `IRule` implementations covering the Data Quality and ETL
scenarios found in tools like n8n, Talend, Apache NiFi, Airbyte, dbt, and Great Expectations.
Every rule follows the same contract: annotated with `[RuleAttribute]`, discoverable through
`AssemblyHandler`, instantiable with either a `(IDMEEditor)` or parameterless constructor.

---

## Motivation — gaps vs. industry tools

| Scenario | n8n node | dbt test | Missing in BeepDM |
|---|---|---|---|
| Null / empty check | "If" node | `not_null` | ✅ needed |
| Range / boundary validation | "Switch" | `accepted_values` | ✅ needed |
| Pattern (regex) match | "IF" + regex | custom | ✅ needed |
| Cross-field comparison | computed | custom | ✅ needed |
| Lookup / referential integrity | `Lookup` node | `relationships` | ✅ needed |
| Numeric aggregation | `SummarizeBy` | `dbt_utils.sum` | ✅ needed |
| String transformation | `Set` node | macros | ✅ needed |
| Date arithmetic | `DateTime` | `dbt_date` | ✅ needed |
| Hashing / masking | crypto node | custom | ✅ needed |
| Schema conformance | Schema node | `expression_is_true` | ✅ needed |

---

## Delivery

### Group A — Data Quality (DQ) Rules

#### `IsNullOrEmpty`
- **Key:** `DQ.IsNullOrEmpty`
- **Input parameters:** `Value` (object)
- **Output:** `bool` — true when value is null, empty string, DBNull, or whitespace
- **Use:** NiFi *"RouteOnAttribute"* equivalent; dbt `not_null` inverse

#### `IsInRange`
- **Key:** `DQ.IsInRange`
- **Inputs:** `Value` (IComparable), `Min`, `Max`, `Inclusive` (bool, default true)
- **Output:** `bool`
- **Use:** boundary gates before ETL sinks

#### `MatchesRegex`
- **Key:** `DQ.MatchesRegex`
- **Inputs:** `Value` (string), `Pattern` (string), `IgnoreCase` (bool, default false)
- **Output:** `bool`
- **Use:** email/phone/postcode validation; replace "Set + IF" patterns

#### `IsInList`
- **Key:** `DQ.IsInList`
- **Inputs:** `Value`, `AllowedValues` (comma-separated string or `List<object>`)
- **Output:** `bool`
- **Use:** dbt `accepted_values` equivalent

#### `MatchesExpectedType`
- **Key:** `DQ.MatchesExpectedType`
- **Inputs:** `Value`, `ExpectedType` (string: "int", "double", "date", "guid", "string")
- **Output:** `bool` + `ParsedValue` (out param in dict)
- **Use:** schema conformance at ETL ingestion

#### `IsUnique`
- **Key:** `DQ.IsUnique`
- **Inputs:** `DataSourceName`, `EntityName`, `FieldName`, `Value`, `IDMEEditor`
- **Output:** `bool`
- **Use:** duplicate detection before insert; dbt `unique` test

#### `ReferentialIntegrity`
- **Key:** `DQ.ReferentialIntegrity`
- **Inputs:** `DataSourceName`, `LookupEntityName`, `LookupField`, `Value`, `IDMEEditor`
- **Output:** `bool` + `MatchedRecord` (dict)
- **Use:** FK-style check in loosely coupled ETL pipelines

---

### Group B — String Transform Rules

#### `TrimAndNormalize`
- **Key:** `Transform.TrimAndNormalize`
- **Inputs:** `Value` (string), `CaseMode` ("upper"|"lower"|"title"|"none")
- **Output:** `string`

#### `ReplacePattern`
- **Key:** `Transform.ReplacePattern`
- **Inputs:** `Value` (string), `Pattern`, `Replacement`
- **Output:** `string`
- **Use:** ETL scrubbing; PII masking patterns

#### `SplitAndTake`
- **Key:** `Transform.SplitAndTake`
- **Inputs:** `Value` (string), `Delimiter`, `Index` (int)
- **Output:** `string`
- **Use:** field decomposition (e.g. "FirstName LastName" split)

#### `PadOrTruncate`
- **Key:** `Transform.PadOrTruncate`
- **Inputs:** `Value` (string), `Length` (int), `PadChar`, `Align` ("left"|"right")
- **Output:** `string`
- **Use:** fixed-width file formatting rules

---

### Group C — Numeric & Aggregate Rules

#### `RoundNumeric`
- **Key:** `Numeric.Round`
- **Inputs:** `Value` (double), `Decimals` (int), `MidpointRounding` (string)
- **Output:** `double`

#### `Clamp`
- **Key:** `Numeric.Clamp`
- **Inputs:** `Value` (double), `Min`, `Max`
- **Output:** `double`

#### `GetEntityAggregate`
- **Key:** `Aggregate.GetEntityAggregate`
- **Inputs:** `DataSourceName`, `EntityName`, `FieldName`, `Function` ("sum"|"avg"|"min"|"max"|"count"), optional `FilterExpression`, `IDMEEditor`
- **Output:** `double`
- **Use:** SLA-gate rules ("only proceed if avg order value > 100")

---

### Group D — Date & Time Rules

#### `AddDatePart`
- **Key:** `Date.AddDatePart`
- **Inputs:** `Value` (DateTime/string), `Amount` (int), `Part` ("days"|"hours"|"months"|"years")
- **Output:** `DateTime`

#### `DateDiff`
- **Key:** `Date.DateDiff`
- **Inputs:** `Start`, `End`, `Part` ("days"|"hours"|"minutes"|"seconds")
- **Output:** `double`
- **Use:** SLA age calculations, expiry checks

#### `IsWithinBusinessHours`
- **Key:** `Date.IsWithinBusinessHours`
- **Inputs:** `Value` (DateTime), `StartHour` (int, default 9), `EndHour` (int, default 17), `TimeZone` (IANA string)
- **Output:** `bool`

---

### Group E — Security / PII Rules

#### `HashValue`
- **Key:** `Security.HashValue`
- **Inputs:** `Value` (string), `Algorithm` ("SHA256"|"SHA512"|"MD5"), `Salt` (optional)
- **Output:** `string` (hex)
- **Use:** pseudonymisation of PII fields in ETL sink

#### `MaskValue`
- **Key:** `Security.MaskValue`
- **Inputs:** `Value` (string), `Mode` ("last4"|"first4"|"middle"|"full"), `MaskChar` (char, default `*`)
- **Output:** `string`
- **Use:** log-safe credit card / SSN masking

#### `TokenizeValue`
- **Key:** `Security.TokenizeValue`
- **Inputs:** `Value` (string), `VaultKey` (string — looked up via IDMEEditor config)
- **Output:** `string` (token), `OriginalLength` (int)
- **Use:** reversible tokenisation in data warehousing

---

## Implementation Guidelines

- All rules live in `DataManagementEngineStandard/Rules/BuiltinRules/`
- Group by subfolder: `DQ/`, `Transform/`, `Numeric/`, `Date/`, `Security/`
- Each class:
  - Is `public sealed` (no inheritance from built-ins)
  - Has `[Rule(ruleKey: "...", ParserKey = "RulesParser", RuleName = "...")]`
  - Accepts `IDMEEditor` via constructor for rules that need data access
  - Returns structured `output` dict plus typed scalar result
  - Validates parameters defensively, adds `"Error"` key to output on failure
- Mirror the pattern of `GetRecordCount` for data-access rules
- Mirror the pattern of `GetSystemDate` for pure-compute rules

---

## Acceptance Criteria

- All 20 rules compile with 0 errors
- `AssemblyHandler.ScanAssembly` discovers each via `ConfigEditor.Rules`
- `RuleRegistry.Discover()` registers all into engine automatically
- Each rule has at least one inline XML doc comment describing parameters
- No rule throws exceptions — all errors surface in `output["Error"]`

---

## File Map

```
Rules/BuiltinRules/
  DQ/
    IsNullOrEmpty.cs
    IsInRange.cs
    MatchesRegex.cs
    IsInList.cs
    MatchesExpectedType.cs
    IsUnique.cs
    ReferentialIntegrity.cs
  Transform/
    TrimAndNormalize.cs
    ReplacePattern.cs
    SplitAndTake.cs
    PadOrTruncate.cs
  Numeric/
    RoundNumeric.cs
    Clamp.cs
    GetEntityAggregate.cs
  Date/
    AddDatePart.cs
    DateDiff.cs
    IsWithinBusinessHours.cs
  Security/
    HashValue.cs
    MaskValue.cs
    TokenizeValue.cs
```
