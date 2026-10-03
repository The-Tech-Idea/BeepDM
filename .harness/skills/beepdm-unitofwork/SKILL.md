---
name: beepdm-unitofwork
description: Use when writing transactional CRUD in BeepDM via typed UnitofWork entities. Hands off to Forms for UI binding, ETL for bulk sinks, and Configuration for entity metadata.
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

- `UnitofWork.Core.cs`, `UnitofWork.CRUD.cs`, `UnitofWork.Core.Extensions.cs`
- `DataManagementModelsStandard/ObservableBindingList/` owns the binding list

## Typical Workflow

```csharp
using var uow = new UnitofWork<Product>(editor, "database", "Products", "Id");
uow.Add(new Product { Name = "Widget", Price = 29.99m });
var result = await uow.Commit();
if (result.Flag != Errors.Ok) { /* inspect Message and secondary Errors */ }
```

## Modes

- **AddNew** — entity is queued for INSERT.
- **Modify** — entity is queued for UPDATE (delta detection by hash / original values).
- **Delete** — entity is queued for DELETE.
- **Commit** — uses a transaction when the datasource capability matrix supports it;
  otherwise writes can be partial.
- **Rollback** (or just drop the UoW) — discards queued changes without touching the datasource.

## ObservableBindingList

`ObservableBindingList<T>` is the list type returned by UoW queries. It raises `ListChanged` events on Add / Remove / Replace, which UI frameworks (WinForms, WPF, Blazor) consume for live binding. Use it in place of `List<T>` whenever the UI needs to react to data changes.

## How this skill works with the rest of the data-management layer

| Handoff | Direction | What flows |
|---|---|---|
| **beepdm-forms** | ← Forms | Forms calls UoW for every save. UoW is the transactional back-end; Forms is the UX. |
| **beepdm-etl** | Separate write path | The built-in datasource sink writes directly; its opt-in transactions do not use UOW. |
| **beepdm-configuration** | ← Config | UoW reads entity metadata from `EntityStructure` (via config cache or runtime discovery). |
| **beepdm-migration** | ← Migration | UoW assumes the schema already exists. If a column is missing, the UoW call should surface the error. |
| **beepdm-setup** | ← Setup | After Setup finishes, UoW is the runtime API the app uses for CRUD. |

## Design Rules

- With transactions, UOW defers OBL acceptance and AfterSave until datasource
  Commit returns Ok. Failed writes/commit keep changes pending; rollback errors
  are secondary diagnostics and do not replace the original failure.
- Confirmed rollback restores generated insert keys. Edits made during commit
  remain pending as updates, not another insert. Provider rollback failure is not
  a confirmed recovery; reconcile actual database state before retrying.
- Consumer AfterSave/PostCommit notification errors are warnings after a confirmed
  commit, not proof that database writes failed.
- Direct OBL `CommitAllAsync(..., order, false)` callers must call `AcceptCommit`
  after database commit or `DiscardCommit` after rollback. Do not use default
  immediate acceptance to wrap a transaction externally.
- Delta detection is automatic; do not pre-emptively mark every entity as Modified.
- Always dispose the UoW (or use `using`); it holds the transaction and change tracker.
- Use `ObservableBindingList<T>` for UI-bound lists, not `List<T>`.
- For bulk operations, use **beepdm-etl** pipelines; do not loop `Commit()`.

## Cross-references

- See **beepdm-forms** for the UI that drives UoW saves.
- See **beepdm-etl** for bulk operations.
- See **beepdm-configuration** for the entity-structure cache.
- See **beepdm-migration** for schema changes UoW depends on.
- See `tests/FrameworkReliabilityTests/UnitOfWorkTransactionTests.cs` for recovery guarantees and regression cases.
