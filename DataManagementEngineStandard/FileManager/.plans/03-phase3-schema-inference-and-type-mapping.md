# Phase 3 - Schema Inference and Type Mapping

## Objective
Improve schema/type inference quality and provide confidence-based mapping decisions.

## Scope
- Column type inference and nullability decisions.
- Mixed-type column handling and fallback policies.
- Drift detection support for recurring files.

## File Targets
- `FileManager/CSVAnalyser.cs`
- `FileManager/CSVTypeMapper.cs`
- `FileManager/CSVDataSource.cs`

## Planned Enhancements
- Replace heuristic-only inference with confidence-scored inference and sample-window controls.
- Centralize conversion behavior through `CSVTypeMapper` (including culture and enum safety).
- Add deterministic schema signature snapshots for drift detection and alerting.

## Audited Hotspots
- `CSVAnalyser.AnalyzeCSVFile(...)` and `DetectDataType(...)`
- `CSVDataSource.GetFieldsbyTableScan(...)`
- `CSVTypeMapper.ConvertValue(...)`

## Real Constraints to Address
- `GetFieldsbyTableScan` uses fragile loop logic and broad catch blocks that hide bad inferences.
- Analyzer uses broad `TryParse` without explicit culture/number styles.
- Enum conversion in `ConvertValue` can throw and bypass fallback behavior.

## Acceptance Criteria
- Inference output includes confidence and rationale.
- Type mapping is deterministic across repeated runs.
- Drift between file versions is detectable and reportable.
