# UOW

Unit of Work implementation for BeepDM stateful CRUD workflows.

## Core Types
- `UnitofWork<T>` (partial: core, CRUD, extensions, utilities)
- `UnitOfWorkWrapper`
- `UnitOfWorkFactory`
- Helper classes under `Helpers/`
- `MultiDataSourceUnitOfWork` (parent folder) for cross-entity coordination

## What `UnitofWork<T>` Provides
- Local state tracking (`InsertedKeys`, `UpdatedKeys`, `DeletedKeys`, `DeletedUnits`).
- Paging and filtered view support (`Units`, `FilteredUnits`).
- Primary key/guid key handling.
- Optional soft delete and optimistic concurrency fields.
- Data validation before commit operations.

## Supporting Helpers
- Collection/data/state/event/validation/default helpers split concerns.
- Interface contracts in `Interfaces/` allow extension and testing.

## Usage Pattern
1. Create via factory or constructor with datasource/entity context.
2. Load records (`Get`) or initialize for add/new flows.
3. Perform add/update/delete operations.
4. Validate and commit.
5. Dispose to release resources.

## Multi-Source Coordination
When parent-child entity consistency is required across units, use `MultiDataSourceUnitOfWork` to manage relationship mapping, navigation, and coordinated commits.

## Optional atomic conditional batches

[Conditional batches](CONDITIONAL-BATCH.md) document the bounded multi-entity physical transaction capability, tenant/clean-owner admission, known rollback and uncertain commit outcomes. It is adopted by the DoitAll approval engine; ordinary Commit keeps its existing contract.
## Detached stored-column projection (2026-10-28)

`IUnitofWorkStoredProjectionRead` is an optional read capability implemented by `UnitofWork<T>` and delegated by `UnitOfWorkWrapper`. Its producer must implement `IBoundedStoredProjectionDataSource`. Select declared scalar columns, including the complete primary key, tenant field when scoped, and order fields. The result is immutable `StoredProjectionPage`/`StoredProjectionRow` data; it never publishes partial entities to `Units` or grants write/authorization authority. Unsupported adapters/providers refuse without a legacy fallback.

The owner retains clean-buffer, scope/schema/target retirement, independent tenant filtering, coherent bounded count/page, strict payload/type/key validation and awaited cancellation. Stored projection reads do not evaluate computed getters; full-record read-only scalar validation remains unchanged. Current native verification is SQL Server LocalDB on Windows/net10.0 through DoitAll's normal SDK/MigrationManager fixture, plus controlled protocol faults; other frameworks/providers and package consumers remain unverified. DoitAll Doc239 records the application regression and focused evidence.
