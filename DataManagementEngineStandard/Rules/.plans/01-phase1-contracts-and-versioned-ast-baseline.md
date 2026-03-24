# Phase 1 - Contracts and Versioned AST Baseline

## Objective
Stabilize `Rules` contracts and introduce a versioned AST/token model baseline shared between engine and models.

## Scope
- Standardize interfaces in `DataManagementModelsStandard/Rules`.
- Introduce version metadata for `RuleStructure`, `Token`, and parser outputs.
- Define forward/backward compatibility rules for serialized rule payloads.

## File Targets
- `DataManagementModelsStandard/Rules/IRule*.cs`
- `DataManagementModelsStandard/Rules/RuleStructure.cs`
- `DataManagementModelsStandard/Rules/Token.cs`
- `DataManagementEngineStandard/Rules/RuleParserFactory.cs`

## Planned Enhancements
- Add immutable/controlled construction patterns for parsed structures.
- Introduce `SchemaVersion` and compatibility checks.
- Add contract test fixtures for all public interfaces.

## Acceptance Criteria
- Engine and models compile with aligned contract surface.
- Rule payloads include version marker and pass compatibility checks.
- Contract tests cover all interface methods and required properties.
