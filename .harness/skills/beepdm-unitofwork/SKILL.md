---
name: beepdm-unitofwork
description: Use when writing transactional CRUD in BeepDM — Add / Modify / Delete / Commit against an `Entity` POCO via `UnitofWork<T>`. Hands off to Forms (UI binding), ETL (batch transactional sinks), and Configuration (entity metadata) skills.
---

# beepdm-unitofwork

`UnitofWork<T>` is BeepDM's **transactional CRUD API**. It tracks new, modified, and deleted entities in memory and persists them with a single `Commit()` call. The observable binding list (`ObservableBindingList<T>`) gives UI binding first-class change notifications.

## When to use this skill

- Adding, modifying, or deleting a small batch of entities in app code.
- Wrapping a multi-step change in a transaction.
- Binding a list to a UI (DataGridView, Blazor grid, etc.) and reflecting changes live.
- Implementing a domain repository on top of `IDataSource`.

## Do NOT use this skill for

- First-run schema creation → use **beepdm-setup** or **beepdm-migration**.
- Bulk data movement between datasources → use **beepdm-etl**.
- Master-detail UI lifecycle → use **beepdm-forms** (which uses UoW internally).

## File Locations

`DataManagementEngineStandard/Editor/UOW/`:

- `UnitofWork.cs` — main class
- `ObservableBindingList.cs` — change-notifying list (in `Editor/Defaults/` or `Editor/UOW/`)

## Typical Workflow

```csharp
var uow = editor.CreateUnitOfWork<Product>();
uow.AddNew(new Product { Name = "Widget", Price = 29.99m });
uow.Modify(existingProduct);
uow.Delete(productToRemove);
uow.Commit();   // single transaction; success or full rollback
```

## Modes

- **AddNew** — entity is queued for INSERT.
- **Modify** — entity is queued for UPDATE (delta detection by hash / original values).
- **Delete** — entity is queued for DELETE.
- **Commit** — runs the queued changes in a single transaction.
- **Rollback** (or just drop the UoW) — discards queued changes without touching the datasource.

## ObservableBindingList

`ObservableBindingList<T>` is the list type returned by UoW queries. It raises `ListChanged` events on Add / Remove / Replace, which UI frameworks (WinForms, WPF, Blazor) consume for live binding. Use it in place of `List<T>` whenever the UI needs to react to data changes.

## How this skill works with the rest of the data-management layer

| Handoff | Direction | What flows |
|---|---|---|
| **beepdm-forms** | ← Forms | Forms calls UoW for every save. UoW is the transactional back-end; Forms is the UX. |
| **beepdm-etl** | ← ETL | ETL sinks wrap per-record writes in UoW when a transactional target needs it. |
| **beepdm-configuration** | ← Config | UoW reads entity metadata from `EntityStructure` (via config cache or runtime discovery). |
| **beepdm-migration** | ← Migration | UoW assumes the schema already exists. If a column is missing, the UoW call should surface the error. |
| **beepdm-setup** | ← Setup | After Setup finishes, UoW is the runtime API the app uses for CRUD. |

## Design Rules

- UoW is **transactional** — Commit either succeeds entirely or rolls back. Do not call Commit in a loop expecting partial success.
- Delta detection is automatic; do not pre-emptively mark every entity as Modified.
- Always dispose the UoW (or use `using`); it holds the transaction and change tracker.
- Use `ObservableBindingList<T>` for UI-bound lists, not `List<T>`.
- For bulk operations, use **beepdm-etl** pipelines; do not loop `Commit()`.

## Cross-references

- See **beepdm-forms** for the UI that drives UoW saves.
- See **beepdm-etl** for bulk operations.
- See **beepdm-configuration** for the entity-structure cache.
- See **beepdm-migration** for schema changes UoW depends on.
- See `.cursor/unitofwork/SKILL.md` for the deep-dive implementation details.
