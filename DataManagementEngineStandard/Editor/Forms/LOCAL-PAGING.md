# Local Cursor Paging

This increment implements the local portion of BF-09/Stage F. It does not close
Stage F or introduce bounded provider paging, provider prefetch, a page-row cache,
or native control redesign. Those remain explicit deliverables of the full plan.

## API And Arithmetic

LoadLocalPageWithOutcomeAsync (optional IFormsLocalPaging) operates over an already
loaded, authorized collection. LoadPageAsync is its compatibility facade: it
returns PageInfo only when PageStatePublished is true; otherwise null. Precancelled
requests throw cancellation before admission; calls after close throw disposal.
Invalid names/nonpositive pages return InvalidRequest. Positive requests beyond
the loaded count clamp to the last local page, including after count shrink.

PageInfo.TotalPagesLong uses exact integer division and remainder; SkipLong uses
long multiplication. Existing int TotalPages/Skip projections now throw checked
overflow rather than wrapping or silently truncating. No double-based page math
is used. Default paging size remains 50; explicit SetBlockPageSize(0) disables
local paging. Empty buffers publish page 1 with zero total pages and no cursor
navigation acknowledgement. An unreadable Count is not an empty result.

Default PagingManager distinguishes unknown count from explicitly stored zero,
uses case-insensitive keys and returns snapshots. Count changes clamp arithmetic
state. Local acceptance replaces a count hint with the actual loaded count: a
remote COUNT value is not evidence that remote rows have been loaded locally.
Raw helper setters are bookkeeping, not proof of navigation or buffer authority.

## Ownership And Navigation

The manager captures registration, page request, managed query revision, buffer,
loaded count, cursor, observed record revision, mode, configuration and policy.
Default ILocalPagingPublication prepares without changing live page state and
uses an opaque revision token. Size/count/page/depth/reset changes expire proposals.
Publication consumes a proposal even if its callback fails; no blind action retry.
Helper -> security revision -> registration is the publication lock order. Only
owned memory belongs in those monitors, never provider/trigger/metadata callbacks.

Local requests are serialized; the newest request supersedes an older unpublished
one. Nested local paging from paging/read callbacks rejects rather than awaiting
its own gate. General navigation/query/detail/commit operations do not share one
global scheduler. Record/configuration/raw helper check-to-use races still need
wider Stage D/F qualification; this is not a whole-command transaction.

The existing navigation pipeline is reused with captured checkpoints before cursor
movement and between subsequent foreign callbacks. A rejected navigation or
cancelled pre-move trigger leaves accepted page state unchanged. An ignored cursor
setter cannot produce a page acknowledgement. After the exact cursor index is
verified, page state is published before post-navigation observers. Subsequent
observer failure/cancellation cannot turn the accepted page into an unpublished one;
NotificationFailures preserves evidence. Existing nonpaging navigation keeps its
legacy Boolean/failure behaviour.

NavigationEffectsPossible records an owned cursor attempt, not all effects of
arbitrary triggers or dirty-save callbacks. A cancellation/retirement inside a
foreign cursor setter can leave a changed cursor with unpublished page state.
Do not silently roll it back or treat null/false as proof of no effects. Existing
dirty navigation policy can prompt/save/discard and detail synchronization can
read providers; local paging itself does not fetch a root datasource page.

Page admission joins manager callback drain, links caller/close cancellation, and
waits for actual awaited trigger completion before releasing the gate. Closing
retires admission immediately, not an already-running foreign callback. Owned
paging resources are disposed after drain, never while a queued operation uses them.

## Authorization And Compatibility

Local cursor movement cannot authorize cached rows. IsBufferAuthorized must remain
true across checkpoints; principal/read-policy changes, buffer replacement/ABA,
unreadable/disposed sources and unsupported scoped preload require an accepted
managed query/detail first. Field-only policy changes expire an in-flight request.
No raw host field access or new receipt is used to recertify older-context data.

Optional publication-capable paging and security helpers are required. Legacy
injected helpers reject before cursor mutation rather than falling back to eager
page-state writes. Custom optional implementations must qualify their publication,
token, callback and lifecycle contracts. Whole cross-form shared-helper ownership,
arbitrary mutable configuration and post-final-window edits remain open.

Virtual UoW/collection buffers and EnterQuery criteria are not local result pages.
They reject, even though existing IDataSource exposes a paged overload. That
overload is not evidence of deterministic bounded provider paging or mandatory
policy. Explicit [bounded provider fetch](PROVIDER-PAGING.md) uses a separate optional
capability. Local navigation over its accepted buffer still counts only loaded
rows, not its remote count/page. Lazy/fetch-ahead settings do not implement prefetch.
PerformanceManager caches block metadata, not an authorized provider page-row cache;
eviction does not itself query or refresh displayed rows.

After an unrelated query/helper/raw buffer change, accepted page metadata can be
outdated until reconciliation or the next successful local request. No automatic
all-buffer page observer or provider page-cache invalidation is implied. UI must
inspect typed outcomes and refresh/focus only against current binding identities.

## Evidence And Remaining Work

[PagingTests](../Forms.Tests/PagingTests.cs) adds 30 cases using actual UoW/wrapper
buffers, strict ignored/unreadable collections, barriers, mock triggers and helper
tokens. Coverage includes local count/clamp, zero/overflow math, cancellation and
physical drain, slow/new request ordering, buffer/cursor ABA, replacement, policy,
config/helper invalidation, observer failures, nesting and capability rejection.
[Implementation Log](IMPLEMENTATION-LOG.md) records source-built results.

The next [provider fetch increment](PROVIDER-PAGING.md) implements opt-in staged
fetch with deterministic key ordering, mandatory policy, row/byte bounds and
long-count evidence. Policy/query/registration-aware bounded prefetch/cache remains
required and unimplemented. No provider or native UI adapter is qualified by
these local cases; the provider document describes the separate SQLite test lane.
All other A-G and earlier intermittent release gates remain intact.
