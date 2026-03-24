# Phase 9 - Integration with ETL, Mapping, and Rules

## Objective
Formalize integration contracts so FileManager works consistently with ETL, Mapping, and Rules systems.

## Scope
- Data handoff contracts for ETL pipelines.
- Schema/mapping alignment for MappingManager.
- Rule evaluation hooks for quality and transforms.

## File Targets
- `FileManager/CSVDataSource.cs`
- `Editor/ETL/*` integration touchpoints
- `Editor/Mapping/*` integration touchpoints
- `Rules/*` integration touchpoints

## Planned Enhancements
- Standard row envelope/context for downstream consumers.
- Mapping-ready schema and quality metadata export.
- Rule-driven pre/post-parse hooks and validation hooks.

## Audited Hotspots
- `CSVDataSource.GetEntity(...)` materialization paths (typed object vs dictionary fallback)
- `CSVDataSource.ValidateRow(...)` and inferred schema output coupling
- `CSVAnalyser.CSVAnalysisResult` metadata payload

## Real Constraints to Address
- Integration outputs are not normalized (mixed shape: runtime types or dictionaries).
- Validation diagnostics are not standardized for ETL/Rules consumption.
- Schema inference and runtime projection lack a shared envelope contract.

## Acceptance Criteria
- ETL/Mapping/Rules consume FileManager outputs via stable contracts.
- Integration tests validate end-to-end ingest -> map -> transform flows.
- Failures are correlated across module diagnostics.
