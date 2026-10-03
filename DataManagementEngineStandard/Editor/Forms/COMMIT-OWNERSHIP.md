# Forms Commit Ownership

The reliability implementation is in progress. This document describes the
current commit contracts, not completion of the full Forms enhancement plan.

## Contracts

- `IEnlistedUnitofWork` is optional. `UnitofWork<T>` implements it and
  `UnitOfWorkWrapper` forwards it when its underlying UoW supports enlistment.
- `PrepareCommitAsync` requires an already-open caller-owned transaction on the
  exact datasource instance. It does not begin, commit or abort that transaction.
  It leaves tracking pending and returns `IUnitofWorkCommitStage`.
- `Complete(Committed)` accepts staged tracking only after confirmed provider
  commit. `Complete(RolledBack)` preserves pending edits and restores captured
  generated keys only when their applied values have not been edited again.
- `Complete(Unknown)` retains the UoW's admission guard. `IsResolved` remains
  false. The caller must investigate the provider before completing with a known
  outcome. A receipt with a confirmed outcome cannot reverse that decision while
  repairing local tracking. Completion callbacks run without a receipt monitor
  lock; overlapping completion is rejected rather than awaited.
- `IFormsCommitOutcomes.CommitFormWithOutcomeAsync()` returns `FormCommitResult`.
  The legacy `CommitFormAsync()` also returns this type through `IErrorsInfo`.
  The existing host interfaces have no new mandatory members.

## Coordinator Behavior

The coordinator captures form instance IDs, block objects, UoWs and datasource
references. It verifies them before writes and again before provider commit.
Targets with the same block name in different forms retain distinct identities.
Datasource groups use object identity, not a connection-name or database-name
equivalence assumption. These are connection/session ownership boundaries.

For a transaction-capable datasource whose participating UoWs all support
enlistment, Forms opens one transaction and stages every block before committing
that provider. Tracking/AfterSave/PostCommit notifications follow confirmed
durability. No nested UoW transaction is opened for those writes.

Legacy or nontransactional UoWs keep independent commits, explicitly reported by
`UsesIndependentCommits` and a warning. There is no invented outer transaction
and no claim of atomicity across these blocks. Independent failed writes are
conservatively unknown because the legacy result does not prove no effects.

Forms commits sharing a datasource instance use a fail-fast admission lease.
A competing Forms commit cannot begin or abort the active Forms transaction.
Unresolved provider outcomes retain that lease until every available receipt is
explicitly reconciled. Failed begin cleanup or legacy failures with no receipts
require a verified fresh datasource/UoW binding; the coordinator cannot infer
their backend state or safely manufacture tracking acceptance.

Raw datasource/UoW operations outside Forms are a trusted low-level escape hatch.
They must not concurrently manipulate a transaction owned by Forms. The current
`IDataSource` transaction triple has no ownership token with which to distinguish
an externally opened transaction. Optional provider-owned handles and external
provider concurrency qualification remain work, not a guarantee of this lease.

## Reading Results

`Blocks` provides one captured block outcome, including its form instance ID.
`DataSources` records independent, opened, committed, rolled-back or unknown
provider outcomes. `HasPartialCommit` identifies a known durable prefix;
`RequiresReconciliation` includes unknown provider resources and unresolved local
tracking completion. Do not use a single Boolean as a distributed-atomicity proof.

A thrown/null/failed provider commit is unknown even if subsequent cleanup
returns success: cleanup does not prove whether the commit acknowledgement was
lost. Already committed providers remain committed; remaining providers are
aborted and unsuccessful aborts are retained in `Errors`.

```csharp
var result = await ((IFormsCommitOutcomes)manager).CommitFormWithOutcomeAsync();
if (result.RequiresReconciliation)
{
    // Investigate the provider and retain the result's reconciliation receipts.
    // Do not retry the save or call Rollback merely to clear its dirty state.
}
```

Only after independent provider verification may the caller resolve the receipt
with `Committed` or `RolledBack`. Resolving a historical result does not rewrite
its block state; `IsResolved` permits a subsequent operation after reconciliation.
For unknown legacy writes, explicitly reconcile/reload into a verified fresh UoW
and datasource instead of resubmitting the same pending inserts.

Failures in post-commit observers are warnings, not failed writes. New edits
made while an acknowledged snapshot commits remain dirty and keep Forms system
status `CHANGED`. Audit notification isolation and lock-release closeout remain
under review; successful write acknowledgement is separate from those actions.

## Retry And Save Accounting

`ICoordinatedBlockSave` supplies per-block results. Missing blocks/UoWs, validation
failures, lookup exceptions and unattempted targets prevent aggregate success.
Injected dirty-state helpers must implement this optional contract to participate
in Forms coordinated saves; unsupported helpers are rejected before provider writes.

Standalone dirty saves honor configured retry counts only when the result or
exception implements `ISafeWriteRetry` and guarantees no durable effects, no
unresolved transaction and no accepted tracking. Timeout/connection message text
is not that guarantee. Enlisted callbacks are not automatically retried.

## Remaining Gates

Stage A is not closed: transitive metadata/configuration pinning, rollback of
propagated foreign keys, full notification/audit/lock cleanup, cross-form graph
qualification and optional provider-owned transaction handles remain. The real
SQLite ADO.NET test adapter does not certify the separately shipped Beep SQLite
plugin or other providers. Lifecycle, query policy, scheduling, UI adapter behavior
and paging have their separate open stages in the
[enhancement plan](RELIABILITY-AND-ENHANCEMENT-PLAN.md).
