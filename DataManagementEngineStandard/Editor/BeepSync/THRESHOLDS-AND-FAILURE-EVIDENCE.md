# Attempt Thresholds And Failed-Run Evidence

## Captured Threshold Admission

An enabled DqPolicy now enables BatchThresholdEnabled by default. The default
ThresholdFailureMode is Required, independently of RecordFailureMode. Record-only
hosts must explicitly set BatchThresholdEnabled=false; a missing required engine
or rule is no longer permission to import. Existing enabled policies that relied
on silent threshold skipping must register the rule, disable the threshold or
explicitly choose Advisory. Invalid configuration still fails closed in Advisory.

SyncBatchQualityAdmission captures engine reference, key, schema ID, failure mode,
numeric limit and depth/time limits before schema preflight/import. Keys must be
nonblank and at most 1024 characters. Reject percentages must be finite 0..100;
depth is bounded at 64 and observed time at 60000 ms. Zero rule-policy limits use
the existing depth-10/time-5000 defaults. Each solve receives a fresh execution
policy, not a mutable static profile. Executable engines remain trusted host code.

## Decision And Denominator

Despite the historical BatchThresholdRuleKey name, this is an attempt-end gate,
not a hook after every internal import batch. It executes once after the admitted
forward/reverse imports, including partial import or conflict-gate failure, before
completion persistence. A failed forward import does not admit reverse writes.
Cancellation propagates at observed boundaries instead of requiring another solve.

Inputs are schemaId, recordCount, rejectCount, rejectRate and maxRejectRate.
recordCount is actual ImportExecutionResult.RecordsAttempted, combined across
admitted directions in this attempt, including rows rejected before provider writes.
It is not RecordsSucceeded, WriteAttempts, an estimated provider total or all
materialized source rows. Transformation failures contribute attempted rows, not
quality rejections. RejectCount counts actual RecordsQualityRejected; warnings and
evaluation failures are distinct. Empty attempts have rate zero and still evaluate
the rule. Earlier zero-acknowledgement retry attempts are not the current denominator.

SolveRule outputs must contain action as the exact string ContinueRun or AbortRun.
Absent/null/unknown/non-string values are evaluation failures; no ToString coercion
or raw error/output logging occurs here. ContinueRun cannot override a reject rate
strictly greater than the captured numeric limit. Equality passes the numeric test;
AbortRun rejects even an empty attempt. A threshold pass never overrides an existing
record/transformation/provider failure or authorizes advancing past rejected rows.

Required rejection, lookup/solve errors, malformed output or observed timeout block
completion and whole-run retry, even with zero writes. Advisory decisions/errors
retain explicit warning evidence without turning failed imports into success.
LastRunBatchThresholdResult exposes immutable Passed/Rejected/EvaluationFailed
evidence, denominator, limit, rate, mode, BlocksCompletion and HasWarning. It is
null when disabled/not evaluated; it contains no record payloads or raw diagnostics.
Synchronous engines are not forcibly interrupted, frozen or sandboxed.

## Failed Checkpoint Publication

Checkpointed runs capture retry settings and checkpoint identity/fingerprint/version/
correlation before execution. Running, Failed and Completed use that admitted
identity, not a caller-rebound policy hash computed after writes. This keeps
checkpoint publication consistent; it does not make all schema/filter/provider
intent immutable or implement the separate promotion protocol.

After acknowledged startup, import/required-threshold failure or observed cancellation
publishes an acknowledged Failed checkpoint. ProcessedOffset is the actual combined
acknowledged count, TotalExpected is the known attempted count and
RequiresReconciliation=true. Version-1 FailureEvidence retains closed failure kind,
attempt/write/quality/reject-store counts, uncertain-write state and any completed
threshold decision. These are acknowledgements, not transaction commits or replay
offsets. Reconciliation reports now use measured import attempted/written/skipped
and quarantine totals; broader default/conflict/SLO telemetry remains unqualified.

Failure publication uses an independent five-second cooperative cleanup token so
already-cancelled callers do not discard acknowledgements. Custom adapters may
ignore it; this is not a hard deadline. LastRunFailureCheckpointStatus reports the
save result. Failed/null/throwing/unsupported acknowledgements return
SyncCheckpointFailureResult with CheckpointStage=Failure and the original
ImportResult/counts. Successful failure publication preserves the original failed
or cancelled result. No publication is attempted after failed startup or uncertain
completion acknowledgement, and completion failure retains the prior Running
evidence rather than trying to rewrite possibly committed Completed state.

Built-in storage validates explicit typed evidence fields and count relationships,
preserves corruption, and refuses same-run Failed mutation/reopening just as it
refuses Completed mutation. Exact repeated content is allowed. Running, Failed or
reconciliation-required evidence blocks restart; operator run-owned clearing is
explicit recovery, never automatic success cleanup. Legacy absent failure evidence
does not establish safe replay. Existing context fingerprints can change with
added policy fields; preserve old artifacts and reconcile/migrate explicitly.

## Verification And Remaining Work

SyncThresholdTests adds 45 cases per configured TFM; SyncPersistenceTests adds 13
failed-evidence cases. Three original red regressions cover missing required engine,
thrown gate permitting success and abort hiding acknowledgements. Tests use actual
mapped generated payloads and exercise both directions, captured mutation, malformed
outputs, real/empty denominators, required/advisory policy, synchronous limits,
adapter statuses, cancellation, restart, corruption, terminal immutability,
separate-process typed reload and actual Windows replacement denial.

Durable file reject identity/triage/CAS and actual acknowledged import/sync row
replay now have a separate verified boundary; read
[Reject Recovery](../Importing/REJECT-RECOVERY.md). Row recovery does not complete
a failed run or advance its cursor. Native-channel recovery, target/reject atomicity, live-provider
recovery, complete run/promotion intent, durable cursor/Completed agreement,
provider execution admission, package-reader compatibility and Unix remain open.
Checkpoint-disabled runs have no equivalent durable failure guarantee. Mutable
public progress objects are not execution leases or immutable authority. No
exactly-once, rollback, bounded streaming or complete rule-safety claim is made.
