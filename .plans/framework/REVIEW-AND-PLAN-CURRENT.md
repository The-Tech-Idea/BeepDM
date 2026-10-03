# BeepDM Framework Review And Enhancement Plan

Reviewed: 2026-10-03. Planning-only refresh of the current dirty worktree.
Baseline commit: `2a837952597b60c8c90821187e16dd8e9913508a`.
Scope: Engine/Models contracts, migration ownership and compensation, sync
governance/publication, configuration, Forms/plugin lifecycle, import/ETL,
observability, tests and package delivery. This is a targeted review, not an
exhaustive audit of every provider or an external security assessment.

This snapshot supersedes current-state claims and next-work ordering in
[the preceding review](REVIEW-AND-PLAN-2026-10-03.md). Preserve historical evidence
in [IMPLEMENTATION-LOG.md](IMPLEMENTATION-LOG.md) and existing P1-P5 work-item IDs.
No framework source, skills, public contracts or completion checkboxes are changed
by this review. Existing uncommitted implementation is preserved.

## Verification Baseline

A fresh Windows SDK `10.0.401` Engine build for `net9.0` was run:

```powershell
dotnet build DataManagementEngineStandard/DataManagementEngine.csproj -f net9.0 --no-incremental -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q
```

Result: **exit 1, 5 errors, 1,987 warnings**. Log:
`C:/Users/f_ald/AppData/Local/Temp/BeepDM-framework-planning-build-net9.log`.
These flags disable package generation and documentation updates, not the projects'
existing external PostBuild copies. No tests were run against stale binaries.
Neither the other TFMs nor a full solution build are newly qualified by this pass.

The preceding ownership-backend increment recorded 5,178 passing executions and
54 new ownership cases per TFM. That is historical evidence for its source state,
not a green baseline for the current partial manager integration. Source-only risks
below are not presented as newly executed fault/concurrency tests.

## Findings In Priority Order

### F01 [P1]: Migration refactor currently prevents compilation

[ExecutionOrchestration.cs:20](../../DataManagementEngineStandard/Editor/Migration/MigrationManager.ExecutionOrchestration.cs#L20)
now stores serialized strings. Resume/get/persist/reload still use checkpoint
objects at lines 528, 600, 887 and 925, producing one CS1503 and three CS0029 errors.
[RolloutGovernance.cs:270](../../DataManagementEngineStandard/Editor/Migration/MigrationManager.RolloutGovernance.cs#L270)
calls instance `AddAuditEvent` from a static method, producing CS0120.

Impact: new tests, consumers and release artifacts cannot be rebuilt. Finish the
refactor coherently before behavioral work; do not merely deserialize back into
public/shared mutable authority or restore global state to silence compilation.

### F02 [P1]: Ownership finalization does not receive actual execution evidence

[ExecutionOwnership.cs:42](../../DataManagementEngineStandard/Editor/Migration/MigrationManager.ExecutionOwnership.cs#L42)
declares scope `ProviderInvoked` and `AcknowledgedCount`, but neither is assigned.
The core updates separate locals at
[ExecutionOrchestration.cs:341](../../DataManagementEngineStandard/Editor/Migration/MigrationManager.ExecutionOrchestration.cs#L341).
The wrapper uses the unassigned scope values for exceptions/finalization at
[ExecutionOwnership.cs:141](../../DataManagementEngineStandard/Editor/Migration/MigrationManager.ExecutionOwnership.cs#L141)
and line 161. A failed provider result that does not already set reconciliation
can consequently select `SafeToRetry` despite invoked, potentially partial DDL.

Impact: after compilation repair, ownership may permit unsafe replay and exception
results can lose acknowledged counts. Connect one execution-owned evidence ledger
to all attempts, exceptions and finalization; a negative acknowledgement is not
proof of no mutation. Invalid claim receipts must not authorize release of a
foreign claim. Failed claim completion must remain distinct from checkpoint save.

### F03 [P1]: Captured storage and public checkpoint routes are incomplete

[ExecutionOwnership.cs:58](../../DataManagementEngineStandard/Editor/Migration/MigrationManager.ExecutionOwnership.cs#L58)
captures a store, but checkpoint writes at
[ExecutionOrchestration.cs:888](../../DataManagementEngineStandard/Editor/Migration/MigrationManager.ExecutionOrchestration.cs#L888)
and reads at line 908 still use the live editor configuration facade. A configuration
root change can separate a claim from its checkpoint evidence.
Public `CreateExecutionCheckpoint` directly calls the core at line 32; the core
dereferences `_executionScope.Value` at line 49 without creating a scope. Completed
resume returns before target/store admission at line 574. The legacy stamp branch
at line 44 does not distinguish newly created from loaded ownershipless records.

Impact: a non-null plan on the direct checkpoint route can encounter a null scope;
storage rerouting and legacy adoption remain unsafe. Capture root/codec/target/
provider once, route authoritative I/O through that capture, fresh-load under
ownership, and publish deep-copy progress observations. Reject incompatible legacy
evidence explicitly. Update harness/worker/host setup: current fixtures provide
neither the new capture capability nor the explicit canonical target identity.

### F04 [P1]: Compensation can act on unapplied operations and report false recovery

[RollbackCompensation.cs:220](../../DataManagementEngineStandard/Editor/Migration/MigrationManager.RollbackCompensation.cs#L220)
builds actions from every plan operation, without filtering to acknowledged
checkpoint steps or provider-confirmed effects. Actual execution has no ownership
admission. At line 260 both AddForeignKey and DropForeignKey compensate by dropping;
at line 275 both CreateIndex and DropIndex also drop. Dropping does not restore an
object removed by a forward drop. Actions run in ascending plan order rather than
a validated inverse dependency order.

Manual playbook text goes into `ExecutedActions` at line 302/307; line 311 can then
report successful completed compensation without executing those actions.
Impact: recovery may remove unaffected objects, fail to restore lost constraints/
indexes, or mislead operators about restored state.

Plan: claim and fresh-load before actual compensation; select confirmed affected
steps; capture original definitions for reversible drops; otherwise require manual
restore evidence. Use inverse dependency order, acknowledged pre/post markers,
typed actual/manual/uncertain outcomes and provider-state reconciliation. Dry-run
must not perform provider writes or masquerade as completed recovery.

### F05 [P1]: Required sync promotion can pass invalid gates and publish partially

[SchemaGovernance.cs:62](../../DataManagementEngineStandard/Editor/BeepSync/BeepSyncManager.SchemaGovernance.cs#L62)
admits null/non-Boolean results unless their string is exactly `false`. Missing
engine/registration skips the gate. Caller state changes at line 97 before two
independent saves. A failure can leave live schema, mapping and version evidence
in disagreement. [SchemaFingerprinter.cs:17](../../DataManagementEngineStandard/Editor/Schema/SchemaFingerprinter.cs#L17)
does not include complete key/filter/policy intent and returns random identity on
failure. Existing bounded NFEL/default fixes do not repair this promotion boundary.

Plan: explicit required/advisory policy; only Boolean true admits a required gate;
complete canonical owned intent; staged acknowledged publication with expected
revision and restart recovery. Hashing failure denies instead of fabricating identity.

### F06 [P1]: Completed sync checkpoint and durable cursor can disagree

[Sync.cs:399](../../DataManagementEngineStandard/Editor/BeepSync/BeepSyncManager.Sync.cs#L399)
acknowledges completion before updating live cursor state at line 410, which still
requires explicit schema save. A crash can retain Completed with an older durable
cursor and cause duplicate work. Current terminal acknowledgement and diagnostic
isolation are delivered, but they are not a checkpoint/cursor transaction.

Plan: versioned recoverable publication decision binding run ID, schema/version,
acknowledged counts and intended cursor. Restart must detect disagreement and
reconcile before replay. Do not claim atomic remote-write/local-file commit or
universal exactly-once execution.

### F07 [P1]: Remaining configuration facades hide persistence failure/corruption

[ComponentConfigManager.cs:233](../../DataManagementEngineStandard/ConfigUtil/Managers/ComponentConfigManager.cs#L233)
replaces failed workflow reads with an empty list. Saves at line 243 catch/log
failure and return normally. Subsequent writes can overwrite unreadable evidence;
callers cannot distinguish durable success from failure.

Plan: inventory all facade routes; extend acknowledged coordinated storage;
distinguish absent/corrupt/unsupported/inaccessible state and preserve original
bytes. Qualify connection exports, older readers and key recovery separately.
Preserve installation keys and the root hosting rule; no plaintext downgrade,
automatic key regeneration or destructive key-ring cleanup.

### F08 [P2]: Timer callbacks lack entry-generation and shutdown ownership

[TimerManager.cs:58](../../DataManagementEngineStandard/Editor/Forms/Helpers/TimerManager.cs#L58)
passes a name, and line 94 looks up whichever entry now has that name. A queued
old callback can update/expire/remove its replacement. `CreateTimer` does not reject
post-disposal calls; shutdown does not drain admitted callbacks.
[PluginHealthMonitor.cs:72](../../DataManagementEngineStandard/AssemblyHandler/PluginSystem/PluginHealthMonitor.cs#L72)
starts async timer work without tracked completion/drain.

Plan: generation-bound callbacks, compare-by-entry replacement/removal, explicit
overlap policy, disposal admission rejection and async drain. Define Forms UI
dispatch and observer exception semantics separately from dictionary thread safety.

### F09 [P2]: Import batching and async ETL do not bound provider source allocation

[DataImportManager.cs:570](../../DataManagementEngineStandard/Editor/Importing/DataImportManager.cs#L570)
materializes the entire source. The async ETL source invokes synchronous
[GetEntity at DataSourcePlugin.cs:83](../../DataManagementEngineStandard/Editor/ETL/Engine/BuiltIn/Sources/DataSourcePlugin.cs#L83)
before per-row cancellation. Batch size does not constrain initial allocation or
interrupt that provider call.

Plan: optional paging/streaming/cancellable Models capabilities, explicit legacy
adapter limits and bounded backpressure across source/transform/retry/sink/rejects.
Measure first-write latency, peak memory, cancellation and partial acknowledgements
before choosing bulk optimizations.

### F10 [P2]: Migration diagnostics are process-global and unbounded

[Observability.cs:28](../../DataManagementEngineStandard/Editor/Migration/MigrationManager.Observability.cs#L28)
retains static diagnostic/audit queues keyed by token, with no store/runtime
ownership or retention boundary. Independent hosts using the same token can see
mixed observations; an instance checkpoint cache alone does not fix this.

Plan: scoped diagnostic ownership, defensive snapshots, bounded retention and
explicit durable audit export. Keep low-cardinality operational metrics separate
from per-run state; redact secrets and never let observers reclassify saved results.

### F11 [P2 Release Gate]: Packaging depends on local machine layout

[Engine.csproj:26](../../DataManagementEngineStandard/DataManagementEngine.csproj#L26)
and the Models project copy outside the checkout. Packaging uses a sibling icon;
both projects share per-project `docs.xml` across TFMs. Forms test dependencies
float. No `global.json` or workflow files were found. Setup bin/obj contains
192 tracked files. These are inspected facts, not a clean-checkout pack result.

Plan: opt-in external copy targets, repository-owned package assets, TFM-isolated
generated output, pinned SDK/dependency policy, dedicated generated-index cleanup,
CI suite inventories, clean pack, API comparison and package-only smoke consumers.

## Enhancement Architecture

Keep Models independent of Engine and `DMEEditor` as the application facade.
Add optional provider/storage/result capabilities instead of breaking every
`IDataSource` implementation. Put ownership, publication, storage and lifecycle
coordination into focused services; avoid a wholesale framework rewrite.

Preserve previously qualified UOW/write outcomes, runtime isolation, atomic file
storage, migration v2 intent, strict transformations/quality admission, required
defaults and bounded NFEL, owned metadata/generated-source identity and file reject
recovery. Their broader provider/OS/package gates remain explicit. Reconciliation
must use actual effects and evidence, not elapsed leases or inferred success.

## Ordered Delivery Plan

Owners below are responsibility roles, not assigned people. Relative size is a
planning estimate: S = narrow repair, M = bounded subsystem slice, L = cross-layer
or provider qualification. No calendar commitment is implied.

| Order | Deliverable / owner / size | Existing work IDs | Acceptance gate |
|---|---|---|---|
| 0 | Complete the in-progress migration refactor; migration maintainer; S-M | P3-06/08, P5-01 | All five errors repaired coherently; direct checkpoint route safe; fixtures declare target/capture capability; fresh solution builds on all TFMs before tests |
| 1A | Actual migration ownership/private progress; migration + Models/storage; M | P3-06/08, P1-10/11 | Same/different-token target barriers; two managers/runtimes/processes; pinned root/codec/provider; defensive getters; provider flags/counts; malformed receipts, failed Finish, cancellation/crash and abandoned-state denial |
| 1B | Correct governed compensation; migration + provider; M-L | P3-06/08, P1-10/11 | Actual ownership and fresh state; only confirmed affected steps; inverse dependency order; proper FK/index restore or explicit manual denial; injected partial-DDL faults never report false recovery |
| 2A | Owned run intent and sync promotion/cursor publication; sync + storage; L | P1-10/12, P2-05/06, P3-07/09 | Both-direction policies/context captured before writes; strict gates/hash sensitivity; every save boundary, competing promotion and process restart; disagreement blocks blind replay |
| 2B parallel | Remaining config/security routes; config + host; M-L | P2-05/07 | Route inventory; acknowledged failure propagation; corrupt evidence preserved; competing-process updates; existing-key/export/old-reader recovery fixtures |
| Start now parallel | Provider/helper capability inventory and real fixtures; provider maintainer; L | P3-01/02/03/09, P1-11 | Exact provider/version/OS support; disposable live tests for identities, writes, transactions, SQL, upsert, cancellation and recovery; generated SQL is not runtime proof |
| 3 | Forms/plugin ownership and shutdown; runtime + UI; M | P2-08/10, P4-08 | Barrier tests for replaced/queued callbacks, overlap and disposal; no post-shutdown admission; drained work and documented UI/exception behavior |
| 4 | Bounded execution and scoped telemetry; execution + diagnostics; L | P4-01 through P4-08 | First write before source exhaustion; bounded full-path memory and measured cancellation; bulk counts match actual acknowledgement; scoped/redacted/bounded observations |
| Start now; final gate | Reproducible release and developer experience; release maintainer; M-L | P5-01 through P5-08 | Clean checkout without siblings; pinned inputs; complete OS/TFM suite discovery; clean pack; API baseline; package consumers and compiling examples; warning budget |

Order 0 precedes any claim that this worktree is green. 1A precedes writable
compensation qualification; 1B may develop tests alongside it. Publication depends
on the intent it publishes, not every unrelated parser feature or benchmark.
Configuration, provider inventory and release preparation need not wait for the
entire migration lane. Performance work depends on measured baselines and explicit
provider capabilities, not on adding Task.Run around blocking calls.

## Validation And Completion Rules

Each slice starts with reproducing regressions, makes the smallest contract/service
change, and closes with source-built tests, route documentation and affected skill
updates. Skill metadata validation does not prove behavior. Keep existing broad
phase checkboxes open until all their gates are met.

After order 0, run a forced full build with package/docs generation disabled, then
the matching no-build/no-restore solution test matrix. Record per-suite/per-TFM
discovery, failures/skips and warning counts; retain initial failures and fixes.
Use barriers/child processes for ownership and lifecycle, injected persistence
faults at every publication boundary, disposable live providers for DDL/write
semantics, and package-only consumers for compatibility. Add clean Windows/Unix
jobs only for explicitly supported components and distinguish skipped branches.

Do not infer provider effects from local mock counts, distributed locking from
local sidecars, power-loss durability from process restart, plugin sandboxing from
parsing, or exactly-once behavior from a Completed checkpoint. Host authorization,
canonical target identity, supported provider/OS policy and storage topology need
explicit maintainer decisions before their gates can be closed.
