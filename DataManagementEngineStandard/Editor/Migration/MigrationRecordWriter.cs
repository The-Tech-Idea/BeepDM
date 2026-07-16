using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    /// <summary>
    /// Single-responsibility helper that builds and appends a
    /// <see cref="MigrationRecord"/> to the config editor's migration history.
    ///
    /// <para>
    /// Four call sites used to construct the record by hand:
    /// </para>
    /// <list type="number">
    ///   <item><description><c>MigrationManager.PersistExecutionCheckpoint</c> — full execution snapshot, with step status.</description></item>
    ///   <item><description><c>MigrationTrackingService.ExecuteTrackedMigration</c> and <c>DatasourceManagementService.AppendSchemaToHistory</c> — plan + execution mapping, one step per plan operation. The <c>namePrefix</c> parameter distinguishes the call sites in the persisted history (<c>"Migration"</c> vs <c>"Schema"</c>).</description></item>
    ///   <item><description><c>MigrationManager.TrackMigration</c> — single-operation record, no execution state.</description></item>
    ///   <item><description><c>MigrationManager.Planning.TryTrackMigrationPlan</c> — plan-only record (no execution result), with deterministic <c>MigrationId = plan.PlanId</c> and plan-metadata <c>Notes</c>.</description></item>
    /// </list>
    /// <para>
    /// All four now go through this writer. Failures are logged via
    /// <see cref="IDMEEditor.AddLogMessage"/> and swallowed (the same behaviour
    /// the inlined code had) — persistence is a side-effect of execution, not a
    /// pre-condition for it.
    /// </para>
    ///
    /// <para><b>MigrationId conventions</b></para>
    /// <list type="bullet">
    ///   <item><description><b>Deterministic anchor (lookup key):</b> <c>WriteExecutionSnapshot</c> uses <c>checkpoint.ExecutionToken</c> (the key into <c>ExecutionPlans[token]</c>); <c>WritePlanArtifact</c> uses <c>plan.PlanId</c> (the key for "rerun this plan overwrites the previous snapshot").</description></item>
    ///   <item><description><b>Plan-prefixed summary (traceable):</b> <c>WritePlanExecution</c> and <c>WriteOperation</c> (when called with a <c>planId</c>) use the format <c>{plan.PlanId}-{rand8}</c>. The prefix makes the record discoverable in the history file by its source plan.</description></item>
    ///   <item><description><b>Unprefixed summary (one-shot):</b> <c>WriteOperation</c> without a <c>planId</c> uses a full random Guid. This is the noise floor — 38 per-operation call sites in <c>TrackMigration</c> don't have a plan available at the call site.</description></item>
    /// </list>
    /// </summary>
    internal static class MigrationRecordWriter
    {
        // ── 1. Full execution-snapshot record ─────────────────────────────

        /// <summary>
        /// Build and append a record that captures a complete execution checkpoint,
        /// with per-step status. Used at every <c>PersistExecutionCheckpoint</c>
        /// call site in <see cref="MigrationManager"/>.
        /// </summary>
        public static void WriteExecutionSnapshot(
            IDMEEditor editor,
            MigrationExecutionCheckpoint checkpoint,
            string datasourceName,
            DataSourceType datasourceType)
        {
            if (checkpoint == null) return;
            try
            {
                var snapshot = JsonSerializer.Serialize(checkpoint);
                var record = new MigrationRecord
                {
                    MigrationId  = checkpoint.ExecutionToken,
                    Name         = "ExecuteMigrationPlan.Checkpoint",
                    AppliedOnUtc = DateTime.UtcNow,
                    Success      = checkpoint.IsCompleted && !checkpoint.HasFailed,
                    Notes        = snapshot,
                    Steps        = checkpoint.Steps?.Select(step => new MigrationStep
                    {
                        Operation   = step.OperationKind.ToString(),
                        EntityName  = step.EntityName,
                        ColumnName  = step.MissingColumns?.Count > 0
                                          ? string.Join(",", step.MissingColumns)
                                          : string.Empty,
                        Success     = step.Status == MigrationExecutionStepStatus.Completed
                                      || step.Status == MigrationExecutionStepStatus.Skipped,
                        Message     = $"{step.Status} | attempts={step.AttemptCount} | elapsedMs={step.ElapsedMilliseconds} | {step.Message}",
                        Sql         = string.Empty
                    }).ToList() ?? new List<MigrationStep>()
                };
                AppendRecord(editor, datasourceName, datasourceType, record);
            }
            catch (Exception ex)
            {
                editor?.AddLogMessage("Beep", $"Failed to persist migration checkpoint: {ex.Message}",
                    DateTime.Now, 0, null, Errors.Warning);
            }
        }

        // ── 2. Plan + execution mapping record ──────────────────────────

        /// <summary>
        /// Build and append a record that maps plan operations to their execution
        /// outcomes, with a top-level message. Used by
        /// <see cref="MigrationTrackingService.ExecuteTrackedMigration"/> (with
        /// <c>namePrefix = "Migration"</c>) and
        /// <c>DatasourceManagementService.AppendSchemaToHistory</c> (with
        /// <c>namePrefix = "Schema"</c>). The prefix appears in the persisted
        /// record's <c>Name</c> field so the two call sites are distinguishable
        /// in the migration history.
        ///
        /// <para>The <c>MigrationId</c> is prefixed with <c>plan.PlanId</c> so
        /// the summary record is traceable to the plan that produced it (see
        /// <see cref="MigrationIdConventions"/>).</para>
        /// </summary>
        public static void WritePlanExecution(
            IDMEEditor editor,
            string datasourceName,
            DataSourceType datasourceType,
            MigrationPlanArtifact plan,
            MigrationExecutionResult? result,
            string namePrefix = "Migration")
        {
            if (plan == null) return;
            try
            {
                var record = new MigrationRecord
                {
                    MigrationId  = BuildPlanPrefixedId(plan.PlanId),
                    Name         = $"{namePrefix}_{datasourceName}_{DateTime.UtcNow:yyyyMMddHHmmss}",
                    AppliedOnUtc = DateTime.UtcNow,
                    Success      = result?.Success ?? false,
                    Notes        = result?.Message ?? string.Empty,
                    Steps        = new List<MigrationStep>()
                };

                if (plan.Operations != null)
                {
                    var completedBySequence = result?.Checkpoint?.Steps?
                        .GroupBy(step => step.Sequence)
                        .ToDictionary(group => group.Key, group => group.First())
                        ?? new Dictionary<int, MigrationExecutionStep>();

                    for (var i = 0; i < plan.Operations.Count; i++)
                    {
                        var op = plan.Operations[i];
                        if (op == null) continue;

                        completedBySequence.TryGetValue(i + 1, out var executionStep);
                        var stepSucceeded = executionStep != null
                            ? executionStep.Status == MigrationExecutionStepStatus.Completed
                              || executionStep.Status == MigrationExecutionStepStatus.Skipped
                            : result?.Success == true;

                        record.Steps.Add(new MigrationStep
                        {
                            Operation  = op.Kind == MigrationPlanOperationKind.None ? "Unknown" : op.Kind.ToString(),
                            EntityName = !string.IsNullOrWhiteSpace(op.EntityName) ? op.EntityName : op.TargetName ?? string.Empty,
                            ColumnName = op.MissingColumns != null && op.MissingColumns.Count > 0
                                             ? string.Join(",", op.MissingColumns)
                                             : string.Empty,
                            Success    = stepSucceeded,
                            Message    = executionStep?.Message ?? op.Note ?? string.Empty
                        });
                    }
                }

                AppendRecord(editor, datasourceName, datasourceType, record);
            }
            catch (Exception ex)
            {
                editor?.AddLogMessage("MigrationTracker",
                    $"Failed to write plan-execution record: {ex.Message}",
                    DateTime.Now, 0, null, Errors.Warning);
            }
        }

        // ── 3. Single-operation record ───────────────────────────────────

        /// <summary>
        /// Build and append a minimal record for a single migration operation
        /// (no execution state). Used by <c>MigrationManager.TrackMigration</c>.
        ///
        /// <para>If <paramref name="planId"/> is supplied, the <c>MigrationId</c>
        /// is prefixed with it so the per-operation record is traceable to the
        /// plan that produced it. The 38 in-tree call sites of
        /// <c>TrackMigration</c> don't have a plan available, so they pass
        /// <c>null</c> and get an unprefixed random id. Future callers that
        /// have a plan should pass it.</para>
        /// </summary>
        public static void WriteOperation(
            IDMEEditor editor,
            string operation,
            string entityName,
            string? columnName,
            string? sql,
            IErrorsInfo? result,
            string datasourceName,
            DataSourceType datasourceType,
            string? planId = null)
        {
            if (editor?.ConfigEditor is not ConfigEditor configEditor) return;
            try
            {
                var record = new MigrationRecord
                {
                    MigrationId  = planId is null ? Guid.NewGuid().ToString() : BuildPlanPrefixedId(planId),
                    Name         = operation,
                    AppliedOnUtc = DateTime.UtcNow,
                    Success      = result?.Flag == Errors.Ok,
                    Steps        = new List<MigrationStep>
                    {
                        new MigrationStep
                        {
                            Operation  = operation,
                            EntityName = entityName,
                            ColumnName = columnName,
                            Sql        = sql ?? string.Empty,
                            Success    = result?.Flag == Errors.Ok,
                            Message    = result?.Message
                        }
                    }
                };
                configEditor.AppendMigrationRecord(datasourceName, datasourceType, record);
            }
            catch (Exception ex)
            {
                editor?.AddLogMessage("Beep",
                    $"Failed to track migration operation '{operation}': {ex.Message}",
                    DateTime.Now, 0, null, Errors.Failed);
            }
        }

        // ── 4. Plan-only record (no execution result) ───────────────────

        /// <summary>
        /// Build and append a plan-only record (no execution result). Used by
        /// <c>MigrationManager.Planning.TryTrackMigrationPlan</c> to persist the
        /// plan snapshot when a plan is generated but execution has not (yet)
        /// happened. <c>MigrationId</c> is deterministic (the plan's
        /// <c>PlanId</c>) so re-running the same plan produces a record that
        /// overwrites the previous one in the history.
        /// </summary>
        public static void WritePlanArtifact(
            IDMEEditor editor,
            MigrationPlanArtifact plan,
            string operationName,
            string datasourceName,
            DataSourceType datasourceType)
        {
            if (plan == null) return;
            try
            {
                var record = new MigrationRecord
                {
                    MigrationId  = plan.PlanId,
                    Name         = operationName,
                    AppliedOnUtc = DateTime.UtcNow,
                    Success      = !plan.ReadinessIssues.Any(issue => issue.Severity == MigrationIssueSeverity.Error),
                    Notes        = $"planHash={plan.PlanHash}; lifecycle={plan.LifecycleState}; pending={plan.PendingOperationCount}; usesDiscovery={plan.UsesDiscovery}",
                    Steps        = plan.Operations.Select(operation => new MigrationStep
                    {
                        Operation  = operation.Kind.ToString(),
                        EntityName = operation.EntityName,
                        ColumnName = operation.MissingColumns.Count > 0 ? string.Join(",", operation.MissingColumns) : string.Empty,
                        Sql        = string.Empty,
                        Success    = operation.Kind != MigrationPlanOperationKind.Error,
                        Message    = operation.Note
                    }).ToList()
                };
                AppendRecord(editor, datasourceName, datasourceType, record);
            }
            catch (Exception ex)
            {
                editor?.AddLogMessage("Beep",
                    $"Failed to track migration plan artifact: {ex.Message}",
                    DateTime.Now, 0, null, Errors.Warning);
            }
        }

        // ── Shared append (swallow the per-record exception) ─────────────

        private static void AppendRecord(
            IDMEEditor editor,
            string datasourceName,
            DataSourceType datasourceType,
            MigrationRecord record)
        {
            editor?.ConfigEditor?.AppendMigrationRecord(datasourceName, datasourceType, record);
        }

        // ── MigrationId helpers ─────────────────────────────────────────────

        /// <summary>
        /// Build a <c>MigrationId</c> of the form <c>{planId}-{rand8}</c> where
        /// <c>rand8</c> is 8 hex characters from a new Guid. The prefix makes
        /// summary records traceable to the plan that produced them; the
        /// suffix keeps summary ids unique even when the same plan runs
        /// multiple times. See the class doc comment for the full conventions.
        /// </summary>
        private static string BuildPlanPrefixedId(string planId)
        {
            return $"{planId}-{Guid.NewGuid().ToString("N")[..8]}";
        }
    }
}
