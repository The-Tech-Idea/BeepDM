# BeepDM Enhancement Roadmap

## Current Planning Gate: 2026-10-03

[Current source review and delivery plan](REVIEW-AND-PLAN-CURRENT.md) records five
errors/1,987 warnings from a fresh net9.0 Engine build. Finish the in-progress
migration cache/audit refactor before behavioral qualification. Next integrate
actual provider evidence and frozen checkpoint storage, then correct owned
compensation, affected-step selection and inverse FK/index recovery. Sync intent/
promotion/cursor agreement, remaining config facades and provider/release inventory
continue as independent lanes. Prior green matrices remain historical; existing
P1-P5 IDs and checkboxes are unchanged. This refresh is planning only.

## Latest Migration Backend Gate: 2026-10-03

Optional Models/ConfigEditor/FileMigrationExecutionOwnership admission storage is
implemented with live-owner handles, durable abandoned claims, explicit reconciliation
and immutable released evidence. Final full Windows matrix: 5,178 passes, zero
failures/skips, 54 new cases and 1,393 reliability cases per TFM. Initial timing/
fixture failures are documented in IMPLEMENTATION-LOG.md. Manager execute/resume/
compensation integration and scoped/private checkpoint progress remain next under
R02/P3-06/08. No phase completion or live-provider/Unix/release claim.

## Latest NFEL Gate: 2026-10-03

[NFEL-1](../../DataManagementEngineStandard/Rules/NFEL.md) now qualifies complete
bounded grammar and actual execution, private source/token/policy admission,
decoded strings/subtraction/unary/power/ternary/lazy Booleans, finite typed values,
lifecycle/timeout checks and bounded defensive history. 99 new cases per TFM;
fresh forced-built Windows matrix 5,016 passes, zero failures/skips, 1,339 reliability
cases per TFM. Warning baseline unchanged at 6,223; zero errors/new NFEL warnings.
Other parser/helper/profile/adapter/provider and full P1-P5 gates remain.
Earlier planning/configuration evidence follows; no broad item is closed.

## Latest Planning Gate: 2026-10-03

[Post-NFEL review and enhancement plan](REVIEW-AND-PLAN-2026-10-03.md) records
a fresh forced build (6,223 warnings, zero errors) and 5,016 passing Windows
executions, zero failures/skips. NFEL core is delivered, not next work. First
bounded P1 slice: migration target/store ownership and private progress. Sync
governed publication/cursor agreement and remaining config facades are independent
P1 lanes; provider/release inventory starts now. B2 depends on its governed
publication inputs, not all unrelated parser/provider qualification. Existing
P1-P5 IDs and delivered boundaries are preserved; no phase completion.

## Current Delivery Gate: 2026-10-03

Subsequent required configuration increment: **4,719** source-built passing executions,
zero failures/skips, **1,240 reliability cases per TFM**. Required row resolution
normalizes once through an editor-owned registry, bypasses metadata-only caches
and denies reported fallbacks with safe diagnostics; the earlier 34 cases per TFM
are followed by 40 capture cases. Caller defaults stay untouched, implicit catalogs
refresh per run, literals are closed/bounded and both declared catalogs are
captured before provider-opening validation, even when empty.
The subsequent 43 roster/nested-context cases pin priorities/instances through
registration edits, protect row SentData and admitted named/column lookup, and
retain safe sticky failures across nested wrappers/selector callback boundaries.
A further 59 outer-grammar cases qualify exact shipped token routing, arity/empty
slots, nested date calls, quoted keys/operands, GUID aliases/formats and invalid
scope names. Required time/hash sequence demonstrations deny rows; qualified
allocation plugins are needed. Query/identity/scope-fallback semantics remain.
A further 47 cases preserve required dot literals/empty strings, reject missing
segments, retain bare-token errors and qualify declared grouping/actual query
filters; public legacy parsing is unchanged. Another 82 cases qualify bounded
shipped required expression/formula ASTs: Boolean-only conditions, precedence,
exact typed comparisons, invariant numbers, Decimal rounding, lazy branches and
pre-read limits/cancellation. Nested overrides retain pinned selection; unused
branches are syntax-checked, not semantically prequalified. Broader numeric,
query-provider/identity/NFEL/plugin gates remain.
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
Read [defaults admission](../../DataManagementEngineStandard/Editor/Importing/DEFAULTS-ADMISSION.md).
Full immutable policy/plugin intent, grammar/provider qualification and
all broader gates remain. No phase item is closed.
The following review/recovery paragraphs retain preceding evidence.

[Previous source review and executable delivery order](REVIEW-REFRESH-2026-10-03.md)
record that planning-only refresh: **2,895** source-built passing executions,
zero failures/skips, **632 reliability cases per TFM**. Required catalog admission
already has nine passing cases per TFM; finish resolver/default ownership and
both-direction qualification rather than rebuilding that boundary. B1 then B2
remain the critical path; C/D and provider/release preparation run independently.
The following recovery paragraph records the preceding implementation baseline.

[Earlier review and ordered delivery plan](REVIEW-2026-10-03.md) supersedes the
historical baseline. The subsequent actual claimed/acknowledged file reject replay
and corruption exception-contract fix restore green verification: **2,868 passing
executions**, zero failures/skips, **623 reliability cases per TFM** on Windows.
Read [recovery contract](../../DataManagementEngineStandard/Editor/Importing/REJECT-RECOVERY.md).
Preserve existing verified slices and P1-P5 IDs. B1 recovery/default qualification
precedes B2 promotion/cursor agreement; C/D configuration/migration admission and
provider/release inventory can proceed independently. Next qualify defaults/
resolvers and native/provider-run recovery; this slice does not reduce phase scope.

Reviewed: 2026-10-02. The original review sampled the dirty worktree and changed
planning documents only. Subsequent implementation/evidence is recorded below.
This is the actionable delivery plan; [current review](CURRENT-REVIEW.md) records
source findings and [implementation log](IMPLEMENTATION-LOG.md) records past work.
Existing phase IDs remain the single completion checklist.

## Assessment

Keep the two-layer architecture: Models owns public contracts; Engine owns
implementations. Keep DMEEditor as the facade and introduce focused services or
optional capabilities instead of expanding IDataSource with breaking members.

Already delivered slices include acknowledged write accounting, deferred UOW
acceptance, conservative retries, normal DI runtime isolation, editor-owned
datasource coordination, atomic file primitives, typed import watermarks and
captured/hash-bound migration plans with mandatory checkpoint acknowledgement.
Built-in connection protection/acknowledgement and post-save observer isolation
are tested on the Windows matrix. Current BeepSync edits add coordinated schema
updates, closed typed cursors, strict persisted identities and immutable version/
checkpoint artifacts. Completion/storage qualification now passes the full local
Windows TFM matrix below. Required record/threshold admission and failed-run counts
are now verified alongside file reject recovery; recoverable promotion and durable cursor
agreement remain.
Do not rewrite these as missing features. They do not establish universal live
provider recovery, thread safety, exactly-once delivery or power-loss durability.

Latest full Windows solution matrix: 5,178 passing executions, zero failures/skips,
including 1,393 reliability cases each on net8/9/10. The earlier planning pass inspected
325 net9.0 cases; subsequent implementation added terminal/diagnostic and schema
identity fixes with source-built matrix evidence. Neither establishes Unix,
live-provider, clean-pack or package-consumer support. See IMPLEMENTATION-LOG.md.

## Current Priority Findings

| Priority | Remaining issue | Source / review finding | Impact |
|---|---|---|---|
| P1 recovery gate | Durable schema/cursor and Completed agreement remains explicit, not transactional | [Sync contract](../../DataManagementEngineStandard/Editor/BeepSync/STORAGE-AND-OUTCOMES.md); R12 | Restart gaps can retain old cursors; terminal acknowledgement/diagnostic isolation themselves are now fixed |
| P1 | Sync promotion mutates state before two saves; approval hash omits governed intent | [SchemaGovernance.cs:97](../../DataManagementEngineStandard/Editor/BeepSync/BeepSyncManager.SchemaGovernance.cs#L97); R13 | Memory, version artifact and schema can disagree after failure or restart |
| P1 remaining gate | Record/threshold/failure counts and file reject recovery are verified; native/provider-run recovery remains | [Reject recovery contract](../../DataManagementEngineStandard/Editor/Importing/REJECT-RECOVERY.md); R14 | Durable IDs/CAS prevent blind row replay; no atomic target/reject or live-provider recovery guarantee |
| P1 remaining gate | Run-owned catalogs/literals/rosters and nested required core tested; full intent/adapters remain | [Defaults contract](../../DataManagementEngineStandard/Editor/Importing/DEFAULTS-ADMISSION.md); R15 | Pinned selection, owned named lookup and sticky failures do not freeze plugin internals/all policies or qualify complete rule semantics |
| P1 remaining integration | Existing-target mapped binding is verified; mapped DDL/immutable policies/providers remain | [Sync contract](../../DataManagementEngineStandard/Editor/BeepSync/STORAGE-AND-OUTCOMES.md); R16 | Missing mapped targets fail closed; no inferred DDL, native upsert or live-provider guarantee |
| P1 release/P2 lifecycle gate | Generated source identity fixed; legacy cache consumers and loaded assemblies need qualification | [Generation contract](../../DataManagementEngineStandard/ConfigUtil/GENERATED-TYPES.md); R17 | Bare-name overrides no longer govern types; bounded references do not unload assemblies |
| P1 remaining integration | Owned metadata is bound for existing-target sync; wider policy/context admission remains | [Metadata contract](../../DataManagementModelsStandard/DataBase/METADATA-SNAPSHOTS.md); R18/R16 | Legacy structure copies still share collections; captured metadata is not complete immutable run intent |
| P1 | Shared migration checkpoints lack atomic execution admission | [ExecutionOrchestration.cs:130](../../DataManagementEngineStandard/Editor/Migration/MigrationManager.ExecutionOrchestration.cs#L130); R02 | Concurrent calls can operate on the same mutable steps; public progress is execution-owned state |
| P1 | Remaining config facades hide persistence failure/corruption; environment writes are direct | [ComponentConfigManager.cs:252](../../DataManagementEngineStandard/ConfigUtil/Managers/ComponentConfigManager.cs#L252); R09 | Callers cannot distinguish saved state from failure; corrupt reads can look fresh |
| P1 release gate | Credential routes, host keys and reader/export compatibility need qualification | [Security contract](../../DataManagementEngineStandard/Security/README.md); R03 | Uncovered adapters, mixed readers or unavailable keys can undermine safe recovery |
| P2 | Timers lack generation ownership and drain | [TimerManager.cs:90](../../DataManagementEngineStandard/Editor/Forms/Helpers/TimerManager.cs#L90); R07 | Queued callbacks can act on replacements; disposal does not await admitted work |
| P2 | Import materializes all rows; async ETL invokes sync reads | [DataImportManager.cs:570](../../DataManagementEngineStandard/Editor/Importing/DataImportManager.cs#L570); R05 | Batch size does not bound source memory or cancellation latency |
| P2 remaining qualification | NFEL-1 grammar/execution/retention core verified; other dialects/adapters/profile workloads remain | [NFEL contract](../../DataManagementEngineStandard/Rules/NFEL.md); R08 | 99 new cases/TFM correct the five probes; no universal parser/plugin sandbox or package-reader guarantee |
| P2 release gate | External package assets/output paths, floating tests and no SDK/CI policy | [Engine.csproj:51](../../DataManagementEngineStandard/DataManagementEngine.csproj#L51); R06 | Local tests do not prove reproducible package delivery |

See [current review](CURRENT-REVIEW.md) for source links, qualifications and
historical fixes. Findings are source-derived unless explicit test evidence is
identified; this is not an exhaustive security or provider audit.

## Delivery Order

Existing P1-P5 work IDs remain authoritative. The slices below order delivery;
they do not introduce a second completion checklist.

### 1. Complete Governed Sync Intent And Publication

Work IDs: P1-05/06/10/12, P2-05/06, P3-07/09. Depends on existing typed storage and
acknowledged outcome contracts. Terminal acknowledgement and diagnostic isolation
have landed. R14/R15 record/threshold/default cores, file reject recovery and NFEL-1
are locally verified prerequisites, not work to repeat. R13 promotion and remaining
R12 cursor agreement depend on the complete governed inputs they publish, not all
unrelated parser/profile qualification. Mapped creation, native/provider-run recovery,
full policy/plugin/host context and package qualification remain. Changes below are
proposed unless identified as delivered; historical increment counts retain their
original scope.

Metadata prerequisites and existing-target mapped binding now have local TFM
evidence: terminating field cloning, owned definitions, source-sensitive generated
types and both destination field lists. Mapping-aware admission accepts supported
renames, validates required targets/unique assignments and includes exact key pairs
once. Both bidirectional configs are validated before either writes. Missing mapped
targets reject even with creation enabled; explicit provisioning, mapped DDL/reload,
policy/context capture and provider/package qualification remain. Do not guess
declared types or clear the global cache to conceal incompatible metadata.

Ordinary import QualityRules and sync DqPolicy.RuleKeys now execute at the actual
post-transformation/pre-write boundary in both directions. Captured required/
advisory policy, strict Boolean sync outputs, Block/Quarantine/Warn, once-per-row
evaluation outside provider retries and acknowledged reject-store accounting have
51 new cases per TFM. Required dependency failures deny target mutation, and
bidirectional failure/cancellation retains earlier write acknowledgements.
See [quality contract](../../DataManagementEngineStandard/Editor/Importing/QUALITY-ADMISSION.md).
Attempt thresholds now capture required/advisory intent before writes, enforce a
real attempted-row denominator (including rejects), evaluate empty attempts and
reject missing/throwing/malformed required rules. Failed/cancelled checkpointed
runs publish actual typed counts under admitted identity; 58 added cases per TFM
cover the boundary and failure storage. See
[threshold/failure contract](../../DataManagementEngineStandard/Editor/BeepSync/THRESHOLDS-AND-FAILURE-EVIDENCE.md).
Stable reject identities, durable file triage/CAS and actual acknowledged row
replay now have 55 new cases per TFM. Claims precede provider invocation; uncertain
writes or failed completion persistence remain blocked. Import/sync use current
record policy without reapplying destination transformations. Native/provider-run
qualification remains; row replay is not an atomic target/reject commit or run completion.

Keep the delivered typed transformation/strict mapping boundary. Configured stage
failures admit no row write, preserve earlier acknowledgements and stop blind sync
replay; safe legacy adapters surface failures. Do not use custom transformation as
the quality gate. Qualify remaining catalog reads, resolver diagnostics and custom
adapters. Preserve delivered actual metadata binding on both sync directions (R16);
tests must inspect transformed payloads rather than merely observe an earlier
validation rejection. Denied admission is not an uncertain write.
Synchronous rule calls may only observe cancellation/limits at boundaries; do not
claim they can be forcibly interrupted or that executable plugins are sandboxed.

Use ISyncPersistenceAcknowledgement for both Running and Completed persistence.
Separate committed outcomes from SLO/alert/history observer failures. Define a
durable schema/watermark commit boundary with conservative reconciliation after
partial failure; a saved Completed record is not a transactional cursor update.
Do not turn run counters into unimplemented key/offset replay.

Stage schema promotion without changing live approval/mapping state. Capture and
version complete governed intent; replace random fingerprint fallback with explicit
failure. Define commit/recovery semantics for version artifact plus schema snapshot
and propagate cancellation. Required DQ gates fail closed; advisory diagnostics
cannot override durable success. A gate failure after writes must report partial
acknowledgement, not fabricated rollback.

Acceptance: concrete-helper tests prove required row rejection and transformation
failure cause zero writes for that row; missing required dependencies cause zero
target mutation; mixed runs preserve acknowledged/rejected counts and do not
advance successful cursors. Required rule lookup/solve exceptions, malformed
outputs, timeout and cancellation fail closed; advisory behavior is explicitly
tested. Include dictionaries, dynamic records and POCOs, forward/reverse paths,
reject-store failure and mutation of caller policy during a run.
Terminal Failed/Cancelled/Unsupported results never yield Success;
throwing diagnostics cannot change committed outcomes; second-save failure and
interrupted promotion preserve a recoverable old/new state; throwing/timeout/
malformed required gates cannot silently pass. Process restart restores supported
cursor types and context. Run focused tests and the full source-built TFM matrix.

### 2. Qualify Remaining Configuration And Connection Protection

Work IDs: P2-05/07/08. Depends on existing atomic storage and result contracts.

Do not restart delivered catalog Save(false), protection or observer-isolation
fixes. Inventory environment, workflow/report/template/query and component writers.
Carry failure through their facades, preserve corrupt evidence and coordinate
complete updates. Qualify custom adapters and remaining credential-bearing routes.
Keep observer callbacks outside storage locks and captured policy runtime-scoped.

Define package/envelope reader compatibility, DPAPI-user scope restrictions,
host-owned portable keys, rotation/retirement and explicit recovery procedures.
Keep keys outside connection files. Audit named fields, compound containers,
exports, imports, promotions, rename identity binding and diagnostics.

Acceptance: sentinel secrets absent from supported stored/exported/redacted routes;
tampering and wrong/missing keys fail explicitly without overwriting evidence;
redaction works without keys; rotation and separate-process reload succeed under
the declared host policy; observer failure never reclassifies a saved write.

### Storage And Cursor Qualification Across Slices 1-2

Work IDs: P2-05/06. Uses the established persistence primitive, not a new store.

Keep a writer/reader inventory with artifact path, owner, format, acknowledgement,
coordination scope, key policy, legacy readers and recovery procedure. Built-in
sync/import/history/catalog primitives are implemented slices, not missing stores.
Verify their public adapters and remaining config callers. Distinguish missing
from corrupt/unsupported state and preserve original recovery evidence.

Qualify current BeepSync envelope compatibility, explicit old-global-root migration,
typed cursor restart and immutable artifact behavior. Define timestamp ordering,
ties, late arrivals and advancement after committed writes. Preserve typed import
watermarks. Sequence/composite execution stays rejected until provider comparison
semantics, ordering and restart behavior are implemented and tested.

Acceptance: competing processes retain updates; empty/malformed/foreign records
remain intact; restart restores exact supported types; failed/cancelled/uncertain
writes never advance cursors. Verify backups and supported OS behavior separately.

### 3. Close Migration And Write-Recovery Gaps

Work IDs: P3-06/07/08 and P1-10/11/12. Provider inventory starts in parallel.

Admit one execution per runtime/store/target/token identity, with a durable
cross-process mechanism for shared stores. Define how different tokens targeting
the same schema are serialized. Replace externally mutable checkpoint exposure
with owned snapshots while preserving compatibility. Bound cache lifetime.

Model provider-normalized expected intermediate schema after acknowledged DDL.
Record uncertain operations separately; reconcile actual provider state before
replay. Implement explicit operator recovery with stable run/record identities,
documented deduplication and durable per-record diagnostics. No blind replay or
universal exactly-once promise. Document imperative versus governed migration routes.

Acceptance: barrier-controlled same-token and same-target concurrency tests plus
separate-process admission tests; progress mutation cannot alter execution;
real provider fixtures cover partial DDL, commit/rollback failures, generated keys,
foreign-key cycles and failed compensation. Tests name exact driver/DB versions.

### 4. Finish Lifecycle And Provider Conformance

Work IDs: P2-08/10 and P3-01/02/03/09. Can progress independently of sync format work.

Use per-generation timer identity, disposed admission guards, a defined overlap
policy, cancellation and drainable shutdown. Define Forms UI dispatch separately
from thread-safe storage. Audit plugin ownership, subscriptions and unload limits.

Build a provider capability inventory and reusable conformance suite. Optional
transaction/upsert/bulk/paging/streaming contracts must distinguish unsupported
features from failed operations. Test helper quoting/type/nullability behavior and
NFEL-1 grammar/execution and hard bounds now have 99 cases per TFM. Retain these
regressions; qualify other dialects, adapters and supported numeric profiles separately.

Acceptance: controlled slow/reentrant callbacks cannot affect replacement timers;
shutdown reaches a documented quiescent state; supported provider capabilities
pass live fixtures; unsupported features fail before writes; malformed expressions
are rejected. SQL generation alone is not database conformance.

### 5. Bound Execution And Improve Diagnostics

Work IDs: P4-01 through P4-08. Depends on provider capabilities and stable outcomes.

Measure baseline UOW/import/sync/forms/plugin workloads first. Introduce bounded
source/transform/sink queues, backpressure and capability-gated bulk writes. Define
batch limits in both records and bytes where large rows affect memory. Do not wrap
legacy synchronous calls and label them truly cancellable asynchronous I/O.

Add run/correlation IDs, redacted structured logs, spans and low-cardinality metrics
for acknowledgements, retries, reconciliation and shutdown. Bound parser/checkpoint/
error history retention without deleting unresolved recovery evidence.

Acceptance: a streaming fixture writes before exhausting its source; memory follows
configured queue/batch bounds; cancellation latency is measured per capability;
telemetry contains no sentinel credentials; regression budgets come from measured
workloads, not invented throughput claims.

### Parallel Lane: Reproducible Release And Developer Experience

Work IDs: P5-01 through P5-08. Start now; final release depends on the slices above.

Choose the supported TFM/OS/provider policy. Pin SDK/test dependencies; make sibling
output copies opt-in, keep icons in-repo and isolate generated documentation per
TFM. Remove tracked build output deliberately, without discarding user changes.

Add clean-checkout CI build/test/pack and package-only CLI/scoped-host smoke
consumers. Establish public API compatibility and warning/nullable baselines.
Compile public examples; reconcile README, skills and legacy agent instructions.
Publish tested capability and upgrade/recovery limits alongside packages.

Acceptance: a checkout without sibling repos produces installable packages; each
declared supported TFM/OS lane passes; consumers restore only produced packages;
API changes and warnings have reviewed baselines; samples use actual public APIs.

## Execution Rules

### First Implementation Slices

These are proposed PR boundaries, not new work IDs or completion checkboxes.

| Order / Lane | Deliverable | Required evidence before merge |
|---|---|---|
| A - verified local slice | Terminal sync acknowledgement and observer outcome isolation | Six failures reproduced then corrected; 16 outcome cases per TFM; actual replacement denial; full source-built Windows matrix. Durable cursor agreement remains open |
| D - backend available; next integrate managers | Migration target/store admission and owned progress | Models/ConfigEditor/File backend exists; manager execute/resume/compensation must actually acquire it, pin canonical target/root, expose defensive progress and pass runtime/process/provider races and crash reconciliation |
| B1 - remaining qualification | Full owned run intent, adapters and native/provider recovery (R14-R18) | Preserve tested metadata/mapping/quality/recovery/defaults and NFEL cores; complete policy/plugin/host context, mapped creation and provider/package behavior remain |
| B2 - P1 independent lane | Recoverable promotion/cursor agreement (R13/remaining R12) | Governed inputs captured first, not all unrelated B1 features; strict required gates; complete hash; each save boundary; cancellation/restart/concurrent promotion; cursor/completion disagreement denies replay |
| C | Remaining configuration facade/storage inventory and fixes | Entry-point matrix; injected serializer/replace/read failures; no fresh-state fallback; competing processes; backup/recovery fixtures |
| Parallel from A | Provider inventory and reproducible build/CI | Versioned capability matrix; clean checkout without sibling assets; explicit TFM/project counts; produced-package consumers |
| After D and provider fixtures | Manual recovery, lifecycle and bounded streaming | Actual partial-DDL/write reconciliation; controlled callback drain; first write before source exhaustion; measured memory/cancellation |

### B1 Implementation Breakdown

These are ordered changes within existing work IDs, not another completion list.
Quality contract design and independent import regressions can proceed alongside
metadata fixes; mapped-sync acceptance depends on the real metadata path.

| Step | Change boundary | Exit evidence |
|---|---|---|
| 1 - core verified | Models field cloning plus an explicit owned metadata snapshot; P3-07/09, P5-05 | 27 cases/TFM cover terminating Clone, merge/copy, aliases, nested copies and rejection limits; legacy shallow semantics remain; actual run binding belongs to step 3 |
| 2 - core verified | Engine dynamic type identity/ownership; P3-09, P4-07 | 26 cases/TFM plus actual generated import targets cover source identity, exact selection and controlled cache races; consumer qualification, loaded assembly lifetime and workload budgets remain |
| 3 - existing-target core verified | Sync translation and mapping-aware preflight; P3-07/09 | Actual fields bind into both destination lists; generated forward/reverse payloads, key pairs, invalid reverse metadata, mutation/cancellation and no-DDL failures are tested; mapped creation/policy/provider qualification remains |
| 4 - record core verified | Optional Models quality outcomes and Engine pre-write admission; P1-10/12, P3-09 | 51 cases/TFM cover captured required/advisory policy, actual transformed rows in ordinary import and both sync directions, evaluation outside retries, Block/Quarantine/Warn, store acknowledgements and retained counts; durable reject identity/provider qualification remain |
| 5 - core verified | Required threshold and failure publication; P1-10/12, P2-06, P3-09 | 58 cases/TFM cover real/empty denominator, strict actions, captured required/advisory policy, failed checkpoint counts, storage failure/cancellation/restart and process reload; full provider recovery remains |
| 5 follow-up - file core verified | Durable reject IDs/operator triage/CAS and actual row replay; P1-10/12, P2-05/06 | 55 cases/TFM cover generated import/sync payloads, current policy, actual acknowledgements, provider keys, concurrent process claims, restart, uncertainty, cancellation and completion-save failure; no run completion/cursor advance or native/live-provider guarantee |
| 6 - catalogs/rosters, nested core, outer/dot/expression/query, identity/scope, date, configuration and NFEL-1 core tested; qualification open | Remaining defaults/adapters and compatibility; P3-09, P5-05/07 | Preserve nine catalog + 34 resolver + 40 capture + 43 roster/context + 59 outer-grammar + 47 dot-literal + 82 strict-expression + 80 query + 56 identity/scope + 84 date + 83 configuration + 99 NFEL cases/TFM; next other parser/profile and wrapper/package qualification, explicit host configuration bridging and owned time/timezone context, owned application identity/context/platform, query isolation/translation and broader numeric qualification, policy/plugin ownership, logging and persisted evidence compatibility; package users and skills agree |

Step 4 conservatively fails required-rejection runs; only explicit Warn/advisory
decisions admit those records. Quarantine counts require store acknowledgement,
and rejected runs do not advance successful cursors. Step 5 now implements empty-run
behavior, attempted-row denominator and durable failed counts; broader recovery is open.
Do not silently treat rejected rows as processed or advance past unresolved rows.
For step 1, explicitly list copied mutable members
and handling of unsupported/cyclic values. For step 2, define compatibility for the
public typeCache and whether generated identities are stable across restart.

Owners are responsibilities rather than assumed team assignments: Models/API
maintainer reviews contract compatibility; subsystem maintainer owns behavior and
regressions; release maintainer owns OS/TFM/package gates; host integrator owns keys,
authorization and UI dispatch. One person can fill multiple roles.

Do not check broad phase items off from a narrow regression or metadata validator.
Each increment records command, OS/SDK/provider versions, test inventory, outcome
and remaining limits in IMPLEMENTATION-LOG.md. No calendar estimates until provider
inventory, support-policy decisions and representative workload baselines exist.

### Decisions Before Broader API Work

- Preserve Models-to-Engine layering, existing public misspellings and legacy
  signatures; introduce optional capabilities/adapters with documented failures.
- Define supported OS/TFM/provider versions and a package-reader migration policy.
- Choose shared-store admission and crash-recovery semantics; file mutation leases
  alone do not own provider execution or make multi-file updates transactional.
- Define required/advisory rules and who may approve or reconcile execution.
- Assign key ownership/rotation/export policy and Forms host dispatch behavior.
- Promise supported at-least-once/deduplication/reconciliation semantics, not
  universal exactly-once, transactional DDL or interruptible legacy synchronous I/O.

### Planning Validation Limits

The latest planning pass inspected post-NFEL source, forced a rebuild and ran all
nine local solution test runs: 5,016 passes, zero failures/skips. It did not add
new fault/race tests or verify live providers. Logs and limits are in the current
planning review. The paragraphs below retain earlier planning evidence.
Framework-plan local links and whitespace are checked separately
from the historical root tracker. That tracker still has seven pre-existing links
to missing editor-managers plans; reconcile them under P5-07 rather than inventing
replacement documents or treating their old completion status as implementation.
The latest planning refresh expanded R14 and added R15 after following concrete
write/transform paths. Existing write tests use an identity transformation mock;
they do not establish quality or transformation admission. That planning pass only
inspected the retained 2,049-test log. Subsequent implementation added concrete
transform coverage and passed 2,136 executions; B1 quality/metadata/default gates
still remain, as recorded above and in IMPLEMENTATION-LOG.md.
The subsequent planning-only refresh expands R16 and records R17/R18 from concrete
validation/cache/clone callers. It re-counted nine retained successful test runs
(2,136 executions) without starting a new test run. No implementation or installed
skills changed; source-derived findings still require targeted regression evidence.

Master: [framework tracker](MASTER-FRAMEWORK-TRACKER.md). Detailed phase scope:
[correctness](PHASE-01-Write-Correctness.md),
[runtime/persistence](PHASE-02-Runtime-And-Persistence.md),
[provider/migration](PHASE-03-Provider-And-Migration-Contracts.md),
[performance](PHASE-04-Performance-And-Observability.md),
[release](PHASE-05-Release-And-Developer-Experience.md).
