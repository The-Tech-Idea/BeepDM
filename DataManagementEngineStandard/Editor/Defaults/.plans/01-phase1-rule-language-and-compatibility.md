# Phase 1 - Rule Language and Compatibility

## Objective
Introduce a unified rule language contract with support for a simple dot-style syntax while preserving backward compatibility.

## Scope
- Define supported forms:
  - Legacy function style: `IF(Age >= 18, 'Adult', 'Minor')`
  - New simple style: `IF.Age>=18.Adult.Minor`
  - Query style: `QUERY.scalar.SELECT COUNT(*) FROM Orders WHERE CustomerId=@CustomerId`
- Rule parsing and normalization pipeline.
- Compatibility mode and versioned rule metadata.

## File Targets
- `Defaults/Interfaces/IDefaultValueInterfaces.cs`
- `Defaults/Resolvers/DefaultValueResolverManager.cs`
- `Defaults/Helpers/DefaultValueValidationHelper.cs`
- `Defaults/DefaultsManager.cs`

## Planned Enhancements
- Add `RuleSyntaxVersion` and optional `RuleOptions` metadata on defaults (non-breaking extension approach).
- Add a normalization layer:
  - parse dot-style tokens,
  - transform to canonical internal AST-like representation,
  - route to resolver.
- Add first-class diagnostics: parse errors, unsupported operator, ambiguous token.

## DSL v1 Draft
- Dot syntax general form:
  - `Operator.arg1.arg2...`
- Examples:
  - `NOW.utc`
  - `ADD.10.5`
  - `COALESCE.Record.NickName.Record.FirstName.Unknown`
  - `QUERY.scalar.Orders.Count.CustomerId=@CustomerId`

## Implementation Rules (Skill Constraints)
- Use `IDMEEditor` as the orchestration boundary (`beepdm`); do not introduce parallel config/runtime entry points.
- Keep persisted rule metadata changes routed through `ConfigEditor` patterns (`configeditor`), not ad-hoc file writes.
- Preserve existing resolver contract behavior (`idatasource` compatibility mindset): avoid breaking existing rule resolution semantics.
- If this phase needs app-folder persistence changes, use `EnvironmentService` path utilities (`environmentservice`) instead of manual paths.
- Ensure startup/bootstrap assumptions remain compatible with shared-editor patterns (`beepservice`), avoiding per-form/per-call editor recreation.

## Acceptance Criteria
- Legacy rules execute unchanged.
- Dot-style rules parse and resolve for initial operators.
- Validation identifies invalid token count and unknown operators.
- Resolver selection remains deterministic.

## Risks and Mitigations
- Risk: dot separator collides with decimal numbers or property paths.
  - Mitigation: escaping/quoting rules and parser precedence definition.
- Risk: syntax fragmentation.
  - Mitigation: canonicalization to one internal representation.

## Test Plan
- Parser unit tests for legacy + dot syntax.
- Compatibility tests with existing examples from all built-in resolvers.
- Error-message quality tests for malformed rules.
