# Phase 7 - Security, Governance, and Masking

## Objective
Add policy-driven security controls and governance lifecycle to Json ingestion and query operations.

## Scope
- Access policy and allowed-operation profiles.
- Sensitive-field masking for logs/diagnostics.
- Governance states and audit trail events.

## File Targets
- `Json/JsonDataSource.cs`
- `Json/JsonDataSourceAdvanced.cs`
- `Json/Helpers/JsonDataHelper.cs`

## Planned Enhancements
- Policy enforcement for paths, operations, and payload size.
- Masking/redaction hooks for selected fields and paths.
- Audit records for policy violations and state transitions.

## Acceptance Criteria
- Unauthorized operations are blocked deterministically.
- Sensitive values are masked consistently in diagnostics.
- Governance events are persisted and queryable.
