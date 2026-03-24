# Expression and Formula Language Reference

## Purpose
This document is the detailed help/reference for the DefaultsManager expression and formula language used by:
- `Defaults/Resolvers/ExpressionResolver.cs`
- `Defaults/Resolvers/FormulaResolver.cs`

It defines supported syntax, operator behavior, type coercion, rule validation, and migration from legacy formats.

## Language Modes

DefaultsManager supports two compatible input styles:

1. Function style (legacy compatible)
- `IF(Age >= 18, 'Adult', 'Minor')`
- `ADD(2, 3)`
- `COALESCE(NickName, FirstName, 'Unknown')`

2. Dot DSL style (new)
- `IF.GTE.Record.Age.18.Adult.Minor`
- `ADD.2.3`
- `COALESCE.Record.NickName.Record.FirstName.Unknown`

Both forms should normalize to one internal operator model.

## Rule Structure

### Function Style
- Pattern: `OP(arg1, arg2, ...)`
- Commas separate arguments
- Strings can be quoted: `'Value'` or `"Value"`

### Dot DSL Style
- Pattern: `OP.arg1.arg2.arg3`
- Dot (`.`) separates tokens
- Use quoting when token contains separators/special text:
  - `COALESCE.'First.Name'.'Display Name'.Unknown`

## Operator Catalog

## Logical and Conditional
- `IF(condition, trueValue, falseValue?)`
- `CASE(testValue, when1, then1, when2, then2, default?)`
- `AND(left, right)`
- `OR(left, right)`
- `NOT(value)`

Dot examples:
- `IF.GT.Record.Score.70.Pass.Fail`
- `CASE.Record.Status.Active.Yes.Inactive.No.Unknown`

## Comparison
- `EQ`, `NE`, `GT`, `GTE`, `LT`, `LTE`

Function examples:
- `EQ(Status, 'Active')`
- `GTE(Age, 18)`

Dot examples:
- `EQ.Record.Status.Active`
- `LTE.Record.Quantity.10`

## Numeric and Formula
- `ADD`, `SUB`, `MUL`, `DIV`
- `ROUND(value, decimals?)`
- `ABS(value)`, `SQRT(value)` (via formula/math support)

Examples:
- `ADD(10, 5)`
- `DIV(Total, Count)`
- `ROUND(Amount, 2)`
- `MUL.Record.UnitPrice.Record.Quantity`

## Null and Value Selection
- `ISNULL(value, defaultValue)`
- `COALESCE(v1, v2, ..., fallback)`

Examples:
- `ISNULL(MiddleName, '')`
- `COALESCE(NickName, FirstName, 'Unknown')`
- `COALESCE.Record.NickName.Record.FirstName.Unknown`

## Query and Lookup-Related (cross-reference Phase 2)
- `QUERY.*` forms and datasource defaults are defined in:
  - `02-phase2-query-defaults-and-datasource-resolver.md`

## Context Access Rules

Resolvers should resolve values from runtime context in this order:
1. Explicit literals in rule
2. Record/Object fields from `IPassedArgs` context (`Record`, `Object`)
3. Named parameters in context
4. Fallback defaults (if operator supports fallback)

Recommended field tokens:
- `Record.<FieldName>`
- `Object.<PropertyName>`

Examples:
- `ADD.Record.SubTotal.Record.Tax`
- `IF.EQ.Record.Country.US.Domestic.International`

## Type Coercion Rules (Deterministic)

- Numeric operations:
  - Attempt parse as `double` using invariant culture.
  - Unparseable numeric token -> validation error in strict mode, `0` in compatibility mode.
- Boolean operations:
  - Accept `true/false` (case-insensitive), numeric truthiness (`0=false`, non-zero=true), and non-empty strings as true in compatibility mode.
- String comparison:
  - Case-insensitive by default unless strict mode requests case-sensitive comparison.
- Null handling:
  - `EQ(null, null) = true`
  - `NE(null, value) = true` for non-null `value`

## Escaping and Quoting

Use quotes if an argument contains:
- dots in dot DSL
- commas in function style
- spaces or reserved keywords

Examples:
- Function: `IF(EQ(Status, 'On Hold'), 'Paused', 'Active')`
- Dot DSL: `EQ.'On.Hold'.'On.Hold'`

## Validation Rules

A rule should fail validation when:
- Operator is unknown
- Arity is wrong (missing required args)
- Dot tokens are malformed
- Query expression violates safety policy (Phase 2)
- Type constraints are impossible in strict mode

Validation output should include:
- `Operator`
- `ArgumentIndex` (if applicable)
- `ErrorCode`
- `SuggestedFix`

## Compatibility and Strict Modes

## Compatibility Mode (default for migration)
- Preserves legacy behavior where possible.
- Lenient coercion rules.
- Best-effort evaluation with fallback behavior.

## Strict Mode (target)
- Enforces arity/type rules.
- Rejects ambiguous coercion.
- Produces deterministic failures with explicit diagnostics.

## Migration Guide (Legacy -> Canonical)

### Common Translations
- `IF(Age >= 18, 'Adult', 'Minor')`
  -> `IF.GTE.Record.Age.18.Adult.Minor`

- `COALESCE(NickName, FirstName, 'Unknown')`
  -> `COALESCE.Record.NickName.Record.FirstName.Unknown`

- `ADD(2, 3)`
  -> `ADD.2.3`

### Migration Steps
1. Parse and classify existing rule.
2. Normalize into canonical operator form.
3. Re-validate under compatibility mode.
4. Compare result parity on sample records.
5. Promote to strict mode once parity is verified.

## Troubleshooting

## Symptom: Rule returns null unexpectedly
- Check operator arity and field token paths.
- Verify context contains expected `Record`/`Object`.
- Validate fallback argument is supplied for null-prone operations.

## Symptom: Numeric operations always return 0
- Check string-to-number parsing in source values.
- Verify decimal formatting/culture mismatch.
- Enable strict validation to catch coercion issues early.

## Symptom: Dot DSL token split errors
- Add quotes around dotted literals or names with spaces.
- Confirm no trailing dot or empty token.

## Recommended Test Cases
- Function vs dot DSL equivalence tests.
- Null matrix tests for `ISNULL`/`COALESCE`.
- Comparison tests across string, numeric, and null values.
- Compatibility-mode vs strict-mode behavior snapshots.

## Implementation Constraints (Skill-Aligned)
- Keep resolution inside `IDMEEditor` orchestration boundaries (`beepdm`).
- Preserve resolver contract stability and `IDataSource`-friendly error handling (`idatasource`).
- Persist syntax/feature flags through `ConfigEditor` (`configeditor`).
- Use `EnvironmentService` for persisted rule artifacts/cache paths (`environmentservice`).
- Keep behavior stable in shared editor/service boot patterns (`beepservice`).
