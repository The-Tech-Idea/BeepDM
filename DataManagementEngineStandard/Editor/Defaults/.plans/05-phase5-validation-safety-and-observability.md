# Phase 5 - Validation, Safety, and Observability

## Objective
Raise runtime safety and diagnosability for default rule execution through stronger validation and telemetry.

## Scope
- Grammar-aware validation.
- Security checks for query and expression rules.
- Resolver execution telemetry and diagnostics.

## File Targets
- `Defaults/Helpers/DefaultValueValidationHelper.cs`
- `Defaults/DefaultsManager.cs`
- `Defaults/Resolvers/DefaultValueResolverManager.cs`
- `Defaults/README_DefaultsManager.md` and `Defaults/Examples/DefaultsManagerExamples.cs`

## Planned Enhancements
- Validation layers:
  - syntax validation (operator + arity),
  - semantic validation (context fields exist),
  - safety validation (query restrictions).
- Observability payload for each rule resolution:
  - resolver name,
  - rule fingerprint/version,
  - duration and outcome,
  - fallback used or not.
- Add troubleshooting guide:
  - common error codes,
  - suggested fix for each validation failure class.

## Implementation Rules (Skill Constraints)
- Keep validation outcomes aligned with BeepDM error-reporting patterns (`IErrorsInfo` / non-exception routine failures) per `idatasource` guidance.
- Emit diagnostics through established editor/logging paths and avoid side-channel telemetry bypassing `IDMEEditor` orchestration (`beepdm`).
- Persist validation policy and rule-quality settings through `ConfigEditor` ownership (`configeditor`).
- Use `EnvironmentService` for any local diagnostics/artifact path creation (`environmentservice`).
- Ensure validation and telemetry hooks are safe under shared editor initialization models (`beepservice`).

## Acceptance Criteria
- Validation clearly differentiates parse, semantic, and runtime failures.
- Unsafe query rules are rejected before execution.
- Logs include enough context to diagnose failures without exposing secrets.

## Risks and Mitigations
- Risk: validation too strict for legacy rules.
  - Mitigation: warning mode before enforce mode.
- Risk: verbose logging leaks sensitive inputs.
  - Mitigation: value redaction policy in diagnostics.

## Test Plan
- Negative tests for invalid/unsafe rules.
- Log redaction tests.
- Regression tests for existing examples and templates.
