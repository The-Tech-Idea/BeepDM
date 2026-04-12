# Phase 04 — Advanced Trigger System

**Status:** Complete (`20 / 20` in [todo-tracker.md](../todo-tracker.md))  
**Priority:** Medium-High  
**Depends on:** Phase 01 core flows and Phase 02 built-ins

---

## Objective

Finish Oracle Forms trigger parity and make trigger execution safe enough for CRUD, navigation, and programmatic DML overrides.

## Primary Implementation Seams

- `FormsManager.KeyTriggers.cs`
- `FormsManager.DmlTriggers.cs`
- Trigger chaining / dependency services
- `Helpers/TriggerLibrary.cs`
- Trigger execution log and dependency manager

## Enhance / Update / Fix Rules

- Triggers may orchestrate behavior, but they must not bypass UoW persistence, rollback, or change tracking.
- Key triggers should layer on top of normal default actions, not duplicate them in ad hoc code.
- DML triggers that replace default persistence must document whether they fully handle insert/update/delete or allow fallback.
- Trigger chaining must be observable, timeout-aware, and cycle-safe.

## UoW and Primary-Key Rules

- `AutoNumberTrigger` is valid only for non-identity fields. If the datasource owns the identity value, the trigger must skip PK assignment.
- Pre-insert triggers may assign sequence or GUID keys before the UoW insert runs, but they must not consume sequence values during validation-only passes.
- If an `ON-INSERT` trigger fully replaces default DML, it must either call `IUnitofWork.InsertAsync(...)` itself or guarantee the record is persisted and the generated PK is written back before post-insert logic executes.
- Trigger failure or timeout must not leave the UoW with partially staged PK state that later commit reuses incorrectly.
- Trigger libraries should treat composite keys as explicit multi-field assignments, not single-field auto-numbering.

## Trigger Templates with PK Impact

- `AutoNumberTrigger`: pre-insert, numeric, non-identity only.
- `AuditStampTriggers`: should not overwrite PK fields.
- `CascadeDeleteTrigger`: must use stable PK/FK values already materialized by insert/commit.
- Custom DML triggers: must document whether PK generation is trigger-owned or UoW-owned.

## Done / Verify Checklist

- Key triggers route through default navigation/commit actions safely.
- DML triggers can override persistence without breaking UoW dirty tracking.
- Trigger chaining logs execution order and failure modes.
- Auto-number logic respects identity vs sequence vs explicit keys.

## Maintenance Notes

- Any change to trigger timing around create/insert/delete requires rechecking Phase 01 and Phase 05 audit capture behavior.