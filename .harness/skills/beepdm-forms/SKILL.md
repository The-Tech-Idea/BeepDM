---
name: beepdm-forms
description: Use when building or running BeepDM FormsManager-based master-detail UIs — form lifecycle, mode transitions (Add/Edit/Delete/Browse), navigation, helper managers, performance configuration, and enhanced data operations. Hands off to UoW (for transactional save), Configuration (for entity structure), and Migration (when schema drift is detected) skills.
---

# beepdm-forms

`FormsManager` is the BeepDM master-detail UI engine. It manages form lifecycles (open, mode-transition, save, close), parent-child navigation between entities, and integration with the underlying `IDataSource` and `UnitofWork<T>` for persistence.

## When to use this skill

- Building a master-detail form on top of an `Entity` POCO.
- Wiring mode transitions (Browse → Add → Edit → Delete → Browse).
- Configuring navigation between parent and child entities.
- Tuning form performance (lazy loading, batch size, binding list refresh).
- Using helper managers for child table lookups, validation, and entity CRUD.
- Enhanced data operations (bulk update, deep clone, recursive child load).

## Do NOT use this skill for

- Persisted connection/driver/mapping management → use **beepdm-configuration**.
- Schema creation on first run → use **beepdm-setup**.
- Schema changes mid-life → use **beepdm-migration**.

## File Locations

`DataManagementEngineStandard/Editor/Forms/`:

- `FormsManager.cs` — main manager (lifecycle, mode)
- `FormsManager.Navigation.cs` — parent/child navigation
- `FormsManager.Operations.cs` — CRUD, validation, lookup
- `FormsManager.Lifecycle.cs` — open/close, dispose, state
- `FormsManager.Helpers.cs` — helper managers (child lookup, validation, …)
- `FormsManager.PerformanceConfig.cs` — perf tuning
- `FormsManager.EnhancedOperations.cs` — bulk update, deep clone

## Mode Lifecycle

```
Browse (read-only)
  ├── Add → Edit → Save → Browse
  ├── Edit → Save / Cancel → Browse
  ├── Delete → confirm → Browse
  └── Navigate(parent → child)
```

Mode transitions are guarded — entering `Edit` from `Browse` requires the entity to be loaded; entering `Save` requires a clean validation pass.

## Helper Managers

- **Child-lookup manager** — fetches child rows for the active parent.
- **Validation manager** — runs data-annotation + custom validators before save.
- **Lookup manager** — resolves foreign-key dropdowns.
- **CRUD helper** — wraps UoW calls for Add/Edit/Delete.
- **Performance config** — batch size, prefetch, refresh strategy.

## How this skill works with the rest of the data-management layer

| Handoff | Direction | What flows |
|---|---|---|
| **beepdm-unitofwork** | → UoW | Every save flows through `UnitofWork<T>`. Forms does not write to the datasource directly. |
| **beepdm-configuration** | ← Config | Forms reads entity structure (`EntityStructure`) — either from config cache or via `IDataSource.GetEntityStructure` at runtime. |
| **beepdm-migration** | → Migration | If Forms detects a schema drift (column missing, type mismatch), it should report and call back into Migration rather than silently fail. |
| **beepdm-setup** | ← Setup | After the wizard finishes, Forms is the runtime UI the user actually sees. Setup is invisible; Forms is visible. |
| **beepdm-etl** | ↔ ETL | Forms can display the output of an ETL run. Forms does not trigger ETL — that's `WorkFlowEngine`. |

## Design Rules

- All writes go through **UoW**, not direct datasource calls. This keeps the change transactional.
- Modes are explicit — never mutate an entity while in `Browse` mode.
- Validation runs in the form **before** UoW save. A failed validation does not touch the datasource.
- Navigation is lazy by default; configure prefetch in `PerformanceConfig` for tight loops.
- Dispose the form cleanly; `FormsManager.Lifecycle.cs` owns the open/close discipline.

## Cross-references

- See **beepdm-unitofwork** for the transactional save path Forms uses.
- See **beepdm-configuration** for entity-structure cache.
- See **beepdm-migration** for schema-drift handling.
- See `.cursor/forms/SKILL.md` for the deep-dive implementation details.
