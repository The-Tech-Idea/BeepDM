# Phase 3 - Conversion and Transform Pipeline

## Objective
Introduce enterprise-grade type conversion and transform chaining for robust field-level mapping.

## Scope
- Conversion policies, null semantics, and transformation steps.
- Custom transform registration and execution order.

## File Targets
- `Mapping/MappingManager.cs`
- `Mapping/Helpers/*` (existing and new helper modules)

## Planned Enhancements
- Typed converters (string->numeric/date/enum/guid, numeric scaling, culture-aware parsing).
- Transformation chain per field:
  - trim
  - normalize casing
  - regex replace
  - format parse/emit
  - custom resolver delegate
- Null-handling modes:
  - preserve null
  - substitute default
  - skip assignment
- Conversion policy profiles per destination datasource/entity.

## Acceptance Criteria
- Per-field conversion/transform chain is configurable and deterministic.
- Conversion failures are reportable and policy-driven (warn/reject/fallback).
- Existing simple conversion path remains available for backward compatibility.
