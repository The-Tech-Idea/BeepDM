# BeepDM Framework Review And Delivery Plan

Subsequent implementation: required resolver registration/normalization/cache/
reported-fallback handling added 34 cases; run-owned default capture adds 40 more
per TFM. A further 43 admitted roster/nested-context cases and 59 shipped outer-
grammar cases, then 47 dot-literal, 82 strict-expression, 80 query and 56 identity/
scope, 84 date and 83 configuration cases yield a
**4,719-pass Windows matrix**, zero failures/skips and 1,240
reliability cases each on net8/net9/net10. Exact routing/arity and nested date calls
are qualified; required sequence demos deny rows. Required dot quotes/empty strings
and grouping/filter roles are tested without changing public legacy parsing.
Bounded shipped required expression/formula ASTs now qualify Boolean-only
conditions, precedence, typed exact comparisons, invariant numbers, Decimal
rounding, lazy branches and pre-read limits/cancellation. Nested overrides retain
pinned selection; unused branches are syntax-checked only. Broader query/identity/
scope/date/NFEL/numeric semantics and complete policy/plugin/provider intent remain.
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
Pinned roster/priority selection, per-row SentData, admitted named/column lookup,
sticky safe nested failures and selector boundaries are now verified. Caller-safe implicit refresh, closed bounded literals
and both declared catalogs before provider-opening validation are now verified.
The earlier required resolver increment had a 2,997-pass Windows
matrix. Read [defaults admission](../../DataManagementEngineStandard/Editor/Importing/DEFAULTS-ADMISSION.md)
and IMPLEMENTATION-LOG.md. The findings/baseline below retain this review's earlier
planning snapshot; complete R15 capture/admission/grammar/plugin/provider gates
and the full five-phase scope remain open.

Reviewed: 2026-10-03. Scope: sampled current-source review of Models, Engine,
import/ETL, sync, migration, configuration, Forms, plugin lifecycle and release
projects. HEAD: `2a837952597b60c8c90821187e16dd8e9913508a`, with substantial
existing uncommitted implementation. This pass changes planning documents only;
existing implementation and test changes are preserved.

This refresh supersedes the earlier [planning snapshot](REVIEW-2026-10-03.md)
for current priorities and verification. That snapshot remains historical.
[Phase work IDs](MASTER-FRAMEWORK-TRACKER.md) remain the sole completion checklist;
the delivery slices below are change boundaries, not additional completion IDs.

## Findings In Priority Order

### P1: Required default resolution can still admit a fallback value (R15)

[DateTimeResolver.cs:146](../../DataManagementEngineStandard/Editor/Defaults/Resolvers/DateTimeResolver.cs#L146)
returns DateTime.Now when ADDDAYS arguments cannot be converted. The import stage
at [DataImportTransformationHelper.cs:143](../../DataManagementEngineStandard/Editor/Importing/Helpers/DataImportTransformationHelper.cs#L143)
rejects null resolution, not this non-null fallback. A malformed required rule
can therefore write a value that was never successfully resolved.
[DefaultsManager.cs:32](../../DataManagementEngineStandard/Editor/Defaults/DefaultsManager.cs#L32)
also retains process-wide mutable editor/resolver state. Initialization is locked,
but the subsequent resolution is not bound to an owned resolver instance; another
runtime can rebind it between initialization and use.

The new catalog boundary is **already present and locally tested**:
[ImportDefaultsAdmission.cs:8](../../DataManagementEngineStandard/Editor/Importing/Helpers/ImportDefaultsAdmission.cs#L8)
and [DataImportManager.cs:515](../../DataManagementEngineStandard/Editor/Importing/DataImportManager.cs#L515)
deny missing/ambiguous/failed implicit catalog lookup before initialization and DDL.
Nine catalog tests pass on each configured TFM. Do not re-plan this as absent.
Qualification remains for repeated configuration use, closed literal snapshots,
custom adapters and both sync directions before either direction writes.
Prepare replaces config.DefaultValues with catalog values; a later run with the
same nonempty list treats those values as explicit rather than refreshing catalog
intent. CaptureRequired also accepts arbitrary value types, which can contain
mutable references. These are source-derived ownership risks, not new reproduced
failures in this pass.

### P1: Sync approval and cursor publication are not one recoverable decision (R13/R12)

[SchemaGovernance.cs:97](../../DataManagementEngineStandard/Editor/BeepSync/BeepSyncManager.SchemaGovernance.cs#L97)
publishes version/approval changes to the caller before independently saving the
version artifact and schema. Either save failure can leave different published
states. The promotion rule at
[line 62](../../DataManagementEngineStandard/Editor/BeepSync/BeepSyncManager.SchemaGovernance.cs#L62)
accepts null/non-Boolean results unless their string equals `false`, and skips a
missing configured dependency. Required versus advisory semantics need definition.
[SchemaFingerprinter.cs:17](../../DataManagementEngineStandard/Editor/Schema/SchemaFingerprinter.cs#L17)
does not bind full key/filter/policy intent and fabricates random identity on failure.

Mandatory terminal checkpoint acknowledgement is fixed. However,
[BeepSyncManager.Sync.cs:386](../../DataManagementEngineStandard/Editor/BeepSync/BeepSyncManager.Sync.cs#L386)
saves Completed before changing in-memory cursors; those cursors require an
explicit schema save. A crash can retain a Completed run with an older durable
cursor. Atomic individual files do not supply artifact agreement or target-write
atomicity. Define recoverable publication, not an exactly-once promise.

### P1: Migration checkpoint lookup does not exclusively admit execution (R02)

[ExecutionOrchestration.cs:130](../../DataManagementEngineStandard/Editor/Migration/MigrationManager.ExecutionOrchestration.cs#L130)
checks for Running work before mutating shared steps, without an atomic execution
claim. A ConcurrentDictionary protects entries, not the full admission/execution
sequence. Competing callers can pass the initial check. The public getter at
[line 571](../../DataManagementEngineStandard/Editor/Migration/MigrationManager.ExecutionOrchestration.cs#L571)
returns the live cached checkpoint. External mutation can affect execution state.

Versioned approved intent and acknowledged history/checkpoints are delivered.
The remaining work is target/store/token ownership, owned progress views and
provider-confirmed recovery after partial DDL. No new concurrency fault injection
was performed in this planning pass.

### P1: Other configuration facades still hide failure and corruption (R09/R03)

[ComponentConfigManager.cs:243](../../DataManagementEngineStandard/ConfigUtil/Managers/ComponentConfigManager.cs#L243)
logs failed workflow saves and returns normally. Its read path at
[line 223](../../DataManagementEngineStandard/ConfigUtil/Managers/ComponentConfigManager.cs#L223)
can turn failure into an empty collection; reports have similar behavior.
Connection and migration-history acknowledgement/protection fixes do not qualify
these other facades. Preserve unreadable bytes and distinguish absent, corrupt,
unsupported and inaccessible state before permitting replacement.
All credential-bearing routes, host key ownership, exports and mixed-reader
compatibility still require a separate security qualification matrix.

### P2: Forms/plugin callback replacement and shutdown lack ownership (R07)

[TimerManager.cs:58](../../DataManagementEngineStandard/Editor/Forms/Helpers/TimerManager.cs#L58)
passes only a name to callbacks. An old callback can resolve a replacement entry
at [line 90](../../DataManagementEngineStandard/Editor/Forms/Helpers/TimerManager.cs#L90).
Creation is not rejected after disposal, and Dispose does not drain admitted
callbacks. Plugin health monitoring uses an async timer callback at
[PluginHealthMonitor.cs:72](../../DataManagementEngineStandard/AssemblyHandler/PluginSystem/PluginHealthMonitor.cs#L72)
without explicit ownership of the asynchronous work during shutdown.
Use generation identity, overlap policy and observable drain; public collection
thread safety does not establish callback lifecycle or UI dispatch safety.

### P2: Batch size does not bound source memory or cancellation latency (R05)

[DataImportManager.cs:560](../../DataManagementEngineStandard/Editor/Importing/DataImportManager.cs#L560)
materializes all source rows before processing batches.
[DataSourcePlugin.cs:83](../../DataManagementEngineStandard/Editor/ETL/Engine/BuiltIn/Sources/DataSourcePlugin.cs#L83)
calls synchronous GetEntity inside an async enumerable. A CancellationToken
between rows cannot interrupt a blocked legacy provider call. Streaming requires
an actual provider capability and bounded buffers throughout the path.

### P2: Rule parsing and release qualification remain incomplete (R08/R06)

[NfelParser.cs:63](../../DataManagementEngineStandard/Rules/BuiltinParsers/NfelParser.cs#L63)
enumerates regex matches without rejecting unmatched input; malformed text such
as `1 @ 2` can yield successful parsing. Balanced aggregate parentheses alone
also do not establish grammar validity. Parsed structures accumulate in a list.

[Engine.csproj:48](../../DataManagementEngineStandard/DataManagementEngine.csproj#L48)
packs an external icon; both package projects have unconditional external copy
targets and shared per-project docs.xml output across TFMs. Forms test packages
float. This checkout has no global.json or workflow files in .github/workflows.
Tracked Setup bin/obj artifacts make test execution dirty the checkout.
Local green tests are not clean-checkout, Unix, live-provider or NuGet-consumer
qualification. Provider capability and helper SQL-generation claims need exact
provider/version evidence before becoming support commitments.
The root TODO tracker's older sections also contain missing editor-managers plan
links outside this refresh. Reconcile them under P5-07 without inventing absent
documents or rewriting unrelated track status.

## Verification Baseline

Source-built on Windows, SDK 10.0.401; both commands exited 0:

```powershell
dotnet test tests/FrameworkReliabilityTests/FrameworkReliabilityTests.csproj -f net9.0 -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
```

| Suite | Executions | Target |
|---|---:|---|
| Forms | 221 | net8.0 |
| Setup | 232 | net9.0 |
| Studio | 66 | net9.0 |
| Migration | 480 | 160 each: net8.0/net9.0/net10.0 |
| Framework reliability | 1,896 | 632 each: net8.0/net9.0/net10.0 |
| Full solution total | **2,895** | **9 successful project/target runs** |

Zero failures/skips. The separate focused run passed 632 net9.0 cases and is not
counted twice in the full-solution total. Logs:
`$env:TEMP/BeepDM-enhancement-review-net9.log` and
`$env:TEMP/BeepDM-enhancement-review-full.log`.
Generated tracked Setup outputs were clean before testing and restored afterward;
pre-existing dirty source/docs are preserved. No pack, external provider tests or
new fault-injection regressions were run. Findings above are source inspection;
passing tests do not reproduce or disprove their untested scenarios.

Keep the delivered architecture and reliability slices: Models contracts/Engine
implementations, DMEEditor facade, normal DI runtime isolation, datasource lifecycle
coordination, acknowledged writes, deferred UOW acceptance, strict transformation,
record/threshold quality gates, file reject recovery, metadata snapshots, generated
source identity, existing-target mapped sync and versioned migration intent.

## Ordered Enhancement Plan

Owners below are responsibility roles, not assumed staffing or delivery dates.
Preserve existing phase checkboxes; a narrow passing suite does not close a phase.

| Order / lane | Change boundary and owner | Existing work IDs | Acceptance evidence |
|---|---|---|---|
| First: finish B1 | Owned required defaults/resolvers and adapters; import/sync + Models maintainers | P1-10/12, P3-09, P5-05/07 | Malformed non-null fallbacks deny writes; two-runtime resolver isolation; repeated config use and literal mutation tests; both directions admitted before writes; safe diagnostics and cancellation; compatibility fixtures |
| Next: B2 | Staged sync promotion and durable completion/cursor agreement; sync/storage maintainers | P2-06, P3-07 | Full intent hash sensitivity; strict required/advisory gates; save-boundary failures; old/new/corrupt artifact recovery; concurrent promotion/run conflicts; process restart never replays uncertain acknowledged work |
| Parallel: C | Inventory and close remaining configuration/security routes; config + host integrator | P2-05/07 | Writer/reader route matrix; corruption preservation; complete coordinated updates; failure propagation through public adapters; sentinel secret tests; real key/export/reader recovery fixtures |
| Parallel: D | Migration exclusive admission and private progress; migration/provider maintainers | P3-06/08 | Barrier-controlled same/competing-token calls; two processes sharing target/store; caller mutation isolation; crash ownership recovery; actual intermediate DDL reconciliation; unsupported rollback visible |
| Start now | Provider/helper capability inventory and reusable conformance fixtures; provider maintainer | P3-01/02/03/09, P1-11 | Provider/version/OS matrix; not-advertised vs unsupported vs tested support; real write/transaction/key/restart tests; dialect quoting/parameterization/type/nullability and malformed rule coverage |
| After ownership contracts | Forms/plugin lifecycle and native/provider-run recovery; runtime/Forms maintainers | P2-08/10, P1-10/12 | Queued old callback cannot affect replacement; no new work after shutdown; defined overlap/UI dispatch; callbacks drain; unloading/subscription retention tests; uncertain native recovery cannot become success |
| After baseline + capabilities | Streaming, bulk writes, telemetry and bounded retention; execution maintainer | P4-01 through P4-08 | First write before source exhaustion; fixed buffer bounds across source/transform/retry/reject paths; measured allocations and cancellation; per-record bulk acknowledgements; redacted bounded diagnostics; large Forms/plugin workloads |
| Start now; final release gate | Reproducible SDK/CI/pack/API/docs; release maintainer | P5-01 through P5-08 | Pinned dependencies/support policy; complete OS/TFM suite inventory; isolated build without sibling assets; produced-package consumers; API compatibility; warning budgets; compiling examples and current skills |

### First Change: Defaults Qualification

1. Add failing concrete regressions for malformed ADDDAYS and other resolver
   fallbacks, cross-editor rebinding and raw diagnostic leakage. Preserve the nine
   catalog cases and assert typed configuration failure with zero fabricated rows.
2. Define an editor/run-owned required resolver result that separates resolved,
   rejected, failed and cancelled outcomes. Decide how existing custom registration
   is adapted before replacing static routing; do not silently discard plugins.
3. Capture catalog intent separately from caller-owned configuration. Define
   explicit defaults, implicit defaults and no-default opt-out without treating a
   previously loaded nonempty catalog list as fresh explicit intent. Use a closed
   literal copy policy, not IsValueType as an immutability guarantee.
4. Admit both forward and reverse configurations before the first write. Capture
   resolver/policy context for the operation; missing required dependencies deny
   admission. Earlier acknowledgements remain evidence if later failure occurs.
5. Qualify lower helper/custom adapter boundaries and persisted failure evidence,
   including old format compatibility, malformed flags and cancellation/restart.
   Run all TFMs, document changed legacy behavior and then update affected skill
   sources plus installed copies. Do not publish proposed APIs as already available.

### Required Design Decisions

- Choose supported provider/version/OS fixtures from an inventory; do not assume
  helper registrations prove live datasource implementations exist in this checkout.
- Decide durable sync intent/version/cursor ownership and old-format admission
  before designing the publication/recovery protocol. Individual atomic replacement
  cannot make remote target writes and local artifacts one transaction.
- Define execution ownership key, conflict behavior and operator reconciliation.
  A process-local lock or expiring claim is insufficient for unresolved remote work.
- Preserve public legacy interfaces and misspellings; add optional contracts in
  Models. Breaking behavior and stored-format changes require explicit migration
  and release decisions plus package-consumer tests.
- Define host authorization/key management, UI dispatch and drain timeouts at the
  integration boundary. Do not replace unreadable evidence or unavailable keys with
  fresh state, plaintext, automatically expired uncertainty or fabricated success.

## Completion Rules

Each change must include a reproduced regression where feasible, scoped tests,
the full local TFM matrix, compatibility notes and updated affected contracts.
Record OS/SDK/provider versions and remaining limits in the execution log.
Release additionally requires supported-platform CI, live-provider conformance,
clean pack and standalone package consumers. Establish workload baselines before
setting numeric performance targets or calendar estimates.
The entire five-phase enhancement remains incomplete; no broad checkbox changes
or framework-wide security/thread-safety/exactly-once claims follow from this review.
