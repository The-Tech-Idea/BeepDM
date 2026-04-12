# Phase 06 — Security & Authorization

**Status:** Complete (`15 / 15` in [todo-tracker.md](../todo-tracker.md))  
**Priority:** Medium  
**Depends on:** Phase 01 CRUD flows and Phase 03 cross-form context

---

## Objective

Apply role-based access, row restrictions, and field protection without letting security rules bypass the UoW or corrupt record identity semantics.

## Primary Implementation Seams

- Security manager interfaces and models
- CRUD enforcement in FormsManager operations
- `ItemPropertyManager` integration for field visibility/editability
- Query restriction composition

## Enhance / Update / Fix Rules

- Security decisions belong at the FormsManager orchestration layer; persistence still flows through UoW operations.
- UI visibility and enabled-state should mirror security, but backend CRUD checks must still reject unauthorized operations.
- Security filters appended to queries must be deterministic and composable with block/default filters.

## UoW and Primary-Key Rules

- PK fields, especially identity and sequence-managed keys, should be non-editable by default unless a privileged override explicitly allows them.
- Security filters must still permit FormsManager to refresh a newly inserted record by its real PK after commit.
- Unauthorized callers must never be able to force PK reassignment through item defaults, triggers, or copied field values.
- Shared-block and multi-form security contexts must not let a second form mutate a UoW using stale privileges.
- Audit/security logs should record denied attempts against key fields distinctly from ordinary field edits.

## Done / Verify Checklist

- Block-level security is enforced on query/insert/update/delete.
- Field-level security updates item visibility, enabled state, and masking consistently.
- Security violations are logged and surfaced without mutating UoW state.
- PK and FK fields honor stricter edit rules than ordinary columns.

## Maintenance Notes

- If new key-generation strategies are added, update security defaults so managed key fields stay protected.