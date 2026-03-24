# BeepDM Rules Engine — Developer Guide

The rules engine lets you compose, store, and execute named business-logic units
without writing boilerplate code in controllers or service classes.  Every rule:

- Implements `IRule` with a single method: `SolveRule(Dictionary<string,object> parameters)`
- Returns `(Dictionary<string,object> outputs, object result)` — the `outputs` dict
  contains named sub-results; `result` is the "primary" return value.
- Is discovered automatically by `RuleRegistry` (attribute-based scanning).
- Can be serialised as a `RuleStructure` (stored in DB / config files).

---

## Quick-Start — Three lines to run a rule

```csharp
// 1. Get the registry (singleton, built by DI / RulesEngine)
IRuleRegistry registry = ruleEngine.Registry;

// 2. Look up the rule by its key
IRule rule = registry.GetRule("Validate.ValidEmail");

// 3. Execute with a parameter bag
var (outputs, result) = rule.SolveRule(new Dictionary<string, object>
{
    ["Value"] = "alice@example.com"
});

bool isValid = (bool)result;                  // true
```

All rules follow exactly this pattern.  The sections below list every built-in
rule, its parameters, and a code example.

---

## Folder Map

```
Rules/
├── BuiltinRules/
│   ├── Compute/          arithmetic, branching, mapping
│   ├── Date/             date arithmetic and business-hours
│   ├── DQ/               data quality / integrity checks
│   ├── Enrich/           add / merge fields into a record
│   ├── Flow/             retry, circuit-breaker, dead-letter
│   ├── Notify/           webhook, audit-trail, logging
│   ├── Numeric/          rounding, clamping, aggregates
│   ├── Query/            parameterised data-source queries
│   ├── Record/           set, copy, increment record fields
│   ├── Route/            routing, bucketing, throttling
│   ├── Schema/           schema / not-null enforcement
│   ├── Security/         hashing, masking, tokenisation
│   ├── Transform/        string cleaning and reshaping
│   └── Validate/         email, phone, SSN, URL, credit-card
├── BuiltinParsers/       expression parsers (CSV, SQL WHERE, Formula, …)
├── Engine/               RulesEngine, RuleRegistry, InMemoryRuleStateStore
├── Models/               IRule, IRuleStructure, IRuleStateStore, …
└── README.md             ← you are here
```

---

## Common Parameter Conventions

| Convention | Meaning |
|---|---|
| `Value` | The primary input scalar (string, number, date). |
| `Record` | An `IDictionary<string,object>` **or** a `"field=val;field2=val2"` string. All Record-group rules return an updated copy in `outputs["Record"]`. |
| `IDMEEditor` | Required by data-access rules; pass an `IDMEEditor` instance. |
| `DataSourceName` | Registered data-source name (matches a `ConnectionProperties.ConnectionName`). |
| `Filters` | Semicolon-separated `"field:op:value"` triples, e.g. `"Status:=:Active;Score:>:50"`. |

---

## Group: Validate

### `Validate.ValidEmail`
Checks an e-mail address using `System.Net.Mail.MailAddress` (RFC 5321/5322).

```csharp
var (_, ok) = rule.SolveRule(new() { ["Value"] = "bob@domain.com" });
// ok → true / false
```

---

### `Validate.ValidPhone`
Validates phone numbers.  `Format` may be `E164`, `US`, or `Any` (default).

```csharp
var (_, ok) = rule.SolveRule(new() {
    ["Value"]  = "+14155552671",
    ["Format"] = "E164"
});
```

---

### `Validate.ValidSSN`
Validates a US Social Security Number (`XXX-XX-XXXX`).
Rejects 000-xx-xxxx, 666-xx-xxxx, 9xx-xx-xxxx, xx-00-xxxx, xxx-xx-0000.

```csharp
var (_, ok) = rule.SolveRule(new() { ["Value"] = "123-45-6789" });
```

---

### `Validate.ValidPostalCode`
Validates postal / ZIP codes.  `Country`: `US` (default), `CA`, `UK`, `DE`, `FR`.

```csharp
var (_, ok) = rule.SolveRule(new() { ["Value"] = "SW1A 1AA", ["Country"] = "UK" });
```

---

### `Validate.ValidUrl`
Validates an absolute URL.  `AllowedSchemes` defaults to `"http,https"`.

```csharp
var (_, ok) = rule.SolveRule(new() {
    ["Value"]          = "https://api.example.com/v1",
    ["AllowedSchemes"] = "https"
});
```

---

### `Validate.ValidCreditCard`
Luhn algorithm check + card-type detection (Visa, Mastercard, Amex, Discover).

```csharp
var (out, ok) = rule.SolveRule(new() { ["Value"] = "4111 1111 1111 1111" });
string cardType = out["CardType"].ToString();  // "Visa"
```

---

## Group: Compute

### `Compute.EvalExpression`
Evaluates a math / string expression with `{FieldName}` substitution, backed by
`System.Data.DataTable.Compute` (safe, no eval/Roslyn).

```csharp
var (out, result) = rule.SolveRule(new() {
    ["Expression"] = "{Price} * {Qty} * (1 - {Discount})",
    ["Price"]      = "100",
    ["Qty"]        = "3",
    ["Discount"]   = "0.1"
});
// result → 270  (as object)
```

Or pass a `Record` dictionary:

```csharp
var (out, result) = rule.SolveRule(new() {
    ["Expression"] = "{UnitPrice} * {Quantity}",
    ["Record"]     = new Dictionary<string,object> { ["UnitPrice"] = 29.99, ["Quantity"] = 5 }
});
```

---

### `Compute.IfElseValue`
Returns `TrueValue` or `FalseValue` based on a field comparison.
Operators: `=`, `!=`, `>`, `<`, `>=`, `<=`, `contains`, `startswith`, `endswith`.

```csharp
var (out, result) = rule.SolveRule(new() {
    ["ConditionField"] = "Status",
    ["Operator"]       = "=",
    ["CompareValue"]   = "Active",
    ["TrueValue"]      = "✓ Active",
    ["FalseValue"]     = "✗ Inactive",
    ["Status"]         = "Active"           // direct field param
});
// result → "✓ Active"
```

---

### `Compute.AgeFromDate`
Computes elapsed time from a date field.  `Unit`: `Years` (default), `Months`, `Days`, `Hours`.

```csharp
var (out, result) = rule.SolveRule(new() {
    ["Value"] = "1990-06-15",
    ["Unit"]  = "Years"
});
int age = Convert.ToInt32(result);          // e.g. 35
int totalDays = (int)out["Days"];
```

---

### `Compute.MapValueTable`
SWITCH / CASE lookup table.  `*` is the catch-all key.

```csharp
var (out, result) = rule.SolveRule(new() {
    ["Value"] = "I",
    ["Map"]   = "A=Active;I=Inactive;S=Suspended;*=Unknown"
});
// result → "Inactive"
// out["Matched"] → true
```

---

## Group: Record

All Record rules accept `Record` as `IDictionary<string,object>` or `"k=v;k2=v2"` and return
an updated copy in `outputs["Record"]`.

### `Record.SetFieldValue`
Sets a field to a literal or `{Token}`-interpolated string.

```csharp
var (out, _) = rule.SolveRule(new() {
    ["FieldName"] = "FullName",
    ["Value"]     = "{FirstName} {LastName}",
    ["Record"]    = new Dictionary<string,object> {
        ["FirstName"] = "Alice", ["LastName"] = "Smith"
    }
});
var updated = (IDictionary<string,object>)out["Record"];
// updated["FullName"] → "Alice Smith"
```

---

### `Record.CopyField`
Copies `SourceField` value to `TargetField`.

```csharp
var (out, _) = rule.SolveRule(new() {
    ["Record"]      = record,
    ["SourceField"] = "Email",
    ["TargetField"] = "ContactEmail"
});
```

---

### `Record.ConditionalSetField`
Sets a field only when a condition holds.  Extra operators: `isnull`, `isnotnull`.

```csharp
var (out, applied) = rule.SolveRule(new() {
    ["Record"]         = record,
    ["ConditionField"] = "Score",
    ["Operator"]       = ">=",
    ["CompareValue"]   = "90",
    ["TargetField"]    = "Grade",
    ["TrueValue"]      = "A",
    ["FalseValue"]     = "B"
});
```

---

### `Record.IncrementField`
Adds (or subtracts) a step to a numeric field.  Optional `Clamp` (`"min:max"`).

```csharp
var (out, newVal) = rule.SolveRule(new() {
    ["Record"]    = record,
    ["FieldName"] = "RetryCount",
    ["Step"]      = "1",
    ["Clamp"]     = "0:10"      // never exceed 10
});
```

---

## Group: Query

All Query rules require `IDMEEditor`, `DataSourceName`, and `EntityName`.

### `Query.RunScalarQuery`
Returns the first field of the first matching row.

```csharp
var (out, result) = rule.SolveRule(new() {
    ["IDMEEditor"]     = editor,
    ["DataSourceName"] = "SalesDB",
    ["EntityName"]     = "Orders",
    ["Filters"]        = "CustomerId:=:C001;Status:=:Shipped",
    ["OutputField"]    = "TotalAmount"
});
decimal total = Convert.ToDecimal(result);
```

---

### `Query.SetFromQuery`
Runs a query and writes the result into a record field.

```csharp
var (out, _) = rule.SolveRule(new() {
    ["IDMEEditor"]     = editor,
    ["DataSourceName"] = "ProductDB",
    ["EntityName"]     = "Products",
    ["Filters"]        = "Sku:=:ABC-123",
    ["OutputField"]    = "Price",
    ["TargetField"]    = "UnitPrice",
    ["Record"]         = orderLineRecord
});
var updated = (IDictionary<string,object>)out["Record"];
// updated["UnitPrice"] now holds the DB price
```

---

### `Query.RunParameterizedLookup`
Classic ID → label lookup.

```csharp
var (out, label) = rule.SolveRule(new() {
    ["IDMEEditor"]     = editor,
    ["DataSourceName"] = "CRM",
    ["EntityName"]     = "Customers",
    ["LookupField"]    = "CustomerId",
    ["LookupValue"]    = "C001",
    ["ReturnField"]    = "FullName",
    ["DefaultValue"]   = "Unknown"
});
// label → "Alice Smith"
```

---

## Group: Data Quality (DQ)

### `DQ.IsNullOrEmpty`
Returns `true` when the value is null, DBNull, empty, or whitespace.

```csharp
var (_, isEmpty) = rule.SolveRule(new() { ["Value"] = "   " });
// isEmpty → true
```

---

### `DQ.IsInRange`
Checks `Min ≤ Value ≤ Max` using `IComparable`.  Optional `Inclusive` (`"true"`/`"false"`).

```csharp
var (_, ok) = rule.SolveRule(new() {
    ["Value"]     = "75",
    ["Min"]       = "0",
    ["Max"]       = "100",
    ["Inclusive"] = "true"
});
```

---

### `DQ.MatchesRegex`
Regex match.  Optional `IgnoreCase` (`"true"`).

```csharp
var (_, ok) = rule.SolveRule(new() {
    ["Value"]      = "Order-2026-001",
    ["Pattern"]    = @"^Order-\d{4}-\d{3}$",
    ["IgnoreCase"] = "false"
});
```

---

### `DQ.IsInList`
Checks the value against a comma-separated allowlist.

```csharp
var (_, ok) = rule.SolveRule(new() {
    ["Value"]      = "Admin",
    ["List"]       = "Admin,Editor,Viewer",
    ["IgnoreCase"] = "true"
});
```

---

### `DQ.MatchesExpectedType`
Checks the value can be parsed as `int`, `double`, `decimal`, `datetime`, `guid`, `bool`, `string`.

```csharp
var (_, ok) = rule.SolveRule(new() { ["Value"] = "2026-01-15", ["Type"] = "datetime" });
```

---

### `DQ.IsUnique`  *(data-access)*
Queries a data source to ensure no other row has the same field value.

```csharp
var (_, unique) = rule.SolveRule(new() {
    ["IDMEEditor"]     = editor,
    ["DataSourceName"] = "AppDB",
    ["EntityName"]     = "Users",
    ["FieldName"]      = "Email",
    ["Value"]          = "alice@example.com"
});
```

---

### `DQ.ReferentialIntegrity`  *(data-access)*
Verifies a value exists as a key in a reference entity (FK check).

```csharp
var (_, ok) = rule.SolveRule(new() {
    ["IDMEEditor"]     = editor,
    ["DataSourceName"] = "AppDB",
    ["EntityName"]     = "Categories",
    ["KeyField"]       = "CategoryId",
    ["Value"]          = "5"
});
```

---

## Group: Transform

### `Transform.TrimAndNormalize`
Trims whitespace and normalises case.  `CaseMode`: `Lower`, `Upper`, `Title`, `None`.

```csharp
var (out, result) = rule.SolveRule(new() {
    ["Value"]    = "  hello world  ",
    ["CaseMode"] = "Title"
});
// result → "Hello World"
```

---

### `Transform.ReplacePattern`
`Regex.Replace` with optional `IgnoreCase`.

```csharp
var (out, result) = rule.SolveRule(new() {
    ["Value"]       = "Phone: 555-1234",
    ["Pattern"]     = @"\d{3}-\d{4}",
    ["Replacement"] = "***-****"
});
```

---

### `Transform.SplitAndTake`
Splits by delimiter and returns the element at a zero-based index.

```csharp
var (out, result) = rule.SolveRule(new() {
    ["Value"]     = "Alice,Bob,Carol",
    ["Delimiter"] = ",",
    ["Index"]     = "1"
});
// result → "Bob"
```

---

### `Transform.PadOrTruncate`
Pads or truncates to a fixed length.  `Mode`: `PadLeft`, `PadRight` (default), `Truncate`.

```csharp
var (out, result) = rule.SolveRule(new() {
    ["Value"]   = "42",
    ["Length"]  = "6",
    ["Mode"]    = "PadLeft",
    ["PadChar"] = "0"
});
// result → "000042"
```

---

## Group: Numeric

### `Numeric.RoundNumeric`
`Math.Round` with configurable `Decimals` and `MidpointRounding` (`AwayFromZero`, `ToEven`).

```csharp
var (_, result) = rule.SolveRule(new() {
    ["Value"]    = "2.5",
    ["Decimals"] = "0",
    ["Rounding"] = "AwayFromZero"
});
// result → 3
```

---

### `Numeric.Clamp`
Clamps a numeric value between `Min` and `Max`.

```csharp
var (_, result) = rule.SolveRule(new() {
    ["Value"] = "150",
    ["Min"]   = "0",
    ["Max"]   = "100"
});
// result → 100
```

---

### `Numeric.GetEntityAggregate`  *(data-access)*
Computes `Sum`, `Avg`, `Min`, `Max`, or `Count` over a field in a data source.

```csharp
var (out, total) = rule.SolveRule(new() {
    ["IDMEEditor"]     = editor,
    ["DataSourceName"] = "SalesDB",
    ["EntityName"]     = "OrderLines",
    ["FieldName"]      = "Amount",
    ["AggregateType"]  = "Sum"
});
```

---

## Group: Date

### `Date.AddDatePart`
Adds years/months/days/hours/minutes/seconds to a date.

```csharp
var (_, result) = rule.SolveRule(new() {
    ["Value"]  = "2026-01-01",
    ["Part"]   = "Days",
    ["Amount"] = "30"
});
// result → DateTime 2026-01-31
```

---

### `Date.DateDiff`
Returns the difference between two dates.  `Unit`: `Days`, `Hours`, `Minutes`, `Seconds`.

```csharp
var (_, diff) = rule.SolveRule(new() {
    ["StartDate"] = "2026-01-01",
    ["EndDate"]   = "2026-03-17",
    ["Unit"]      = "Days"
});
// diff → 75
```

---

### `Date.IsWithinBusinessHours`
Checks whether a date/time falls within configurable business hours,
with timezone and weekend suppression.

```csharp
var (_, ok) = rule.SolveRule(new() {
    ["Value"]          = DateTime.UtcNow,
    ["StartHour"]      = "9",
    ["EndHour"]        = "17",
    ["TimeZone"]       = "Eastern Standard Time",
    ["ExcludeWeekend"] = "true"
});
```

---

## Group: Security

### `Security.HashValue`
Hashes a value with `SHA256` (default), `SHA512`, or `MD5`.  Optional `Salt`.

```csharp
var (out, hash) = rule.SolveRule(new() {
    ["Value"]     = "p@ssw0rd",
    ["Algorithm"] = "SHA256",
    ["Salt"]      = "random-salt"
});
string hex = hash.ToString();
```

---

### `Security.MaskValue`
Masks a string using modes: `Last4`, `First4`, `Middle`, `All`.

```csharp
var (_, masked) = rule.SolveRule(new() {
    ["Value"] = "4111111111111111",
    ["Mode"]  = "Last4"
});
// masked → "************1111"
```

---

### `Security.TokenizeValue`
HMACSHA256 deterministic tokenisation — same input + secret always yields the same token,
but you cannot reverse the token back to the original value.

```csharp
var (out, token) = rule.SolveRule(new() {
    ["Value"]  = "123-45-6789",
    ["Secret"] = "my-vault-key"
});
// out["Token"] → hex token
// out["Original"] → stored in in-process vault (keyed by token)
```

---

## Group: Enrich

### `Enrich.LookupEnrich`  *(data-access)*
Looks up a row in a data source and copies a named field into the output.

```csharp
var (out, val) = rule.SolveRule(new() {
    ["IDMEEditor"]       = editor,
    ["DataSourceName"]   = "CRM",
    ["LookupEntityName"] = "Customers",
    ["LookupField"]      = "CustomerId",
    ["Value"]            = customerId,
    ["ReturnField"]      = "CreditLimit",
    ["DefaultValue"]     = "0"
});
```

---

### `Enrich.MergeFields`
Merges a `"key=value;…"` string or `IDictionary` into the output key/value bag.

```csharp
var (out, _) = rule.SolveRule(new() {
    ["Source"] = "Region=EMEA;Tier=Gold",
    ["Record"] = existingRecord
});
var enriched = (IDictionary<string,object>)out["Record"];
```

---

### `Enrich.AddMetadataFields`
Stamps system fields onto a record: `__timestamp`, `__ruleKey`, `__version`, plus custom `__tag_*` pairs.

```csharp
var (out, _) = rule.SolveRule(new() {
    ["Record"]  = record,
    ["RuleKey"] = "Order.Process",
    ["Version"] = "1.0",
    ["Tags"]    = "env=prod;region=us-east"
});
```

---

## Group: Route

### `Route.RouteOnCondition`
Evaluates `"condition=>route"` pairs and returns the first matching route label.

```csharp
var (out, route) = rule.SolveRule(new() {
    ["Value"]  = "80",
    ["Routes"] = ">=90=>HighScore;>=60=>Pass;<60=>Fail"
});
// route → "Pass"
```

---

### `Route.SplitIntoBuckets`
Maps a numeric value to a bucket label using `"min-max=label"` ranges.

```csharp
var (out, bucket) = rule.SolveRule(new() {
    ["Value"]   = "45",
    ["Buckets"] = "0-25=Low;26-75=Medium;76-100=High"
});
// bucket → "Medium"
```

---

### `Route.ThrottleRule`
Sliding-window rate limiter backed by `IRuleStateStore`.
Returns `true` when the call is allowed, `false` when the limit is exceeded.

```csharp
var (out, allowed) = rule.SolveRule(new() {
    ["StateKey"]    = "api:userId:123",
    ["MaxCalls"]    = "10",
    ["WindowSecs"]  = "60"
});
if (!(bool)allowed) throw new RateLimitExceededException();
```

---

## Group: Flow

### `Flow.RetryWithBackoff`
Manages exponential backoff retry state in `IRuleStateStore`.
Call once per attempt; returns whether to retry and how many ms to wait.

```csharp
var (out, shouldRetry) = rule.SolveRule(new() {
    ["StateKey"]   = $"job:{jobId}",
    ["MaxRetries"] = "5",
    ["BaseDelayMs"]= "500"
});
if ((bool)shouldRetry)
    await Task.Delay((int)out["WaitMs"]);
```

---

### `Flow.CircuitBreaker`
Classic circuit-breaker (Closed → Open → HalfOpen).
Pass `CallSucceeded = "true"/"false"` to report the previous call outcome.

```csharp
// Before calling external service:
var (out, allowed) = rule.SolveRule(new() {
    ["StateKey"]          = "ext:PaymentGateway",
    ["FailureThreshold"]  = "3",
    ["ResetSeconds"]      = "30",
    ["CallSucceeded"]     = lastCallOk.ToString()
});
if (!(bool)allowed) return ServiceUnavailable();
```

---

### `Flow.DeadLetterQueue`  *(data-access)*
Writes a failed record to a dead-letter table so it can be inspected and reprocessed.

```csharp
rule.SolveRule(new() {
    ["IDMEEditor"]     = editor,
    ["DataSourceName"] = "AppDB",
    ["EntityName"]     = "DeadLetterQueue",
    ["RecordId"]       = order.Id.ToString(),
    ["FailureReason"]  = ex.Message,
    ["Payload"]        = JsonSerializer.Serialize(order)
});
```

---

## Group: Notify

### `Notify.SendWebhook`
HTTP POST a JSON payload to a URL.  Optional `Headers` (`"key:value;…"`) and `TimeoutMs`.

```csharp
rule.SolveRule(new() {
    ["Url"]     = "https://hooks.example.com/order-created",
    ["Payload"] = JsonSerializer.Serialize(orderEvent),
    ["Headers"] = "Authorization:Bearer token123"
});
```

---

### `Notify.EmitAuditTrail`  *(data-access)*
Inserts an audit row into a data source.

```csharp
rule.SolveRule(new() {
    ["IDMEEditor"]     = editor,
    ["DataSourceName"] = "AuditDB",
    ["AuditEntity"]    = "AuditLog",
    ["Action"]         = "OrderShipped",
    ["UserId"]         = currentUser,
    ["RecordId"]       = orderId,
    ["Detail"]         = "Tracking: 1Z999..."
});
```

---

### `Notify.LogMessage`
Writes a `{Token}`-interpolated message to `IDMEEditor.AddLogMessage` or `Console`.

```csharp
rule.SolveRule(new() {
    ["IDMEEditor"] = editor,
    ["Level"]      = "Info",
    ["Template"]   = "Order {OrderId} dispatched to {Customer}",
    ["OrderId"]    = order.Id,
    ["Customer"]   = order.CustomerName
});
```

---

## Group: Schema

### `Schema.ValidateSchemaFields`
Checks required fields are present and optionally validates their types.
`FieldTypes` format: `"field:type;…"` where type is `int`, `double`, `decimal`, `bool`, `datetime`, `guid`, `string`.

```csharp
var (out, ok) = rule.SolveRule(new() {
    ["Record"]         = incomingRecord,
    ["RequiredFields"] = "OrderId,CustomerId,TotalAmount",
    ["FieldTypes"]     = "OrderId:int;TotalAmount:decimal"
});
if (!(bool)ok)
    Console.WriteLine($"Missing: {out["MissingFields"]}  Invalid: {out["InvalidFields"]}");
```

---

### `Schema.EnforceNotNull`
Enforces that named fields are non-null and non-empty.

```csharp
var (out, ok) = rule.SolveRule(new() {
    ["Record"] = record,
    ["Fields"] = "Name,Email,PhoneNumber"
});
if (!(bool)ok)
    Console.WriteLine($"Null fields: {out["NullFields"]}");
```

---

## Builtin Parsers

Parsers translate expression strings into `RuleStructure` token lists.

| Parser key | Class | Use |
|---|---|---|
| `CsvColumnParser`    | `BuiltinParsers/CsvColumnParser.cs`    | `[alias.]ColumnName[index]` |
| `SqlWhereParser`     | `BuiltinParsers/SqlWhereParser.cs`     | SQL WHERE expressions |
| `JsonRuleTreeParser` | `BuiltinParsers/JsonRuleTreeParser.cs` | `{rule, params, children}` JSON trees |
| `FormulaParser`      | `BuiltinParsers/FormulaParser.cs`      | Excel-style formulas (`SUM(A1,B2)`) |
| `NfelParser`         | `BuiltinParsers/NfelParser.cs`         | Natural formula expressions (`x > 0 AND y != null`) |

---

## Chaining Rules in a Pipeline

```csharp
// Validate → Enrich → Compute → Persist
var record = new Dictionary<string, object> {
    ["Email"] = rawEmail, ["BirthDate"] = rawDate
};

// 1. Validate e-mail
var (_, emailOk) = registry.GetRule("Validate.ValidEmail")
    .SolveRule(new() { ["Value"] = record["Email"] });
if (!(bool)emailOk) return ValidationFailed("Bad email");

// 2. Compute age
var (ageOut, _) = registry.GetRule("Compute.AgeFromDate")
    .SolveRule(new() { ["Value"] = record["BirthDate"], ["Unit"] = "Years" });
record["Age"] = ageOut["Result"];

// 3. Map age bucket
var (bucketOut, bucket) = registry.GetRule("Route.SplitIntoBuckets")
    .SolveRule(new() { ["Value"] = record["Age"].ToString(),
                       ["Buckets"] = "0-17=Minor;18-64=Adult;65-120=Senior" });
record["AgeGroup"] = bucket;

// 4. Enforce required fields before save
var (schemaOut, schemaOk) = registry.GetRule("Schema.EnforceNotNull")
    .SolveRule(new() { ["Record"] = record, ["Fields"] = "Email,Age,AgeGroup" });
if (!(bool)schemaOk) return ValidationFailed(schemaOut["NullFields"].ToString());

// 5. Persist
customerRepository.Save(record);
```

---

## Writing Your Own Rule

```csharp
[Rule(ruleKey: "MyApp.TaxCalculator", ParserKey = "RulesParser", RuleName = "TaxCalculator")]
public sealed class TaxCalculator : IRule
{
    public string       RuleText  { get; set; } = "MyApp.TaxCalculator";
    public IRuleStructure Structure { get; set; } = new RuleStructure();

    public (Dictionary<string, object> outputs, object result) SolveRule(
        Dictionary<string, object> parameters = null)
    {
        var output = new Dictionary<string, object>();
        if (!parameters.TryGetValue("Amount", out var amt)) { output["Error"] = "Amount required"; return (output, null); }

        double amount   = Convert.ToDouble(amt);
        double rate     = parameters.TryGetValue("Rate", out var r) ? Convert.ToDouble(r) : 0.2;
        double tax      = amount * rate;

        output["Tax"]    = tax;
        output["Total"]  = amount + tax;
        return (output, tax);
    }
}
```

The `RuleRegistry` picks up any `[Rule]`-attributed class in loaded assemblies automatically — no registration code needed.

