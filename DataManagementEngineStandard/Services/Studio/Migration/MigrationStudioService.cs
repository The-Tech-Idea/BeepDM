// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Migration;
using TheTechIdea.Beep.Studio.Migration;
using TheTechIdea.Beep.Studio.Migration.Ledger;
using TheTechIdea.Beep.Studio.Schema;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Services.Studio.Migration.Ledger;

namespace TheTechIdea.Beep.Studio.Migration;

/// <summary>
/// Default implementation of <see cref="IMigrationStudioService"/>. Wraps the
/// engine's <see cref="IMigrationManager"/> with a <see cref="StudioResult{T}"/>-
/// shaped API, an <see cref="IStudioProgress"/> reporter, and an in-memory
/// cache of plan handles. Real DDL previews are emitted by the engine at
/// execute time; v1 of the Studio surfaces a placeholder for the
/// pre-execution preview.
/// </summary>
public sealed class MigrationStudioService : IMigrationStudioService
{
    private readonly IDMEEditor _editor;
    private readonly IMigrationManager _migration;
    private readonly IMigrationLedger _ledger;

    public IMigrationLedger Ledger => _ledger;

    public MigrationStudioService(IDMEEditor editor, IMigrationLedger? ledger = null)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _ledger = ledger ?? new JsonMigrationLedger(
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BeepDM", "Studio"));
    }

    /// <summary>Map of plan hash → last built plan handle (per-target). Re-used for dry-run / preflight / apply.</summary>
    private readonly Dictionary<string, MigrationPlanHandle> _planCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Map of execution token → last-seen state.</summary>
    private readonly Dictionary<string, MigrationExecutionState> _execStates = new(StringComparer.OrdinalIgnoreCase);

    public MigrationStudioService(IDMEEditor editor)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _migration = new MigrationManager(editor);
    }

    /// <inheritdoc />
    public Task<StudioResult<MigrationPlanHandle>> BuildPlanAsync(
        MigrationRequest request,
        IStudioProgress? progress = null,
        CancellationToken ct = default)
    {
        if (request == null)
            return Task.FromResult(StudioResult<MigrationPlanHandle>.Fail(StudioErrorCode.InvalidArgument, "request is required."));
        if (string.IsNullOrWhiteSpace(request.TargetSourceName))
            return Task.FromResult(StudioResult<MigrationPlanHandle>.Fail(StudioErrorCode.InvalidArgument, "request.TargetSourceName is required."));

        try
        {
            progress?.Report(new StudioProgressUpdate(
                OperationId: Guid.NewGuid().ToString("N"),
                OperationName: "Building migration plan",
                Stage: StudioProgressStage.Begin,
                CurrentStep: $"Target: {request.TargetSourceName}",
                Percent: 0,
                Severity: StudioProgressSeverity.Info,
                Timestamp: DateTimeOffset.UtcNow,
                Payload: null));

            var state = _editor.OpenDataSource(request.TargetSourceName);
            if (state != System.Data.ConnectionState.Open)
                return Task.FromResult(StudioResult<MigrationPlanHandle>.Fail(
                    StudioErrorCode.ConnectionFailed,
                    $"Target data source '{request.TargetSourceName}' could not be opened (state={state})."));

            try
            {
                Assembly? asm = !string.IsNullOrWhiteSpace(request.AssemblyPath) && System.IO.File.Exists(request.AssemblyPath)
                    ? Assembly.LoadFrom(request.AssemblyPath)
                    : null;

                var plan = asm != null
                    ? _migration.BuildMigrationPlan(asm.GetName().Name, asm, request.DetectRelationships, request.ApplyForeignKeys, request.ApplyIndexes)
                    : _migration.BuildMigrationPlan(request.NamespaceName, null, request.DetectRelationships, request.ApplyForeignKeys, request.ApplyIndexes);

                if (plan.ReadinessIssues.Any(i => i.Severity == MigrationIssueSeverity.Error))
                    return Task.FromResult(StudioResult<MigrationPlanHandle>.Fail(
                        StudioErrorCode.PlanRejected,
                        "Plan has blocking readiness issues. See plan.ReadinessIssues for details."));

                var handle = new MigrationPlanHandle(
                    PlanId: plan.PlanId,
                    PlanHash: plan.PlanHash,
                    SourceSourceName: request.SourceSourceName ?? string.Empty,
                    TargetSourceName: request.TargetSourceName,
                    BuiltAt: DateTimeOffset.UtcNow,
                    Operations: (plan.Operations ?? new List<MigrationPlanOperation>())
                        .Select(o => new DdlOperationVm(
                            EntityName: o.EntityName ?? string.Empty,
                            Operation: o.Kind.ToString(),
                            DdlPreview: "(emitted at execute time; see plan.AuditTrail)",
                            RiskLevel: o.RiskLevel.ToString(),
                            DdlSource: "(see GetDdlEvidence)",
                            DdlHash: string.Empty,
                            Dependants: o.ProviderAssumptions ?? new List<string>(),
                            Warnings: o.FallbackTasks ?? new List<string>()))
                        .ToList());

                _planCache[CacheKey(request.TargetSourceName, request.SourceSourceName, handle.PlanHash)] = handle;

                progress?.Report(new StudioProgressUpdate(
                    OperationId: plan.PlanId,
                    OperationName: "Building migration plan",
                    Stage: StudioProgressStage.Complete,
                    CurrentStep: $"Plan built: {handle.Operations.Count} operations",
                    Percent: 100,
                    Severity: StudioProgressSeverity.Info,
                    Timestamp: DateTimeOffset.UtcNow,
                    Payload: new Dictionary<string, object?> { ["planId"] = handle.PlanId }));

                return Task.FromResult(StudioResult<MigrationPlanHandle>.Ok(handle));
            }
            finally
            {
                try { _editor.CloseDataSource(request.TargetSourceName); } catch { /* best-effort */ }
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(StudioResult<MigrationPlanHandle>.Fail(StudioErrorCode.InternalError, $"Build plan failed: {ex.Message}", ex));
        }
    }

    /// <inheritdoc />
    public Task<StudioResult<MigrationDryRunReport>> DryRunAsync(MigrationPlanHandle planHandle, CancellationToken ct = default)
    {
        var plan = ResolvePlan(planHandle);
        if (plan == null) return Task.FromResult(StudioResult<MigrationDryRunReport>.Fail(StudioErrorCode.NotFound, "Plan not in cache; rebuild before re-running."));
        try
        {
            var report = _migration.GenerateDryRunReport(plan);
            return Task.FromResult(StudioResult<MigrationDryRunReport>.Ok(new MigrationDryRunReport(
                PlanHash: plan.PlanHash,
                Entities: (report.Operations ?? new List<MigrationDryRunOperation>())
                    .Select(o => new DryRunEntityReport(
                        EntityName: o.EntityName ?? string.Empty,
                        TargetState: o.Kind.ToString(),
                        DdlPreview: string.Join("\n", o.DdlPreview ?? new List<string>()),
                        Diffs: o.Diagnostics ?? new List<string>()))
                    .ToList())));
        }
        catch (Exception ex)
        {
            return Task.FromResult(StudioResult<MigrationDryRunReport>.Fail(StudioErrorCode.InternalError, ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public Task<StudioResult<MigrationPreflightReport>> PreflightAsync(MigrationPlanHandle planHandle, CancellationToken ct = default)
    {
        var plan = ResolvePlan(planHandle);
        if (plan == null) return Task.FromResult(StudioResult<MigrationPreflightReport>.Fail(StudioErrorCode.NotFound, "Plan not in cache."));
        try
        {
            var report = _migration.RunPreflightChecks(plan);
            return Task.FromResult(StudioResult<MigrationPreflightReport>.Ok(new MigrationPreflightReport(
                PlanHash: plan.PlanHash,
                IsValid: report.CanApply,
                Checks: (report.Checks ?? new List<MigrationPreflightCheck>())
                    .Select(c => new PreflightCheckResult(
                        Name: c.Code ?? string.Empty,
                        Status: c.Decision.ToString(),
                        Message: c.Message))
                    .ToList())));
        }
        catch (Exception ex)
        {
            return Task.FromResult(StudioResult<MigrationPreflightReport>.Fail(StudioErrorCode.InternalError, ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public Task<StudioResult<MigrationImpactReport>> ImpactAsync(MigrationPlanHandle planHandle, CancellationToken ct = default)
    {
        var plan = ResolvePlan(planHandle);
        if (plan == null) return Task.FromResult(StudioResult<MigrationImpactReport>.Fail(StudioErrorCode.NotFound, "Plan not in cache."));
        try
        {
            var report = _migration.BuildImpactReport(plan);
            return Task.FromResult(StudioResult<MigrationImpactReport>.Ok(new MigrationImpactReport(
                PlanHash: plan.PlanHash,
                Impacted: (report.Entries ?? new List<MigrationImpactEntry>())
                    .Select(e => new ImpactedEntity(
                        EntityName: e.EntityName ?? string.Empty,
                        ReferencedBy: e.UsageHints ?? new List<string>(),
                        RowCountEstimate: 0))                       // engine doesn't expose row count
                    .ToList())));
        }
        catch (Exception ex)
        {
            return Task.FromResult(StudioResult<MigrationImpactReport>.Fail(StudioErrorCode.InternalError, ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public Task<StudioResult<CiValidationReport>> ValidateForCiAsync(MigrationPlanHandle planHandle, CancellationToken ct = default)
    {
        var plan = ResolvePlan(planHandle);
        if (plan == null) return Task.FromResult(StudioResult<CiValidationReport>.Fail(StudioErrorCode.NotFound, "Plan not in cache."));
        try
        {
            var report = _migration.ValidatePlanForCi(plan);
            return Task.FromResult(StudioResult<CiValidationReport>.Ok(new CiValidationReport(
                Pass: report.CanMerge,
                Checks: Array.Empty<CiCheckResult>(),              // engine exposes MigrationCiGateResult per gate, not a flat list
                PlanHash: plan.PlanHash)));
        }
        catch (Exception ex)
        {
            return Task.FromResult(StudioResult<CiValidationReport>.Fail(StudioErrorCode.InternalError, ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public Task<StudioResult<PolicyEvaluationResult>> EvaluatePolicyAsync(MigrationPlanHandle planHandle, CancellationToken ct = default)
    {
        var plan = ResolvePlan(planHandle);
        if (plan == null) return Task.FromResult(StudioResult<PolicyEvaluationResult>.Fail(StudioErrorCode.NotFound, "Plan not in cache."));
        try
        {
            var eval = _migration.EvaluateMigrationPlanPolicy(plan);
            var hasBlock = (eval.Findings ?? new List<MigrationPolicyFinding>())
                .Any(f => f.Decision == MigrationPolicyDecision.Block);
            return Task.FromResult(StudioResult<PolicyEvaluationResult>.Ok(new PolicyEvaluationResult(
                IsAllowed: !hasBlock,
                Violations: (eval.Findings ?? new List<MigrationPolicyFinding>())
                    .Where(f => f.Decision != MigrationPolicyDecision.Pass)
                    .Select(f => new PolicyViolation(f.RuleId ?? string.Empty, f.Message ?? string.Empty, f.Decision.ToString()))
                    .ToList())));
        }
        catch (Exception ex)
        {
            return Task.FromResult(StudioResult<PolicyEvaluationResult>.Fail(StudioErrorCode.InternalError, ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public async Task<StudioResult<MigrationExecutionHandle>> ApplyAsync(
        MigrationPlanHandle planHandle,
        MigrationExecutionPolicy policy,
        IStudioProgress? progress = null,
        CancellationToken ct = default)
    {
        var plan = ResolvePlan(planHandle);
        if (plan == null) return StudioResult<MigrationExecutionHandle>.Fail(StudioErrorCode.NotFound, "Plan not in cache.");

        if (!string.IsNullOrWhiteSpace(planHandle.PlanHash))
        {
            var applied = await _ledger.IsAppliedAsync(planHandle.PlanHash, planHandle.TargetSourceName);
            if (applied.IsSuccess && applied.Value)
                return StudioResult<MigrationExecutionHandle>.Fail(StudioErrorCode.AlreadyExists,
                    $"Plan already applied — idempotency gate skipped.");
        }

        var enginePolicy = new MigrationExecutionPolicy
        {
            MaxTransientRetries = policy.MaxTransientRetries,
            RequireOperatorInterventionOnHardFail = policy.RequireOperatorInterventionOnHardFail
            // PR 17: RollbackOnFailure is no longer part of the engine's
            // MigrationExecutionPolicy class (it was a Studio-only field that
            // never matched the engine). The engine's IMigrationManager
            // decides rollback semantics from the failure markers.
        };

        try
        {
            progress?.Report(new StudioProgressUpdate(
                OperationId: plan.PlanId,
                OperationName: "Applying migration plan",
                Stage: StudioProgressStage.Begin,
                CurrentStep: "Starting execution",
                Percent: 0,
                Severity: StudioProgressSeverity.Info,
                Timestamp: DateTimeOffset.UtcNow,
                Payload: new Dictionary<string, object?> { ["planHash"] = plan.PlanHash }));

            var progressAdapter = new StudioProgressToEngineAdapter(progress, plan.PlanId);
            var result = await Task.Run(() => _migration.ExecuteMigrationPlanAsync(plan, enginePolicy, null, progressAdapter, ct), ct);

            var state = result.Success ? MigrationExecutionState.Succeeded : MigrationExecutionState.Failed;
            _execStates[result.ExecutionToken] = state;

            progress?.Report(new StudioProgressUpdate(
                OperationId: plan.PlanId,
                OperationName: "Applying migration plan",
                Stage: result.Success ? StudioProgressStage.Complete : StudioProgressStage.Failed,
                CurrentStep: result.Success ? "Completed" : result.Message,
                Percent: 100,
                Severity: result.Success ? StudioProgressSeverity.Info : StudioProgressSeverity.Error,
                Timestamp: DateTimeOffset.UtcNow,
                Payload: new Dictionary<string, object?> { ["token"] = result.ExecutionToken, ["applied"] = result.AppliedCount }));

            if (result.Success)
            {
                // Record ledger entry
                var entry = new MigrationLedgerEntry
                {
                    Kind = MigrationKind.Schema,
                    Direction = MigrationDirection.Up,
                    Status = MigrationLedgerStatus.Succeeded,
                    PlanId = planHandle.PlanId,
                    PlanHash = planHandle.PlanHash,
                    ExecutionToken = result.ExecutionToken,
                    DatasourceName = planHandle.TargetSourceName ?? planHandle.SourceSourceName ?? "unknown",
                    StepCount = planHandle.Operations?.Count ?? 0,
                    AppliedBy = "system",
                    AppliedAt = DateTimeOffset.UtcNow,
                    CompletedAt = DateTimeOffset.UtcNow,
                };
                _ = _ledger.RecordAsync(entry);

                // Phase 9: record the resulting database version (in-DB marker + JSON mirror), so every
                // Studio caller (web, …) gets version tracking without recomputing it in the UI.
                var versionedDs = planHandle.TargetSourceName ?? planHandle.SourceSourceName;
                if (!string.IsNullOrWhiteSpace(versionedDs))
                    new MigrationTrackingService(_editor).StampDatabaseVersion(versionedDs, plan);

                return StudioResult<MigrationExecutionHandle>.Ok(new MigrationExecutionHandle(
                    ExecutionToken: result.ExecutionToken, PlanId: plan.PlanId, PlanHash: plan.PlanHash,
                    StartedAt: DateTimeOffset.UtcNow, State: state));
            }
            else
                return StudioResult<MigrationExecutionHandle>.Fail(StudioErrorCode.ApplyFailed, result.Message);
        }
        catch (OperationCanceledException)
        {
            return StudioResult<MigrationExecutionHandle>.Fail(StudioErrorCode.Cancelled, "Apply was cancelled.");
        }
        catch (Exception ex)
        {
            return StudioResult<MigrationExecutionHandle>.Fail(StudioErrorCode.InternalError, ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public Task<StudioResult<MigrationExecutionHandle>> ResumeAsync(
        string executionToken,
        MigrationExecutionPolicy? policy = null,
        IStudioProgress? progress = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(executionToken))
            return Task.FromResult(StudioResult<MigrationExecutionHandle>.Fail(StudioErrorCode.InvalidArgument, "executionToken is required."));

        try
        {
            var result = _migration.ResumeMigrationPlan(executionToken,
                policy != null ? new MigrationExecutionPolicy
                {
                    MaxTransientRetries = policy.MaxTransientRetries,
                    RequireOperatorInterventionOnHardFail = policy.RequireOperatorInterventionOnHardFail
                    // PR 17: RollbackOnFailure dropped — see note on ApplyAsync.
                } : null,
                progress != null ? new StudioProgressToEngineAdapter(progress, executionToken) : null);

            var state = result.Success ? MigrationExecutionState.Succeeded : MigrationExecutionState.Failed;
            _execStates[executionToken] = state;

            return Task.FromResult<StudioResult<MigrationExecutionHandle>>(result.Success
                ? StudioResult<MigrationExecutionHandle>.Ok(new MigrationExecutionHandle(
                    ExecutionToken: executionToken, PlanId: string.Empty, PlanHash: string.Empty,
                    StartedAt: DateTimeOffset.UtcNow, State: state))
                : StudioResult<MigrationExecutionHandle>.Fail(StudioErrorCode.ApplyFailed, result.Message));
        }
        catch (Exception ex)
        {
            return Task.FromResult(StudioResult<MigrationExecutionHandle>.Fail(StudioErrorCode.InternalError, ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public Task<StudioResult<bool>> CancelAsync(string executionToken, CancellationToken ct = default)
    {
        // The engine doesn't expose a token-level cancel; the host's
        // CancellationToken is the supported path. v1: no-op.
        return Task.FromResult(StudioResult<bool>.Ok(false));
    }

    /// <inheritdoc />
    public Task<StudioResult<MigrationRollbackReport>> RollbackAsync(
        string executionToken,
        RollbackPolicy policy,
        IStudioProgress? progress = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(executionToken))
            return Task.FromResult(StudioResult<MigrationRollbackReport>.Fail(StudioErrorCode.InvalidArgument, "executionToken is required."));

        try
        {
            var result = _migration.RollbackFailedExecution(executionToken, dryRun: policy.UseCompensationPlan);

            // Record ledger entry for rollback
            if (result.Success)
            {
                var entry = new MigrationLedgerEntry
                {
                    Kind = MigrationKind.Schema,
                    Direction = MigrationDirection.Down,
                    Status = MigrationLedgerStatus.RolledBack,
                    ExecutionToken = executionToken,
                    DatasourceName = "rollback",
                    AppliedBy = "system",
                    AppliedAt = DateTimeOffset.UtcNow,
                    CompletedAt = DateTimeOffset.UtcNow,
                };
                _ = _ledger.RecordAsync(entry);
            }

            return Task.FromResult(StudioResult<MigrationRollbackReport>.Ok(new MigrationRollbackReport(
                Success: result.Success,
                RolledBackOperations: 0,
                TotalOperations: 0,
                Warnings: result.ExecutedActions ?? new List<string>(),
                ErrorMessage: result.Success ? null : result.Message)));
        }
        catch (Exception ex)
        {
            return Task.FromResult(StudioResult<MigrationRollbackReport>.Fail(StudioErrorCode.RollbackFailed, ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public Task<StudioResult<IReadOnlyList<MigrationHistoryItem>>> GetHistoryAsync(string? sourceName = null, int skip = 0, int take = 100, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            return Task.FromResult(StudioResult<IReadOnlyList<MigrationHistoryItem>>.Ok(Array.Empty<MigrationHistoryItem>()));

        try
        {
            var tracker = new MigrationTrackingService(_editor);
            var history = tracker.GetMigrationHistory(sourceName);
            // MigrationHistory has .Migrations: List<MigrationRecord> with MigrationId, Name, AppliedOnUtc, Success, Notes, Steps.
            var items = (history?.Migrations ?? new List<MigrationRecord>())
                .OrderByDescending(r => r.AppliedOnUtc)
                .Skip(Math.Max(0, skip))
                .Take(Math.Clamp(take, 1, 1000))
                .Select(r => new MigrationHistoryItem(
                    SourceName: sourceName,
                    AppliedAt: r.AppliedOnUtc,
                    PlanHash: r.MigrationId ?? string.Empty,
                    StepCount: r.Steps?.Count ?? 0,
                    Success: r.Success,
                    ErrorMessage: r.Notes,
                    ApprovedBy: r.Name))
                .ToList();
            return Task.FromResult(StudioResult<IReadOnlyList<MigrationHistoryItem>>.Ok(items));
        }
        catch (Exception ex)
        {
            return Task.FromResult(StudioResult<IReadOnlyList<MigrationHistoryItem>>.Fail(StudioErrorCode.InternalError, ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public Task<StudioResult<MigrationExecutionState>> GetExecutionStateAsync(string executionToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(executionToken))
            return Task.FromResult(StudioResult<MigrationExecutionState>.Fail(StudioErrorCode.InvalidArgument, "executionToken is required."));
        return Task.FromResult(_execStates.TryGetValue(executionToken, out var s)
            ? StudioResult<MigrationExecutionState>.Ok(s)
            : StudioResult<MigrationExecutionState>.Ok(MigrationExecutionState.Queued));
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private MigrationPlanArtifact? ResolvePlan(MigrationPlanHandle handle)
    {
        // We re-issue BuildMigrationPlan only as a fallback. The fast path is
        // the engine's own ExecutionPlans dictionary (keyed by plan id). For
        // v1, return null on miss and let the caller rebuild.
        var key = CacheKey(handle.TargetSourceName, handle.SourceSourceName, handle.PlanHash);
        return _planCache.ContainsKey(key) ? LookupEnginePlan(handle.PlanId) : null;
    }

    private MigrationPlanArtifact? LookupEnginePlan(string planId)
    {
        // The engine's IMigrationManager has an internal ExecutionPlans
        // dictionary; without a public lookup we return null and the caller
        // re-builds. A future PR can add a "GetCachedPlan" method to the engine.
        return null;
    }

    private static string CacheKey(string target, string? source, string planHash)
        => $"{(source ?? string.Empty).ToLowerInvariant()}|{target.ToLowerInvariant()}|{planHash}";

    // Note: the IStudioProgress → IProgress<PassedArgs> adapter now lives in
    // StudioProgressToEngineAdapter.cs (shared across Migration, Sync, Driver
    // services). The previous private nested class was removed in PR 17.
}
