# Phase 5 - Quality Validation and Error Handling

## Objective
Provide enterprise-grade data-quality validation and structured error handling.

## Scope
- Row-level and column-level validation hooks.
- Structured diagnostics with severity and location.
- Error store/replay support.

## File Targets
- `FileManager/CSVDataSource.cs`
- `FileManager/CSVAnalyser.cs`
- `FileManager/TextFieldParser.cs`

## Planned Enhancements
- Wire a mandatory validation stage into write/query mutation paths (insert/update/bulk/import).
- Replace swallow-catch patterns with structured diagnostics (`Code`, `Severity`, `Row`, `Column`, `Operation`).
- Persist failed-row artifacts with replay context and deterministic reason codes.

## Audited Hotspots
- `CSVDataSource.ValidateRow(...)`
- `CSVDataSource.UpdateEntity(...)`, `DeleteEntity(...)`, `InsertEntity(...)`, `BulkInsert(...)`
- `CSVAnalyser.AnalyzeCSVFile(...)` row parse error handling
- `CSVDataSource.GetDataTable(...)` conversion failures

## Real Constraints to Address
- Many catch blocks ignore exceptions and continue silently.
- `ValidateRow` exists but is not consistently enforced by all mutations.
- Error outputs are mostly logs; machine-readable diagnostics are missing.

## Acceptance Criteria
- Validation failures are machine-readable and actionable.
- Error handling distinguishes hard failures vs soft warnings.
- Replay artifacts are produced for failed batches.
