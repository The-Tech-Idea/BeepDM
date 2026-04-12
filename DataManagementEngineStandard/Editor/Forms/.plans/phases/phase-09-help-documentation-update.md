# Phase 09 — Help Documentation Update

**Status:** Complete — help-site rewrite applied on 2026-04-09  
**Priority:** Documentation follow-through after implementation and test coverage  
**Depends on:** Phases 01 through 08 implemented in code, with tracker and mapping docs aligned

---

## Objective

Rewrite `Help/formsmanager.html` so it reflects the real final FormsManager API surface, not an outdated subset of the partial classes and helper managers.

## Current Audit State

- Phases 01 through 08 were already complete for planning purposes because the corresponding runtime capabilities existed in code.
- `Help/formsmanager.html` has now been rewritten and aligned with the current FormsManager surface.
- The tracker, enhancement plan, Oracle mapping, and help page now describe the same audited runtime state.

## Primary Implementation Seams

- `Help/formsmanager.html`
- FormsManager public partials and interfaces
- Helper manager READMEs and examples
- Oracle Forms parity matrix and upgrade guidance

## Documentation Rules

- The help file should describe the public API that actually ships, not roadmap items that remain incomplete.
- Use the completed phase docs and the tracker as the source of truth when deciding what to document as implemented vs planned.
- Document orchestration responsibilities separately from UoW responsibilities so consumers understand the boundary.
- Do not use stale historical planning notes outside `.plans` as the source of truth for missing features.

## UoW and Primary-Key Documentation Requirements

- Add a dedicated section for record creation and key assignment.
- Explain the precedence order: explicit key, datasource identity, datasource/UoW sequence, FormsManager sequence provider, item defaults/custom factories.
- Explain that FormsManager orchestrates key assignment but the UoW remains responsible for persisted identity/sequence interactions and final database state.
- Document how identity-generated keys become available only after insert/commit refresh.
- Document the safe patterns for detail FKs when parent PKs are sequence-backed versus identity-backed.
- Document the trigger and audit implications of PK generation.

## Done / Verify Checklist

- [x] `formsmanager.html` covers all active public partials and helper managers.
- [x] Oracle Forms mapping table is current.
- [x] Examples include create, insert, and commit scenarios with sequence, identity, and default-value handling.
- [x] Help text matches the real tracker status and test-backed behavior.

## Maintenance Notes

- Treat this phase as the final documentation consolidation step after implementation, not as the place to invent new behavior.
- Keep the help page synchronized with the README, migration guide, tracker, and Oracle Forms mapping whenever the FormsManager public surface changes.