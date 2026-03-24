# Phase 3 - Expression Language and Formula Unification

## Objective
Create one expression engine contract across `ExpressionResolver` and `FormulaResolver` to support both readable DSL and deterministic evaluation.

Detailed language help and usage guide:
- `expression-formula-language-reference.md`

## Scope
- Unify arithmetic, logical, null-handling, and conditional operations.
- Support dot-style operations and function aliases.
- Add typed conversion and predictable coercion rules.

## File Targets
- `Defaults/Resolvers/ExpressionResolver.cs`
- `Defaults/Resolvers/FormulaResolver.cs`
- `Defaults/Resolvers/BaseDefaultValueResolver.cs`
- `Defaults/Helpers/DefaultValueValidationHelper.cs`

## Planned Enhancements
- Canonical operator set:
  - logical: `IF`, `CASE`, `AND`, `OR`, `NOT`
  - numeric: `ADD`, `SUB`, `MUL`, `DIV`, `ROUND`
  - null/value: `COALESCE`, `ISNULL`
  - comparison: `EQ`, `NE`, `GT`, `GTE`, `LT`, `LTE`
- Dot syntax examples:
  - `ADD.2.3`
  - `IF.GTE.Record.Age.18.Adult.Minor`
  - `COALESCE.Record.NickName.Record.FirstName.Unknown`
- Keep function aliases:
  - `ADD(2,3)` remains valid and maps to same internal operator.
- Define coercion rules:
  - string->number parse strategy,
  - null comparisons,
  - boolean truthy/falsy behavior.

## Implementation Rules (Skill Constraints)
- Keep expression evaluation wired through the existing resolver manager orchestration boundary in `IDMEEditor` workflows (`beepdm`).
- Preserve backward-compatible resolver API behavior and avoid breaking existing consumers of `ResolveValue` (`idatasource` contract discipline).
- Store any expression-engine settings or feature flags through `ConfigEditor`-managed configuration channels (`configeditor`).
- Avoid introducing environment-specific path logic; use `EnvironmentService` helpers if expression caches/config artifacts are persisted (`environmentservice`).
- Ensure expression behavior remains stable across shared editor lifecycles used in desktop/service bootstraps (`beepservice`).

## Acceptance Criteria
- Same expression yields same output across function and dot syntax.
- Formula and expression operators do not conflict in routing.
- Type coercion behavior is documented and tested.
- Error messages include operator and argument index.

## Risks and Mitigations
- Risk: existing formulas change behavior.
  - Mitigation: compatibility mode + rule-level opt-in to strict semantics.
- Risk: evaluator complexity grows.
  - Mitigation: modular operator handlers and shared helper library.

## Test Plan
- Equivalence tests (function syntax vs dot syntax).
- Cross-type coercion matrix tests.
- Property-based tests for arithmetic/boolean operator consistency.
