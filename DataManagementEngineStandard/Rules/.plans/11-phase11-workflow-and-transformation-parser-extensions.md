# Phase 11 — Workflow & Transformation Parser Extensions

## Purpose
Add specialised `IRuleParser` implementations that understand domain-specific expression
syntaxes commonly used in ETL and workflow automation tools. Rather than forcing everything
through the general infix-expression parser, teams can register parsers that speak the
language native to their data pipeline.

---

## Motivation — parser landscape in ETL/workflow tools

| Tool | Expression language | What BeepDM needs |
|---|---|---|
| n8n | JMESPath / Handlebars `{{$json["field"]}}` | JSONPath parser |
| dbt | Jinja2 `{{ config(...) }}` + SQL | SQL WHERE parser |
| Apache NiFi | NFEL `${field:toUpper()}` | Function-call parser |
| Airbyte | JSONata `$sum(Account.(Order.Product.(Price * Quantity)))` | JSONata-subset parser |
| CSV imports | Column-index or header-name references | CSV-column parser |
| Excel-like formulas | `=IF(A1>0, A1*1.1, 0)` | Formula parser |
| Condition builder (UI) | Drag-and-drop rule tree serialised as JSON | JSON Rule Tree parser |

---

## Planned Parser Implementations

### `CsvColumnParser` — `[RuleParser(parserKey: "CsvColumn")]`
**Purpose:** Parse rule expressions that reference CSV columns by name or 0-based index.

**Supported syntax:**
```
[FirstName]               → field by header name
[2]                       → field by 0-based column index
[TotalPrice] * 1.1        → arithmetic on column value
NOT IsNullOrEmpty([Email]) → DQ rule on column
```

**Token additions required:**
- `CsvColumnRef` token type (bracket-delimited identifier or integer)

**Output:** `ParseResult` with `RuleStructure` whose tokens resolve `CsvColumnRef` from
the `parameters["ColumnValues"]` dict at evaluation time.

**Use case:** Bulk CSV ingest rules — describe column-level transforms and validations
in the same language as the rest of the rules engine.

---

### `SqlWhereParser` — `[RuleParser(parserKey: "SqlWhere")]`
**Purpose:** Accept a SQL WHERE clause fragment and translate it into `Token` AST for
the standard RPN evaluator.

**Supported syntax (subset of SQL-92 WHERE):**
```sql
age >= 18 AND country IN ('US','CA')
UPPER(email) LIKE '%@EXAMPLE.COM'
created_at BETWEEN '2024-01-01' AND '2024-12-31'
status IS NOT NULL
```

**Mapping to existing tokens:**
| SQL construct | Token type |
|---|---|
| `AND` / `OR` / `NOT` | `And` / `Or` / `Not` |
| `IS NULL` / `IS NOT NULL` | special `NullCheck` compound |
| `IN (...)` | `InList` compound token |
| `BETWEEN x AND y` | expands to `>= x AND <= y` |
| `LIKE` pattern | `MatchesRegex` rule reference |
| `UPPER(x)` | `FunctionCall` token → built-in `Transform.TrimAndNormalize` |

**Diagnostics:** line/column tracking from SQL lexer; meaningful messages
("IN list must be non-empty", "BETWEEN: lower bound must precede upper bound").

**Use case:** ETL mapping filters, data sync row-level predicates, Defaults
conditions authored by SQL-literate data engineers without learning new syntax.

---

### `JsonRuleTreeParser` — `[RuleParser(parserKey: "JsonRuleTree")]`
**Purpose:** Deserialise a JSON rule-tree document (as produced by UI drag-and-drop
rule builders like React QueryBuilder, n8n's condition nodes, etc.) into the standard
`RuleStructure` / `Token` AST.

**JSON schema:**
```json
{
  "combinator": "and",
  "rules": [
    { "field": "age",    "operator": ">=",     "value": 18 },
    { "field": "status", "operator": "in",     "value": ["active","trial"] },
    {
      "combinator": "or",
      "rules": [
        { "field": "country", "operator": "=",  "value": "US" },
        { "field": "country", "operator": "=",  "value": "CA" }
      ]
    }
  ]
}
```

**Mapping:** Each `rule` node → `EntityField` + operator + literal `Token`s.
Nested `combinator` groups → parenthesised token sub-sequences.

**Diagnostics:** Schema validation errors before tokenisation (missing `field`,
unknown `operator`, type mismatch between `value` and expected operand type).

**Use case:** Visual rule builder output; persisted UI filter state; REST API
rule submission (`POST /rules` with JSON body).

---

### `FormulaParser` — `[RuleParser(parserKey: "Formula")]`
**Purpose:** Accept Excel/Google-Sheets-style formula syntax for use in column derivation
and ETL transform rules.

**Supported functions (first iteration):**
| Formula function | Maps to built-in rule |
|---|---|
| `IF(cond, t, f)` | ternary expression node |
| `ISNULL(x)` | `DQ.IsNullOrEmpty` |
| `UPPER(x)` / `LOWER(x)` | `Transform.TrimAndNormalize` |
| `TRIM(x)` | `Transform.TrimAndNormalize` |
| `LEN(x)` | string length numeric literal |
| `DATEADD(part, n, d)` | `Date.AddDatePart` |
| `DATEDIFF(part, s, e)` | `Date.DateDiff` |
| `ROUND(x, n)` | `Numeric.Round` |
| `CLAMP(x, min, max)` | `Numeric.Clamp` |
| `HASH(x)` | `Security.HashValue` |
| `MASK(x)` | `Security.MaskValue` |

**Token additions required:**
- `FunctionCall` token type carrying function name + arity
- `Comma` already exists (re-use)

**Diagnostics:** arity checks ("IF requires exactly 3 arguments"),
type-hint mismatches ("ROUND: second argument must be integer literal").

**Use case:** Self-service data transformation rules authored by analysts;
Defaults column-derivation expressions; Mapping transform fields.

---

### `NfelParser` — `[RuleParser(parserKey: "NFEL")]`
**Purpose:** Accept a NiFi Expression Language (NFEL) subset for users migrating
Apache NiFi flows into BeepDM pipelines.

**Supported syntax:**
```
${fieldName}
${fieldName:toUpper()}
${fieldName:substring(0,5)}
${fieldName:matches('[A-Z]+')}
${fieldName:gt(100):and(${otherField:isNull():not()})}
```

**Mapping:** chained calls convert to nested built-in rule references in the AST.
`${field}` → `EntityField` token. Function calls → rule references.

**Diagnostics:** brace-matching errors; unknown function names list known equivalents
as suggestions.

**Use case:** NiFi migration path; BeepDM becomes a viable drop-in alternative.

---

## Architecture for Parser Extensibility

### New token types required (add to `enums.cs`)

```csharp
CsvColumnRef,      // [header] or [0]
FunctionCall,      // IF(, UPPER(, DATEADD( — carries FunctionName and Arity
InList,            // IN (a, b, c) — compound
NullCheck,         // IS NULL / IS NOT NULL — compound
BetweenRange,      // BETWEEN x AND y — compound (expands in evaluator)
```

### `FunctionCallToken` (new model class in Models/Rules)
```csharp
public sealed class FunctionCallToken : Token
{
    public string FunctionName { get; init; }
    public int    Arity        { get; init; }   // expected argument count; -1 = variadic
}
```

### Evaluator additions required (`RulesEngine.ExpressionEvaluation.cs`)
- Handle `FunctionCall` tokens: dispatch to registered built-in rule by function-name map
- Handle `InList` compound: evaluate membership
- Handle `NullCheck` compound
- Handle `BetweenRange` compound (expand to two comparisons)

---

## Implementation Guidelines

- All parsers live in `DataManagementEngineStandard/Rules/BuiltinParsers/`
- Each is `public sealed class XxxParser : IRuleParser`
- Each carries `[RuleParser(parserKey: "Xxx")]`
- Each has a parameterless constructor (for `RuleRegistry` discovery)
- Parsers are not `IDisposable` unless they hold file handles
- The `SqlWhereParser` may internally use `Tokenizer` for identifier/literal scanning
  then layer SQL-specific token enrichment on top

---

## Acceptance Criteria

- All 5 parsers compile with 0 errors
- `RuleRegistry.Discover()` registers all via `ConfigEditor.RuleParserClasses`
- `SqlWhereParser` correctly tokenises the reference suite (see Phase 10 test vectors)
- `JsonRuleTreeParser` round-trips from JSON rule tree → AST → evaluate
- `FormulaParser` maps `IF`, `UPPER`, `TRIM`, `ROUND` → correct built-in rule calls
- Each parser has XML doc comments on the class and `ParseRule` method

---

## File Map

```
Rules/BuiltinParsers/
  CsvColumnParser.cs
  SqlWhereParser.cs
  JsonRuleTreeParser.cs
  FormulaParser.cs
  NfelParser.cs
```
