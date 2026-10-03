# BeepDM Framework Review And Enhancement Plan

Current planning snapshot: [review and enhancement plan](REVIEW-AND-PLAN-CURRENT.md).
Fresh net9.0 Engine build: five errors, 1,987 warnings in the partial migration
manager integration. The prior 5,178-pass matrix below is historical evidence.
Repair compilation, connect ownership to actual provider evidence and captured
checkpoint I/O, and qualify safe compensation before claiming integration complete.
Sync publication/configuration, provider inventory and release gates remain open.
No source edits or checkbox changes were made by this review.

Latest migration ownership backend: optional Models capability and ConfigEditor file
adapter are implemented with live owner handles, durable crash evidence, explicit
reconciliation and immutable archives. 54 new cases per TFM; final Windows matrix
5,178 passes, zero failures/skips, reliability 1,393 per TFM. Earlier timing/fixture
failures are retained in IMPLEMENTATION-LOG.md. MigrationManager still needs actual
execute/resume/compensation admission and defensive checkpoint progress. The full
five-phase scope and P3-06/08 remain open; no backend-only completion claim.

Latest NFEL increment: 99 new cases per TFM; 5,016 passing Windows executions,
zero failures/skips, 1,339 reliability cases per TFM. Complete NFEL-1 grammar,
actual evaluated results, supplied-rule/source admission, policy/lifecycle, typed/
lazy values and bounded owned history are locally qualified. Read Engine Rules/
NFEL.md and IMPLEMENTATION-LOG.md. Other parser/helper/profile/adapter/provider,
P1 sync/config/migration and broader phase gates remain; no phase checkbox changes.

Latest planning-only review: [post-NFEL findings and enhancement plan](REVIEW-AND-PLAN-2026-10-03.md).
This pass forced a build: 6,223 warnings, zero errors. Its Windows matrix: 5,016
passes, zero failures/skips, 1,339 reliability cases per TFM. Historical NFEL
probes are corrected. Next bounded slice is migration target/store ownership and
private progress; sync/config P1 lanes and provider/release preparation proceed
independently. Delivered work and existing IDs are preserved; no implementation/
skill changes or completion checkbox changes in this review.

Latest required configuration increment: **4,719 passing executions**, zero failures/
skips; **1,240 reliability cases per TFM**. Required imports now use editor-owned
registration, single normalization, cache bypass and reported fallback denial.
The subsequent 40 capture cases verify caller-safe implicit refresh, closed bounded
literals/per-row byte copies and both-direction catalogs before provider-opening
validation. A further 43 cases pin roster/priorities through source/provider edits,
isolate row SentData, qualify owned named/column lookup and retain safe sticky
nested failures/selector boundaries. Full policy/plugin immutability remains open.
Another 59 cases qualify shipped outer arity/token routing, nested dates, quoted
keys/operands, GUID aliases/formats and sequence-placeholder denial. Query/
identity/scope-fallback semantics and plugins remain. A further 47 cases
preserve required dot literals/empty strings, reject missing segments and qualify
declared grouping/actual filters; public legacy parsing is unchanged. Another 82
cases qualify bounded shipped required expression/formula ASTs, Boolean-only
conditions, precedence, typed exact comparisons, invariant numbers, Decimal
rounding, lazy branches and pre-read limits/cancellation. Nested overrides retain
pinned selection; unused branches are syntax-checked only. Broader numeric/NFEL/
provider semantics and complete policy/plugin qualification remain.
Another 80 cases qualify exact shipped required query/filter plans, invariant actual
row/named bindings, null-versus-empty results, typed aggregates, safe provider/
enumerator/disposal failures, bounded streaming and cancellation. Scalar bytes
are copied; first-record results remain actual objects. Earlier acknowledgements
remain Partial without replay. Explicit query context, hidden failures, eager
provider allocation, isolation and live-provider translation remain unqualified.
Another 56 cases qualify required identity/scope rules: explicit string email/
application-role context without row spoofing or inferred substitutes, strict
conflicts/types/limits, getter cancellation/failure, actual OS SID/membership and
folders, STA/MTA Downloads ownership and exact environment scopes. SYSTEMPATH is
Machine PATH; ENV(PATH) is Process PATH. Supplied host strings are not authorization
proof, and OS/host context is not immutable or all-platform qualified.
Another 84 cases qualify required dates: Gregorian ISO literals without host-culture
fallback, retained UTC kind/explicit offsets, exact fractional tick/calendar
arithmetic, invariant bounded formats, pre-callback known-date-tree checks and
actual typed nested/property results through the pinned roster. Clock aliases have
explicit kinds and retain dynamic time; no immutable run clock or named-zone DST
policy is claimed. Legacy direct and custom subclass contracts remain separate.
Another 83 cases qualify required configuration: exact declared environment
prefixes, named flat string maps, function/colon/dot aliases, full bounded capture
before key selection, no row/editor inference and connection alias conflict denial.
Missing/invalid explicit sources cannot fall back. Callback/disposal failures and
cancellation deny with safe Partial counts. Capture is per resolution, not run-owned
configuration or automatic host/credential integration; legacy/custom paths remain.
Read DEFAULTS-ADMISSION.md under Engine Editor/Importing and IMPLEMENTATION-LOG.md.
Remaining B1 intent/adapters/plugin/provider gates and the full five-phase scope
are unchanged; no broad checkbox is completed. Earlier planning evidence follows.

Previous planning-only review: [2026-10-03 refreshed source findings and delivery plan](REVIEW-REFRESH-2026-10-03.md).
Source-built Windows matrix at that review: **2,895 passes**, zero failures/skips,
**632 reliability cases per TFM**. Required catalog admission has nine passing
cases per TFM. Finish B1 resolver/default ownership and adapters, then B2 sync
promotion/cursor agreement; C/D configuration/migration and provider/release
inventory run in parallel. No runtime edits or phase completion in this review.
The following paragraph records the preceding recovery implementation evidence.

Earlier planning-only review: [source findings and delivery plan](REVIEW-2026-10-03.md).
That review's red baseline is corrected by acknowledged file reject recovery:
latest Windows solution matrix **2,868 passed**, zero failures/skips, including
**623 reliability cases per TFM**. Actual import/sync row writes, operator claims,
blocked uncertainty and process reload are verified. Native/provider-run recovery,
defaults/resolvers, promotion/cursor agreement and wider gates remain. No existing
phase checkbox is changed; see IMPLEMENTATION-LOG.md and the recovery contract.

Review date: 2026-10-02. Status: implementation in progress.
Original review baseline: commit `2a837952597b60c8c90821187e16dd8e9913508a`.
Source line anchors below describe that baseline; see the execution log for fixes.

## Current Review

[Current-state review and delivery plan](CURRENT-REVIEW.md) is the entry point
for the 2026-10-02 follow-up. It reviews the dirty worktree, distinguishes existing
fixes from remaining risks, and defines the next implementation order. The latest
planning-only refresh updates the [prioritized enhancement roadmap](ENHANCEMENT-ROADMAP.md)
with current findings, ordered PR boundaries, dependencies and acceptance evidence. The subsequent
connection increment fixes observer isolation and missing-key overwrite, verifies
protected fallback/catalog paths and versions catalog packages. That earlier matrix
passes 2,868 executions on Windows, including 160 migration and 623 reliability
cases per configured TFM. Typed sync storage, mandatory terminal acknowledgement,
persisted identity validation and observer outcome isolation are verified local
slices. Required record/threshold behavior and failed-run counts are also verified;
file operator reject recovery is now locally verified; recoverable promotion,
durable cursor agreement and native/provider-run recovery remain.
An earlier planning-only source refresh expands R14 to include disconnected
per-record rules and adds R15 for transformation fallback after errors. Deliver
actual record admission and strict required-stage outcomes before promotion/cursor
work; see roadmap slices B1/B2. That review was planning-only. Subsequent strict
transformation implementation reproduces six defects and adds 29 cases per TFM;
core failures no longer write input or trigger blind sync replay. Default/resolver
qualification, native/provider-run recovery/complete intent and wider R16 mapping
gates remain open; actual record-quality admission is now locally verified.
Broader credential/host qualification remains. Earlier runtime/test compilation findings
are fixed and are no longer listed as current defects. Remaining priorities are
acknowledged/protected persistence, remaining migration recovery, lifecycle
closure, provider/rule conformance and bounded execution. Reproducible build/CI
work starts in parallel. See the execution log for commands and evidence limits.
An earlier planning-only refresh expands R16 rename/key validation, adds R17
schema-insensitive generated type caching and R18 recursive field cloning, and
orders safe metadata capture before actual mapped quality admission. The roadmap's
B1 breakdown specifies change boundaries, policy decisions and exit evidence.
It changes planning documents only and inspects the retained 2,136-execution log;
no new runtime verification or phase completion is claimed.
Subsequent R18 implementation corrects recursive field cloning and adds Models
EntityMetadataSnapshot.Capture; 27 cases per TFM verify copies/aliases/observers
and rejection limits. This is a tested prerequisite, not automatic run capture.
R17's subsequent core fix adds 26 cases per TFM and replaces pre-seeded strict
import targets with actual compilation. Source-sensitive DM/factory/compiler
identity, exact type selection and bounded completed-cache races are verified.
Public bare-name cache overrides intentionally no longer govern generation;
package consumers and loaded assembly lifecycle remain open. Subsequent R16
existing-target binding validates actual forward/reverse payloads, renames and
required coverage before either writes. Missing mapped targets reject; mapped
creation and wider gates remain. Subsequent R14 record admission adds 51 cases per
TFM for ordinary imports and mapped forward/reverse sync. Captured required/advisory
policy, strict Boolean outputs, quarantine acknowledgements and retained counts
are verified. Subsequent attempt thresholds and failed-run publication add 58 cases
per TFM, including strict required/advisory decisions, real/empty denominators,
failure-save outcomes and durable process reload. Durable reject/operator recovery,
complete intent and cursor/promotion agreement remain.
The original baseline and findings below are historical, not a list of all
currently unfixed defects.

## Implementation Status

[Execution log](IMPLEMENTATION-LOG.md) records verified changes and remaining gates.
Phase 1 correctness work is largely implemented, but live-provider recovery and
idempotent manual resume are not yet verified. Phase 2 persistence is in progress;
deferred DI registration, editor-owned lifecycle coordination and close/removal
results are implemented and tested. Broader plugin/editor ownership, timer shutdown,
broader persistence acknowledgement and all-route secret auditing remain open.
Built-in connection acknowledgement/protection and post-commit observers are now
covered by 102 new cases per TFM; package consumers/Unix/key policy remain open. Migration
history/checkpoint acknowledgement, corruption preservation and process reload
are implemented; sync storage paths and lease-held sections avoid async scheduling
dependence, with a bounded-pool child regression. Phase 3 now has
captured schema/target/policy intent, versioned hashes and fail-closed approval/
reload behavior; P3-04/05 are verified on the local Windows matrix. Concurrent
tokens/owned progress and provider-confirmed partial DDL recovery keep P3-06 open. Phases 4-5
remain required.
Studio and the new three-TFM reliability test project are now in the solution.
Latest full-solution verification: 5,178 executions, zero failures/skips on
Windows, SDK 10.0.401. See CURRENT-REVIEW.md for evidence limits.
Current edits have local all-TFM evidence (1,393 reliability cases each);
broader durable cursor/recovery/platform gates remain open. The original review
was planning-only; subsequent implementation is recorded in the execution log.

## Scope And Architecture

This review sampled framework architecture, dependency injection, persistence,
UnitOfWork, importing, ETL, BeepSync, migration, tests, and packaging. It is not a
complete provider audit or security assessment. Findings below distinguish
source inspection from behavior demonstrated by tests.

Keep the existing architecture: DataManagementModelsStandard owns public
contracts and models; DataManagementEngineStandard owns implementations. Both
currently target net8.0, net9.0, and net10.0 and declare package version 3.1.1.
DMEEditor remains the facade over datasource, configuration, assembly, and
editor services. Strengthen its guarantees before adding more facade features.

## Historical Review Baseline

| Test project | Passed | Failed | Runtime target |
|---|---:|---:|---|
| MigrationManagerTests | 103 | 0 | net9.0 |
| SetupWizardTests | 231 | 1 | net9.0 |
| FormsManager.Tests | 221 | 0 | net8.0 |
| StudioRepositoryTests | 66 | 0 | net9.0 |
| Total | 621 | 1 | Mixed |

Commands used:

```powershell
dotnet test BeepDM.sln --no-restore -p:GeneratePackageOnBuild=false -v:q
dotnet test tests/SetupWizardTests/SetupWizardTests.csproj --no-build --no-restore --filter "FullyQualifiedName~ApplyAppUpdate_FullInstall_MaterializesFlips_AndRecordsVersion" -v:n
dotnet test tests/StudioRepositoryTests/StudioRepositoryTests.csproj -p:GeneratePackageOnBuild=false -v:q
```

The isolated full-install update test reproduces the failure: expected Errors.Ok,
received Errors.Failed at
[AppUpdateServiceComposeTests.cs:89](../../tests/SetupWizardTests/AppUpdateServiceComposeTests.cs#L89).
Its transport fixture uses relative feed/manifest/blob keys, whereas the service
resolves URLs against the feed. Fixture drift is a hypothesis, not a confirmed
production defect; inspect the returned message and requested URLs first.

At the original baseline, Studio required a separate invocation because the
project was not in BeepDM.sln; it is now included. No checked-in CI workflow was
found. The original tests exercised selected
targets, not runtime behavior on all three TFMs. No live datasource transaction,
cross-process persistence, or standalone NuGet consumer was verified here.

## Original Findings By Priority

F01-F03 have substantial fixes in the worktree; live-provider and manual-recovery
gates remain. F06 now has unsupported-mode rejection, but durable typed cursor
work remains. F04/F05/F07/F09 remain open; F08 has partial test-discovery progress.
See CURRENT-REVIEW.md for current source anchors and additional credential/close
findings rather than applying the historical descriptions as current behavior.

### F01 - P1: UnitOfWork accepts changes before transaction commit

[UnitofWork.Core.Extensions.cs:330](../../DataManagementEngineStandard/Editor/UOW/UnitofWork.Core.Extensions.cs#L330)
calls CommitAllAsync before committing the database transaction. Successful
individual operations clear dirty state and delete bookkeeping in
[ObservableBindingList.Tracking.cs:644](../../DataManagementModelsStandard/ObservableBindingList/ObservableBindingList.Tracking.cs#L644).
If another operation fails and the transaction rolls back, the earlier changes
are already accepted in memory. The commit-failure return path also needs
transaction cleanup. Retries can omit rolled-back changes.

Defer acceptance until a confirmed commit, or retain sufficient snapshots to
restore all state after rollback. Test inserts with generated keys, updates,
deletes, partial operation failures, commit failures, and edits during save.
This finding is source-backed; no new rollback regression test was run.

### F02 - P1: Import failure and cancellation can become sync success

[DataImportBatchHelper.cs:113](../../DataManagementEngineStandard/Editor/Importing/Helpers/DataImportBatchHelper.cs#L113)
increments recordsProcessed even when InsertEntity returns Failed. The batch
can consequently return Ok despite all attempted inserts failing.
[DataImportManager.cs:595](../../DataManagementEngineStandard/Editor/Importing/DataImportManager.cs#L595)
counts batch size rather than acknowledged writes; its cancellation handler
returns Errors.Ok at line 621.
[BeepSyncManager.Sync.cs:207](../../DataManagementEngineStandard/Editor/BeepSync/BeepSyncManager.Sync.cs#L207)
only stops on Failed and later performs success/watermark bookkeeping.

Introduce explicit attempted, succeeded, failed, skipped, and cancelled results.
Advance checkpoints only for acknowledged, committed work under an explicit
partial-success policy. Ensure subscriptions are removed in finally blocks.

### F03 - P1: ETL sink discards write results and overstates completion

[DataSinkPlugin.cs:80](../../DataManagementEngineStandard/Editor/ETL/Engine/BuiltIn/Sinks/DataSinkPlugin.cs#L80)
ignores insert/update results and increments TotalRecordsWritten. Upsert falls
back to insertion for any failed update, including failures unrelated to a
missing record. CommitAsync and RollbackAsync do not manage a transaction.

Propagate failed acknowledgements to the pipeline retry/error policy, distinguish
not-found from other failures, and use explicit provider transaction/upsert
capabilities. Do not imply atomic rollback for providers that cannot provide it.

### F04 - P1: Scoped registration returns a process-cached service

[RegisterBeepinServiceCollection.cs:626](../../DataManagementEngineStandard/Services/RegisterBeepinServiceCollection.cs#L626)
uses static initialization/cache state. Scoped and transient registration
factories at line 700 return the captured beepService instance instead of a new
scope-owned instance. Independent containers can inherit the first runtime and
configuration. DMEEditor's datasource list also has unsynchronized check/create
paths in
[DMEEditor.cs:299](../../DataManagementEngineStandard/Editor/DM/DMEEditor.cs#L299).

Make ownership container/scope-specific, preserve explicitly requested singleton
behavior, and separate immutable registries from mutable connections and editor
state. Test two containers, concurrent scopes, and disposal ownership.

### F05 - P1: JSON persistence can truncate before serialization succeeds

[JsonLoader.cs:174](../../DataManagementEngineStandard/JsonLoaderService/JsonLoader.cs#L174)
opens the destination for writing before serialization. A later exception can
leave a truncated file, and the internal catch only logs to Console. The config
fallback calls this loader in
[DataConnectionManager.cs:303](../../DataManagementEngineStandard/ConfigUtil/Managers/DataConnectionManager.cs#L303).
Watermark and import error stores also need cross-instance read/write review.

Serialize first, write a temporary file in the destination directory, and use an
atomic replacement strategy with observable failure. Reuse proven primitives
from the Studio repository where applicable, with platform-specific tests.
Audit every credential persistence path for protection consistency; this review
does not establish that every path leaks plaintext secrets.

### F06 - P1: CDC implementation does not honor advertised cursor modes

[WatermarkPolicy.cs:16](../../DataManagementModelsStandard/Editor/BeepSync/WatermarkPolicy.cs#L16)
advertises timestamp, sequence, and composite-key modes.
[BeepSyncManager.Cdc.cs:16](../../DataManagementEngineStandard/Editor/BeepSync/BeepSyncManager.Cdc.cs#L16)
builds a scalar filter and sets the new cursor to the window end DateTime without
branching on mode. A sequence cursor can therefore become a timestamp. Stored
cursor string conversion also loses type information across restart.

Implement versioned, typed cursors and mode-specific comparisons. Until supported,
reject sequence/composite modes during preflight. Define ordering, tie-breakers,
late-arrival handling, and cursor advancement against committed records.

### F07 - P1: Migration plan hash omits the create-table schema

[MigrationManager.Planning.cs:436](../../DataManagementEngineStandard/Editor/Migration/MigrationManager.Planning.cs#L436)
hashes operation summaries rather than complete desired schema snapshots.
[ExecutionCheckpointResumeTests.cs:86](../../tests/MigrationManagerTests/ExecutionCheckpointResumeTests.cs#L86)
explicitly asserts that adding a create-entity column does not change PlanHash;
this characterization test passes. Changed desired schema can retain the same
checkpoint/approval identity.

Hash a canonical, versioned schema and execution-context snapshot, persist the
snapshot, and execute/resume that approved plan instead of re-deriving its intent.
Replace the characterization test when fixing this behavior. Persisted checkpoint
reload and opt-in destructive column operations already exist; do not re-plan
them as missing features.

### F08 - P2: Release verification is incomplete and environment-dependent

Fix or explain the reproducible setup failure before calling the baseline green.
Add Studio to normal test discovery and CI. There are 192 tracked entries under
SetupWizardTests bin/obj, so test runs modify tracked generated output.
Project packaging includes external icon paths, shared docs output, and copy
targets to directories outside the checkout. Forms test dependencies use floating
versions. These need clean-checkout and package-consumer verification, not just
a successful local build.

### F09 - P2: Batch APIs do not bound import memory or provider latency

[DataImportManager.cs:558](../../DataManagementEngineStandard/Editor/Importing/DataImportManager.cs#L558)
materializes the entire source, then materializes its batches.
[DataSourcePlugin.cs:72](../../DataManagementEngineStandard/Editor/ETL/Engine/BuiltIn/Sources/DataSourcePlugin.cs#L72)
uses synchronous GetEntity inside the async source path. Core legacy datasource
APIs do not provide a cancellation-token streaming write/read contract.

Add optional streaming/paging/async capabilities without breaking IDataSource
implementations. Apply backpressure and bounded batches end-to-end. Document
that cancellation cannot interrupt an already-running legacy synchronous call.

## Delivery Order

| Phase | Focus | Dependencies | Status |
|---|---|---|---|
| [1](PHASE-01-Write-Correctness.md) | Acknowledged writes, rollback, cancellation, CDC safeguards | None | In progress |
| [2](PHASE-02-Runtime-And-Persistence.md) | Runtime isolation, atomic persistence, typed stored cursors | Phase 1 result semantics | In progress |
| [3](PHASE-03-Provider-And-Migration-Contracts.md) | Provider conformance, canonical migration plans | Phases 1-2 | In progress |
| [4](PHASE-04-Performance-And-Observability.md) | Streaming, bounded memory, diagnostics | Phase 3 capabilities | Proposed |
| [5](PHASE-05-Release-And-Developer-Experience.md) | Reproducible releases, API checks, samples and skills | All phases for release gate | Proposed |

For the current partially implemented worktree, use the delivery slices in
CURRENT-REVIEW.md rather than restarting Phase 1. Capture/hash/payload delivery
has already landed, including acknowledged migration-history updates and
corruption preservation; do not restart it as missing work. Next qualify migration
target/store ownership and private progress (R02). Sync durable cursor semantics,
required gates and recoverable promotion (remaining R12-R16), and remaining P2-05
configuration facades/security are independent P1 lanes. Preserve tested NFEL and
required-default cores. Lifecycle/provider recovery follow their ownership and
capability contracts; provider inventory and Phase 5 build/CI start in parallel.
Release gates remain last. Estimate dates only after regression tests, workload
measurements and provider inventory; do not invent a calendar schedule.

## Compatibility And Completion Gates

- Prefer additive optional provider contracts and typed result models; preserve
  public legacy APIs with adapters and documented error mapping.
- Keep public models/contracts in Models and behavior in Engine. Do not add
  cross-layer implementation dependencies.
- Version persisted plans and cursors, with explicit rejection/migration behavior
  for old formats. Do not silently resume against incompatible plan identities.
- Reconcile [existing core plans](../MASTER-TODO-TRACKER.md),
  [migration plans](../migration/MASTER-MIGRATION-TRACKER.md), and
  [setup plans](../setup/MASTER-SETUP-TRACKER.md) against current code before
  selecting overlapping work.
- Each phase requires regression tests, documented semantics, and a clean source
  diff. No completion checkbox should be inferred from old roadmap status.
- A release requires all baseline tests green, relevant provider integration
  tests, all-TFM builds, reproducible pack, and standalone package consumers.

The original review changed only planning documents. Implementation now changes
runtime code and updates affected repository and installed Codex skills. Continue
updating skills when implementation contracts change; do not describe unfinished
phases as available or verified functionality.
