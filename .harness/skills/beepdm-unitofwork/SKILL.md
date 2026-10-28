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
## Detached stored-column projection (2026-10-28)

`IUnitofWorkStoredProjectionRead` is an optional read capability implemented by `UnitofWork<T>` and delegated by `UnitOfWorkWrapper`. Its producer must implement `IBoundedStoredProjectionDataSource`. Select declared scalar columns, including the complete primary key, tenant field when scoped, and order fields. The result is immutable `StoredProjectionPage`/`StoredProjectionRow` data; it never publishes partial entities to `Units` or grants write/authorization authority. Unsupported adapters/providers refuse without a legacy fallback.

The owner retains clean-buffer, scope/schema/target retirement, independent tenant filtering, coherent bounded count/page, strict payload/type/key validation and awaited cancellation. Stored projection reads do not evaluate computed getters; full-record read-only scalar validation remains unchanged. Current native verification is SQL Server LocalDB on Windows/net10.0 through DoitAll's normal SDK/MigrationManager fixture, plus controlled protocol faults; other frameworks/providers and package consumers remain unverified. DoitAll Doc239 records the application regression and focused evidence.
## Atomic stored-projection reference guards (2026-10-28)

`IUnitofWorkStoredProjectionGuardRead.PrepareStoredProjectionGuardAsync` captures a complete first-page cohort through the existing scoped staged projection owner, without publishing partial entities. It preserves the qualified selector, selected fields and complete key. Partial observations refuse. Adapters replacing projection read/prepare paths need their own qualification; the base preparation refuses to bypass them.

`IUnitofWorkProjectionGuardedBatch` requires `IProjectionGuardedMutationBatchDataSource`; it never falls back to an ordinary batch. `ProjectionGuardedMutationBatchRequest` composes the unchanged batch identity/authority/tenant/outcome contract with at most16 complete reference cohorts, each at most5000 rows, with a combined16 MiB canonical/request bound. The original128 row-group/cohort-count limits stay unchanged. The RDB owner counts and hashes every selected stored column in one serializable physical transaction before mutations and again before commit. Digests are order-independent, retain exact string/binary facts, normalize decimal scale/integer width while distinguishing decimal/binary32/binary64 domains, and preserve stored DateTime ticks and kind without zone conversion. They are logical source observations, never security grants or signatures.

Implementation uses existing bounded query/scalar generation; no unbounded/legacy read or separate-connection guard. Capability refusal, changed/added source rows, final-check rollback and unconfirmed acknowledgement remain explicit. Qualification so far is Windows/net10.0 SQL Server LocalDB and controlled protocol checks in DoitAll's normal SDK/MigrationManager fixture; other providers/TFMs, package/concurrency/scale acceptance and adoption by actual saved Cash Flow decisions remain open. See DoitAll Doc239 and its physical-guard evidence; a prepared guard alone supplies neither policy nor actor approval.

Complete-count comparisons: the projection envelope may also carry up to eight ConditionalCohortComparison pairs. IComparedCohortMutationBatchDataSource is an explicit additional capability; UnitOfWork refuses missing capability before dispatch. The RDB owner compiles each count through the existing declared-metadata/predicate and long-count owners, and requires equal complete counts before writes and before commit in the same serializable transaction. Counts impose no historical row materialization limit and supplement bounded content/authority guards. DoitAll Doc239 qualifies current source preparation and349 native/control cases plus one API outage check on Windows/net10 LocalDB; shared net8/net9 compile only. Other live providers, concurrency/scale/packages, actor/policy admission and actual saved decision/audit acceptance remain open.
