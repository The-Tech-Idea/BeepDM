# Governed Migration Intent

## Current Contract

Plans built by the type/discovery and ORM-model paths use `PlanHashVersion = 2`.
Each operation carries a Models-owned `MigrationEntitySnapshot`: cloned desired
metadata, observed existence and cloned baseline metadata. Relational operations
carry the same captured intent. Dry-run and execution use copies of these schemas,
not later reflection or the model cache.

The canonical SHA-256 identity includes schema definitions, operations in execution
order, target fingerprint, retry/failure policy, governance settings, performance
policy and capability flags. Object keys and unrelated descriptors are sorted
ordinally. Field ordinals/indexes and composite key/index/relation column order are
semantic. Null and empty defaults remain distinct. Generated metadata GUIDs, UI
captions, timestamps, lifecycle status and diagnostic wording do not identify DDL.
Snapshot serializers use explicit settings, not ambient JsonConvert.DefaultSettings;
host-wide naming/converter changes cannot omit fields from canonical identity.

Artifact classes remain mutable for compatibility. Mutation is not prevented by
the type system: apply/preflight/approval validate identity, and execution works
from a private copy. Do not edit schema/operations after planning. Rebuild a plan
for changed schema/target state; do not manually overwrite its hash.
Provider step routing/dependencies are frozen from approved operations before
iteration; progress callbacks cannot redirect them through mutable checkpoint fields.

## Policy And Approval

The default retry/failure policy is captured at planning time. A supplied execution
policy must match it. For an explicit policy/environment change, use the concrete
manager's `CreateMigrationPlanRevision(plan, executionPolicy, policyOptions)`.
It returns a new plan identity and checkpoint. Review and re-approve that revision;
do not carry over the old token or approval. Existing IMigrationManager signatures
are preserved; concrete revision/load methods are additive APIs.

High-risk approval options require `ApprovedPlanHash = plan.PlanHash` in addition
to the existing approver/reason and backup/restore checks. Governance settings
must match the captured context. `ApproveMigrationPlan` validates intent before
recording its audit event; approval lifecycle changes do not alter the hash.
Hosts still own authentication, authorization and approver separation. A content
hash is an identity check, not a signature or tamper-proof audit store.

```csharp
var manager = new MigrationManager(editor, dataSource);
var plan = manager.BuildMigrationPlanForTypes(types, includeDestructive: true);
// For custom policy/environment, create and review a revision before approval.
plan.RollbackReadinessReport = manager.CheckRollbackReadiness(
    plan, backupConfirmed: true, restoreTestEvidenceProvided: true);
var approval = new MigrationPolicyOptions
{
    ApprovedPlanHash = plan.PlanHash,
    Approver = "operator",
    OverrideReason = "Reviewed schema change and recovery evidence"
};
var result = await manager.ExecuteMigrationPlanAsync(plan, policyOptions: approval);
```

## Persistence And Resume

Plan records preserve their legacy summary fields and add `PlanArtifactJson`.
`LoadMigrationPlan(planId)` loads and validates that payload for this datasource.
Checkpoints carry a non-recursive `ApprovedPlan`, preserving schema, risk flags,
target and policy across store reload. Resume uses it rather than reconstructing
intent from step labels. Step targets/dependencies must still match approved intent.
Token/hash mismatch does not replace the original checkpoint snapshot.

Legacy hash versions and summary-only checkpoints/plans are explicitly rejected
for execution/resume, including completed checkpoints without captured intent.
There is no automatic migration of old approvals. Rebuild, reconcile any already
applied work, obtain new approval and use a new token.

Governed checkpoints require the optional Models `IMigrationHistoryPersistence`
capability. ConfigEditor implements acknowledged, coordinated history writes with
strict corruption/identity validation. Custom stores must explicitly acknowledge
writes; a returned legacy void append is not enough. See
`../../Services/Persistence/README.md` for format, promotion and filesystem limits.

Planning/revision remain available without a working store. Inspect
`PlanPersistenceStatus` and `PlanPersistenceErrorCode`; a preview checkpoint token
reserves intent only in this process, not a durable execution start. Loading a
validated stored plan reports Saved. Explicit CreateExecutionCheckpoint throws
if storage cannot acknowledge it. Execution returns failure before DDL when the
start or before-attempt checkpoint cannot be saved. Check the concrete result's
`CheckpointPersisted` and `CheckpointPersistenceStatus`, not just operation counts.

Each provider attempt first saves a Running marker. A failed save after an
acknowledged DDL call stops later work, keeps AppliedCount, leaves Success=false
and reports RequiresReconciliation/RequiresOperatorIntervention. It does not
automatically rerun DDL. Memory-state resume blocks reconciliation-required
checkpoints; restarted Running markers also block replay. Corrupt/empty or
wrong-token stored checkpoints are not treated as missing fresh executions.
A primary provider failure remains visible if its failure-checkpoint save also
fails. Diagnostic/imperative summary records remain best-effort and do not prove
that a recovery checkpoint was saved.

Tests include fault-injected acknowledgements and actual filesystem plan/checkpoint
reload in a separate process, with no DDL during completed resume. Recording
providers do not establish real database crash recovery, and Saved is not a
power-loss or distributed-storage guarantee.

## Drift And Recovery Limits

Preflight compares live metadata with the captured baseline and verifies target
identity/configuration. The fingerprint stores a digest, not raw connection
strings/parameters. Hashing is not a substitute for credential protection.
Providers remain responsible for reporting accurate live target/metadata.

Baseline checks are deliberately conservative: a partly applied schema may no
longer match and be blocked on resume. This increment does not project every
successful DDL operation into an expected intermediate provider schema. Failed
steps that partly changed a database need reconciliation, not blind retry.
Concurrent execution-token admission, immutable checkpoint progress, real-provider
metadata normalization and provider-confirmed partial
DDL recovery are remaining P3-06 gates. Do not claim universal resumability,
transactional DDL, exactly-once execution or compensation.

Skipped UpToDate steps satisfy dependencies. Continue-on-failure can apply other
independent steps, but any failed step leaves Success=false and an incomplete,
failed checkpoint; it never publishes a completed success solely because the
outer loop finished.

Verification: `tests/MigrationManagerTests/PlanIntentTests.cs`, plus existing
execution/destructive/rollback tests, on net8.0/net9.0/net10.0. These use recording
providers, not external database integration fixtures.
