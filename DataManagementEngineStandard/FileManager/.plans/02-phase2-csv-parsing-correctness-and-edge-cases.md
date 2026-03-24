# Phase 2 - CSV Parsing Correctness and Edge Cases

## Objective
Harden parser behavior for real-world CSV/text edge cases while preserving deterministic outputs.

## Scope
- Quote escaping, multiline fields, delimiter collisions.
- BOM and encoding edge cases.
- Strict vs lenient parse modes.

## File Targets
- `FileManager/TextFieldParser.cs`
- `FileManager/CSVDataSource.cs`
- `FileManager/CSVAnalyser.cs`

## Planned Enhancements
- Introduce strict/lenient parser profiles and remove ad-hoc parsing (`Split`) from non-parser paths.
- Emit row/column-aware parser diagnostics (`LineNumber`, `ErrorLine`, parser op).
- Unify malformed-row handling strategy for query, paging, and write workflows.

## Audited Hotspots
- `TextFieldParser.ParseFieldAfterOpeningQuote(...)`
- `CSVDataSource.ValidateCSVHeaders(...)`
- `CSVDataSource.GetEntity(...)` and paged `GetEntity(...)` malformed row handling
- `CSVAnalyser.AnalyzeCSVFile(...)` quoting issue detection

## Real Constraints to Address
- Header validation currently uses simple split and can break on quoted delimiters.
- Query paths silently skip malformed rows without structured reporting.
- Analyzer quote checks run on already parsed field values, reducing signal quality.

## Acceptance Criteria
- Regression suite covers enterprise CSV edge cases.
- Strict mode fails predictably with precise diagnostics.
- Lenient mode recovers with clear warning semantics.
