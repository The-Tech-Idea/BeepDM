# Phase 6 — Audit, Reporting & Telemetry

**Goal:** Make every setup run answerable: who ran what, against which environment, when, with what
result — and keep the record.

**Pre-condition:** Phase 5 (an audit trail without an actor is a log, not an audit trail).

**Files touched:** `DataManagementModelsStandard/SetUp/`, `DataManagementEngineStandard/SetUp/`

---

## ✅ Status: complete

All items P6-01..05 landed (per-item summary in the master tracker). 199/199 tests green;
`AuditTests.cs` (6) covers it.

Notes:

- **`BeepAuditSetupSink.QueryAsync` returns empty by design.** The engine chain stores `AuditEvent`s,
  not `SetupAuditEvent`s; reconstructing the latter would be lossy. Query `IBeepAudit` directly
  (`Source == "SetUp"`). Documented in the code, not hidden.
- **Auditing swallows its own errors** — the one place this framework deliberately inverts the
  "don't swallow" rule, because a dead audit sink must not fail a migration. `AuditSinkFailure_DoesNotFailRun`
  locks it in; enterprise audit-or-abort wraps the sink.
- **The method on `SetupWizard` is `EmitAudit`, not `Audit`** — `Audit` is the namespace, and the
  collision is a real C# ambiguity (`Audit.SetupAuditAction` parses as the method otherwise).
- P6-03 was mostly already done: P1-09 wrote timestamped reports to `ReportOutputPath`, P5 added the
  actor. This phase just derives the default JSONL audit path from the same option.

---

## What's wrong today

| Problem | Evidence |
|---|---|
| Reports evaporate | `SetupReport` is in-memory, returned by `GetReport()`, dropped |
| `ReportOutputPath` unread | never referenced (P1-1I) |
| **Each run destroys the last** | checkpoint is `File.Move(overwrite: true)` — no prior-run record |
| Timings discarded | `SetupStepResult.Elapsed` is measured, then thrown away with the report |
| No telemetry | only `ILogger` + `IProgress` — no metrics, traces, or spans |
| Approvals unverifiable | self-granted with a literal label (P5-D) |

The engine **already has** `Services/Audit/IBeepAudit` — hash-chained, tamper-evident, with retention
and redactors. Setup should use it, not reinvent it.

---

## 6-A  `ISetupAuditSink`

**New:** `DataManagementModelsStandard/SetUp/Audit/ISetupAuditSink.cs`

```csharp
public interface ISetupAuditSink
{
    Task RecordAsync(SetupAuditEvent evt, CancellationToken token = default);
    Task<IReadOnlyList<SetupAuditEvent>> QueryAsync(SetupAuditQuery query, CancellationToken token = default);
}

public sealed record SetupAuditEvent
{
    public string RunId { get; init; }
    public string WizardId { get; init; }
    public string AppId { get; init; }              // P7
    public string Environment { get; init; }
    public string DefinitionHash { get; init; }     // P2 ContentHash — what was applied
    public string StepId { get; init; }
    public SetupAuditAction Action { get; init; }
    public string ActorId { get; init; }            // P5
    public bool ActorAuthenticated { get; init; }   // P5 — never imply solo was authenticated
    public bool Succeeded { get; init; }
    public string Message { get; init; }
    public TimeSpan Elapsed { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}

public enum SetupAuditAction { RunStarted, StepStarted, StepCompleted, StepSkipped, StepFailed,
                               Approved, RollbackStarted, RollbackCompleted, RunCompleted }
```

- **Solo** — `JsonlSetupAuditSink`: **append-only** `.jsonl` beside the state file. Append-only is
  the requirement; the current overwrite is the bug.
- **Enterprise** — `BeepAuditSetupSink`: adapts to the existing `IBeepAudit` for hash-chaining and
  tamper-evidence. Map `SetupAuditEvent` → the engine's audit shape in the adapter.

> `Services/Audit`'s `AuditQuery` has a **different field set** than any Studio-side query — the
> Studio effort hit exactly this and needed a mapper. Read
> `Services/Audit/Models/AuditQuery.cs` before writing the adapter; don't assume field names.

## 6-B  Record the run

`SetupWizard.Run` emits `RunStarted` / `StepStarted` / `Step*` / `RunCompleted`;
`RollbackOrchestrator` (P4) emits `Rollback*`. Every event carries the P2 `DefinitionHash` — that's
what makes "what was applied" answerable rather than inferred.

**Auditing must never fail the run.** A sink error is logged and swallowed:

```csharp
try { await _audit.RecordAsync(evt, token); }
catch (Exception ex) { Logger?.WriteLog($"Audit sink failed (continuing): {ex.Message}"); }
```

This is the one place the repo's "don't swallow" rule is deliberately inverted — an audit outage must
not take down a migration. Enterprise deployments that need audit-or-abort can wrap the sink.

## 6-C  Persist the report

Honour `ReportOutputPath` (P1-1I):

```
{ReportOutputPath}/{wizardId}-{runId}-{yyyyMMddTHHmmssZ}.report.json
```

Timestamped, never overwritten. `SetupReport.ContentHash` (SHA-256 over results) already exists —
keep it stable and exclude timestamps from the hash so two identical runs hash identically.

## 6-D  Telemetry

```csharp
internal static readonly ActivitySource Source = new("TheTechIdea.Beep.SetUp", "1.0.0");
```

One span per run, one child span per step, tagged `beep.setup.step_id`, `beep.setup.wizard_id`,
`beep.setup.environment`, `beep.setup.definition_hash`, `beep.setup.actor_id`.
`SetupStepResult.Elapsed` becomes the span duration instead of being discarded. `ActivitySource` is
zero-cost when nothing listens — no opt-out needed for solo.

Do **not** tag connection strings, credentials, or seed payloads.

## 6-E  Tests

| Test | Guards |
|---|---|
| `AuditLog_IsAppendOnly_AcrossRuns` | 6-A — the overwrite bug |
| `AuditEvent_Carries_DefinitionHash_And_Actor` | 6-B |
| `AuditSinkFailure_DoesNotFailRun` | 6-B |
| `Report_WrittenTo_ReportOutputPath` | 6-C |
| `Report_ContentHash_StableAcrossIdenticalRuns` | 6-C |
| `Spans_Emitted_PerStep` | 6-D |
| `Audit_NeverContains_ConnectionString` | 6-D redaction |

## Files summary

| Action | File | Est. |
|---|---|---|
| New | `Models/SetUp/Audit/ISetupAuditSink.cs` + events | ~90 |
| New | `Engine/SetUp/Audit/JsonlSetupAuditSink.cs` | ~110 |
| New | `Engine/SetUp/Audit/BeepAuditSetupSink.cs` | ~120 |
| New | `Engine/SetUp/Telemetry/SetupActivitySource.cs` | ~40 |
| Modify | `Engine/SetUp/SetupWizard.cs` | ~60 |
| Modify | `Engine/SetUp/Rollback/RollbackOrchestrator.cs` | ~20 |
| New | `tests/SetupWizardTests/AuditTests.cs` | ~200 |
