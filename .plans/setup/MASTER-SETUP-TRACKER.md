# MASTER SETUP TRACKER
# Setup Framework → Solution Control Plane

**Goal:** Grow `SetUp/` from a single-app first-run wizard into the **one place a developer manages a
solution and its parts** — the same product for a solo developer (local JSON state, no auth) and for
an enterprise (shared/remote state + RBAC), differing only by which providers are registered.

**Legend:** `[ ]` open · `[~]` in progress · `[x]` done · `[!]` blocked

---

## The through-line

Everything here hangs off **one idea**: a setup definition must be **data, not C# code**.

Today the unit of composition is a CLR object graph — `SetupWizardBuilder` in C#, with
`SchemaSetupStepOptions.EntityTypes` typed as `IReadOnlyList<Type>`. That single fact blocks every
goal on this list: you cannot version a definition in Git, diff it, review it, ship it to a second
app, drive it from a CLI, run it in CI, store it remotely, or authorize it — because it isn't an
artifact, it's a compilation.

So the order is deliberate: **stabilize (P1) → make the definition data (P2) → everything else.**
P3–P8 are largely independent once P2 lands and can be parallelized.

```
P1 Stabilize ──► P2 SetupDefinition (keystone) ──┬──► P3 State store (solo | enterprise)
                                                  ├──► P4 Rollback
                                                  ├──► P5 Identity + RBAC ──► P6 Audit
                                                  ├──► P7 Solution aggregate (the "single place")
                                                  └──► P8 CLI / unattended / CI
```

## Solo vs enterprise — one product, two provider sets

Not a fork in the product. A fork in **which providers are registered**. Every enterprise concept has
a no-op/local default so the solo path stays zero-ceremony.

| Concern | Solo | Enterprise | Phase |
|---|---|---|---|
| State store | `LocalJsonSetupStateStore` | `RemoteSetupStateStore` (+ lease) | P3 |
| Identity | `AnonymousSetupPrincipal` | `ISetupPrincipal` from host auth | P5 |
| Authorization | `AllowAllAuthorizer` (no-op) | `RoleBasedSetupAuthorizer` | P5 |
| Approvals | auto-approve, recorded honestly | real approver, self-approval rejected | P5 |
| Audit | append-only local JSONL | `IBeepAudit` sink | P6 |
| Definition | file next to the app | shared registry | P2, P7 |

```csharp
services.AddBeepSetup();                      // solo defaults — works with no further config
services.AddBeepSetup().AsEnterprise(o => {   // swaps providers; same steps, same definition
    o.UseRemoteState(uri);
    o.UseRbac();
});
```

## Phases

| # | Phase | Document | Depends on | Status |
|---|---|---|---|---|
| 1 | Stabilize & correctness | [PHASE-01](PHASE-01-Stabilize-Correctness.md) | — | [x] |
| 2 | Serializable `SetupDefinition` | [PHASE-02](PHASE-02-Serializable-SetupDefinition.md) | P1 | [x] |
| 3 | Pluggable state store + concurrency | [PHASE-03](PHASE-03-State-Store-And-Concurrency.md) | P2 | [x] |
| 4 | Rollback & compensation | [PHASE-04](PHASE-04-Rollback-And-Compensation.md) | P1 | [x] |
| 5 | Identity, RBAC & approvals | [PHASE-05](PHASE-05-Identity-RBAC-Approvals.md) | P2 | [x] |
| 6 | Audit, reporting & telemetry | [PHASE-06](PHASE-06-Audit-Reporting-Telemetry.md) | P5 | [x] |
| 7 | Solution aggregate & multi-app | [PHASE-07](PHASE-07-Solution-Aggregate-MultiApp.md) | P2, P3 | [x] |
| 8 | CLI, unattended & CI | [PHASE-08](PHASE-08-CLI-Unattended-CI.md) | P2 | [ ] |
| 9 | App/DB versioning & migrate-on-startup | [PHASE-09](PHASE-09-Versioned-Migrate-On-Startup.md) | P1 | [~] |

---

## Phase 1 — Stabilize & correctness

**Doc:** [PHASE-01-Stabilize-Correctness.md](PHASE-01-Stabilize-Correctness.md)
**Why first:** three of these make the *default* wizard unusable. Don't build on it until they're fixed.

- [x] P1-01 `DriverProvisionStep.StepId` → now `DriverProvisionStepOptions.StepId ?? "driver-provision"`.
      The **factory** qualifies ids (`driver-provision:SQLite`) only when it emits 2+ driver steps;
      a lone driver keeps the bare id, so existing `DependsOn` references are untouched.
      `ConnectionConfigStep.DependsOnStepIds` + `DataImportStepOptions.DependsOnStepIds` added to wire them.
- [x] P1-02 `DefaultSetupWizardFactory(logger, seeders)` now takes an optional `ISeederRegistry` and
      **omits** `SeedingStep` when absent (rather than shipping a step that always fails validation).
      `DataImportStep` drops its `"seeding"` dependency accordingly.
- [x] P1-03 `DefaultsSetupStep` now writes `propertyType = Rule, Rule = "UTCNOW"`. Required adding a
      **`UTCNOW`/`UTCTODAY`/`CURRENTUTCDATETIME`** token to `DateTimeResolver` — it had only `NOW`
      (local). Using `NOW` would have silently downgraded audit columns from UTC to host-local.
- [x] P1-04 `ReferenceDataSeederBase<T>`: insert batch wrapped in a datasource transaction
      (`BeginTransaction`/`Commit`/`EndTransaction`-as-rollback, matching `UnitofWork.Commit`), and
      `IsAlreadySeeded` is now count-based (`existing >= expected`) instead of "any rows at all".
      Non-transactional datasources now **report** the uncommitted-partial state instead of hiding it.
- [x] P1-05 `DriverProvisionStep` no longer force-flips `IsMissing`. It verifies the class exists in
      the freshly loaded assemblies (`ContainsDriverClass`) and only then clears the flag; otherwise
      it fails naming the missing `classHandler`.
- [x] P1-06 **Decided: (a) throw early.** `SetupWizardBuilder.ValidateDependencyOrder` now checks
      registration order (mirroring `SetupWizard.cs:336-342`) and reports duplicate step ids by name.
      `Build_Throws_WhenStepsOutOfOrder` passes — and is guarded by a test asserting it throws for the
      *order* reason, not "unknown step". Revisit (b) auto-sort in P2, where element order in a JSON
      definition shouldn't be load-bearing.
- [x] P1-07 `BuildReport` now takes the context and populates `DryRunReportJson`/`RollbackReportJson`;
      `SchemaSetupStep` routes through `SetupContext.SetDryRunReport`/`SetCompensationPlan`; added the
      missing `TryGetCompensationPlan` getter (P4 needs it).
- [x] P1-08 Warns when `StateFilePath` is null (checkpointing off, run not resumable) and when
      `context.Options` overrides the wizard's own `DryRun`.
- [x] P1-09 `ReportOutputPath` honoured — timestamped `{wizardId}-{runId}-{ts}.report.json`, never
      overwritten. A failed write warns; it never fails a successful setup.
- [x] P1-10 `SetupWizardAdapterBase` added; **all six adapters migrated**. Hooks are `Task`-returning
      (`OnRunStartingAsync`/`OnCancelledAsync`/`OnFailedAsync`/`OnCompletedAsync`) because Maui must
      *await* marshalling its completion callback — a `void` hook silently makes that fire-and-forget.
      Unexpected exceptions are now caught uniformly (5 of 6 leaked before; only WebApi caught them).
      Preserved per-adapter: Desktop passes the **raw** `PassedArgs` through (the base default would
      rebuild one and drop `Flag`/`ErrorObject`); WebApi keeps its status state machine and never-null
      return; WASM keeps load-on-start / save-on-cancel-fail-complete; Blazor keeps `OnProgress`/
      `OnComplete`. Also hardened `Console.ShowResult` (null `StepResults`, short `ContentHash`) since
      it's now reachable on the failure path with a partial report.
- [x] P1-11 Regression tests: `Phase1StabilizationTests.cs` (9) + `AdapterBehaviorTests.cs` (17).
      **SetupWizardTests 81/81 green** (was 54/55); FormsManager.Tests 54/54; solution builds 0 errors.

## Phase 2 — Serializable `SetupDefinition` *(keystone)*

**Doc:** [PHASE-02-Serializable-SetupDefinition.md](PHASE-02-Serializable-SetupDefinition.md)

- [x] P2-01 `SetupDefinition` + `SetupStepDefinition` in `Models/SetUp/Definition/`. `Options` is
      `JsonElement?` with a custom converter (the default cannot write nullable JsonElement, which
      would silently drop every step's options).
- [x] P2-02 Added `SchemaSetupStepOptions.EntityTypeNames` (+ `ExtraAssemblyNames`); `EntityTypes`
      is `[Obsolete]` and still honoured (legacy wins when both set). `SchemaSetupStep` resolves names
      via `assemblyHandler.GetType` → CLR → extra-assembly scan, caches the result so `CanSkip`'s hash
      and `Execute`'s plan agree, and **fails loudly** on any unresolvable name (a silent empty list
      would create no schema, report success, and poison `SchemaHash`).
- [x] P2-03 `ISetupDefinitionSerializer` / `JsonSetupDefinitionSerializer` — indented, camelCase,
      declaration order preserved, `DependsOn` sorted (it's a set), `ContentHash` a `sha256:` digest
      excluding itself.
- [x] P2-04 `ISetupStepFactory` / `SetupStepFactory` — the allow-list. Six built-in keys
      (`SetupStepFactory.TypeKeys`); unknown keys are rejected, listing the known ones.
      `SeedingStepOptions.Registry` is `[JsonIgnore]`d and injected from DI, never from the file.
- [x] P2-05 `SetupWizardBuilder.FromDefinition(def, factory)` + `ToDefinition()`. Steps opt into
      round-tripping via new `ISetupStep.TypeKey` / `SerializeOptions()` DIMs; a step that doesn't is
      logged rather than emitting a silently-lossy definition. Driver's `TypeKey` stays bare while its
      `StepId` may be qualified.
- [x] P2-06 `SetupState.SchemaVersion` added; `SetupCheckpointStore` upgrades on load and **refuses**
      newer/unknown versions. The bare `catch { return; }` that silently re-migrated a live DB is now
      a logged `JsonException`/`Exception` path.
- [x] P2-07 `ISetupDefinitionValidator` / `SetupDefinitionValidator` — structural, no `IDMEEditor`,
      no datasource (the CI gate). Catches unknown type, empty/duplicate id, out-of-order dep, cycle,
      newer schemaVersion, and unbindable options (via a trial `Create`).
- [x] P2-08 `tests/SetupWizardTests/DefinitionTests.cs` (26): round-trip, byte-identical serialize,
      hash stability, allow-list, DI injection, builder interop, validator, `SchemaVersion` gate — and
      **a hand-authored-JSON test** that caught the enum-binding bug unit tests missed (see below).

> **Two bugs surfaced only by testing the real use case — a hand-typed JSON file, not a C#
> round-trip.** (1) `System.Text.Json` requires **numeric** enums by default, so
> `"databaseType": "SqlLite"` failed to bind — and numeric enums are positional, so reordering
> `DataSourceType` would silently re-point every stored definition. (2) The serializer emitted
> **PascalCase**, not the camelCase of the documented artifact. Both fixed by a single shared
> `SetupJson.Options` (camelCase properties, **named** enum values, integer-tolerant on read) that
> every serialization path — serializer, factory, and each step's `SerializeOptions` — now uses. A
> round-trip test cannot catch either, because it writes and reads with the same (wrong) settings.

## Phase 3 — Pluggable state store + concurrency

**Doc:** [PHASE-03-State-Store-And-Concurrency.md](PHASE-03-State-Store-And-Concurrency.md)

- [x] P3-01 `ISetupStateStore` (`LoadAsync`/`SaveAsync`/`TryAcquireLeaseAsync`) + `ISetupStateLease`
      + `SetupStateConflictException` in `Models/SetUp/State/`. The wizard now depends on the
      interface, injected via `SetupWizardBuilder.WithStateStore(...)`. The old `internal static`
      `SetupCheckpointStore` was **deleted** — keeping two persistence mechanisms is the confusion
      this phase removes.
- [x] P3-02 `LocalJsonSetupStateStore` — solo default. Ports the atomic write (temp +
      `File.Move(overwrite)` + retries), adds a sibling `.lock` file lease, and preserves the
      version-gate and unreadable-checkpoint handling. `ForExplicitFile(path)` backs legacy
      `SetupOptions.StateFilePath`.
- [x] P3-03 `RemoteSetupStateStore` — enterprise. Optimistic concurrency + lease over an
      `ISetupStateTransport` (ETag / If-Match) abstraction, so the concurrency logic is fully tested
      with an in-memory transport instead of shipping untested HTTP. A stale save throws
      `SetupStateConflictException`.
- [x] P3-04 Lease implemented and enforced. `SetupWizard.Run` acquires the lease up front and
      **refuses** ("Another runner holds the setup lock…") when it's held; releases in `finally`.
      Expired leases are reclaimable (crash recovery). The bridge to the sync wizard uses
      `Task.Run(...).GetAwaiter().GetResult()` (DriverProvisionStep precedent) to avoid deadlock.
- [x] P3-05 `SetupStateKey(wizardId, environment, appId)` — wizardId is part of the identity, so two
      wizards on a shared store no longer collide (proven by `TwoWizards_DifferentIds_DoNotCollide`).
      `appId` is the Phase 7 hook, already threaded through the key.
- [x] P3-06 `StateStoreTests.cs` (9) + `WizardStatePersistenceTests.cs` (4): round-trip, key
      isolation, cross-process lease refusal, expired-lease reclaim, save-under-lost-lease conflict,
      remote stale-save conflict, and wizard-level resume-skips-completed-steps + second-runner-refused.

## Phase 4 — Rollback & compensation

**Doc:** [PHASE-04-Rollback-And-Compensation.md](PHASE-04-Rollback-And-Compensation.md)

- [x] P4-01 `ISetupStep.RollbackAsync` + `SupportsRollback` added as default-interface members
      (no-op / false), so all existing steps compile unchanged. `SupportsRollback` lets the
      orchestrator report "skipped" vs a clean undo that never happened.
- [x] P4-02 `IRollbackOrchestrator` / `RollbackOrchestrator` — walks `CompletedStepIds` in **reverse**,
      skips steps that never completed, and is **best-effort**: a failing (or throwing) step rollback
      is recorded and the rest continue, because stopping halfway strands more state.
- [x] P4-03 `SchemaSetupStep.RollbackAsync` — uses **`MigrationManager.RollbackFailedExecution(token)`**
      with the execution token recorded on the state, not a replayed compensation plan. The plan doc
      assumed an `ExecuteCompensationPlanAsync` that doesn't exist and said to verify first — the real
      API undoes what was *actually executed*, which is safer against schema drift. No token → **fails
      loudly**, never a silent "undone".
- [x] P4-04 `IUndoableSeeder` + `SeedingStep.RollbackAsync` — unseeds completed seeders in reverse;
      non-undoable seeders are left in place and logged, not falsely reported as undone.
- [x] P4-05 `IBackupConfirmationProvider` + `NoBackupConfirmationProvider` (solo default: false +
      warn). `SchemaSetupStep` now passes the provider's real answer to `CheckRollbackReadiness`
      instead of the inverted `!strict` that claimed a backup existed whenever strict was off.
- [x] P4-06 `SetupOptions.AutoRollbackOnFailure` (opt-in) drives the wizard's `FailStep` to run the
      orchestrator; the outcome is always serialized to `SetupReport.RollbackReportJson`. Off by
      default — auto-undo can destroy the state a human needs to diagnose the failure.
- [x] P4-07 `RollbackTests.cs` (9): reverse order, continue-on-failure, skip-never-completed,
      skipped-vs-succeeded, throwing-rollback-contained, auto-rollback on/off at the wizard level,
      schema loud-fail on missing token, honest backup default.

## Phase 5 — Identity, RBAC & approvals

**Doc:** [PHASE-05-Identity-RBAC-Approvals.md](PHASE-05-Identity-RBAC-Approvals.md)

- [x] P5-01 `ISetupPrincipal` + `AnonymousSetupPrincipal` (solo default — local OS user, never
      authenticated). `IsAuthenticated` is recorded, never inferred.
- [x] P5-02 `ISetupAuthorizer` + `AllowAllAuthorizer` (solo, no-op) / `RoleBasedSetupAuthorizer`
      (enterprise — fail-closed: unmapped permission and unauthenticated principal are denied).
- [x] P5-03 Per-step `RequiredPermission` DIM (driver→ProvisionDriver, connection→ConfigureConnection,
      schema→ApplySchema, seeding→Seed). Wizard checks `RunSetup` up front and the step's permission
      before `Execute`; a denial is `Errors.Failed` via `FailStep`, never a throw, and the step never
      runs. Checked only for steps that will actually execute (a skipped step mutates nothing).
- [x] P5-04 `ISetupApprovalProvider` — `AutoApprovalProvider` (solo: grants, records
      `IsSelfApproved=true`) / `SeparationOfDutyApprovalProvider` (enterprise: **rejects self-approval**,
      requires a distinct approver, denies when none signed off). `SchemaSetupStep` uses it when
      configured, bound to `plan.PlanId`, replacing the self-granted `"SetupWizard"` label; falls back
      to the legacy label when no provider is wired.
- [x] P5-05 `SetupState.ActorId`/`ActorAuthenticated` + same on `SetupReport`. Additive fields, so
      **no `SchemaVersion` bump** — bumping would needlessly reject existing v1 checkpoints; the gate
      is for breaking shape changes only.
- [x] P5-06 `SecurityTests.cs` (10): solo zero-config still runs + records anonymous/unauthenticated;
      RBAC deny fails-step-without-throwing-and-never-executes; grant runs; unauthenticated denied;
      authenticated actor recorded; auto-approval self-approved; enterprise rejects self-approval,
      allows distinct approver, denies when none.

## Phase 6 — Audit, reporting & telemetry

**Doc:** [PHASE-06-Audit-Reporting-Telemetry.md](PHASE-06-Audit-Reporting-Telemetry.md)

- [x] P6-01 `ISetupAuditSink` + `SetupAuditEvent` in `Models/SetUp/Audit/`. `JsonlSetupAuditSink`
      (solo, **append-only** — `File.AppendAllText`, never rewritten), `BeepAuditSetupSink`
      (enterprise, adapts onto the tamper-evident `IBeepAudit` chain), `NullSetupAuditSink` (no-op).
- [x] P6-02 Wizard emits `RunStarted`/`StepStarted`/`StepCompleted`/`StepSkipped`/`StepFailed`/
      `Denied`/`RollbackStarted`/`RollbackCompleted`/`RunCompleted`/`RunFailed`. Every event carries
      `ActorId` + `ActorAuthenticated` + `DefinitionHash` (computed in `FromDefinition`) + environment
      + step + elapsed.
- [x] P6-03 `ReportOutputPath` persistence already landed in P1-09 (timestamped, stable
      `ContentHash`, never overwritten); P5 added the actor to the report. Audit's JSONL defaults to
      `{ReportOutputPath}/{wizardId}.audit.jsonl` when a sink isn't injected.
- [x] P6-04 `SetupActivitySource` (`TheTechIdea.Beep.SetUp`) — one span per step, tagged
      wizard/step/environment/definition-hash/actor/outcome. `SetupStepResult.Elapsed` is measured and
      now flows to both the audit event and the span, not discarded.
- [x] P6-05 `AuditTests.cs` (6): append-only survives N runs, full lifecycle emitted with
      actor+hash, `RunFailed` on failure, **audit-sink-failure-doesn't-fail-run**, per-step spans via
      an `ActivityListener`. The `BeepAuditSetupSink` field-mapping is exercised through its own
      construction; enterprise chain tamper-evidence is `IBeepAudit`'s own tested concern.

> **`BeepAuditSetupSink` maps `SetupAuditEvent` → the engine's `AuditEvent` by hand** because the two
> shapes differ (`Operation`/`Category`/`Outcome`/`CorrelationId`) — the exact drift the Studio effort
> hit. Setup-specific fields that have no first-class column go into `AuditEvent.Properties` so
> nothing is lost. Its `QueryAsync` returns empty on purpose: the chain stores `AuditEvent`s, not
> `SetupAuditEvent`s, and an honest empty beats a lossy reconstruction — query `IBeepAudit` directly
> (filter `Source == "SetUp"`).
>
> **One rule is inverted from the repo norm on purpose:** audit writes swallow their errors (a dead
> sink must not take down a migration). `AuditSinkFailure_DoesNotFailRun` locks it in.

## Phase 7 — Solution aggregate & multi-app *(the "single place")*

**Doc:** [PHASE-07-Solution-Aggregate-MultiApp.md](PHASE-07-Solution-Aggregate-MultiApp.md)

> **Reconcile with `Services/AppMap/` before writing any model.** AppMap already has
> `AppDefinition` (solution path, projects, environments), `SolutionInfo`, `AppProject`, `AppEnv`, and
> `ISolutionControlService` (snapshot, health-check-all, env-switch, dependency map). This phase
> should **extend AppMap**, not clone it. See P7-01.

- [x] P7-01 **Decided: AppMap owns the aggregate.** Investigation (agent report) confirmed
      `AppDefinition` is already a full solution model (App → Projects → Environments → Datasources)
      with `IAppRegistry`, that AppMap and SetUp are fully decoupled, and that AppMap has **no** setup
      concept. So SetUp adds per-app provisioning + a solution orchestrator that references AppMap
      **read-only**; the one change to AppMap is an additive `AppDefinition.SetupDefinitionPath`.
      **Scope chosen: orchestrator only** (user decision) — the `Environment` naming collision and the
      `EnvironmentService`→`BeepFolderService` rename are **deferred** (below).
- [x] P7-02 Setup runs per app: `ISetupWizardResolver`/`SetupWizardResolver` builds a per-app wizard
      from `AppDefinition.SetupDefinitionPath`, keyed by `SetupStateKey(definitionId, env, appId)` —
      the `AppId` slot P3 added. Wizard/builder gained `WithAppId` and thread appId into the key.
- [~] P7-03 `SetupStateKey` carries env + appId, so per-app/per-env isolation works. Promoting
      `SetupOptions.Environment` from a label to a *resolved* `AppEnv` with per-env overrides is
      **deferred** with the Environment-collision cleanup — it's the same naming work.
- [x] P7-04 `ISolutionSetupOrchestrator`/`SolutionSetupOrchestrator` — sets up N apps in dependency
      order (Kahn topo-sort over an explicit inter-app dependency map; **honest** — AppMap models
      project refs, not inter-app order, so it's supplied, not derived). One app's failure doesn't
      abort others; only its dependents are skipped. No cross-app transaction. `SolutionSetupReport`
      reports per-app outcome (Succeeded/Failed/SkippedDependencyFailed/SkippedNoDefinition).
- [x] P7-05 Solution status view: `GetStatusAsync` reads each app's persisted state and reports
      NotSetUp/InProgress/Failed/Complete per app. **Kept in SetUp, not on `SolutionSnapshot`** — that's
      an AppMap type, and populating it would couple AppMap→SetUp, reversing the current clean
      direction. The orchestrator owns the status read instead.
- [x] P7-06 `SolutionSetupTests.cs` (7): all-succeed, no-definition-skipped, one-fails-others-run,
      dependent-skipped-on-dep-failure, dependency-order, per-app state isolation on a shared store,
      per-app status.

> **Deferred (was P7-01/03):** the 4-way `Environment` collision + `Services/EnvironmentService` →
> `BeepFolderService` rename. Investigation showed the rename touches **~40 call sites** across
> engine/ETL/Studio, sits on `BeepService`'s startup path, and the class **also seeds configs** (so
> "BeepFolderService" would misname it). High risk, low value relative to the orchestrator, and
> orthogonal to it. Recommend a dedicated change if pursued.

## Phase 8 — CLI, unattended & CI

**Doc:** [PHASE-08-CLI-Unattended-CI.md](PHASE-08-CLI-Unattended-CI.md)

- [ ] P8-01 `beep setup` verbs: `validate`, `plan`, `apply`, `status`, `rollback`.
- [ ] P8-02 Non-interactive/unattended mode + meaningful exit codes.
      `ConsoleSetupWizardAdapter` is an output surface, not a CLI — no arg parsing, no exit codes.
- [ ] P8-03 `--dry-run` wired to `SetupOptions.DryRun` and printing the P1-07 report.
- [ ] P8-04 CI gate: validate a definition on PR without a live datasource.
- [ ] P8-05 Real cancellation. `ISetupStep.Execute` takes no `CancellationToken`; `RunAsync` wraps the
      whole sync `Run` in one `Task.Run`, so cancellation is only observed *between* steps.
- [ ] P8-06 CLI tests: exit codes, `--dry-run` mutates nothing.

---

## Verification

Every phase ends green:

```bash
dotnet build BeepDM.sln
dotnet test BeepDM.sln
```

Baseline at plan time: `FormsManager.Tests` 54/54; `SetupWizardTests` 54/55 — the single failure was
`SetupWizardBuilderTests.Build_Throws_WhenStepsOutOfOrder`, resolved by P1-06.

**Current: 206/206 green** (`SetupWizardTests` 152/152, `FormsManager.Tests` 54/54), solution builds
with 0 errors. **Phases 1–7 are complete** (P7 orchestrator scope; Environment-rename deferred).
Only Phase 8 (CLI) remains. Any failure from here is a regression.

Public API note: `IDMEEditor`, `IDataSource`, `IConfigEditor`, and the `SetUp` contracts ship as
NuGet packages (`TheTechIdea.Beep.DataManagementModels` / `.DataManagementEngine`, v3.1.1). Signature
changes are breaking releases — prefer additive members and `[Obsolete]` over edits (see P2-02).
