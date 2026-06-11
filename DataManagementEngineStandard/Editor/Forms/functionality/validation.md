# FormsManager — Validation

This document covers the validation system: when it runs, what rules are available, how to write custom rules, and how it composes with triggers and cross-block rules.

## Validation timing

The engine implements three validation timings, matching Oracle Forms:

| Timing | When it fires | Use case |
| --- | --- | --- |
| `OnChange` | Per-field, when the field's value changes | Field-level rules (required, range, length, regex). |
| `OnValidate` | On record-level validation (explicit or before commit) | Record-level rules (cross-field comparisons). |
| `PreCommit` | Before the commit is finalized | Form-level rules (cross-record, cross-block). |

## Built-in rules (`ValidationRuleLibrary`)

The `ValidationRuleLibrary` provides the standard Oracle Forms built-in validations:

| Rule | Args | What it does |
| --- | --- | --- |
| `Required` | `(fieldName, message?)` | Field must not be null or empty. |
| `Range` | `(fieldName, min, max, message?)` | Numeric value must be in range. |
| `Length` | `(fieldName, min, max, message?)` | String length must be in range. |
| `Regex` | `(fieldName, pattern, message?)` | String must match the regex. |
| `CompareField` | `(fieldName, otherField, operator, message?)` | Field must compare to another field with `==`, `!=`, `>`, `<`, etc. |
| `Lookup` | `(fieldName, lovName, message?)` | Value must exist in the named LOV. |

The library is the **first-class** validation surface. Custom rules can be added via `ValidationRuleBuilder` or by implementing `ValidationRule` directly.

## How to add a rule

```csharp
// Built-in rule, fluent syntax
manager.Validation.Rules.Add(
    ValidationRule.Required("OrderDate"));

manager.Validation.Rules.Add(
    ValidationRule.Range("Quantity", min: 1, max: 9999));

manager.Validation.Rules.Add(
    ValidationRule.Regex("Email", @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$"));

// Cross-field rule
manager.Validation.Rules.Add(
    ValidationRule.CompareField(
        field: "EndDate",
        otherField: "StartDate",
        operator: ">",
        message: "EndDate must be after StartDate"));

// Custom rule — implement ValidationRule
public class UniqueOrderNumberRule : ValidationRule
{
    public override ValidationResult Validate(ValidationContext ctx)
    {
        if (ctx.Record == null) return ValidationResult.Skip();
        var orderNumber = ctx.GetValue("OrderNumber") as string;
        // ... check uniqueness against the current record set ...
        if (duplicate)
            return ValidationResult.Fail("Order number must be unique", ValidationSeverity.Error);
        return ValidationResult.Ok();
    }
}

manager.Validation.Rules.Add(new UniqueOrderNumberRule());
```

## `ValidationContext`

When a rule is invoked, it receives a `ValidationContext` with:

- The current record (the `object` being validated).
- The current block name.
- The timing (`OnChange` / `OnValidate` / `PreCommit`).
- The field name (for `OnChange` rules).
- Helpers: `ctx.GetValue(fieldName)`, `ctx.GetInt(fieldName)`, `ctx.GetString(fieldName)`, etc.

The context is constructed per-rule-invocation, so rules can be stateless or capture context-scoped state safely.

## `ValidationResult`

Every rule returns a `ValidationResult`:

- `Ok()` — validation passed.
- `Skip()` — rule doesn't apply in this context (e.g. an `OnChange` rule that needs a record but received a query record).
- `Fail(message, severity, fieldName?)` — validation failed. `severity` is `Error` / `Warning` / `Info`.

The engine aggregates rule results. **Any Error-severity failure blocks the commit.** Warning-severity failures are reported but don't block. Info-severity failures are silent.

## When each method triggers validation

| Method | Validation timing | Notes |
| --- | --- | --- |
| `ValidateField(blockName, fieldName, value)` | `OnChange` (manual) | Single-field validation, no record context. |
| `ValidateBlock(blockName)` | `OnValidate` (per record) | All records in the block. |
| `ValidateForm()` | `OnValidate` per record + `PreCommit` cross-block | All blocks. |
| `CommitFormAsync()` | `OnValidate` (if `Configuration.ValidateBeforeCommit` is true) + cross-block + `PreCommit` | All blocks. |
| `CommitFormBatchAsync` / `CommitBlockBatchAsync` | same as above | |
| Field change (via UoW event) | `OnChange` | Per-field, automatic. |
| `OnFormValidate` event | n/a | UI-level validation hook. |

## The `ValidationManager` events

Three events for telemetry / UI integration:

- `ValidationStarting` — raised before a validation pass starts. Args include the block name, timing, and record count.
- `ValidationCompleted` — raised after a validation pass completes. Args include the result count by severity.
- `ValidationFailed` — raised on each individual rule failure. Args include the field, the message, the severity.

A UI host can use `ValidationFailed` to highlight invalid items in the canvas.

## Cross-block validation

`CrossBlockValidationManager` lets you define rules that span multiple blocks. The engine has both built-in cross-block rules (used by `ValidateAllBlocksForModeTransitionAsync` and `CommitFormAsync`) and user-defined rules.

User-defined cross-block rules:

```csharp
manager.RegisterCrossBlockRule(new CrossBlockValidationRule
{
    Name = "OrderTotal matches order items",
    BlockNames = { "ORDERS", "ORDER_ITEMS" },
    Validate = (blockGetter) =>
    {
        var order = blockGetter("ORDERS").CurrentRecord;
        var items = blockGetter("ORDER_ITEMS").Records;
        if (items.Sum(i => i.LineTotal) != order.Total)
            return new[] { "Order total does not match sum of items" };
        return Array.Empty<string>();
    }
});
```

`ValidateCrossBlock()` returns the list of error messages (empty if all rules passed).

## Validation severity

Three levels:

| Severity | Blocks commit? | Reported to UI? | Logged? |
| --- | --- | --- | --- |
| `Error` | yes | yes | yes |
| `Warning` | no | yes | yes |
| `Info` | no | no | yes |

## Validation flow (worked example: `CommitFormAsync`)

1. Fire `OnFormCommit` (cancellable).
2. If `Configuration.ValidateBeforeCommit`, call `ValidateForm()`.
3. If cross-block validation is configured, call `_crossBlockValidation.Validate()`.
4. If any Error-severity failures, return `IErrorsInfo { Flag = Failed }` with the messages.
5. Otherwise, commit each block's dirty state.
6. Fire `OnPreCommit` / `OnPostCommit` per block.

## Cross-block rule execution

`CrossBlockValidationManager.Validate()` returns a list of `CrossBlockValidationFailure` objects (with rule name, block name, severity, message). `_crossBlockValidation.HasErrorSeverityFailures(failures)` is the check used by the orchestrator to decide whether to block commit.

## Built-in rules vs. custom rules

The `ValidationRuleLibrary` is **open for extension** — add your own rule types by implementing `ValidationRule` or using `ValidationRuleBuilder`. The engine does not have a closed enum of rule types.

## Notes for callers

- The `OnChange` timing is **automatic** — every field change triggers validation. If you need to disable this for performance, set `manager.Configuration.ValidateOnChange = false`.
- Custom rules must be **thread-safe** — they may be invoked from any thread. Avoid mutable state; if you need it, use `ConcurrentDictionary` or per-record state.
- The `ValidationContext.Record` is the **typed record** if available, otherwise an `IDictionary<string, object>`. Use the typed accessors (`ctx.GetInt(fieldName)`) where possible.
- The validation result aggregation is **strict** — any Error-severity failure blocks the commit. To allow commit-with-warnings, set `Configuration.TreatWarningsAsErrors = false` (default).
- The `ValidationStarting` / `ValidationCompleted` events can be heavy under fast typing — they're raised on every field change. Subscribe only if you need the telemetry.

## See also

- [`triggers.md`](triggers.md) — `WHEN-VALIDATE-ITEM` / `WHEN-VALIDATE-RECORD` triggers that fire on validation.
- [`audit.md`](audit.md) — the persisted audit trail.
- [`ORACLE-FORMS-MAPPING.md`](../ORACLE-FORMS-MAPPING.md) section 8 — the validation mapping.
