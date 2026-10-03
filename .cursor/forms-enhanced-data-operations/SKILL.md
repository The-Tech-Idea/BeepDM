---
name: forms-enhanced-data-operations
description: Detailed guidance for FormsManager enhanced CRUD and query operations in BeepDM. Use when implementing CreateNewRecord, InsertRecordEnhancedAsync, UpdateCurrentRecordAsync, or ExecuteQueryEnhancedAsync with validation, relationship sync, and audit/default handling.
---

# Forms Enhanced Data Operations

Use this skill when operating on data through `FormsManager.EnhancedOperations`.

## File Locations
- `DataManagementEngineStandard/Editor/Forms/FormsManager.EnhancedOperations.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.QueryExecution.cs`
- `DataManagementEngineStandard/Editor/Forms/Helpers/FormsSimulationHelper.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.Relationships.cs`

## Core APIs
- `CreateNewRecord(...)`
- `InsertRecordEnhancedAsync(...)`
- `UpdateCurrentRecordAsync(...)`
- `ExecuteQueryEnhancedAsync(...)`
- `ExecuteQueryWithOutcomeAsync(..., cancellationToken)`
- `GetCurrentRecord(...)`
- `GetRecordCount(...)`
- `CopyFields(...)`
- `ApplyAuditDefaults(...)`

## Working Rules
1. Prefer enhanced methods over ad-hoc reflection in callers.
2. Let enhanced query execution own mode transition and policy compilation; do not pre-clear buffers or merge a mandatory policy twice.
3. Preserve validation and relationship synchronization after successful DML.
4. Treat warnings distinctly from failures after query execution.

## Managed Read Boundary

Read `DataManagementEngineStandard/Editor/Forms/QUERY-POLICY.md` and
`FormsManager.ManagedReadPolicy.cs` for query/count/detail/aggregate changes.
The same boundary applies even when enhanced execution starts in Query mode.
Caller/default/security filters are ANDed once; unsupported grammar, unknown
fields and unresolved parameters must fail before provider execution.

Count uses the optional parameterized scalar capability or a closed dialect
fallback and returns -1 for invalid/fractional/overflow/stale results. Source
aggregates accept a declared-field aggregate, not caller SQL. Do not put raw
filter text into scalar SQL or use the permissive legacy parser for authorization.

Default datasource UoWs/wrappers stage basic/enhanced/detail reads. Implicit queries
retain records/cursor/mode until acceptance; explicit ENTER_QUERY still clears and
fires its trigger. Newer same-registration queries supersede unpublished older ones;
query/detail reads serialize and awaited nested reads reject. Prefer the typed token
API; after-admission cancellation returns an outcome after physical acknowledgement.
RecordsPublished and NotificationFailures distinguish accepted rows from observers.
Legacy UoWs can self-publish/pre-clear. Read `READ-PUBLICATION.md` for capability,
identity and compatibility rules. `RECORD-TARGETS.md` separately guards manager
validation/LOV/editor annotations and writes. Typed editor outcomes distinguish
provider OK from accepted setters and enforce popup editability/raw-disclosure.
Auxiliary lookups/caches, broad scheduling and masking outside popups remain open.
For opt-in presenter writes/focus read `UI-BINDING-CONTRACTS.md`: captured binding
targets and revisioned field policy reject stale work. Do not replace durable commit
outcomes with failed UI-notification state or duplicate an adapter's legacy edit bridge.

## Related Skills
- [`forms`](../forms/SKILL.md)
- [`forms-mode-transitions`](../forms-mode-transitions/SKILL.md)
- [`forms-helper-managers`](../forms-helper-managers/SKILL.md)

## Detailed Reference
Read `PROVIDER-PAGING.md` for FetchPageWithOutcomeAsync and its optional bounded
provider/stage contracts. Require RecordsPublished before treating ProviderPage
count evidence as an accepted UI page. Dirty/stale/policy failures retain prior
rows; never fall back to unbounded Get or replay accepted observer failures.
Provider prefetch/cache remains unimplemented.

Read `PERMISSION-PROJECTION.md` for configured versus effective permissions,
rule removal, registry/revision-gated default projection and injected-helper limits.
Setters configure permissions; getters include policy. Clearing rules neither lifts
authored restrictions nor authorizes old rows. Keep callbacks outside ownership locks.

For cached UI rows, read `BUFFER-AUTHORIZATION.md` in the checkout. Fresh binding
requires an accepted managed read after principal/block-policy changes; clearing or
re-registering rows is not authorization. Field-only changes permit fresh remasking.
Read `POLICY-REPAINT.md` for optional notifications, queued clearing/remasking and
explicit reconciliation after UI failures. Immediate privacy requires hiding before
principal switch; drain UI work and inspect outcomes. Native adapters remain unqualified.

Use [`reference.md`](./reference.md) for flow examples, pitfalls, and verification checks.
