# Phase 07 — Data Sync Orchestration (`ISyncStudioService`)

> **Scope:** implement `ISyncStudioService` — the Studio's data-sync orchestration
> sub-service. Wraps the engine's `BeepSyncManager` with a view-model surface, a
> `StudioResult<T>`-shaped API, an `IStudioProgress` reporter, and a `SyncRunQueue`
> + `SyncRunnerHostedService` so sync runs can outlive the host UI (a Blazor tab
> closing doesn't kill an in-flight sync).

> Legend: `[ ]` open · `[~]` in-progress · `[x]` done · `[!]` blocked

---

## Why this phase

The engine's `BeepSyncManager` is the right thing, but:

1. Its surface returns `IErrorsInfo` (engine error type) and uses
   `IProgress<PassedArgs>` (engine progress type). The Studio needs a
   `StudioResult<T>` + `IStudioProgress` shape.
2. Sync runs are inherently long-running. The Blazor Server host can't
   tie a sync run to a single circuit — the user might close the tab
   while a sync is running. The Studio adds a `Channel<SyncRunRequest>` +
   `BackgroundService` so runs survive the circuit.
3. The conflict-resolution UI needs a `ConflictEvidenceVm` the host can
   render in a grid. The engine's `ConflictEvidence` is fine but is not
   directly bindable.
4. Watermarks need to be exposed in a UI-friendly way (current value, last
   advance, overlap window, dedupe strategy).

This phase adds those four things without touching the engine.

## Public surface (this phase fills in)

```csharp
// Contracts/ISyncStudioService.cs
public interface ISyncStudioService
{
    // ---- schema CRUD ----
    Task<StudioResult<IReadOnlyList<SyncSchemaSummary>>> ListSchemasAsync(SyncListFilter? filter = null, CancellationToken ct = default);
    Task<StudioResult<SyncSchemaVm>> GetSchemaAsync(string schemaId, CancellationToken ct = default);
    Task<StudioResult<SyncSchemaVm>> SaveSchemaAsync(SyncSchemaVm schema, CancellationToken ct = default);
    Task<StudioResult<bool>> DeleteSchemaAsync(string schemaId, CancellationToken ct = default);
    Task<StudioResult<ValidationReport>> ValidateSchemaAsync(string schemaId, CancellationToken ct = default);

    // ---- preflight ----
    Task<StudioResult<SyncPreflightReport>> RunPreflightAsync(string schemaId, CancellationToken ct = default);

    // ---- execution ----
    Task<StudioResult<SyncRunHandle>> EnqueueRunAsync(string schemaId, SyncRunOptions? options = null, IStudioProgress? progress = null, CancellationToken ct = default);
    Task<StudioResult<bool>> StopRunAsync(string runId, CancellationToken ct = default);
    Task<StudioResult<SyncRunStatus>> GetRunStatusAsync(string runId, CancellationToken ct = default);
    Task<StudioResult<SyncReconciliationVm>> GetReconciliationAsync(string runId, CancellationToken ct = default);

    // ---- conflicts ----
    Task<StudioResult<IReadOnlyList<ConflictEvidenceVm>>> ListConflictsAsync(string schemaId, int skip = 0, int take = 100, CancellationToken ct = default);
    Task<StudioResult<ConflictResolutionResult>> ResolveConflictAsync(string schemaId, string conflictId, ConflictResolutionAction action, string? decider = null, string? comment = null, CancellationToken ct = default);

    // ---- history ----
    Task<StudioResult<IReadOnlyList<SyncRunHistoryItem>>> GetRunHistoryAsync(string schemaId, int skip = 0, int take = 100, CancellationToken ct = default);
}
```

## Models

```csharp
public sealed record SyncListFilter(
    string? SourceName = null,
    string? DestinationName = null,
    string? SyncType = null,                            // "Full" | "Incremental"
    string? SyncDirection = null,                        // "OneWay" | "Bidirectional"
    bool? HasConflicts = null,
    int Skip = 0,
    int Take = 100);

public sealed record SyncSchemaSummary(
    string SchemaId,
    string Name,
    string SourceSourceName,
    string DestinationSourceName,
    string SyncType,
    string SyncDirection,
    DateTimeOffset? LastRunAt,
    SyncRunState LastRunState,
    int UnresolvedConflictCount);

public enum SyncRunState { Idle, Queued, Running, Succeeded, Failed, Stopped, PartialSuccess }

public sealed record SyncSchemaVm(
    string SchemaId,
    string Name,
    string SourceSourceName,
    string DestinationSourceName,
    string SyncType,                                    // "Full" | "Incremental"
    string SyncDirection,                                // "OneWay" | "Bidirectional"
    WatermarkSnapshotVm Watermark,
    ConflictPolicyVm ConflictPolicy,
    RetryPolicyVm RetryPolicy,
    SyncMappingPolicyVm MappingPolicy,
    SyncDefaultsPolicyVm DefaultsPolicy,
    SyncRulePolicyVm RulePolicy,
    IReadOnlyList<FieldMappingVm> FieldMappings,
    IReadOnlyList<AppFilterVm> Filters);

public sealed record FieldMappingVm(
    string SourceField,
    string DestinationField,
    string? TransformRuleKey,
    string? DefaultValueRuleKey);

public sealed record AppFilterVm(
    string FieldName,
    string Operator,                                     // "Eq" | "Gt" | "Lt" | "In" | "Between" | ...
    object? Value,
    string? RuleKey);

public sealed record WatermarkSnapshotVm(
    string Mode,                                         // "Timestamp" | "Sequence" | "CompositeKey"
    string Field,
    string? LastValue,
    int OverlapWindowSeconds,
    string DedupeStrategy);                             // "LastWrite" | "SourcePrimary" | "None"

public sealed record ConflictPolicyVm(
    string ResolutionRuleKey,
    string? QuarantineDsName,
    string? QuarantineEntity,
    bool CaptureEvidence,
    int MaxConflictsPerRun,
    string OnMaxExceededAction);                        // "Abort" | "Continue" | "QuarantineRest"

public sealed record RetryPolicyVm(
    int MaxAttempts,
    int BaseDelayMs,
    string BackoffMode,                                  // "Linear" | "Exponential" | "Fixed"
    string? ErrorCategoryRuleKey);

public sealed record SyncMappingPolicyVm(
    bool Enabled,
    int MinQualityScore,
    string RequiredApprovalState,
    string OnDriftAction);

public sealed record SyncDefaultsPolicyVm(
    bool Enabled,
    bool ApplyOnInsert,
    bool ApplyOnUpdate,
    string? ProfileKey);

public sealed record SyncRulePolicyVm(
    bool Enabled,
    string? CatalogVersion,
    int MaxDepth,
    int MaxExecutionMs);

public sealed record ConflictEvidenceVm(
    string ConflictId,
    string SchemaId,
    DateTimeOffset DetectedAt,
    string EntityName,
    string Key,
    object? SourceValue,
    object? DestinationValue,
    string Policy,
    string ResolutionRuleKey);

public enum ConflictResolutionAction { SourceWins, DestinationWins, LatestTimestampWins, Quarantine, ManualOverride }

public sealed record ConflictResolutionResult(
    bool Success,
    string Action,
    string RuleKeyWritten);

public sealed record SyncRunOptions(
    int Priority = 0,
    string? Requestor = null,
    bool WaitForCompletion = false,
    TimeSpan? WaitTimeout = null);

public sealed record SyncRunHandle(
    string RunId,
    string SchemaId,
    DateTimeOffset QueuedAt,
    SyncRunState State);

public sealed record SyncRunStatus(
    string RunId,
    string SchemaId,
    SyncRunState State,
    DateTimeOffset QueuedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int? RowsProcessed,
    int? RowsTotal,
    string? CurrentPhase,
    string? CurrentEntity,
    int? CurrentBatch,
    int? TotalBatches,
    string? ErrorMessage);

public sealed record SyncReconciliationVm(
    string RunId,
    string SchemaId,
    int RowsInserted,
    int RowsUpdated,
    int RowsDeleted,
    int RowsSkipped,
    int ConflictsDetected,
    int ConflictsResolved,
    int ConflictsQuarantined,
    TimeSpan Duration,
    DateTimeOffset WatermarkBefore,
    DateTimeOffset WatermarkAfter);

public sealed record SyncRunHistoryItem(
    string RunId,
    string SchemaId,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    SyncRunState State,
    int RowsProcessed,
    int Conflicts,
    string? ErrorMessage);

public sealed record ValidationReport(
    bool IsValid,
    IReadOnlyList<ValidationIssue> Issues);

public sealed record ValidationIssue(
    string Code,
    string Message,
    string Severity);                                    // "Info" | "Warn" | "Error"
```

## Folder layout (this phase creates)

```
Services/Studio/
├── Contracts/ISyncStudioService.cs                   ← DONE in Phase 1
├── Models/  (all the Sync*Vm records above)
└── Sync/
    ├── SyncStudioService.cs                          ← implements ISyncStudioService
    ├── SyncSchemaDesigner.cs                         ← wraps BeepSyncManager.AddSyncSchema / UpdateSyncSchema / ValidateSchema
    ├── SyncSchemaPersistence.cs                      ← read/write of sync-schemas.json
    ├── SyncRunQueue.cs                               ← Channel<SyncRunRequest> singleton
    ├── SyncRunRequest.cs                             ← internal record
    ├── SyncRunnerHostedService.cs                    ← BackgroundService that dequeues
    ├── SyncProgressForwarder.cs                      ← IStudioProgress → SyncRunStatus
    ├── SyncConflictResolver.cs                       ← wraps the Rule Engine (sync.conflict.* keys)
    ├── SyncReconciliationView.cs                     ← wraps LastRunReconciliationReport
    ├── SyncWatermarkReader.cs                        ← reads WatermarkPolicy from a schema
    └── SyncTelemetry.cs                              ← emits SLO + alert metrics via IBeepLog
```

## Background runner

`SyncRunnerHostedService` is a `BackgroundService` (registered in
`AddBeepStudio()` via `services.AddHostedService<SyncRunnerHostedService>()`).
It:

1. Waits for items on `SyncRunQueue`.
2. For each item: opens the `BeepSyncManager`, calls `SyncDataAsync(schema, ct, progress, ...)`.
3. Forwards `IProgress<PassedArgs>` to `IStudioProgress` (the host's callback).
4. Updates `SyncRunStatus` as the run progresses.
5. Writes a `SyncRunHistoryItem` to `sync-run-history.json` on completion.
6. Records an `IBeepAudit` event (wired in Phase 8).
7. Emits SLO + alert events via the engine's existing `EmitSloMetrics` /
   `EvaluateAlertRules` paths.

The host's Blazor/WinForms/WPF UI subscribes to the progress via a
SignalR hub (Blazor) or a `IProgress<T>` callback (WinForms/WPF/Maui).
The `SyncProgressForwarder` is the cross-platform adapter.

## Conflict resolution bridge

`SyncConflictResolver.ResolveConflictAsync` writes a `ConflictResolutionRule`
to the engine's existing **Rule Engine** under the key
`sync.conflict.<action>` (e.g. `sync.conflict.source-wins`,
`sync.conflict.destination-wins`, `sync.conflict.latest-timestamp-wins`,
`sync.conflict.fail-on-conflict`). The engine's built-in rule keys are
defined in `DataManagementModelsStandard/Editor/BeepSync/ConflictPolicy.cs:14-15`.

The host can also write a custom rule key and the Studio forwards it as-is.

## Cross-cutting

- The Studio does **not** call `BeepSyncManager.SyncAllDataParallelAsync(...)` — that
  is the engine's bulk path and the Studio uses it only when the user explicitly
  asks for "sync all schemas in parallel."
- The `SyncRunQueue` is a `Channel<SyncRunRequest>` registered as a **singleton**
  (per-process). The hosted service is the only consumer.
- Every `EnqueueRunAsync` records an `IBeepAudit` event with the schema id, the
  requestor, the priority, and the run id (Phase 8).
- The `SyncSchemaVm` is **immutable** from the host's perspective — the host can
  read it and pass it back to `SaveSchemaAsync`, but the in-memory state lives in
  the engine.

---

## Todo Tracker

| # | Task | Status | Notes |
|---|------|--------|-------|
| P07-01 | All `Models/*.cs` for this phase (~25 POCOs) | ⬜ | |
| P07-02 | `Sync/SyncSchemaPersistence.cs` — read/write of `sync-schemas.json` | ⬜ | |
| P07-03 | `Sync/SyncSchemaDesigner.cs` — wraps `BeepSyncManager.AddSyncSchema` / `UpdateSyncSchema` / `ValidateSchema` | ⬜ | |
| P07-04 | `Sync/SyncConflictResolver.cs` — Rule Engine bridge | ⬜ | |
| P07-05 | `Sync/SyncReconciliationView.cs` — wraps `LastRunReconciliationReport` | ⬜ | |
| P07-06 | `Sync/SyncWatermarkReader.cs` — reads `WatermarkPolicy` | ⬜ | |
| P07-07 | `Sync/SyncProgressForwarder.cs` — `IProgress<PassedArgs>` → `IStudioProgress` | ⬜ | |
| P07-08 | `Sync/SyncRunQueue.cs` — `Channel<SyncRunRequest>` | ⬜ | |
| P07-09 | `Sync/SyncRunRequest.cs` — internal record | ⬜ | |
| P07-10 | `Sync/SyncRunnerHostedService.cs` — `BackgroundService` | ⬜ | |
| P07-11 | `Sync/SyncStudioService.cs` — implements `ISyncStudioService` | ⬜ | |
| P07-12 | `Sync/SyncTelemetry.cs` — SLO + alert hooks | ⬜ | |
| P07-13 | Wire `ISyncStudioService`, `SyncRunQueue`, `SyncRunnerHostedService` into `AddBeepStudio()` | ⬜ | |
| P07-14 | Tests: `SyncRunQueueTests` (2+), `SyncSchemaDesignerTests` (3+), `SyncStudioServiceTests` (3+), `SyncConflictResolverTests` (2+), `SyncProgressForwarderTests` (2+) | ⬜ | |
| P07-15 | Update `00-overview-and-scope.md` + `MASTER-TODO-TRACKER.md` to mark Phase 07 done | ⬜ | |

---

## Validation (definition of done)

- [ ] `dotnet build DataManagementEngineStandard` succeeds with **0 errors**.
- [ ] `SaveSchemaAsync` with a OneWay Sqlite → SQLServer schema persists the schema to `sync-schemas.json`.
- [ ] `ValidateSchemaAsync` on a well-formed schema returns `IsValid = true`.
- [ ] `EnqueueRunAsync` returns a `SyncRunHandle` immediately; the run executes in the background.
- [ ] `GetRunStatusAsync` reports `Running` while the run is in flight; `Succeeded` after it finishes.
- [ ] `ListConflictsAsync` on a schema with 3 conflicts returns 3 `ConflictEvidenceVm` records.
- [ ] `ResolveConflictAsync` with `SourceWins` writes a `sync.conflict.source-wins` rule to the engine's Rule Engine.
- [ ] `SyncRunnerHostedService` is registered as a `IHostedService` and the host can `IHostedService.StartAsync(...)` it.
- [ ] All 12+ new tests pass.

---

## Pitfalls

1. **Don't run sync in the Blazor Server circuit** — use the hosted service. The circuit dies on browser close; the hosted service is process-lifetime.
2. **Don't lose the `LastWatermarkValue` on failure** — `BeepSyncManager` checkpoints it; never advance it manually.
3. **Don't bypass the `BeepSyncManager` strict destination-acceptance preflight** — `RunPreflightAsync` is a public method; the host calls it before `EnqueueRunAsync` and surfaces the report.
4. **Don't resolve conflicts in the Studio** — write the rule to the Rule Engine and let the next sync run apply it.
5. **Don't write the same `SyncRunHistoryItem` twice** — key by `(schemaId, runId)`. The `SyncRunnerHostedService` is the only writer.
6. **Don't expose `IErrorsInfo` to the host** — convert to `StudioError` with the right `StudioErrorCode`.
7. **Don't enable `SyncAllDataParallelAsync` by default** — the Studio runs schemas sequentially unless the user explicitly asks for parallel.

---

## Related

- Phase 01 — contracts (this phase implements `ISyncStudioService`)
- Phase 06 — migration orchestration (parallel feature for schema, not data)
- Phase 08 — governance (every run is audited)
- Phase 09 — platform adapters (the `SyncProgressForwarder` lives there; this phase declares the interface)
- `BeepDM/DataManagementEngineStandard/Editor/BeepSync/BeepSyncManager.Core.cs:30` — the engine surface we wrap
- `BeepDM/DataManagementEngineStandard/Editor/BeepSync/Examples/` — 8 example schemas to learn from
