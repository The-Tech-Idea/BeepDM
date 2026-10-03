# Durable Reject Triage And Acknowledged Replay

The built-in file reject store implements optional Models
`IImportRejectRecoveryStore`. `DataImportManager.ReplayRejectedRecordAsync` and
the direction-aware BeepSync overload perform actual writes. This is row-level
recovery, not completion of a failed sync run or universal exactly-once delivery.

## Identity And Payload

Configured ordinary/sync quality rejection captures a new lowercase GUID-N
RejectId plus admitted RunId, four exact connection/entity names and the available
destination GuidID. ImportExecutionResult.RunId exposes the admission identity;
sync directions share the admitted checkpoint run ID. Optional ImportRunId lets
a host supply its own correlation identity. Index/batch coordinates remain
diagnostics, never replay keys.

Use `DataImportManager.GetRejectContextKey(config)` consistently:
`sourceConnection/sourceEntity->destinationConnection/destinationEntity`.
The former slash-only replay key is not silently interpreted. Storage path case
folding remains compatible; recovery compares the captured binding names exactly.
Names/GuidID are observed identity, not proof of tenant authorization or provider
execution ownership. Hosts must control configuration/provider mutation.

OriginalDestinationPayload is the already-transformed destination row, not raw
source input. Managed file records omit RawRecord; custom/native reject stores
still receive the original contract and own serialization/privacy. Corrections
must also be destination-shaped. Replay never reruns mapping, defaults or custom
transformation. It rehydrates an actual generated target from current owned
provider metadata, refuses unknown/missing-required fields and incompatible
values, and evaluates captured current record-quality policy before writing.
Targets must already exist and be open; replay does not open connections or run DDL.

Payloads use a closed tagged codec: supported scalar values, timestamps with
kind/offset, Guid, bytes and nested string-key dictionaries/object arrays. No
arbitrary CLR type activation. Duplicate properties, foreign tag members,
unsupported objects/cycles and oversized graphs fail safely. Limits include
1,024 row fields/key characters, depth 24, 10,000 nodes, a conservative aggregate
serialization budget and encoded length <= 1,048,576 characters. Unsupported
capture counts as RejectStoreFailures and never admits the denied row.

## Durable State Machine

| State / Operation | Meaning |
|---|---|
| Pending | Not eligible for automatic replay |
| PrepareReplayAsync -> Prepared | Operator records destination correction and expected revision |
| ClaimReplayAsync -> Claimed | Persisted CAS revision/claim owner before quality/provider work |
| CompleteReplayAsync(Acknowledged) | One Errors.Ok provider response; Replayed=true |
| CompleteReplayAsync(Denied/DefinitivelyFailed) -> Pending | No admitted write or explicit Errors.Failed; fresh operator preparation required |
| CompleteReplayAsync(Uncertain) -> ReconciliationRequired | Exception, null or ambiguous acknowledgement; no blind retry |
| ReconcileReplayAsync(Applied) -> ReconciledApplied | Host/operator confirms provider state with evidence reference |
| ReconcileReplayAsync(NotApplied) -> Pending | Host/operator confirms not applied; fresh preparation required |
| DismissRejectAsync -> Dismissed | Explicit terminal triage; never Replayed=true |

File transitions validate identity, format, payload/state and expected revision
inside the same coordinated update lease. Duplicate durable IDs and stale owners
fail; acknowledged/reconciled/dismissed terminal rows cannot reopen. Legacy
MarkReplayedAsync requires one unambiguous legacy coordinate and refuses managed
records. Clear refuses unresolved managed rows and validates corruption before
deletion. Preserve `.beep.lock` sidecars; never expire claims by deleting locks.

Claimed records remain blocked across restart, even if a crash preceded the write:
the restarted process cannot infer where the previous owner stopped. Before
reconciliation the host must stop/fence that owner and establish the actual
provider outcome. A supplied operator name/evidence string is not authentication
or independent proof; hosts own authorization and audit evidence.

## Use

```csharp
var store = new JsonFileImportErrorStore(hostRejectFolder);
config.ErrorStore = store;
var context = DataImportManager.GetRejectContextKey(config);
var reject = (await store.LoadPendingAsync(context, token)).Single();
var prepared = await store.PrepareReplayAsync(context, reject.Recovery.RejectId,
    reject.Recovery.Revision, authorizedOperator, correctedDestinationRow, token);
var outcome = await manager.ReplayRejectedRecordAsync(config, context,
    prepared.Recovery.RejectId, prepared.Recovery.Revision, workerIdentity, token);
if (outcome.RequiresReconciliation)
{
    // Preserve store/provider evidence; stop the old owner and reconcile explicitly.
}
```

Record policy must remain explicitly configured; an empty policy cannot quietly
waive the original rejection. Required/advisory behavior remains the
[quality admission contract](QUALITY-ADMISSION.md). Missing/legacy-only recovery
stores fail closed; custom optional capabilities are trusted acknowledged stores.

`ReplayFailedRecordsAsync` preserves its legacy signature but only processes
operator-prepared durable rows. Pending/legacy rows report failure, and any
Claimed/ReconciliationRequired row blocks bulk recovery instead of reporting Ok
from an empty pending list. Progress exceptions increment DiagnosticFailures
without changing an acknowledged outcome. No replay path invokes legacy
index-based marking to infer a successful write.

BeepSyncManager's overload takes schema/store/context/reject ID/revision/owner and
captures current forward/reverse DqPolicy record rules through IntegrationContext.
Reverse recovery is available only for Bidirectional schemas. It does not advance
watermarks, publish Completed, clear failed checkpoints or evaluate a whole-attempt
threshold. Whole-run reconciliation remains separate.

## Outcomes And Limits

ImportRejectReplayResult distinguishes admitted records, actual WriteAttempts,
provider RecordsAcknowledged, denial/failure/unsupported records, persistence and
diagnostic failures, cancellation, uncertain writes and reconciliation requirements.
Counts represent provider acknowledgements, not assumed database commits.
Optional `IImportReplayDataSource.InsertReplay` receives the durable RejectId;
that provider owns deduplication/commit semantics. Otherwise exactly one ordinary
InsertEntity attempt is made per admitted claim, without automatic retry.

Cancellation before invocation causes no provider attempt. Cancellation after
Errors.Ok preserves acknowledgement and durable terminal marking while returning
Cancelled/Failed. A cancelled/throwing in-flight write is uncertain. Claim
completion uses an independent five-second cooperative cleanup token; it cannot
force-stop synchronous providers or uncooperative custom stores.
If completion persistence fails, the result retains provider acknowledgement and
requires reconciliation. Durable state may still be Claimed or may have committed
the terminal transition before acknowledgement was lost; neither permits blind
replay. Invalid/ambiguous claim responses are not automatically released.

This does not establish atomic target/reject commits, native-channel triage,
provider-backed run fencing, complete governed intent, live-provider transactions,
Unix/clean-pack/consumer support or scalable retention. JSONL updates still parse
and rewrite complete files; hosts own sensitive payload access, encryption and
retention. Read [persistence limits](../../Services/Persistence/README.md) and
[sync storage limits](../BeepSync/STORAGE-AND-OUTCOMES.md).
