# Phase 02 — Oracle Forms Built-in Emulation

**Status:** Complete (`25 / 25` in [todo-tracker.md](../todo-tracker.md))  
**Priority:** High  
**Depends on:** Phase 01 core registration and CRUD paths

---

## Objective

Expose Oracle Forms-style block, navigation, alert, sequence, default-value, and timer built-ins without breaking the underlying UoW lifecycle.

## Primary Implementation Seams

- `FormsManager.Sequences.cs`
- `FormsManager.Navigation.cs`
- `FormsManager.KeyTriggers.cs`
- `FormsManager.cs`
- `Helpers/SequenceProvider.cs`
- `Helpers/ItemPropertyManager.cs`
- `Interfaces/IUnitofWorksManagerInterfaces.cs`

## Enhance / Update / Fix Rules

- Built-ins are orchestration APIs, not alternate persistence APIs. They must delegate into the same UoW-backed block state used everywhere else.
- `SetBlockProperty` / `GetBlockProperty` should mutate and read `DataBlockInfo`, not parallel state.
- Navigation built-ins must respect validation, locks, and current-record semantics already enforced elsewhere.
- Alert and timer services must stay UI-agnostic via injected providers/interfaces.

## UoW and Primary-Key Rules

- `GetNextSequence(...)` and `ISequenceProvider` are FormsManager-level utilities, but datasource-backed inserts should still prefer `IUnitofWork.GetSeq(...)` when a real DB sequence exists.
- `ExecuteSequence(...)` should remain a bridge to UoW-backed sequence resolution first, with the in-memory sequence provider as the non-database fallback.
- Built-ins must never overwrite identity / auto-increment fields that the datasource owns.
- `SetItemDefault(...)` is appropriate for GUIDs, timestamps, user stamps, and client-generated business keys; it is not a substitute for datasource identity retrieval.
- For key fields marked `IsAutoIncrement` / identity, UI metadata should keep them read-only and post-insert refresh should be the source of truth.

## Key Scenarios to Preserve

- Oracle-style preallocated sequence key on create, then normal UoW insert.
- Database identity key left empty on create and materialized after insert/commit.
- Default factory for GUID or document number prefixes.
- Copy-field built-ins that move values between current records without bypassing change tracking.

## Done / Verify Checklist

- Block property built-ins mutate `DataBlockInfo` cleanly.
- Navigation built-ins route through FormsManager navigation services.
- `ShowAlertAsync`, timer APIs, and message APIs stay interface-driven.
- Sequence/default built-ins do not consume values during read-only operations.
- Identity-key records still round-trip correctly through UoW insert and commit.

## Maintenance Notes

- If sequence behavior changes, update Phase 01, Phase 04, and Phase 09 docs as well.
- If datasource helpers gain stronger identity metadata, reflect that in item-property rules before adding more UI behavior.