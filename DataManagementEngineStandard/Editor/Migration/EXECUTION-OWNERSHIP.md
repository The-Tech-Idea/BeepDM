# Migration Execution Ownership Backend

## Available Boundary

Models adds optional `IMigrationExecutionOwnership`, immutable claim/admission
observations and an `IMigrationExecutionLease`. Engine supplies
`FileMigrationExecutionOwnership`; ConfigEditor delegates to it under
`Config.ConfigPath/Migrations/ExecutionOwnership`. A missing/empty configuration
root fails; this capability does not fall back to a working directory.

**MigrationManager does not yet acquire this capability automatically.** Its
execute/resume/compensation and imperative paths still require integration, scoped
checkpoint authority and defensive public progress views. Installing this backend
does not fix the existing concurrent-manager race. P3-06/08 remain open.

## Conflict Domain

The host supplies one explicit canonical physical-target identity. For example,
a provider/endpoint/database/schema identity must stay the same across connection
aliases, credential rotations, process/runtime GUIDs and execution tokens. Do not
derive ownership from a plan hash: different plans for the same target conflict.
Unsupported/ambiguous provider endpoints need an explicit host identity decision;
the backend does not guess connection-string equivalence or resolve DNS aliases.

Identity comparison is ordinal. All cooperating executors must share the same
physical root and identity spelling. Separate roots or different identities do
not coordinate. Root configuration must remain pinned through a run/reconciliation.
This is not a distributed/network-filesystem lock, an authorization check or a
substitute for the approved target fingerprint. Hosts own reconciliation authority.

Target identity is SHA-256 keyed in paths and claim documents, not persisted raw.
Execution token and plan hash are bounded opaque identifiers, not credentials.
Target/evidence references allow up to 1,024 UTF-16 characters; token/hash/actor
allow 256. Empty/trim-changing/control/surrogate-containing values deny. Claim IDs
are generated GUIDs. No timeout or heartbeat automatically expires a claim.

## Admission And Lifetime

`TryAcquireMigrationExecution(targetIdentity, executionToken, planHash, token)`
returns an explicit status; only Acquired provides a lease:

| Status | Meaning |
|---|---|
| Acquired | Owner handle held and version-one Owned claim durably acknowledged |
| Busy | The exclusive owner sidecar cannot currently be acquired; no checkpoint/provider mutation is admitted by this backend |
| RequiresReconciliation | No live handle, but unfinished/uncertain durable work remains; includes an immutable existing claim |
| Failed/Cancelled/Unsupported | Admission not acknowledged; no owner lease is returned |

The permanent `.owner.lock` sidecar is held with exclusive sharing for the whole
lease. The separate AtomicFileStore mutation lease coordinates each JSON update.
No internal collection lock spans provider/user work. Never unlink either sidecar
to bypass contention: replacing the path can create ABA owners on another inode.

`lease.Finish(disposition, token)` performs an acknowledged expected-claim-ID and
revision transition while the owner handle is held. Completed/SafeToRetry release
the durable claim; RequiresReconciliation keeps it blocked. The executor must
choose from actual provider/checkpoint evidence, not assume failed calls made no
changes. OperatorReconciled is not an allowed owner disposition. Repeated finish,
disposed ownership, unknown dispositions, changed revisions and save failures deny.
The owner handle stays held until Dispose, even after acknowledged Finish.

Dispose only closes the handle. It never saves, deletes or declares success.
An exception/crash/disposal without acknowledged Finish leaves Owned durable work;
the next admission returns RequiresReconciliation rather than blindly executing.
Failed Finish likewise retains the owned observation and blocks live competitors.
Cancellation before a commit does not release work; acknowledged commits are not
reclassified by later cancellation.

## Explicit Reconciliation And Evidence

`ReadMigrationExecutionClaim(identity)` returns a new immutable observation or null
only for a genuinely absent document. Corrupt/inaccessible/unsupported data throws
safe exceptions without raw payloads/paths or nested error messages.

`ReconcileMigrationExecution(identity, expectedClaimId, actor, evidenceReference,
token)` first obtains the exclusive owner handle. A live executor therefore cannot
be cleared by an operator racing it. Validate the existing version/identity/state;
an absent, stale, already released or corrupt claim is not silently replaced.
A saved decision increments revision and records OperatorReconciled plus actor and
evidence reference. Those strings are assertions by the authorized host/operator,
not provider-state proof, signatures or an automatic database reconciliation engine.
Use non-secret durable evidence references, not credentials or arbitrary stack traces.

Before admitting the next owner, archive the previous released snapshot immutably
under `Completed/<target-key>-<claim-id>.json`. Archive failure or conflicting bytes
blocks replacement of current evidence. A crash after archive but before admission
can safely reuse an identical archive. The current claim remains authoritative;
archive and new admission are not a multi-file transaction. Archive retention/export
and host-signed audit policies remain open; never delete unresolved claims to prune.

Strict readers reject duplicate/unknown/missing JSON properties, trailing content,
unsupported versions, foreign target keys, invalid IDs/revisions/timestamps and
inconsistent state/disposition/evidence. These checks preserve corrupted bytes;
they are not protection against an actor who can replace every file and handle.
Envelope parsing is depth/character bounded; AtomicFileStore's initial read still
materializes the file before validation, not a global allocation bound.

## Verification And Remaining Work

`MigrationOwnershipTests` exercise same/different-token competing instances and
child processes, live-owner reconciliation denial, killed-owner detection, explicit
reconciliation/readmission, immutable observations/evidence archives, corruption,
revision races/overflow, cancellation, concurrent Finish and replacement failure.
The replacement-denial fixture is Windows-specific and returns without exercising
that branch on other OSes; do not count it as Unix replacement qualification.

The local Windows TFM matrix is recorded in the framework implementation log.
It does not establish live-provider recovery, remote filesystem semantics,
power-loss durability or automatic MigrationManager admission. Next integrate
execute/resume and compensation, pin store/target identity, publish defensive
checkpoint views and qualify actual provider intermediate-state reconciliation.
