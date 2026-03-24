# Phase 13 — Developer & Application Rules

## Goal

Add rules that everyday developers reach for when building CRUD apps, business logic, and data pipelines: field validators, arithmetic computations, record mutation helpers, and parameterized data-source queries.

These rules mirror what you'd manually code in a controller, service layer, or data-entry form — but expressed declaratively as configurable `IRule` instances that can be composed, stored, and reused.

---

## Group: Validate (6 rules)

| Rule Key | File | Purpose |
|---|---|---|
| `Validate.ValidEmail`       | `BuiltinRules/Validate/ValidEmail.cs`       | RFC-compliant e-mail check via `MailAddress` |
| `Validate.ValidPhone`       | `BuiltinRules/Validate/ValidPhone.cs`       | E.164 and US phone formats with configurable format |
| `Validate.ValidSSN`         | `BuiltinRules/Validate/ValidSSN.cs`         | US SSN XXX-XX-XXXX with invalid-range rejection |
| `Validate.ValidPostalCode`  | `BuiltinRules/Validate/ValidPostalCode.cs`  | Postal codes for US, CA, UK, DE, FR or generic |
| `Validate.ValidUrl`         | `BuiltinRules/Validate/ValidUrl.cs`         | HTTP/HTTPS URL using `Uri.TryCreate` |
| `Validate.ValidCreditCard`  | `BuiltinRules/Validate/ValidCreditCard.cs`  | Luhn algorithm + card-type detection |

### Common parameters
- `Value` — the string to validate
- Rule-specific: `Format`, `Country`, `AllowedSchemes`

---

## Group: Compute (4 rules)

| Rule Key | File | Purpose |
|---|---|---|
| `Compute.EvalExpression`  | `BuiltinRules/Compute/EvalExpression.cs`  | `{Field}` interpolation + `DataTable.Compute` arithmetic |
| `Compute.IfElseValue`     | `BuiltinRules/Compute/IfElseValue.cs`     | `=`, `!=`, `>`, `<`, `contains`, etc. → pick TrueValue/FalseValue |
| `Compute.AgeFromDate`     | `BuiltinRules/Compute/AgeFromDate.cs`     | Age in Years/Months/Days/Hours from a date field |
| `Compute.MapValueTable`   | `BuiltinRules/Compute/MapValueTable.cs`   | SWITCH/CASE: `"A=Active;I=Inactive;*=Unknown"` |

### EvalExpression
- Parameters: `Expression` (e.g. `{Price} * {Qty} * (1 - {Discount})`), `Record` or individual field params
- Outputs: `Result` (numeric), `Expression` (expanded string)

### IfElseValue
- Parameters: `ConditionField`, `Operator` (=|!=|>|<|>=|<=|contains|startswith|endswith), `CompareValue`, `TrueValue`, `FalseValue`, `Record`

### AgeFromDate
- Parameters: `Value` (date), `Unit` (Years|Months|Days|Hours), `ReferenceDate`
- Outputs: `Result`, `Years`, `Months`, `Days`

### MapValueTable
- Parameters: `Value`, `Map` (`"key1=out1;key2=out2;*=default"`), `Default`
- Outputs: `Result`, `Matched` (bool)

---

## Group: Record (4 rules)

| Rule Key | File | Purpose |
|---|---|---|
| `Record.SetFieldValue`         | `BuiltinRules/Record/SetFieldValue.cs`         | Set or overwrite a field with a literal or `{Token}` expression |
| `Record.CopyField`             | `BuiltinRules/Record/CopyField.cs`             | Copy SourceField value to TargetField |
| `Record.ConditionalSetField`   | `BuiltinRules/Record/ConditionalSetField.cs`   | Set a field only when a condition is met |
| `Record.IncrementField`        | `BuiltinRules/Record/IncrementField.cs`        | Add/subtract numeric step; optional min:max clamping |

### Common pattern
All Record rules accept `Record` as:
- `IDictionary<string, object>` — modified copy returned in outputs  
- `"field=val;field2=val2"` string — parsed and returned as updated dict

### ConditionalSetField operators
`=`, `!=`, `>`, `<`, `>=`, `<=`, `contains`, `startswith`, `endswith`, `isnull`, `isnotnull`

---

## Group: Query (3 rules)

| Rule Key | File | Purpose |
|---|---|---|
| `Query.RunScalarQuery`          | `BuiltinRules/Query/RunScalarQuery.cs`          | Run filtered query → return first field of first row |
| `Query.SetFromQuery`            | `BuiltinRules/Query/SetFromQuery.cs`            | Run query → write result field into a record |
| `Query.RunParameterizedLookup`  | `BuiltinRules/Query/RunParameterizedLookup.cs`  | Single key field lookup → return a named field |

### Filters syntax
`"field1:=:value1;field2:>:value2"` — colon-separated triples parsed into `List<AppFilter>`.

### SetFromQuery-specific
- Additional parameters: `Record` (dict or string), `TargetField` (field to write result into)  
- Output: `Record` (updated copy), `Result` (value written)

---

## Implementation Notes

- All rules follow existing conventions: `[Rule(ruleKey:..., ParserKey = "RulesParser", RuleName = ...)]`
- All implement `IRule` with `SolveRule(Dictionary<string, object> parameters)`.
- Data-access rules inject `IDMEEditor` from parameters and use `task.Wait()` sync bridge.
- `AppFilter` is in `TheTechIdea.Beep.Report` namespace.
- `EvalExpression` uses `System.Data.DataTable.Compute` for safe arithmetic — no `eval` or `Roslyn`.

---

## File Count

- 6 Validate + 4 Compute + 4 Record + 3 Query = **17 new rule files**
- Namespace roots: `TheTechIdea.Beep.Rules.BuiltinRules.{Validate|Compute|Record|Query}`
