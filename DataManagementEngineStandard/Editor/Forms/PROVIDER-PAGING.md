# Bounded Provider Paging

Scope: explicit opt-in managed fetch, not local cursor navigation, automatic
virtual scrolling or an authorized row-page cache. Stage F remains in progress.

## Entry Points

[IFormsProviderPaging](../../../DataManagementModelsStandard/Editor/Forms/Interfaces/IFormsProviderPaging.cs)
exposes `FetchPageWithOutcomeAsync(block, BoundedPageRequest, token)` and returns
the existing `FormQueryResult`. Default
[UoW staging](../UOW/UnitofWork.ReadStage.cs) and
[wrapper forwarding](../UOW/UnitOfWorkWrapper.cs) require
[IStagedUnitofWorkPageRead](../../../DataManagementModelsStandard/Editor/IStagedUnitofWorkPageRead.cs)
and [IBoundedPagedDataSource](../../../DataManagementModelsStandard/DataBase/IBoundedPagedDataSource.cs).
The existing int `IDataSource.GetEntity(...page...)`/`PagedResult` overload is not
that capability. Unsupported/custom override paths reject; no unbounded fallback.

Set block `Configuration.PageSize` at session setup. It must be positive and
match the explicit request size, within positive `MaxRecordsPerFetch` and
`MaxRecords`. These are per-fetch row limits, not a cap on the remote count.
Request rows are additionally limited to 5,000 and payload to 16 MiB; use smaller
explicit byte limits for the application. Page/offset/count are long, offset
multiplication is checked, and total-page math uses integer division/remainder.
These ceilings bound accepted encoded rows, not all provider memory, SQL work,
latency or the aggregate heap of materialized records and metadata.

## Producer And Consumer Contracts

Requests own copied filters and immutable order descriptors. Returned filter
lists are fresh copies. Only known column names are accepted; caller SQL order
fragments and nonempty `DefaultOrderByClause` reject before the provider. All
declared primary-key columns are appended as deterministic unique tie-breakers.
Declare the complete actual unique key; a consumer cannot prove database key
uniqueness from metadata. No-key and incomplete/unbindable record schemas reject.

The producer must apply every filter before both count and page, use the full
order and obtain a coherent count/page snapshot. It must bound at production,
not materialize all rows and call Take afterward. Reject a too-large cell/page,
do not truncate fields, drop rows or fabricate count. If a provider cannot honor
long offsets or the snapshot/order/byte contract, it must reject the request.
Its task must acknowledge physical completion: no detached provider work.

The response owns a copied UTF-8 JSON array, not provider-owned record objects or
an arbitrary row enumerator. Actual encoded byte length is checked against the
request, not a provider-supplied byte estimate. The
[default decoder](../UOW/UnitofWork.PageRead.cs) validates strict UTF-8, matching
request nonce/page/size, nonnegative count and exact count-consistent row length
within the page limit before constructing candidates. Every declared field must
appear exactly once, case-insensitively, and bind to a readable/writable scalar
record property. Unknown/missing/duplicate fields, nested objects/arrays, invalid
types, null nonnullable values and duplicate/null primary keys reject. Binary
fields use base64 strings; enum values use numeric JSON. Arbitrary record graphs
are not a supported bounded page format.

Envelope matching and receiver bounds do not prove a malicious producer honored
filters, ordering, count snapshot or physical allocation limits. Each provider
must separately qualify its implementation. Mutable metadata is rechecked for
field/type/key changes at preparation/publication checkpoints, not atomically
frozen against arbitrary final-window edits.

## Acceptance And Outcomes

Provider fetch uses the
[shared query pipeline](FormsManager.QueryExecution.cs), mandatory compiled
caller/default/security filters, the authoritative local UoW tenant predicate,
query/detail ordering, registration/query revision, policy publication gate and
[buffer authorization receipt](BUFFER-AUTHORIZATION.md). Rejected implicit reads
retain prior records/cursor/mode. Dirty target or captured detail blocks replacement
before fetching and again before acceptance. Provider execution and observers
remain outside publication monitors.

Cancellation after admission waits for the actual provider task. Close cancels
and drains this work; nested awaited query/detail/page operations and self-drain
reject. A newer same-registration query/page supersedes an unpublished older
candidate. This is not a general edit/commit/navigation scheduler.

`ProviderPage` is an immutable validated count/page observation, not publication
authority. Check `RecordsPublished`, `State`, registration/request identity and
notification failures before UI delivery. An out-of-range page after count shrink
returns `PageOutOfRange`, observed count and no replacement; it is not silently
clamped or re-fetched. Request a valid page explicitly. Empty page one/count zero
is a valid accepted empty result. An owned out-of-range stage cannot publish.
After acknowledgement, notification failures retain Completed/accepted evidence.

Long provider page numbers never overwrite int `DataBlockInfo.CurrentPage` or
`PageInfo`: those describe [local cursor pages](LOCAL-PAGING.md) within currently
loaded rows. `LastQuery` is not replaced with an unpaged SELECT that would falsely
describe the provider page SQL. Hosts own dispatch and generation checks; fresh
binding requires the same accepted receipt as other managed reads.

## Qualification And Remaining Work

[ProviderPagingTests](../Forms.Tests/ProviderPagingTests.cs) adds 63 cases using
actual UoW/wrapper/manager, malicious response fixtures, barriers and a native
SQLite ADO.NET test producer. SQLite count/page use one transaction, bound LIMIT/
OFFSET, complete key order, framework-compiled bound filters, a cell-length guard
before materialization and a byte-limited encoder sink. Cases exercise multi-page
ties, hostile literals, conflicting tenant filters, producer byte rejection,
long count/offset, malformed evidence, dirty details, policy/config/key changes,
supersession, physical cancellation/drain, nesting, observer errors and rejected
custom-stage evidence from another request.

This qualifies the test lane, not production SQLite plugins or other providers.
Policy/query/registration-aware bounded prefetch and cache remain required and
unimplemented; `FetchAheadDepth`, `EnableLazyLoad`, TTL and metadata cache settings
do not activate them. Broader operation/lifetime/metadata/UI and all A-G release
gates remain open. See [Implementation Log](IMPLEMENTATION-LOG.md).
