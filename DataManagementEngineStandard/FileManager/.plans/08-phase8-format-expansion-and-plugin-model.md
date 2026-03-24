# Phase 8 - Format Expansion and Plugin Model

## Objective
Evolve FileManager beyond CSV by introducing plugin-driven format adapters.

## Scope
- Adapter model for TSV, fixed-width, JSONL, and future formats.
- Shared ingestion contracts and capability negotiation.
- Backward compatibility for existing CSV workflows.

## File Targets
- `FileManager/ICSVDataReader.cs` (or generalized reader contract)
- `FileManager/CSVDataSource.cs`
- `FileManager/README.md`

## Planned Enhancements
- Introduce generic `IFileDataReader` style abstraction.
- Register adapters by format and capability metadata.
- Reuse validation/diagnostic pipeline across adapters.

## Audited Hotspots
- `CSVDataSource` monolithic responsibilities (connection, schema inference, query, mutation, transactions)
- `TextFieldParser` parser + writer mixed responsibilities
- `CSVTypeMapper` reusable conversion surface

## Real Constraints to Address
- Current code couples CSV concerns tightly to datasource orchestration.
- Adapter extensibility is blocked by missing neutral contracts and shared option model.
- Parser utility methods are not isolated for reuse by non-CSV adapters.

## Acceptance Criteria
- New format adapters can be added without core datasource rewrites.
- CSV behavior remains stable under compatibility tests.
- Adapter selection is deterministic and observable.
