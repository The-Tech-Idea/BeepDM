# Phase 03 — Multi-Form & Cross-Form Communication

**Status:** Complete (`20 / 20` in [todo-tracker.md](../todo-tracker.md))  
**Priority:** High  
**Depends on:** Phase 01 core block/UoW registration

---

## Objective

Support Oracle Forms-style multi-form flows (`CALL_FORM`, `OPEN_FORM`, `NEW_FORM`) while keeping shared state, shared UoWs, and cross-form record coordination deterministic.

## Primary Implementation Seams

- Form registry / form lifecycle services
- Multi-form navigation APIs on FormsManager
- Global variable and form message bus services
- Shared block registration and lock coordination

## Enhance / Update / Fix Rules

- Each form keeps its own orchestration context, but shared blocks must never fork into duplicate UoW instances silently.
- Cross-form messaging is notification/orchestration only; it should not mutate another form's block state behind the receiving form's validation/lock rules.
- Form call stack and modality decisions must be explicit and reversible.

## UoW and Primary-Key Rules

- A shared block must either reuse the same UoW instance or a wrapper that preserves one authoritative current list, current record, dirty state, and commit history.
- If a parent form creates a record with a sequence-generated key before commit, that reserved key can be passed to child forms for provisional FK use.
- If the parent record uses a datasource-generated identity key, dependent forms must not assume the FK is stable until insert/commit refresh returns the actual value.
- Cross-form return payloads should carry stable business identifiers or committed PKs, not transient row indexes.
- Global variables and message bus payloads should treat PK values as opaque data, not as permission to bypass the source form's UoW.

## Cross-Form Safety Rules

- Never commit the same shared UoW twice from two forms concurrently.
- Lock coordination must happen at the shared-block/UoW layer, not only at the form shell.
- When a form closes, unregister messaging and registry state without disposing shared UoWs still owned elsewhere.

## Done / Verify Checklist

- Form registry resolves active forms reliably.
- `CallFormAsync`, `OpenFormAsync`, `NewFormAsync`, and `ReturnToCallerAsync` preserve lifecycle semantics.
- Shared blocks propagate change notifications without cloning persisted state.
- Sequence-backed and identity-backed parent/child workflows both behave predictably across forms.

## Maintenance Notes

- Any future UI adapters must treat this phase as the model for orchestration ownership, but adapter work is outside the current migration scope.