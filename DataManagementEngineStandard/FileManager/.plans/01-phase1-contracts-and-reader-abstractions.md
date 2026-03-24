# Phase 1 - Contracts and Reader Abstractions

## Objective
Define stable ingestion contracts and capability-driven reader abstractions.

## Scope
- Extend reader interfaces with explicit lifecycle/capability metadata.
- Standardize parser options and datasource-reader handoff contracts.

## File Targets
- `FileManager/ICSVDataReader.cs`
- `FileManager/CSVDataSource.cs`
- `FileManager/README.md`

## Planned Enhancements
- Define explicit `ICSVDataReader` invariants for `GetName/GetOrdinal/GetValue` under projected columns.
- Add reader options contract (delimiter, quote mode, trim mode, malformed-row policy, encoding hints).
- Decouple parser/writer responsibility from `CsvTextFieldParser` by moving write utility out.

## Audited Hotspots
- `ICSVDataReader.cs` / `CSVDataReader.Read()`, `GetName(int)`, `GetOrdinal(string)`, `GetValue(int)`
- `CSVDataSource.GetDataReader(...)`
- `TextFieldParser.WriteEntityStructureToFile(...)`

## Real Constraints to Address
- `GetName(int)` can return names from entity field index instead of projected column index.
- `GetOrdinal(string)` can return `-1` for projection miss, then callers can fail later.
- Reader behavior for missing/unknown columns is currently implicit and inconsistent across methods.

## Acceptance Criteria
- Reader and datasource contracts are explicit and backward-compatible.
- Capability checks drive runtime decisions (no implicit assumptions).
- Unit tests verify lifecycle and option-binding behavior.
