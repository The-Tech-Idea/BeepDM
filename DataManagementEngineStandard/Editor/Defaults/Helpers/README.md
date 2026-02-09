# Defaults Helpers

## Purpose
This folder contains operational helpers for loading, validating, and applying default-value definitions across data sources.

## Key Files
- `DefaultValueHelper.cs`: CRUD and lookup operations for default definitions.
- `DefaultValueValidationHelper.cs`: Validation of field names, rules, and default payload correctness.

## Runtime Flow
1. Load defaults for a data source (`GetDefaults`).
2. Resolve and apply defaults during insert/update operations.
3. Validate values and rules before persisting default definitions.

## Extension Guidelines
- Validate defaults before save to prevent invalid runtime rule execution.
- Preserve backward compatibility for persisted default schemas.
- Keep field-name validation aligned with entity metadata from `IDMEEditor`.
