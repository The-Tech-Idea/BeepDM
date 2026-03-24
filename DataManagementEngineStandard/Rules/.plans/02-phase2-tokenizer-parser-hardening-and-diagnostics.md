# Phase 2 - Tokenizer/Parser Hardening and Diagnostics

## Objective
Harden lexical and parse layers with deterministic diagnostics and better recovery for malformed rules.

## Scope
- Improve `Tokenizer` and parser behavior for edge cases.
- Add structured parse diagnostics (code, severity, span, suggestion).
- Ensure parser behavior remains deterministic across frameworks.

## File Targets
- `DataManagementEngineStandard/Rules/Tokenizer.cs`
- `DataManagementEngineStandard/Rules/RulesParser.cs`
- `DataManagementEngineStandard/Rules/RuleParserFactory.cs`
- `DataManagementModelsStandard/Rules/enums.cs`

## Planned Enhancements
- Tokenization error taxonomy (`UnknownToken`, `UnterminatedString`, `InvalidNumeric`, etc.).
- Parse result model with `Success`, `Diagnostics`, and partial AST.
- Standard precedence/associativity validation and mismatched parenthesis reporting.

## Acceptance Criteria
- Invalid expressions return structured diagnostics instead of opaque failures.
- Same input yields same token stream + diagnostic list deterministically.
- Regression suite covers operator precedence and parenthesis edge cases.
