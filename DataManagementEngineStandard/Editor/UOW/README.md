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
## Atomic stored-projection reference guards (2026-10-28)

`IUnitofWorkStoredProjectionGuardRead.PrepareStoredProjectionGuardAsync` captures a complete first-page cohort through the existing scoped staged projection owner, without publishing partial entities. It preserves the qualified selector, selected fields and complete key. Partial observations refuse. Adapters replacing projection read/prepare paths need their own qualification; the base preparation refuses to bypass them.

`IUnitofWorkProjectionGuardedBatch` requires `IProjectionGuardedMutationBatchDataSource`; it never falls back to an ordinary batch. `ProjectionGuardedMutationBatchRequest` composes the unchanged batch identity/authority/tenant/outcome contract with at most16 complete reference cohorts, each at most5000 rows, with a combined16 MiB canonical/request bound. The original128 row-group/cohort-count limits stay unchanged. The RDB owner counts and hashes every selected stored column in one serializable physical transaction before mutations and again before commit. Digests are order-independent, retain exact string/binary facts, normalize decimal scale/integer width while distinguishing decimal/binary32/binary64 domains, and preserve stored DateTime ticks and kind without zone conversion. They are logical source observations, never security grants or signatures.

Implementation uses existing bounded query/scalar generation; no unbounded/legacy read or separate-connection guard. Capability refusal, changed/added source rows, final-check rollback and unconfirmed acknowledgement remain explicit. Qualification so far is Windows/net10.0 SQL Server LocalDB and controlled protocol checks in DoitAll's normal SDK/MigrationManager fixture; other providers/TFMs, package/concurrency/scale acceptance and adoption by actual saved Cash Flow decisions remain open. See DoitAll Doc239 and its physical-guard evidence; a prepared guard alone supplies neither policy nor actor approval.

Complete-count comparisons: the projection envelope may also carry up to eight ConditionalCohortComparison pairs. IComparedCohortMutationBatchDataSource is an explicit additional capability; UnitOfWork refuses missing capability before dispatch. The RDB owner compiles each count through the established declared-metadata/predicate and long-count owners, and requires equal complete counts before writes and before commit in the same serializable transaction. Count comparisons impose no historical row materialization limit. They supplement bounded content projections and the unchanged actor/tenant/row guards; equality alone does not establish source content or write authority. DoitAll Doc239 owns consumer qualification; current349 focused consumer cases and one API-outage check pass on Windows/net10 LocalDB; shared net8/net9 compile only, while other-provider/concurrency/scale/package and saved-decision acceptance remain open.
