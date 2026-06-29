// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Migration;

namespace TheTechIdea.Beep.Studio.Migration;

/// <summary>
/// Migration orchestration. Implemented in Phase 5. Wraps the engine's
/// <c>IMigrationManager</c> with a <see cref="StudioResult{T}"/>-shaped API,
/// a <see cref="MigrationPlanHandle"/> for resumable / cancellable runs,
/// and a <see cref="IStudioProgress"/> reporter.
/// </summary>
public interface IMigrationStudioService
{
    // ---- plan building ----
    Task<StudioResult<MigrationPlanHandle>> BuildPlanAsync(MigrationRequest request, IStudioProgress? progress = null, CancellationToken ct = default);

    // ---- plan analysis ----
    Task<StudioResult<MigrationDryRunReport>> DryRunAsync(MigrationPlanHandle planHandle, CancellationToken ct = default);
    Task<StudioResult<MigrationPreflightReport>> PreflightAsync(MigrationPlanHandle planHandle, CancellationToken ct = default);
    Task<StudioResult<MigrationImpactReport>> ImpactAsync(MigrationPlanHandle planHandle, CancellationToken ct = default);
    Task<StudioResult<CiValidationReport>> ValidateForCiAsync(MigrationPlanHandle planHandle, CancellationToken ct = default);
    Task<StudioResult<PolicyEvaluationResult>> EvaluatePolicyAsync(MigrationPlanHandle planHandle, CancellationToken ct = default);

    // ---- execution ----
    Task<StudioResult<MigrationExecutionHandle>> ApplyAsync(MigrationPlanHandle planHandle, MigrationExecutionPolicy policy, IStudioProgress? progress = null, CancellationToken ct = default);
    Task<StudioResult<MigrationExecutionHandle>> ResumeAsync(string executionToken, MigrationExecutionPolicy? policy = null, IStudioProgress? progress = null, CancellationToken ct = default);
    Task<StudioResult<bool>> CancelAsync(string executionToken, CancellationToken ct = default);
    Task<StudioResult<MigrationRollbackReport>> RollbackAsync(string executionToken, RollbackPolicy policy, IStudioProgress? progress = null, CancellationToken ct = default);

    // ---- history & state ----
    Task<StudioResult<IReadOnlyList<MigrationHistoryItem>>> GetHistoryAsync(string? sourceName = null, int skip = 0, int take = 100, CancellationToken ct = default);
    Task<StudioResult<MigrationExecutionState>> GetExecutionStateAsync(string executionToken, CancellationToken ct = default);
}

/// <summary>A request to build a migration plan.</summary>
public sealed record MigrationRequest(
    string SourceSourceName,
    string TargetSourceName,
    string? NamespaceName = null,
    string? AssemblyPath = null,
    IReadOnlyList<string>? EntityNames = null,
    bool DetectRelationships = true,
    bool ApplyForeignKeys = false,
    bool ApplyIndexes = false);

/// <summary>Immutable handle to a built plan. Reused across dry-run / preflight / apply.</summary>
public sealed record MigrationPlanHandle(
    string PlanId,
    string PlanHash,
    string SourceSourceName,
    string TargetSourceName,
    DateTimeOffset BuiltAt,
    IReadOnlyList<DdlOperationVm> Operations);

/// <summary>A single DDL operation in a plan.</summary>
public sealed record DdlOperationVm(
    string EntityName,
    string Operation,
    string DdlPreview,
    string RiskLevel,
    string DdlSource,
    string DdlHash,
    IReadOnlyList<string> Dependants,
    IReadOnlyList<string> Warnings);

// PR 17: removed the Studio's local MigrationExecutionPolicy record — it
// collided with the engine's class in TheTechIdea.Beep.Editor.Migration.
// Callers now use the engine type directly.

/// <summary>Handle to a running or completed migration execution.</summary>
public sealed record MigrationExecutionHandle(
    string ExecutionToken,
    string PlanId,
    string PlanHash,
    DateTimeOffset StartedAt,
    MigrationExecutionState State);

/// <summary>The lifecycle state of a migration execution.</summary>
public enum MigrationExecutionState
{
    /// <summary>The execution has been queued but not started.</summary>
    Queued = 0,

    /// <summary>The execution is in flight.</summary>
    Running = 1,

    /// <summary>The execution was paused by the operator.</summary>
    Paused = 2,

    /// <summary>The execution completed without errors.</summary>
    Succeeded = 3,

    /// <summary>The execution failed. A rollback may be in progress or pending.</summary>
    Failed = 4,

    /// <summary>The execution was cancelled by the operator.</summary>
    Cancelled = 5,

    /// <summary>The execution was rolled back after a failure.</summary>
    RolledBack = 6
}

/// <summary>Policy controlling a rollback.</summary>
public sealed record RollbackPolicy(
    bool UseCompensationPlan = true,
    bool RequireApproval = true,
    TimeSpan? OperationTimeout = null);

/// <summary>Result of a rollback attempt.</summary>
public sealed record MigrationRollbackReport(
    bool Success,
    int RolledBackOperations,
    int TotalOperations,
    IReadOnlyList<string> Warnings,
    string? ErrorMessage);

/// <summary>Dry-run report (one row per entity).</summary>
public sealed record MigrationDryRunReport(
    string PlanHash,
    IReadOnlyList<DryRunEntityReport> Entities);

/// <summary>Dry-run row for a single entity.</summary>
public sealed record DryRunEntityReport(
    string EntityName,
    string TargetState,
    string DdlPreview,
    IReadOnlyList<string> Diffs);

/// <summary>Preflight report (one row per check).</summary>
public sealed record MigrationPreflightReport(
    string PlanHash,
    bool IsValid,
    IReadOnlyList<PreflightCheckResult> Checks);

/// <summary>Result of a single preflight check.</summary>
public sealed record PreflightCheckResult(
    string Name,
    string Status,
    string? Message);

/// <summary>Impact report (which entities are referenced, row counts).</summary>
public sealed record MigrationImpactReport(
    string PlanHash,
    IReadOnlyList<ImpactedEntity> Impacted);

/// <summary>A single entity in an impact report.</summary>
public sealed record ImpactedEntity(
    string EntityName,
    IReadOnlyList<string> ReferencedBy,
    int RowCountEstimate);

/// <summary>CI gate result. Used by the CI/CD service (Future Work) and the build pipeline.</summary>
public sealed record CiValidationReport(
    bool Pass,
    IReadOnlyList<CiCheckResult> Checks,
    string PlanHash);

/// <summary>Result of a single CI check.</summary>
public sealed record CiCheckResult(
    string Name,
    bool Pass,
    string? Message);

/// <summary>Result of a policy evaluation against a plan.</summary>
public sealed record PolicyEvaluationResult(
    bool IsAllowed,
    IReadOnlyList<PolicyViolation> Violations);

/// <summary>A single policy violation.</summary>
public sealed record PolicyViolation(
    string Code,
    string Message,
    string Severity);

/// <summary>A history row for a previously-applied migration.</summary>
public sealed record MigrationHistoryItem(
    string SourceName,
    DateTimeOffset AppliedAt,
    string PlanHash,
    int StepCount,
    bool Success,
    string? ErrorMessage,
    string? ApprovedBy);
