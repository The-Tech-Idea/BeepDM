# Phase 01 — Core Completion & Stabilization

**Status:** Complete (`30 / 30` in [todo-tracker.md](../todo-tracker.md))  
**Priority:** Critical  
**Depends on:** None

---

## Objective

Make FormsManager safe as the orchestration layer for create, enhance, update, and fix work by finishing the core CRUD, trigger, validation, LOV, and interface seams before higher-level features build on them.

## Primary Implementation Seams

- `FormsManager.GenericOperations.cs`
- `FormsManager.EnhancedOperations.cs`
- `FormsManager.FormOperations.cs`
- `FormsManager.Sequences.cs`
- `FormsManager.cs`
- `Models/DataBlockInfo.cs`
- `Interfaces/IUnitofWorksManagerInterfaces.cs`
- `IUnitofWork` / `IUnitofWork<T>` contracts

## Enhance / Update / Fix Rules

- Keep `DataBlockInfo.EntityType` authoritative for typed record creation.
- Keep `CreateNewRecord` and `InsertRecordEnhancedAsync` aligned; do not let one path bypass validation, defaults, or triggers that the other path uses.
- Field-change automation belongs at `OnBlockFieldChanged`; do not duplicate validation and LOV glue in unrelated partials.
- Form commit remains form-level orchestration; block persistence still runs through each block's UoW.

## UoW and Primary-Key Rules

- `IUnitofWork` owns persisted key behavior. FormsManager should ask the UoW for real sequence values via `GetSeq(...)` when the datasource supports it.
- `CreateNewRecord` must produce a typed record, apply audit defaults, then apply item defaults and key-generation policy before insert.
- Identity / auto-increment key fields must stay unset until insert succeeds. After insert or commit, refresh the record from the UoW / datasource before downstream logic treats the key as stable.
- Sequence-managed numeric keys may be reserved before insert when child records need a parent FK early, but that reservation must happen on the create/insert path only.
- Composite keys require explicit per-field defaults or triggers. Never assume a single numeric generator can satisfy the full key.
- If a caller supplies a key explicitly, treat it as authoritative unless security or validation rules reject it.

## Recommended Record-Creation Pipeline

1. Resolve `DataBlockInfo` and ensure block mode is valid for create.
2. Instantiate record from `EntityType`.
3. Apply audit defaults.
4. Apply configured item defaults.
5. Apply PK strategy:
   - preserve explicit key
   - reserve sequence from UoW when configured
   - skip identity fields
   - apply GUID/custom factory when configured
6. Fire `WHEN-CREATE-RECORD` and pre-insert validation.
7. Call `InsertAsync` on the block UoW.
8. Refresh database-generated values and synchronize dependent blocks.

## Done / Verify Checklist

- `RegisterBlock<T>()`, `GetBlock<T>()`, and typed insert overloads stay consistent.
- `CreateNewRecord` never falls back to `ExpandoObject` for known entity types.
- `InsertRecordEnhancedAsync` fires pre/post insert triggers and honors cancellation.
- `CommitFormAsync` validates blocks and cross-block rules before save.
- PK behavior is covered for explicit keys, identity keys, sequences, GUIDs, and composite keys.

## Maintenance Notes

- If record creation changes, update Phase 02 and Phase 04 docs too because built-ins and trigger templates depend on the same PK rules.
- If UoW insert semantics change, review `FormsSimulationHelper.ExecuteSequence(...)`, `TriggerLibrary.AutoNumberTrigger(...)`, and audit commit behavior together.