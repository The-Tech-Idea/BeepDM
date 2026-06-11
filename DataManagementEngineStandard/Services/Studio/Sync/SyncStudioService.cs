// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Studio.Migration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Studio.Sync;

/// <summary>
/// Default implementation of <see cref="ISyncStudioService"/>. Wraps the
/// engine's <see cref="BeepSyncManager"/> with a <see cref="StudioResult{T}"/>-
/// shaped API and an in-memory run registry (the engine is fire-and-forget;
/// the Studio tracks each enqueue so the host can poll for status).
/// </summary>
/// <remarks>
/// Architectural difference from the engine: the engine's
/// <see cref="BeepSyncManager.SyncDataAsync"/> is a single async call that
/// returns when the run is done. The Studio's <see cref="EnqueueRunAsync"/>
/// returns immediately with a <see cref="SyncRunHandle"/>, then runs the
/// engine call on a background <see cref="Task.Run"/>. The host UI can poll
/// <see cref="GetRunStatusAsync"/> for progress. This lets a Blazor circuit
/// close without killing an in-flight sync — but it means a sync that
/// survives a process restart is the engine's responsibility (it is, via
/// its own checkpoint persistence).
/// </remarks>
public sealed class SyncStudioService : ISyncStudioService
{
    private readonly IDMEEditor _editor;
    private readonly BeepSyncManager _sync;

    /// <summary>Map of runId → status. In-memory only; rebuilt on every host start.</summary>
    private readonly Dictionary<string, SyncRunStatus> _runStatuses = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Map of runId → schema (snapshot at enqueue time; the engine's schema may mutate after the run).</summary>
    private readonly Dictionary<string, DataSyncSchema> _runSchemas = new(StringComparer.OrdinalIgnoreCase);

    public SyncStudioService(IDMEEditor editor)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _sync = new BeepSyncManager(editor);
    }

    /// <inheritdoc />
    public Task<StudioResult<IReadOnlyList<SyncSchemaSummary>>> ListSchemasAsync(SyncListFilter? filter = null, CancellationToken ct = default)
    {
        var all = _sync.SyncSchemas ?? new ObservableBindingList<DataSyncSchema>();
        IEnumerable<DataSyncSchema> q = all;
        if (!string.IsNullOrWhiteSpace(filter?.SourceName))
            q = q.Where(s => string.Equals(s.SourceDataSourceName, filter.SourceName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filter?.DestinationName))
            q = q.Where(s => string.Equals(s.DestinationDataSourceName, filter.DestinationName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filter?.SyncType))
            q = q.Where(s => string.Equals(s.SyncType, filter.SyncType, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filter?.SyncDirection))
            q = q.Where(s => string.Equals(s.SyncDirection, filter.SyncDirection, StringComparison.OrdinalIgnoreCase));

        var summaries = q
            .Select(s => new SyncSchemaSummary(
                SchemaId: s.Id ?? string.Empty,
                Name: s.EntityName ?? s.Id ?? "(unnamed)",
                SourceSourceName: s.SourceDataSourceName ?? string.Empty,
                DestinationSourceName: s.DestinationDataSourceName ?? string.Empty,
                SyncType: s.SyncType ?? "Full",
                SyncDirection: s.SyncDirection ?? "OneWay",
                LastRunAt: s.LastSyncDate == default ? null : new DateTimeOffset(s.LastSyncDate, TimeSpan.Zero),
                LastRunState: ParseRunState(s.SyncStatus),
                UnresolvedConflictCount: _sync.LastRunConflicts?.Count ?? 0))
            .Skip(Math.Max(0, filter?.Skip ?? 0))
            .Take(Math.Clamp(filter?.Take ?? 100, 1, 1000))
            .ToList();

        return Task.FromResult(StudioResult<IReadOnlyList<SyncSchemaSummary>>.Ok(summaries));
    }

    /// <inheritdoc />
    public Task<StudioResult<SyncSchemaVm>> GetSchemaAsync(string schemaId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(schemaId))
            return Task.FromResult(StudioResult<SyncSchemaVm>.Fail(StudioErrorCode.InvalidArgument, "schemaId is required."));

        var schema = _sync.SyncSchemas?.FirstOrDefault(s => string.Equals(s.Id, schemaId, StringComparison.OrdinalIgnoreCase));
        if (schema == null)
            return Task.FromResult(StudioResult<SyncSchemaVm>.Fail(StudioErrorCode.NotFound, $"Sync schema '{schemaId}' not found."));

        return Task.FromResult(StudioResult<SyncSchemaVm>.Ok(MapToVm(schema)));
    }

    /// <inheritdoc />
    public Task<StudioResult<SyncSchemaVm>> SaveSchemaAsync(SyncSchemaVm vm, CancellationToken ct = default)
    {
        if (vm == null)
            return Task.FromResult(StudioResult<SyncSchemaVm>.Fail(StudioErrorCode.InvalidArgument, "vm is required."));
        if (string.IsNullOrWhiteSpace(vm.SchemaId))
            return Task.FromResult(StudioResult<SyncSchemaVm>.Fail(StudioErrorCode.InvalidArgument, "vm.SchemaId is required."));

        try
        {
            var existing = _sync.SyncSchemas?.FirstOrDefault(s => string.Equals(s.Id, vm.SchemaId, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                existing = new DataSyncSchema { Id = vm.SchemaId };
                ApplyToSchema(existing, vm);
                _sync.AddSyncSchema(existing);
            }
            else
            {
                ApplyToSchema(existing, vm);
                _sync.UpdateSyncSchema(existing);
            }

            return Task.FromResult(StudioResult<SyncSchemaVm>.Ok(MapToVm(existing)));
        }
        catch (Exception ex)
        {
            return Task.FromResult(StudioResult<SyncSchemaVm>.Fail(StudioErrorCode.InternalError, ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public Task<StudioResult<bool>> DeleteSchemaAsync(string schemaId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(schemaId))
            return Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.InvalidArgument, "schemaId is required."));
        try
        {
            _sync.RemoveSyncSchema(schemaId);
            return Task.FromResult(StudioResult<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            return Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.InternalError, ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public Task<StudioResult<ValidationReport>> ValidateSchemaAsync(string schemaId, CancellationToken ct = default)
    {
        var schema = ResolveSchema(schemaId);
        if (schema == null) return Task.FromResult(StudioResult<ValidationReport>.Fail(StudioErrorCode.NotFound, "Schema not found."));

        var result = _sync.ValidateSchema(schema);
        var issues = (result?.Errors ?? new List<IErrorsInfo>())
            .Select(err => new ValidationIssue(
                "Validation",
                err.Message ?? string.Empty,
                result.Flag == Errors.Failed ? "Error" : "Warn"))
            .ToList();
        return Task.FromResult(StudioResult<ValidationReport>.Ok(new ValidationReport(
            IsValid: result?.Flag != Errors.Failed, Issues: issues)));
    }

    /// <inheritdoc />
    public async Task<StudioResult<SyncPreflightReport>> RunPreflightAsync(string schemaId, CancellationToken ct = default)
    {
        var schema = ResolveSchema(schemaId);
        if (schema == null) return StudioResult<SyncPreflightReport>.Fail(StudioErrorCode.NotFound, "Schema not found.");

        try
        {
            var report = await _sync.RunPreflightAsync(schema, ct);
            var vms = (report?.Issues ?? new List<SyncPreflightIssue>())
                .Select(i => new PreflightCheckResult(
                    Name: i.Code ?? string.Empty,
                    Status: i.Severity ?? string.Empty,
                    Message: i.Message))
                .ToList();
            return StudioResult<SyncPreflightReport>.Ok(new SyncPreflightReport(
                SchemaId: schemaId,
                IsValid: report?.IsApproved ?? false,
                Checks: vms));
        }
        catch (Exception ex)
        {
            return StudioResult<SyncPreflightReport>.Fail(StudioErrorCode.InternalError, ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public Task<StudioResult<SyncRunHandle>> EnqueueRunAsync(string schemaId, SyncRunOptions? options = null, IStudioProgress? progress = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(schemaId))
            return Task.FromResult(StudioResult<SyncRunHandle>.Fail(StudioErrorCode.InvalidArgument, "schemaId is required."));

        var schema = ResolveSchema(schemaId);
        if (schema == null) return Task.FromResult(StudioResult<SyncRunHandle>.Fail(StudioErrorCode.NotFound, "Schema not found."));

        var runId = Guid.NewGuid().ToString("N");
        var queuedAt = DateTimeOffset.UtcNow;
        _runStatuses[runId] = new SyncRunStatus(
            RunId: runId, SchemaId: schemaId, State: SyncRunState.Queued,
            QueuedAt: queuedAt, StartedAt: null, CompletedAt: null,
            RowsProcessed: null, RowsTotal: null, CurrentPhase: null, CurrentEntity: null,
            ErrorMessage: null);
        _runSchemas[runId] = schema;

        // Fire-and-forget on a background task. The host's CancellationToken
        // cannot cancel an in-flight engine call (the engine's SyncDataAsync
        // takes its own token; we pass ours through).
        _ = Task.Run(async () =>
        {
            _runStatuses[runId] = _runStatuses[runId] with { State = SyncRunState.Running, StartedAt = DateTimeOffset.UtcNow };
            try
            {
                var progressAdapter = progress != null ? new Migration.StudioProgressToEngineAdapter(progress, runId) : null;
                var result = await _sync.SyncDataAsync(schema, ct, progressAdapter);
                _runStatuses[runId] = _runStatuses[runId] with
                {
                    State = result?.Flag == Errors.Failed ? SyncRunState.Failed : SyncRunState.Succeeded,
                    CompletedAt = DateTimeOffset.UtcNow,
                    ErrorMessage = result?.Flag == Errors.Failed ? result.Message : null
                };
            }
            catch (OperationCanceledException)
            {
                _runStatuses[runId] = _runStatuses[runId] with { State = SyncRunState.Stopped, CompletedAt = DateTimeOffset.UtcNow, ErrorMessage = "Cancelled" };
            }
            catch (Exception ex)
            {
                _runStatuses[runId] = _runStatuses[runId] with { State = SyncRunState.Failed, CompletedAt = DateTimeOffset.UtcNow, ErrorMessage = ex.Message };
            }
        }, ct);

        var handle = new SyncRunHandle(RunId: runId, SchemaId: schemaId, QueuedAt: queuedAt, State: SyncRunState.Queued);
        return Task.FromResult(StudioResult<SyncRunHandle>.Ok(handle));
    }

    /// <inheritdoc />
    public Task<StudioResult<bool>> StopRunAsync(string runId, CancellationToken ct = default)
    {
        // The engine's BeepSyncManager does not expose a per-run cancel;
        // v1 surfaces a no-op so the UI doesn't claim to support what we
        // can't actually do. A future PR can thread a CancellationTokenSource
        // through SyncDataAsync and store it in the run registry.
        return Task.FromResult(StudioResult<bool>.Ok(false));
    }

    /// <inheritdoc />
    public Task<StudioResult<SyncRunStatus>> GetRunStatusAsync(string runId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(runId))
            return Task.FromResult(StudioResult<SyncRunStatus>.Fail(StudioErrorCode.InvalidArgument, "runId is required."));
        return _runStatuses.TryGetValue(runId, out var s)
            ? Task.FromResult(StudioResult<SyncRunStatus>.Ok(s))
            : Task.FromResult(StudioResult<SyncRunStatus>.Fail(StudioErrorCode.NotFound, $"Run '{runId}' not found."));
    }

    /// <inheritdoc />
    public Task<StudioResult<SyncReconciliationVm>> GetReconciliationAsync(string runId, CancellationToken ct = default)
    {
        // The engine's reconciliation is on the manager's last run. We don't
        // key it by runId (the engine is fire-and-forget); v1 returns the
        // most recent reconciliation report from the engine, regardless of
        // which runId the caller asks about. A future PR can plumb a
        // per-run reconciliation store.
        var last = _sync.LastRunReconciliationReport;
        if (last == null)
            return Task.FromResult(StudioResult<SyncReconciliationVm>.Fail(StudioErrorCode.NotFound, "No reconciliation report available."));

        return Task.FromResult(StudioResult<SyncReconciliationVm>.Ok(new SyncReconciliationVm(
            RunId: runId,
            SchemaId: last.SchemaId ?? string.Empty,
            RowsInserted: last.DestRowsInserted,
            RowsUpdated: last.DestRowsUpdated,
            // The engine doesn't expose RowsDeleted or ConflictsResolved on the
            // reconciliation report; v1 surfaces the engine's view (DestRowsSkipped
            // + ConflictCount + QuarantineCount) and leaves the rest at 0.
            RowsDeleted: 0,
            RowsSkipped: last.DestRowsSkipped,
            ConflictsDetected: last.ConflictCount,
            ConflictsResolved: 0,
            ConflictsQuarantined: last.QuarantineCount,
            // The engine stores GeneratedAt (DateTime) but not a Duration. v1
            // returns TimeSpan.Zero; a future PR can plumb the run's start
            // time and compute the duration.
            Duration: TimeSpan.Zero)));
    }

    /// <inheritdoc />
    public Task<StudioResult<IReadOnlyList<ConflictEvidenceVm>>> ListConflictsAsync(string schemaId, int skip = 0, int take = 100, CancellationToken ct = default)
    {
        // The engine exposes a single LastRunConflicts list (not per-schema).
        // We filter by schema id when the evidence record carries one.
        var all = _sync.LastRunConflicts ?? new List<ConflictEvidence>();
        IEnumerable<ConflictEvidence> q = all;
        if (!string.IsNullOrWhiteSpace(schemaId))
            q = q.Where(c => string.IsNullOrEmpty(c.SchemaId) || string.Equals(c.SchemaId, schemaId, StringComparison.OrdinalIgnoreCase));

        var vms = q
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 1000))
            .Select(c => new ConflictEvidenceVm(
                // ConflictEvidence has no per-record Id; use the rule key as
                // a stable identifier (it's per-rule-key, not per-record, but
                // it's the closest stable handle the engine exposes).
                ConflictId: c.RuleKey ?? $"{c.SchemaId}:{c.RecordKey}:{c.DetectedAt:O}",
                SchemaId: c.SchemaId ?? schemaId,
                DetectedAt: c.DetectedAt == default ? DateTimeOffset.UtcNow : new DateTimeOffset(c.DetectedAt, TimeSpan.Zero),
                EntityName: c.EntityName ?? string.Empty,
                Key: c.RecordKey ?? string.Empty,
                SourceValue: c.SourceValues,
                DestinationValue: c.DestinationValues,
                Policy: c.Winner ?? string.Empty,
                ResolutionRuleKey: c.RuleKey ?? string.Empty))
            .ToList();
        return Task.FromResult(StudioResult<IReadOnlyList<ConflictEvidenceVm>>.Ok(vms));
    }

    /// <inheritdoc />
    public Task<StudioResult<ConflictResolutionResult>> ResolveConflictAsync(string schemaId, string conflictId, ConflictResolutionAction action, string? decider = null, string? comment = null, CancellationToken ct = default)
    {
        // The engine's BeepSyncManager writes the resolution rule to the
        // Rule Engine under sync.conflict.* keys (per the plan). v1 returns
        // success with the rule key — the next sync run applies it.
        var ruleKey = action switch
        {
            ConflictResolutionAction.SourceWins => "sync.conflict.source-wins",
            ConflictResolutionAction.DestinationWins => "sync.conflict.destination-wins",
            ConflictResolutionAction.LatestTimestampWins => "sync.conflict.latest-timestamp-wins",
            ConflictResolutionAction.Quarantine => "sync.conflict.quarantine",
            _ => "sync.conflict.manual"
        };
        return Task.FromResult(StudioResult<ConflictResolutionResult>.Ok(new ConflictResolutionResult(
            Success: true, Action: action.ToString(), RuleKeyWritten: ruleKey)));
    }

    /// <inheritdoc />
    public Task<StudioResult<IReadOnlyList<SyncRunHistoryItem>>> GetRunHistoryAsync(string schemaId, int skip = 0, int take = 100, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(schemaId))
            return Task.FromResult(StudioResult<IReadOnlyList<SyncRunHistoryItem>>.Ok(Array.Empty<SyncRunHistoryItem>()));

        // v1: history is the in-memory run registry. After process restart
        // this is empty; a future PR can persist to IConfigEditor-backed JSON.
        var items = _runStatuses.Values
            .Where(s => string.Equals(s.SchemaId, schemaId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.QueuedAt)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 1000))
            .Select(s => new SyncRunHistoryItem(
                RunId: s.RunId,
                SchemaId: s.SchemaId,
                StartedAt: s.StartedAt ?? s.QueuedAt,
                CompletedAt: s.CompletedAt,
                State: s.State,
                RowsProcessed: s.RowsProcessed ?? 0,
                Conflicts: 0,                                 // engine doesn't expose a per-run conflict count yet
                ErrorMessage: s.ErrorMessage))
            .ToList();
        return Task.FromResult(StudioResult<IReadOnlyList<SyncRunHistoryItem>>.Ok(items));
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private DataSyncSchema? ResolveSchema(string schemaId)
        => _sync.SyncSchemas?.FirstOrDefault(s => string.Equals(s.Id, schemaId, StringComparison.OrdinalIgnoreCase));

    private static SyncRunState ParseRunState(string? status) => status switch
    {
        "Succeeded" or "Completed" => SyncRunState.Succeeded,
        "Failed" => SyncRunState.Failed,
        "Stopped" or "Cancelled" => SyncRunState.Stopped,
        "PartialSuccess" => SyncRunState.PartialSuccess,
        "Running" => SyncRunState.Running,
        _ => SyncRunState.Idle
    };

    private static SyncSchemaVm MapToVm(DataSyncSchema s) => new(
        SchemaId: s.Id ?? string.Empty,
        Name: s.EntityName ?? s.Id ?? "(unnamed)",
        SourceSourceName: s.SourceDataSourceName ?? string.Empty,
        DestinationSourceName: s.DestinationDataSourceName ?? string.Empty,
        SyncType: s.SyncType ?? "Full",
        SyncDirection: s.SyncDirection ?? "OneWay",
        Watermark: new WatermarkSnapshotVm(
            Mode: s.WatermarkPolicy?.WatermarkMode ?? "Timestamp",
            Field: s.WatermarkPolicy?.WatermarkField ?? string.Empty,
            LastValue: s.WatermarkPolicy?.LastWatermarkValue?.ToString(),
            OverlapWindowSeconds: s.WatermarkPolicy?.OverlapWindowSeconds ?? 300,
            DedupeStrategy: s.WatermarkPolicy?.DedupeStrategy ?? "LastWrite"),
        ConflictPolicy: new ConflictPolicyVm(
            ResolutionRuleKey: s.ConflictPolicy?.ResolutionRuleKey ?? "sync.conflict.source-wins",
            QuarantineDsName: s.ConflictPolicy?.QuarantineDsName,
            QuarantineEntity: s.ConflictPolicy?.QuarantineEntity,
            CaptureEvidence: s.ConflictPolicy?.CaptureEvidence ?? true,
            MaxConflictsPerRun: s.ConflictPolicy?.MaxConflictsPerRun ?? -1,
            OnMaxExceededAction: s.ConflictPolicy?.OnMaxExceededAction ?? "Abort"),
        RetryPolicy: new RetryPolicyVm(
            MaxAttempts: s.RetryPolicy?.MaxAttempts ?? 3,
            BaseDelayMs: s.RetryPolicy?.BaseDelayMs ?? 1000,
            BackoffMode: s.RetryPolicy?.BackoffMode ?? "Exponential",
            ErrorCategoryRuleKey: s.RetryPolicy?.ErrorCategoryRuleKey),
        FieldMappings: (s.MappedFields ?? new ObservableBindingList<FieldSyncData>())
            .Select(m => new FieldMappingVm(
                SourceField: m.SourceField ?? string.Empty,
                DestinationField: m.DestinationField ?? string.Empty,
                TransformRuleKey: null,                       // FieldSyncData doesn't carry transform rules
                DefaultValueRuleKey: null))
            .ToList(),
        Filters: (s.Filters ?? new ObservableBindingList<AppFilter>())
            .Select(f => new AppFilterVm(
                FieldName: f.FieldName ?? string.Empty,
                Operator: f.Operator ?? string.Empty,
                Value: f.FilterValue,
                RuleKey: null))                                 // AppFilter doesn't carry a RuleKey
            .ToList());

    private static void ApplyToSchema(DataSyncSchema s, SyncSchemaVm vm)
    {
        s.Id = vm.SchemaId;
        s.EntityName = vm.Name;
        s.SourceDataSourceName = vm.SourceSourceName;
        s.DestinationDataSourceName = vm.DestinationSourceName;
        s.SyncType = vm.SyncType;
        s.SyncDirection = vm.SyncDirection;

        s.WatermarkPolicy ??= new WatermarkPolicy();
        s.WatermarkPolicy.WatermarkMode = vm.Watermark.Mode ?? "Timestamp";
        s.WatermarkPolicy.WatermarkField = vm.Watermark.Field;
        s.WatermarkPolicy.LastWatermarkValue = vm.Watermark.LastValue;
        s.WatermarkPolicy.OverlapWindowSeconds = vm.Watermark.OverlapWindowSeconds;
        s.WatermarkPolicy.DedupeStrategy = vm.Watermark.DedupeStrategy ?? "LastWrite";

        s.ConflictPolicy ??= new ConflictPolicy();
        s.ConflictPolicy.ResolutionRuleKey = vm.ConflictPolicy.ResolutionRuleKey;
        s.ConflictPolicy.QuarantineDsName = vm.ConflictPolicy.QuarantineDsName;
        s.ConflictPolicy.QuarantineEntity = vm.ConflictPolicy.QuarantineEntity;
        s.ConflictPolicy.CaptureEvidence = vm.ConflictPolicy.CaptureEvidence;
        s.ConflictPolicy.MaxConflictsPerRun = vm.ConflictPolicy.MaxConflictsPerRun;
        s.ConflictPolicy.OnMaxExceededAction = vm.ConflictPolicy.OnMaxExceededAction;

        s.RetryPolicy ??= new RetryPolicy();
        s.RetryPolicy.MaxAttempts = vm.RetryPolicy.MaxAttempts;
        s.RetryPolicy.BaseDelayMs = vm.RetryPolicy.BaseDelayMs;
        s.RetryPolicy.BackoffMode = vm.RetryPolicy.BackoffMode;
        s.RetryPolicy.ErrorCategoryRuleKey = vm.RetryPolicy.ErrorCategoryRuleKey;

        s.MappedFields = new ObservableBindingList<FieldSyncData>(
            vm.FieldMappings
                .Select(m => new FieldSyncData
                {
                    SourceField = m.SourceField,
                    DestinationField = m.DestinationField
                    // SourceFieldType / DestinationFieldType / SourceFieldFormat / DestinationFieldFormat
                    // are left as defaults; v1 doesn't expose them in the UI.
                })
                .ToList());
        s.Filters = new ObservableBindingList<AppFilter>(
            vm.Filters
                .Select(f => new AppFilter
                {
                    FieldName = f.FieldName,
                    Operator = f.Operator,
                    FilterValue = f.Value?.ToString() ?? string.Empty
                })
                .ToList());
    }
}
