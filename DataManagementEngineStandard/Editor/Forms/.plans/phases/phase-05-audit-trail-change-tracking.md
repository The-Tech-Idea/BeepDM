# Phase 05 — Audit Trail & Change Tracking

**Status:** Complete (`15 / 15` in [todo-tracker.md](../todo-tracker.md))  
**Priority:** Medium  
**Depends on:** Phase 01 commit, record-change, and UoW tracking flows

---

## Objective

Capture enterprise-grade change history from field changes through commit, without divorcing audit records from the underlying UoW timeline.

## Primary Implementation Seams

- `FormsManager.Audit.cs`
- `CommitFormAsync` integration
- Audit manager / audit store abstractions
- Audit models and configuration

## Enhance / Update / Fix Rules

- Audit must observe the same record lifecycle the UoW sees. Do not build a separate shadow history that ignores rollback or refresh.
- Pending field changes should remain provisional until commit succeeds.
- Audit storage must be pluggable, but entry semantics must stay stable across stores.

## UoW and Primary-Key Rules

- Audit entries need a stable record identifier. For sequence-generated keys assigned before insert, audit can use the real PK immediately.
- For datasource-generated identity keys, provisional audit entries may need a correlation token until the real PK is available after insert/commit.
- `BeforeImage` and `AfterImage` should come from UoW-tracked values whenever possible, not from ad hoc reflection snapshots alone.
- Rollback must discard or mark uncommitted audit entries consistently with the UoW rollback result.
- Composite keys should be serialized in a deterministic field order so history lookup remains stable.

## Done / Verify Checklist

- Field-level changes enter pending audit state.
- Commit flushes pending changes only after save succeeds.
- Audit queries can retrieve by block, field, and record key.
- Identity-generated PKs are reconciled correctly in committed audit entries.

## Maintenance Notes

- If commit ordering changes, revalidate audit flush timing against detail-block synchronization and lock release.