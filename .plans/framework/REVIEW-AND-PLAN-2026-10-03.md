# BeepDM Framework Review And Enhancement Plan

This is the current planning review, refreshed after the NFEL implementation.
The five historical NFEL probes below are corrected: 99 regression cases per TFM
qualify the bounded NFEL-1 core. Preserve that delivered work; do not implement it
again. Read [NFEL.md](../../DataManagementEngineStandard/Rules/NFEL.md) and
[implementation evidence](IMPLEMENTATION-LOG.md). Other dialects, package adapters
and complete provider/plugin semantics remain qualification work.

Reviewed: 2026-10-03, after the required configuration and NFEL increments.
Scope: targeted source review of Engine/Models, import/ETL/sync, migration,
configuration, rules, Forms/plugin lifecycle, tests and package delivery.
Baseline: dirty worktree at HEAD `2a837952597b60c8c90821187e16dd8e9913508a`.
Existing changes are preserved. This pass changes planning documents only;
it does not implement fixes, change skills or complete any phase work item.

This is the latest planning snapshot. It supersedes current-state claims and
remaining-work ordering in [the preceding refresh](REVIEW-REFRESH-2026-10-03.md),
not the evidence in [the implementation log](IMPLEMENTATION-LOG.md).
The [roadmap](ENHANCEMENT-ROADMAP.md) and existing P1-P5 IDs remain the delivery
structure; the sections below are change boundaries, not another checklist.

## Findings In Priority Order

### P1: Sync promotion admits invalid gate results and publishes before persistence

At [SchemaGovernance.cs:62](../../DataManagementEngineStandard/Editor/BeepSync/BeepSyncManager.SchemaGovernance.cs#L62),
non-Boolean results pass unless their string is exactly `false`; null passes.
The gate is skipped when its engine or registration is absent. At
[line 97](../../DataManagementEngineStandard/Editor/BeepSync/BeepSyncManager.SchemaGovernance.cs#L97),
the caller's version/mapping state changes before two independent saves.
Either save failure can leave memory, schema and version artifact inconsistent.
The [fingerprint](../../DataManagementEngineStandard/Editor/Schema/SchemaFingerprinter.cs#L17)
omits complete key/filter/policy intent and returns random identity on failure.
An approval therefore does not identify all execution-affecting inputs.

Plan: explicit Boolean results for required gates, separate advisory policy,
complete governed intent capture/hash, staged acknowledged publication and restart
reconciliation. Missing required dependencies deny; preserve original state/evidence
when staging fails. Evidence is source inspection, not new injected save failures.
Existing work: R13, P2-05/06, P3-07/09.

### P1: Completed checkpoints and durable cursors can disagree

[Sync.cs:399](../../DataManagementEngineStandard/Editor/BeepSync/BeepSyncManager.Sync.cs#L399)
persists Completed before updating the watermark at
[line 410](../../DataManagementEngineStandard/Editor/BeepSync/BeepSyncManager.Sync.cs#L410).
Those cursor changes still require explicit schema save. A crash can leave completed
work with an older durable cursor and admit duplicate processing unless reconciled.

Mandatory terminal acknowledgement and diagnostic isolation are already fixed.
Add a versioned recoverable publication decision binding run identity, acknowledged
counts, intended cursor and schema/version. Restart detects disagreement instead
of blindly replaying. Remote writes are not atomically committed with local files;
do not promise universal exactly-once behavior. Evidence is source inspection.
Existing work: remaining R12, P1-10/12, P2-06.

### P1: Migration admission and public progress lack exclusive ownership

[ExecutionOrchestration.cs:130](../../DataManagementEngineStandard/Editor/Migration/MigrationManager.ExecutionOrchestration.cs#L130)
checks Running work before execution mutates shared checkpoints. GetOrAdd coordinates
insertion, not exclusive execution. Competing callers can pass the admission check;
different tokens for one target have no explicit target-level serialization. The
[getter](../../DataManagementEngineStandard/Editor/Migration/MigrationManager.ExecutionOrchestration.cs#L571)
returns the cached mutable checkpoint, allowing external progress mutation.

Plan: one owner per store/target/plan/token conflict domain; durable claims for shared
stores; conservative abandoned/uncertain-work recovery; owned progress views.
Reconcile expected intermediate provider state after partial DDL before replay.
Do not hold locks across arbitrary callbacks or treat lease expiration as proof
no DDL occurred. Approved intent and acknowledged history are delivered boundaries.
Evidence is source inspection; new concurrency/provider faults were not injected.
Existing work: R02, P3-06/08, P1-10/11.

### P1: Remaining configuration facades hide save failure and corruption

[ComponentConfigManager.cs:243](../../DataManagementEngineStandard/ConfigUtil/Managers/ComponentConfigManager.cs#L243)
logs failed workflow saves and returns normally. Its
[read failure](../../DataManagementEngineStandard/ConfigUtil/Managers/ComponentConfigManager.cs#L235)
replaces state with an empty list; reports/definitions have similar paths.
Callers cannot distinguish saved state from failure or fresh state from unreadable
evidence, risking loss when later operations overwrite it.

Plan: inventory readers/writers; extend acknowledged coordinated persistence
through remaining public facades. Distinguish absent, corrupt, unsupported and
inaccessible state; preserve unreadable bytes/backups. Qualify credential exports,
old readers and host keys separately. Connection/history/sync fixes are not universal
coverage. Preserve existing installation keys and hosting rules; no automatic key
regeneration or plaintext downgrade. Evidence is source inspection.
Existing work: R09/R03, P2-05/07.

### Historical P2: NFEL Defects Corrected; Broader Qualification Remains

The preceding planning review reproduced match-subset admission, signed-number
subtraction, undecoded strings, ignored ternary and malformed nesting in the old
NFEL implementation. Its shared legacy evaluator and unbounded mutable history
were not suitable NFEL admission boundaries. These are historical observations,
not findings against the current bounded parser and private execution tree.

These probes ran against freshly rebuilt net10.0 Engine/Models DLLs through the
public NfelParser and RuleEngine.EvaluateExpression APIs:

| Expression | Parse result | Execution result |
|---|---|---|
| `1 + 2 @` | Success; unmatched `@` absent from tokens | Returns `3` |
| `1-2` | Success; operands `1` and `-2`, no Minus | RuleEvaluationException |
| `true ? 1 : 2` | Success; `?` and `:` are Unknown | RuleEvaluationException |
| `name == 'Alice'` with `name = "Alice"` | Success; literal retains quotes | Returns false |
| `)(` | Success; aggregate depth is zero | RuleEvaluationException |

The subsequent [NFEL admission tests](../../tests/FrameworkReliabilityTests/NfelAdmissionTests.cs)
cover the five probes and source/token mutation, grammar, typed evaluation,
policy/lifecycle, limits, lazy callbacks and bounded defensive history on all TFMs.
Remaining plan: qualify other parser profiles and wrappers/package consumers,
complete numeric workloads and diagnostic routes; preserve existing enum ordinals
and declared transport behavior. Parsing is not a plugin sandbox.
Existing work: R08, P3-09, P4-07, P5-05/07.

### P2: Timer replacement and shutdown lack callback generation ownership

[TimerManager.cs:58](../../DataManagementEngineStandard/Editor/Forms/Helpers/TimerManager.cs#L58)
passes only a name. A queued old callback can resolve a replacement at
[line 94](../../DataManagementEngineStandard/Editor/Forms/Helpers/TimerManager.cs#L94)
and increment/expire/remove it. CreateTimer accepts calls after disposal; Dispose
does not drain admitted callbacks. The plugin monitor also starts
[async timer work](../../DataManagementEngineStandard/AssemblyHandler/PluginSystem/PluginHealthMonitor.cs#L72)
without explicit task drain.

Plan: generation-bound callbacks, compare-by-entry removal/replacement, explicit
overlap policy, shutdown admission rejection and observable async drain. Define
UI dispatch/exception handling separately from collection thread safety. Use barrier
tests rather than timing-only races. Evidence is source inspection.
Existing work: R07, P2-08/10, P4-08.

### P2: Batch size does not bound source memory or interrupt provider I/O

[DataImportManager.cs:570](../../DataManagementEngineStandard/Editor/Importing/DataImportManager.cs#L570)
materializes all source rows before batching. The ETL async enumerable invokes
[synchronous GetEntity](../../DataManagementEngineStandard/Editor/ETL/Engine/BuiltIn/Sources/DataSourcePlugin.cs#L83)
before checking cancellation between rows. Async syntax does not establish streaming
or interruptible provider calls.

Plan: optional Models paging/streaming/cancellation capabilities; compatible legacy
adapters with explicit limits; bounded backpressure through source, transforms,
retry, sinks and rejects. Bulk operations require per-record/partial acknowledgements.
Measure memory and cancellation baselines before optimizing. Evidence is source
inspection, not a new benchmark. Existing work: R05, P3-01/02/03, P4-01/02/03/04.

### P2 Release Gate: Delivery still depends on local machine layout

The [Engine project](../../DataManagementEngineStandard/DataManagementEngine.csproj#L26)
copies outputs outside the checkout and
[packs an external icon](../../DataManagementEngineStandard/DataManagementEngine.csproj#L48).
Both package projects share per-project docs.xml across TFMs.
[Forms dependencies](../../DataManagementEngineStandard/Editor/Forms.Tests/FormsManager.Tests.csproj#L13)
float. No global.json or workflow files were found; the workflows directory exists.
Setup bin/obj has 192 tracked files and rebuilding changes tracked artifacts.

Plan: opt-in external copies, repository-owned assets, isolated docs outputs,
pinned SDK/dependency policy, dedicated generated-file index cleanup, OS/TFM CI
inventories, clean pack, API checks and package-only consumers. The 6,223 warnings
are a measured baseline to reduce, not permission to suppress new warnings.
Evidence is project/index inspection and the fresh build, not clean isolated pack.
Existing work: R06, P5-01 through P5-08.

## Architecture Direction

Keep Models independent of Engine. Prefer optional capabilities/result contracts
over breaking all IDataSource implementations or correcting public legacy spellings.
Keep DMEEditor as the facade; place new ownership/storage/parsing logic in focused
services. Avoid a framework rewrite or cosmetic layer split.

Preserve delivered UOW/write acknowledgement, runtime isolation, atomic storage,
migration identity, strict transformation/quality admission, owned metadata,
generated-source identity and file reject recovery. Required catalog/roster and
expression/query/identity/date/configuration slices now have concrete tests; the
older resolver-fallback finding is not an unimplemented current core defect.
Full owned policies/plugin internals, immutable host configuration/identity/time,
mapped creation and live-provider semantics still require qualification.

## Ordered Delivery Plan

Owners are responsibility roles, not staffing assumptions. No calendar estimates
before provider inventory and support-policy decisions. Existing phase IDs remain
the completion checklist.

| Lane / order | Deliverable and owner | Existing IDs | Merge gate |
|---|---|---|---|
| D: first P1 ownership slice | Migration claims and private progress; migration + Models maintainers | P3-06/08, P1-10/11 | Canonical target/store conflict domain; same/different-token barriers; two runtimes/processes; mutation isolation; abandoned claims; provider-confirmed partial-DDL reconciliation |
| B1: remaining intent | Owned run policies/context and adapters; import/sync + host integrator | P1-10/12, P3-07/09 | Both directions capture before writes; edits/getters/plugins cannot silently change approved intent; typed denial and persisted evidence compatibility |
| B2: governed publication | Staged promotion and cursor agreement; sync/storage maintainer | P2-05/06, P3-07 | Full hash sensitivity; strict gates; every save fault, cancellation, concurrent promotion and restart; disagreement denies blind replay |
| C: P1 parallel lane | Remaining config/security routes; config + host integrator | P2-05/07 | Route inventory; serializer/read/replace failure; corruption retained; competing-process updates; key/export/reader recovery fixtures |
| B1: qualification, not a core rewrite | Other rule profiles, numeric/host context and package adapters; rules + host maintainers | P3-09, P4-07, P5-05/07 | Preserve 99 NFEL cases/TFM; actual consumer dispatch, versioned reader behavior, diagnostic redaction and measured retention/lifetime |
| Start now in parallel | Provider/helper inventory and fixtures; provider maintainer | P3-01/02/03/09, P1-11 | Exact implementation/version/OS; generated SQL separated from tested runtime capabilities; keys, transactions, upsert, cancellation/recovery |
| After ownership contracts | Forms/plugin lifecycle and broader recovery; runtime/Forms maintainers | P2-08/10, P1-10/12 | Old callbacks cannot affect replacements; shutdown rejects/drains; UI/overlap policy; uncertain work stays blocked |
| After capabilities/baselines | Streaming/bulk/telemetry/bounds; execution maintainer | P4-01 through P4-08 | First write before exhaustion; full-path bounded buffers; measured memory/cancellation; redacted telemetry equals acknowledged results |
| Start now; final release gate | SDK/CI/pack/API/docs; release maintainer | P5-01 through P5-08 | Clean checkout without siblings; complete suite inventory; supported OS/TFM/provider jobs; package consumers; compiling samples; API/warning baselines |

B2 depends on the governed inputs used by publication, not every unrelated rule
feature or provider benchmark. Do not delay independent P1 migration/configuration
fixes until all B1 qualification is finished. Provider/release inventory starts now.

### First Migration Change Boundary

Subsequent implementation delivers the optional Models capability, ConfigEditor
delegation and FileMigrationExecutionOwnership backend from steps 3-5. Read
[the ownership contract](../../DataManagementEngineStandard/Editor/Migration/EXECUTION-OWNERSHIP.md).
It does not yet integrate MigrationManager admission or defensive progress;
manager/process/provider regressions and broader P3-06/08 remain open. Continue
with actual execute/resume/compensation integration, not another local-lock facade.

1. Reproduce competing same-token executions, different tokens for the same target,
   token reuse across independent runtimes/stores and public checkpoint mutation.
   Use barriers and inspect actual provider payloads, not timing-only assertions.
2. Define the ownership key separately from the approval fingerprint. Runtime GUIDs,
   connection aliases and credentials must not allow two owners of the same physical
   target. Require an explicit host target identity when endpoint normalization is
   ambiguous. Storage roots/process boundaries must be part of the documented scope.
3. Add an optional Models ownership capability and a built-in shared-store adapter;
   legacy IConfigEditor implementations must not silently claim durable ownership.
   Coordinate admission before checkpoint mutation and provider calls. A short file
   mutation lease or process-local lock alone does not own remote DDL execution.
4. Persist claim identity and expose owned progress snapshots. Keep exclusive
   admission through provider work, but do not hold internal collection/mutation
   locks across arbitrary callbacks. Make completion/release failures observable.
5. Crash after each claim/checkpoint/provider acknowledgement boundary. Abandoned
   or uncertain claims block replay until an explicit operator/provider reconciliation
   decision; lease expiry is not evidence that DDL did not occur. Include resume,
   compensation and imperative route inventory so alternate paths are not overlooked.
6. Run all TFMs plus two-process tests; update migration contracts and the canonical,
   harness and directly installed skills only after the implementation is qualified.

### Sync And Configuration Acceptance

For B2, capture the complete mapping/key/filter/DQ/rule/context intent used by
publication. Required promotion gates accept only Boolean true; missing required
engine/registration or invalid output denies before state changes. Separate
advisory behavior explicitly. Stage immutable version/schema artifacts, publish
an acknowledged versioned decision and reconcile it on restart. Bind run identity,
acknowledged counts and intended cursor to that decision; never infer atomicity
between local files and remote provider writes. Inject failure/cancellation at
each durable boundary, competing promotion and crash between Completed and cursor
publication. Failed staging must leave caller state and prior bytes intact.

For C, inventory each ConfigEditor/manager/environment read, save, import/export
and fallback entry point. Reuse the existing acknowledged persistence primitives;
distinguish absent, corrupt, unsupported and inaccessible state. Inject serializer,
read, replacement and competing-process faults through the public facade. Retain
unreadable evidence and test legacy adapters/reader versions rather than treating
log-only failures as success. Existing host keys are preserved; no automatic key
regeneration, key-ring deletion or plaintext fallback.

### Decisions Before Merge

| Decision | Responsibility | Required result |
|---|---|---|
| Shared target/store identity and operator reconciliation authority | Migration/storage + host integrator | Explicit conflict domain and restart state machine; approval hashes are not authorization |
| Required/advisory gates and complete run intent | Sync/import + rules maintainers | Immutable governed inputs, typed denial and versioned compatibility policy |
| Supported provider/OS/TFM versions | Provider + release maintainers | Tested capability matrix, not inferred SQL generation support |
| Secret ownership, exports and old readers | Security/config + host integrator | Preserved keys and safe failure/recovery fixtures |
| Timer dispatch/overlap and drain | Forms/plugin + host integrator | Generation ownership, shutdown admission and callback completion semantics |

Merge each bounded slice with red-to-green regressions, source-built matrix results,
contract/skill updates and API/persisted-format decisions. Do not close a broad
P1-P5 item from one fixture, claim exactly-once delivery, or schedule dates before
provider inventory and representative workload measurements.

## Fresh Verification And Limits

Windows, SDK 10.0.401. This planning pass forced a rebuild: exit 0, 6,223 warnings,
zero errors. Full solution tests: exit 0, nine successful runs, 5,016 passes,
no failures/skips. Green existing tests do not qualify the source-reviewed races.

| Suite | Executions | Targets |
|---|---:|---|
| Forms | 221 | net8.0 |
| Setup | 232 | net9.0 |
| Studio | 66 | net9.0 |
| Migration | 480 | 160 each on net8.0/net9.0/net10.0 |
| Framework reliability | 4,017 | 1,339 each on net8.0/net9.0/net10.0 |

```powershell
dotnet build BeepDM.sln --no-incremental -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q
dotnet test BeepDM.sln --no-build --no-restore -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
```

Current logs: `$env:TEMP/BeepDM-framework-review-current-build.log` and
`$env:TEMP/BeepDM-framework-review-current-tests.log`. The earlier planning-refresh
logs retain the historical NFEL probe baseline; the current suites include the
99-per-TFM NFEL regressions. No new race/fault regression or benchmark was added
in this planning pass.

Docs generation was disabled to preserve existing dirty docs.xml. Tracked Setup
bin/obj was clean before verification; only this run's generated changes are
restored. No runtime/test/skill source changes or new phase completion.

No live external providers, Unix, clean isolated pack, package-only consumers,
credential-route audit or benchmarks were verified. Source-reviewed races/storage
gaps need deterministic regressions during implementation. Approval identity is
not authorization; host ownership remains explicit. The five-phase scope is open.
