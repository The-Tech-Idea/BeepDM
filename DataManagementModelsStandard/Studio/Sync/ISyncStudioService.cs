// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Studio.Migration;

namespace TheTechIdea.Beep.Studio.Sync;

/// <summary>
/// Data sync orchestration. Implemented in Phase 6. Wraps the engine's
/// <c>BeepSyncManager</c> with a <see cref="StudioResult{T}"/>-shaped API,
/// a <see cref="IStudioProgress"/> reporter, and a background run queue.
/// </summary>
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

/// <summary>Filter for <see cref="ISyncStudioService.ListSchemasAsync"/>.</summary>
public sealed record SyncListFilter(
    string? SourceName = null,
    string? DestinationName = null,
    string? SyncType = null,
    string? SyncDirection = null,
    bool? HasConflicts = null,
    int Skip = 0,
    int Take = 100);

/// <summary>Summary row for a sync schema.</summary>
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

/// <summary>Lifecycle state of a sync run.</summary>
public enum SyncRunState
{
    /// <summary>No run in flight. The schema is idle.</summary>
    Idle = 0,

    /// <summary>The run is queued but not yet started.</summary>
    Queued = 1,

    /// <summary>The run is in flight.</summary>
    Running = 2,

    /// <summary>The run completed without errors.</summary>
    Succeeded = 3,

    /// <summary>The run failed with an unrecoverable error.</summary>
    Failed = 4,

    /// <summary>The run was stopped by the operator.</summary>
    Stopped = 5,

    /// <summary>The run completed but some rows were skipped (e.g. conflicts, mapping issues).</summary>
    PartialSuccess = 6
}

/// <summary>Full view-model of a sync schema. Bindable in any UI.</summary>
public sealed record SyncSchemaVm(
    string SchemaId,
    string Name,
    string SourceSourceName,
    string DestinationSourceName,
    string SyncType,
    string SyncDirection,
    WatermarkSnapshotVm Watermark,
    ConflictPolicyVm ConflictPolicy,
    RetryPolicyVm RetryPolicy,
    IReadOnlyList<FieldMappingVm> FieldMappings,
    IReadOnlyList<AppFilterVm> Filters);

/// <summary>Current watermark state.</summary>
public sealed record WatermarkSnapshotVm(
    string Mode,
    string Field,
    string? LastValue,
    int OverlapWindowSeconds,
    string DedupeStrategy);

/// <summary>The conflict policy attached to a sync schema.</summary>
public sealed record ConflictPolicyVm(
    string ResolutionRuleKey,
    string? QuarantineDsName,
    string? QuarantineEntity,
    bool CaptureEvidence,
    int MaxConflictsPerRun,
    string OnMaxExceededAction);

/// <summary>The retry policy attached to a sync schema.</summary>
public sealed record RetryPolicyVm(
    int MaxAttempts,
    int BaseDelayMs,
    string BackoffMode,
    string? ErrorCategoryRuleKey);

/// <summary>A field mapping (source field → destination field, with optional transforms).</summary>
public sealed record FieldMappingVm(
    string SourceField,
    string DestinationField,
    string? TransformRuleKey,
    string? DefaultValueRuleKey);

/// <summary>A filter applied to the source rows before sync.</summary>
public sealed record AppFilterVm(
    string FieldName,
    string Operator,
    object? Value,
    string? RuleKey);

/// <summary>Options for a single sync run.</summary>
public sealed record SyncRunOptions(
    int Priority = 0,
    string? Requestor = null,
    bool WaitForCompletion = false,
    TimeSpan? WaitTimeout = null);

/// <summary>Handle to a queued or in-flight sync run.</summary>
public sealed record SyncRunHandle(
    string RunId,
    string SchemaId,
    DateTimeOffset QueuedAt,
    SyncRunState State);

/// <summary>Current status of a sync run.</summary>
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
    string? ErrorMessage);

/// <summary>Reconciliation report for a completed run.</summary>
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
    TimeSpan Duration);

/// <summary>A single conflict evidence row.</summary>
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

/// <summary>The action to take on a conflict.</summary>
public enum ConflictResolutionAction
{
    /// <summary>Source wins. The destination row is overwritten with the source value.</summary>
    SourceWins = 0,

    /// <summary>Destination wins. The source row is dropped.</summary>
    DestinationWins = 1,

    /// <summary>The row with the latest timestamp wins.</summary>
    LatestTimestampWins = 2,

    /// <summary>The conflict is moved to a quarantine schema for manual review.</summary>
    Quarantine = 3,

    /// <summary>A human will resolve the conflict later.</summary>
    ManualOverride = 4
}

/// <summary>Result of a conflict resolution.</summary>
public sealed record ConflictResolutionResult(
    bool Success,
    string Action,
    string RuleKeyWritten);

/// <summary>A history row for a previously-completed sync run.</summary>
public sealed record SyncRunHistoryItem(
    string RunId,
    string SchemaId,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    SyncRunState State,
    int RowsProcessed,
    int Conflicts,
    string? ErrorMessage);

/// <summary>Pre-flight report (the result of <see cref="ISyncStudioService.RunPreflightAsync"/>).</summary>
public sealed record SyncPreflightReport(
    string SchemaId,
    bool IsValid,
    IReadOnlyList<PreflightCheckResult> Checks);

/// <summary>Validation report for a sync schema definition.</summary>
public sealed record ValidationReport(
    bool IsValid,
    IReadOnlyList<ValidationIssue> Issues);

/// <summary>A single validation issue.</summary>
public sealed record ValidationIssue(
    string Code,
    string Message,
    string Severity);
