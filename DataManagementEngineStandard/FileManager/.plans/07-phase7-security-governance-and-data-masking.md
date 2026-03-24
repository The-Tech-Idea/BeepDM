# Phase 7 - Security, Governance, and Data Masking

## Objective
Add policy-driven security controls and governance lifecycle for file ingestion.

## Scope
- Policy profiles for allowed paths, file patterns, and size limits.
- Sensitive-data masking/redaction at ingest and logs.
- Governance/audit trail for ingestion profile changes.

## File Targets
- `FileManager/CSVDataSource.cs`
- `FileManager/README.md`
- Security/governance integration touchpoints

## Planned Enhancements
- Path whitelist/denylist policy enforcement.
- PII masking hooks for preview/logging outputs.
- Audit events for profile updates and policy violations.

## Audited Hotspots
- `CSVDataSource.Openconnection(...)` path open/validation
- `CSVDataSource.ExportDataToCSV(...)` outbound data path
- `CSVDataSource` logging paths in query/write/transaction operations

## Real Constraints to Address
- File operations currently trust configured path/file names without policy-layer enforcement.
- Logs can contain raw values/errors without field-level masking hooks.
- No governance trail for delimiter/format/connection behavior changes.

## Acceptance Criteria
- Unauthorized paths/formats are blocked deterministically.
- Sensitive fields are masked in diagnostics/logs based on policy.
- Governance events are queryable for compliance review.
