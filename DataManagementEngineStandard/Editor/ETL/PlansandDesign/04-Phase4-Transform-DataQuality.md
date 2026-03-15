# Phase 4 — Transformation Pipeline & Data Quality Layer

**Version:** 1.0  
**Date:** 2026-03-13  
**Status:** Design  
**Depends on:** Phase 1, Phase 2

---

## 1. Objective

Build the full plug-in library of **Transformers**, **DQ Validators**, **Filters**, **Enrichers**, **Aggregators**, and the **Expression Engine** that evaluates field-mapping expressions and filter predicates at pipeline runtime. This is the "T" in ETL — the value-add layer that makes raw data fit for its destination.

---

## 2. Transformer Library

Each transformer is an `IPipelineTransformer` plugin decorated with `[PipelinePlugin]`.

### 2.1 FieldMapTransformer — Column Rename / Select / Drop
```
Config:
  Mappings: { "dest_col": "src_col" }   // explicit 1:1 rename
  IncludeUnmapped: true | false          // pass-through unmapped cols
  DropColumns: ["col_to_remove"]

Behaviour:
  - Projects input records onto new schema
  - IncludeUnmapped=true: src cols not in Mappings pass through unchanged
  - IncludeUnmapped=false: only explicitly mapped cols appear in output
```

### 2.2 ExpressionTransformer — Computed Fields
```
Config:
  Expressions: {
    "full_name":   "FirstName + ' ' + LastName",
    "total_price": "Quantity * UnitPrice",
    "is_adult":    "Age >= 18",
    "fiscal_year": "YEAR(OrderDate) + (MONTH(OrderDate) >= 7 ? 1 : 0)"
  }

Supported expression functions:
  String:  UPPER, LOWER, TRIM, SUBSTRING, REPLACE, CONCAT, LEN, COALESCE, ISNULL
  Date:    YEAR, MONTH, DAY, DATEADD, DATEDIFF, TODAY, NOW, FORMAT
  Math:    ROUND, FLOOR, CEIL, ABS, POWER, SQRT, MOD
  Logic:   IF(cond, true_val, false_val), CASE WHEN ... THEN ... END
  Convert: ToString, ToInt, ToDecimal, ToDate

Engine: Roslyn-based ExpressionEvaluator (sandboxed, no file/network access)
```

### 2.3 TypeCastTransformer — Type Coercion
```
Config:
  Rules: { "OrderDate": "datetime", "Amount": "decimal", "Active": "bool" }
  OnCastError: Skip | Null | Default | Fail
  Locale: "en-US"         // for date/decimal parsing
  DateFormats: ["MM/dd/yyyy", "yyyy-MM-dd"]
```

### 2.4 FilterTransformer — Row Predicate
```
Config:
  Expression: "Country == 'US' AND Amount > 100"
  Mode: Keep | Reject    // Keep = forward matching rows, Reject = forward non-matching rows

Note: Rejected rows are simply discarded (not routed to error sink).
To route non-matching rows, use two FilterTransformers + Split step.
```

### 2.5 DeDuplicateTransformer
```
Config:
  KeyFields: ["CustomerId", "OrderDate"]
  Strategy: KeepFirst | KeepLast | KeepMax(field) | KeepMin(field)
  WindowSize: 100000   // max rows to buffer for dedup (streaming dedup uses bloom filter beyond this)
```

### 2.6 LookupTransformer — Reference Data Enrichment
```
Config:
  LookupSource: "ReferenceDB"
  LookupEntity: "CountryCodes"
  LookupKey:    "Code"           // field in lookup entity to match
  InputKey:     "CountryCode"    // field in stream record to match against
  OutputFields: { "CountryName": "Name", "Region": "Region" }
  OnMiss:       Null | Reject | Default(value)
  CacheSize:    10000            // LRU cache for lookup results
```

### 2.7 AggregateTransformer — Group-by / Rollup
```
Config:
  GroupBy: ["Country", "Category"]
  Aggregates: {
    "TotalAmount": "SUM(Amount)",
    "OrderCount":  "COUNT(*)",
    "AvgAmount":   "AVG(Amount)",
    "MaxDate":     "MAX(OrderDate)"
  }

NOTE: Aggregation buffers all records in-memory per group.
For very large datasets, use RDBMS-side aggregation via a native query source.
```

### 2.8 SplitTransformer — Fan-Out by Expression
```
Config:
  Routes: [
    { "Tag": "EU",  "Filter": "Region == 'EU'" },
    { "Tag": "US",  "Filter": "Region == 'US'" },
    { "Tag": "Rest","Filter": "true" }           // catch-all
  ]

Output: Records get Meta["__route_tag"] = "EU"/"US"/"Rest"
WorkFlow routing step then fans to different sinks by tag.
```

### 2.9 ScriptTransformer — Inline C# Transform
```
Config:
  Script: |
    // 'record' is the current PipelineRecord
    // 'ctx' is PipelineRunContext
    // return modified record or null to drop it
    record["FullName"] = record["First"] + " " + record["Last"];
    return record;

Engine: Roslyn scripting with sandbox restrictions (no I/O, no reflection)
Script cache: compiled once on first run, reused for all subsequent records.
```

---

## 3. Data Quality (DQ) Validator Library

Each validator is an `IPipelineValidator` plugin. Any number of validators stack on a step. A record that fails any configured validator is routed to the `ErrorSink`.

### 3.1 NotNullValidator
```
Config:
  Fields: ["CustomerId", "OrderDate", "Amount"]
  Message: "Required field {Field} is null"
```

### 3.2 RegexValidator
```
Config:
  Rules:
    - { Field: "Email",   Pattern: "^[^@]+@[^@]+\\.[^@]+$", Message: "Invalid email" }
    - { Field: "PostCode",Pattern: "^[0-9]{5}$",             Message: "Invalid ZIP" }
```

### 3.3 RangeValidator
```
Config:
  Rules:
    - { Field: "Age",       Min: 0,    Max: 150 }
    - { Field: "Amount",    Min: 0.01 }
    - { Field: "OrderDate", Min: "2000-01-01", Max: "TODAY" }
```

### 3.4 ReferentialIntegrityValidator
```
Config:
  Rules:
    - { Field: "CountryCode", LookupSource: "ReferenceDB", LookupEntity: "Countries", LookupField: "Code" }
```

### 3.5 UniquenessValidator
```
Config:
  Fields: ["InvoiceNumber"]
  Scope: RunLocal       // check against records seen in this run
    | DataSource(name)  // check against existing rows in a data source
```

### 3.6 CustomExpressionValidator
```
Config:
  Rules:
    - { Expression: "Quantity > 0 OR Status == 'CANCELLED'", Message: "Invalid quantity" }
```

### 3.7 SchemaValidator
```
Validates that PipelineRecord values are type-compatible with the schema.
Auto-applied when TypeCastTransformer is NOT in the chain and schema is strict.
```

---

## 4. Expression Engine Design

The expression evaluator powers both `ExpressionTransformer` and `FilterTransformer` without using `eval`/reflection. It is:
- **Fast**: expressions compile on first use via Roslyn, subsequent calls hit cache
- **Safe**: sandboxed — can only access record fields and built-in functions
- **Debuggable**: parse errors surface with column/token info

```csharp
namespace TheTechIdea.Beep.Pipelines.Expressions
{
    public interface IExpressionEvaluator
    {
        /// <summary>
        /// Compile an expression string once.
        /// Returns a compiled delegate for repeated fast evaluation.
        /// </summary>
        ExpressionDelegate Compile(string expression, PipelineSchema schema);
    }

    public delegate object? ExpressionDelegate(PipelineRecord record, PipelineRunContext ctx);

    public class RoslynExpressionEvaluator : IExpressionEvaluator
    {
        private readonly ConcurrentDictionary<string, ExpressionDelegate> _cache = new();

        public ExpressionDelegate Compile(string expression, PipelineSchema schema)
        {
            return _cache.GetOrAdd(BuildCacheKey(expression, schema), _ =>
                CompileInternal(expression, schema));
        }

        private ExpressionDelegate CompileInternal(string expression, PipelineSchema schema)
        {
            // 1. Wrap expression in a method body
            // 2. Add helper functions (UPPER, YEAR, IF, etc.)
            // 3. Compile with Roslyn ScriptingEngine with restricted assembly references
            // 4. Return compiled delegate
        }
    }
}
```

### Expression Grammar (EBNF sketch)
```
expr       = or-expr
or-expr    = and-expr ( "OR" and-expr )*
and-expr   = not-expr ( "AND" not-expr )*
not-expr   = "NOT"? compare-expr
compare-expr = additive-expr ( ("==" | "!=" | "<" | ">" | "<=" | ">=" | "LIKE" | "IN") additive-expr )?
additive-expr = mult-expr ( ("+" | "-") mult-expr )*
mult-expr  = primary ( ("*" | "/" | "%") primary )*
primary    = literal | field-ref | func-call | "(" expr ")"
field-ref  = identifier
func-call  = identifier "(" expr* ")"
literal    = string | number | bool | "null" | "TODAY" | "NOW"
```

---

## 5. DQ Report Model

Every validator run produces a structured report that becomes part of `PipelineRunResult`.

```csharp
public class DQReport
{
    public string RunId         { get; set; } = string.Empty;
    public string PipelineId    { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public long TotalRecords    { get; set; }
    public long PassedRecords   { get; set; }
    public long WarnedRecords   { get; set; }
    public long RejectedRecords { get; set; }

    public double PassRate  => TotalRecords == 0 ? 1.0 : (double)PassedRecords / TotalRecords;

    /// <summary>Per-rule statistics.</summary>
    public List<DQRuleResult> RuleResults { get; set; } = new();
}

public class DQRuleResult
{
    public string RuleName       { get; set; } = string.Empty;
    public string ValidatorPluginId { get; set; } = string.Empty;
    public long   FailCount      { get; set; }
    public long   WarnCount      { get; set; }
    /// <summary>Up to 100 sample failing records for diagnostics.</summary>
    public List<PipelineRecord> SampleFailures { get; set; } = new();
}
```

---

## 6. Column-Level Data Lineage

Every time a transformer reads field A and writes field B, a lineage record is emitted:

```csharp
public class DataLineageRecord
{
    public string RunId       { get; set; } = string.Empty;
    public string StepId      { get; set; } = string.Empty;
    public string StepName    { get; set; } = string.Empty;
    public string TransformerPluginId { get; set; } = string.Empty;

    public string SourceDataSource  { get; set; } = string.Empty;
    public string SourceEntity      { get; set; } = string.Empty;
    public string SourceField       { get; set; } = string.Empty;

    public string DestDataSource    { get; set; } = string.Empty;
    public string DestEntity        { get; set; } = string.Empty;
    public string DestField         { get; set; } = string.Empty;

    public string TransformExpression { get; set; } = string.Empty; // e.g. "UPPER(src_field)"
    public DateTime Timestamp       { get; set; } = DateTime.UtcNow;
}
```

Lineage records are written to `ExePath/Lineage/{runId}.lineage.json` and can be queried via `PipelineManager.GetLineageAsync(runId)`.

---

## 7. Transformer Composition API

Application code and Designer UI compose transformers using a fluent builder:

```csharp
var pipeline = new PipelineDefinition { Name = "Orders ETL" }
    .WithSource("beep.source.datasource", new {
        DataSourceName = "OrdersDB",
        EntityName     = "Orders"
    })
    .AddTransformer("beep.transform.fieldmap", new {
        Mappings = new Dictionary<string,string> {
            ["customer_id"]   = "CustomerID",
            ["order_amount"]  = "TotalAmount",
            ["created_date"]  = "OrderDate"
        }
    })
    .AddTransformer("beep.transform.expression", new {
        Expressions = new Dictionary<string,string> {
            ["year"] = "YEAR(created_date)",
            ["tax"]  = "order_amount * 0.1"
        }
    })
    .AddValidator("beep.validate.notnull",  new { Fields = new[] { "customer_id", "order_amount" } })
    .AddValidator("beep.validate.range",    new { Rules = new[] { new { Field="order_amount", Min=0.01 } }})
    .WithSink("beep.sink.datasource", new {
        DataSourceName = "DataWarehouse",
        EntityName     = "fact_orders"
    })
    .WithErrorSink("beep.sink.errorlog")
    .WithBatchSize(1000)
    .WithRetries(3)
    .Build();
```

The `.WithSource` / `.AddTransformer` etc. are extension methods on `PipelineDefinition`.

---

## 8. Deliverables (Implementation Checklist)

- [ ] `ExpressionEvaluator` (Roslyn-based, sandboxed) with unit tests
- [ ] `FieldMapTransformer`
- [ ] `ExpressionTransformer`
- [ ] `TypeCastTransformer`
- [ ] `FilterTransformer`
- [ ] `DeDuplicateTransformer`
- [ ] `LookupTransformer`
- [ ] `AggregateTransformer`
- [ ] `SplitTransformer`
- [ ] `ScriptTransformer` (Roslyn inline)
- [ ] `NotNullValidator`
- [ ] `RegexValidator`
- [ ] `RangeValidator`
- [ ] `ReferentialIntegrityValidator`
- [ ] `UniquenessValidator`
- [ ] `CustomExpressionValidator`
- [ ] `DQReport` + `DQRuleResult` models
- [ ] `DataLineageRecord` emit from transformers
- [ ] `PipelineDefinition` fluent builder API
- [ ] Unit tests for each transformer (deterministic input → expected output)
- [ ] Unit tests for each validator (pass, warn, reject cases)

---

## 9. Estimated Effort

| Task | Days |
|------|------|
| Expression evaluator + grammar | 4 |
| 9 transformer plugins | 5 |
| 6 validator plugins | 3 |
| DQ report model + aggregation | 1 |
| DataLineage emission | 1 |
| Fluent builder API | 1 |
| Unit tests | 3 |
| **Total** | **18 days** |
