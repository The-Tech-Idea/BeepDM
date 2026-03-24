# Rules Language Reference (Baseline)

## Purpose
Define canonical syntax and semantics for rule expressions consumed by parser and runtime engine.

## Core Grammar (Simplified)
- Literals: numeric, string, boolean, null.
- Identifiers: parameter or field references.
- Operators:
  - Arithmetic: `+ - * /`
  - Comparison: `= != > >= < <=`
  - Logical: `AND OR NOT`
- Grouping: `( ... )`
- Rule reference: named rule lookup (engine-specific binding).

## Evaluation Rules
- Arithmetic precedence over comparison; comparison over logical.
- Left-associative operators unless explicitly defined otherwise.
- Null handling must follow explicit coercion table (Phase 3).

## Diagnostics Contract
- Every parser/runtime failure should expose:
  - `Code`
  - `Message`
  - `Start/Length` (token span when available)
  - `Severity` (`Info`, `Warning`, `Error`)
  - optional `Hint`

## Compatibility Notes
- New operators require:
  - tokenizer support
  - parser support
  - runtime evaluator support
  - test vectors in compatibility suite
