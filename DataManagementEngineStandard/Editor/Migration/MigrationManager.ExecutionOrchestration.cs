using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Core;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    public partial class MigrationManager
    {
        private static readonly ConcurrentDictionary<string, MigrationExecutionCheckpoint> ExecutionCheckpoints = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, MigrationPlanArtifact> ExecutionPlans = new(StringComparer.OrdinalIgnoreCase);

        public MigrationExecutionCheckpoint CreateExecutionCheckpoint(MigrationPlanArtifact plan, string executionToken = null)
        {
            if (plan == null)
                return new MigrationExecutionCheckpoint();

            var token = string.IsNullOrWhiteSpace(executionToken) ? Guid.NewGuid().ToString("N") : executionToken.Trim();
            var checkpoint = ExecutionCheckpoints.GetOrAdd(token, _ => BuildNewCheckpoint(plan, token));
            checkpoint.UpdatedOnUtc = DateTime.UtcNow;

            if (checkpoint.Steps.Count == 0 && plan.Operations.Count > 0)
            {
                checkpoint.Steps = BuildExecutionSteps(plan.Operations);
                checkpoint.LastCompletedStep = checkpoint.Steps
                    .Where(step => step.Status == MigrationExecutionStepStatus.Completed)
                    .Select(step => step.Sequence)
                    .DefaultIfEmpty(-1)
                    .Max();
            }

            ExecutionPlans[token] = plan;
            PersistExecutionCheckpoint(checkpoint);
            return checkpoint;
        }

        public MigrationExecutionResult ExecuteMigrationPlan(MigrationPlanArtifact plan, MigrationExecutionPolicy policy = null, string executionToken = null, IProgress<PassedArgs> progress = null)
        {
            var result = new MigrationExecutionResult();
            policy ??= new MigrationExecutionPolicy();

            if (MigrateDataSource == null)
            {
                result.Success = false;
                result.Message = "Migration data source is not set.";
                return result;
            }

            if (plan == null)
            {
                result.Success = false;
                result.Message = "Migration plan is null.";
                return result;
            }

            var checkpoint = CreateExecutionCheckpoint(plan, executionToken);
            result.ExecutionToken = checkpoint.ExecutionToken;
            result.Checkpoint = checkpoint;
            RecordExecutionStarted(plan, checkpoint);
            plan.PerformancePlan ??= BuildPerformancePlan(plan);

            plan.CompensationPlan ??= BuildCompensationPlan(plan);
            var hasHighRisk = plan.Operations.Any(operation =>
                operation != null &&
                (operation.RiskLevel == MigrationPlanRiskLevel.High ||
                 operation.RiskLevel == MigrationPlanRiskLevel.Critical ||
                 operation.IsDestructive ||
                 operation.IsTypeNarrowing ||
                 operation.HasNullabilityTightening));
            if (hasHighRisk && (plan.CompensationPlan.Actions == null || plan.CompensationPlan.Actions.Count == 0))
            {
                result.Success = false;
                result.Message = "High-risk operations require compensation actions before apply.";
                RecordDiagnostic(checkpoint.ExecutionToken, checkpoint.CorrelationId, "exec-compensation-missing", MigrationDiagnosticSeverity.Error, string.Empty, result.Message, "Build compensation actions for high-risk operations before apply.");
                RecordExecutionFinished(plan, checkpoint, success: false, notes: result.Message);
                return result;
            }

            plan.RollbackReadinessReport = CheckRollbackReadiness(
                plan,
                backupConfirmed: plan.RollbackReadinessReport?.BackupConfirmed ?? false,
                restoreTestEvidenceProvided: plan.RollbackReadinessReport?.RestoreTestEvidenceProvided ?? false,
                restoreTestEvidence: plan.RollbackReadinessReport?.RestoreTestEvidence);
            if (!plan.RollbackReadinessReport.IsReady)
            {
                result.Success = false;
                result.Message = "Rollback readiness checks failed. Backup/restore evidence is required for protected execution.";
                RecordDiagnostic(checkpoint.ExecutionToken, checkpoint.CorrelationId, "exec-rollback-readiness", MigrationDiagnosticSeverity.Error, string.Empty, result.Message, "Provide backup confirmation and restore-test evidence.");
                RecordExecutionFinished(plan, checkpoint, success: false, notes: result.Message);
                return result;
            }

            plan.PreflightReport = RunPreflightChecks(plan);
            if (!plan.PreflightReport.CanApply)
            {
                result.Success = false;
                result.Message = "Preflight blocked migration plan execution.";
                result.Checkpoint.HasFailed = true;
                result.Checkpoint.FailureCategory = "Preflight";
                result.Checkpoint.FailureReason = result.Message;
                result.Checkpoint.UpdatedOnUtc = DateTime.UtcNow;
                RecordDiagnostic(checkpoint.ExecutionToken, checkpoint.CorrelationId, "exec-preflight-block", MigrationDiagnosticSeverity.Error, string.Empty, result.Message, "Review preflight findings and regenerate plan if needed.");
                PersistExecutionCheckpoint(result.Checkpoint);
                RecordExecutionFinished(plan, checkpoint, success: false, notes: result.Message);
                return result;
            }
            var performancePolicy = plan.PerformancePlan?.Policy ?? new MigrationPerformancePolicy();
            var runStopwatch = Stopwatch.StartNew();
            var processedInBatch = 0;

            foreach (var step in checkpoint.Steps.OrderBy(item => item.Sequence))
            {
                if (step.Status == MigrationExecutionStepStatus.Completed || step.Status == MigrationExecutionStepStatus.Skipped)
                    continue;

                if (step.DependsOn.Count > 0)
                {
                    var depBlocked = step.DependsOn.Any(dep => checkpoint.Steps.All(item =>
                        item.Sequence != dep || item.Status != MigrationExecutionStepStatus.Completed));
                    if (depBlocked)
                    {
                        step.Status = MigrationExecutionStepStatus.Failed;
                        step.Message = "Dependency steps are not complete.";
                        checkpoint.HasFailed = true;
                        checkpoint.FailureCategory = "Dependency";
                        checkpoint.FailureReason = step.Message;
                        checkpoint.UpdatedOnUtc = DateTime.UtcNow;
                        checkpoint.ElapsedMilliseconds += runStopwatch.ElapsedMilliseconds;
                        PersistExecutionCheckpoint(checkpoint);
                        result.Success = false;
                        var outcomes = DescribeFailureRollbackOutcomes(plan, step);
                        result.RollbackOutcome = outcomes.rollbackOutcome;
                        result.CompensationOutcome = outcomes.compensationOutcome;
                        result.Message = $"Execution blocked at step {step.Sequence} due to dependency failure. {result.RollbackOutcome} {result.CompensationOutcome}";
                        RecordDiagnostic(checkpoint.ExecutionToken, checkpoint.CorrelationId, "exec-dependency-failed", MigrationDiagnosticSeverity.Error, step.EntityName, step.Message, "Resolve upstream step failures before resume.");
                        RecordExecutionFinished(plan, checkpoint, success: false, notes: result.Message);
                        return result;
                    }
                }

                var stepWatch = Stopwatch.StartNew();
                var completed = false;
                while (!completed)
                {
                    step.AttemptCount++;
                    step.Status = MigrationExecutionStepStatus.Running;
                    PersistExecutionCheckpoint(checkpoint);

                    var stepResult = ExecuteStep(step);
                    var decision = ClassifyFailure(stepResult?.Message, policy);
                    var ok = IsStepSuccess(stepResult);

                    if (ok)
                    {
                        completed = true;
                        step.Status = MigrationExecutionStepStatus.Completed;
                        step.Message = stepResult?.Message ?? "Completed.";
                        checkpoint.LastCompletedStep = step.Sequence;
                        checkpoint.HasFailed = false;
                        checkpoint.FailureCategory = string.Empty;
                        checkpoint.FailureReason = string.Empty;
                        progress?.Report(new PassedArgs { Messege = $"Migration step {step.Sequence} completed: {step.EntityName} [{step.OperationKind}]" });
                    }
                    else if (decision == "transient" && step.AttemptCount <= Math.Max(0, policy.MaxTransientRetries))
                    {
                        step.Message = $"Transient failure attempt {step.AttemptCount}: {stepResult?.Message}";
                        progress?.Report(new PassedArgs { Messege = $"Retrying step {step.Sequence} after transient failure: {stepResult?.Message}" });
                        RecordRetryCount();
                        RecordDiagnostic(checkpoint.ExecutionToken, checkpoint.CorrelationId, "exec-step-retry", MigrationDiagnosticSeverity.Warning, step.EntityName, step.Message, "Transient failure detected; retry policy applied.");
                        if (policy.RetryDelayMilliseconds > 0)
                            Thread.Sleep(policy.RetryDelayMilliseconds);
                    }
                    else
                    {
                        step.Status = MigrationExecutionStepStatus.Failed;
                        step.Message = stepResult?.Message ?? "Failed.";
                        checkpoint.HasFailed = true;
                        checkpoint.FailureCategory = decision == "hard" ? "HardFail" : "Failure";
                        checkpoint.FailureReason = step.Message;
                        checkpoint.UpdatedOnUtc = DateTime.UtcNow;
                        step.ElapsedMilliseconds += stepWatch.ElapsedMilliseconds;
                        checkpoint.ElapsedMilliseconds += runStopwatch.ElapsedMilliseconds;
                        PersistExecutionCheckpoint(checkpoint);

                        result.Success = false;
                        result.RequiresOperatorIntervention = decision == "hard" && policy.RequireOperatorInterventionOnHardFail;
                        var outcomes = DescribeFailureRollbackOutcomes(plan, step);
                        result.RollbackOutcome = outcomes.rollbackOutcome;
                        result.CompensationOutcome = outcomes.compensationOutcome;
                        RecordDiagnostic(
                            checkpoint.ExecutionToken,
                            checkpoint.CorrelationId,
                            "exec-step-failed",
                            result.RequiresOperatorIntervention ? MigrationDiagnosticSeverity.Critical : MigrationDiagnosticSeverity.Error,
                            step.EntityName,
                            step.Message,
                            result.CompensationOutcome);
                        result.Message = result.RequiresOperatorIntervention
                            ? $"Step {step.Sequence} failed and requires operator intervention. {policy.OperatorInterventionHint} {result.RollbackOutcome} {result.CompensationOutcome}"
                            : $"Step {step.Sequence} failed: {step.Message}. {result.RollbackOutcome} {result.CompensationOutcome}";
                        RecordExecutionFinished(plan, checkpoint, success: false, notes: result.Message);
                        return result;
                    }
                }

                step.ElapsedMilliseconds += stepWatch.ElapsedMilliseconds;
                RecordStepDuration(stepWatch.ElapsedMilliseconds);
                checkpoint.UpdatedOnUtc = DateTime.UtcNow;
                PersistExecutionCheckpoint(checkpoint);

                processedInBatch++;
                if (performancePolicy.EnableThrottledMode && performancePolicy.ThrottleDelayMilliseconds > 0)
                {
                    Thread.Sleep(performancePolicy.ThrottleDelayMilliseconds);
                }

                if (performancePolicy.BatchSize > 0 && processedInBatch >= performancePolicy.BatchSize)
                {
                    processedInBatch = 0;
                    RecordDiagnostic(
                        checkpoint.ExecutionToken,
                        checkpoint.CorrelationId,
                        "exec-batch-boundary",
                        MigrationDiagnosticSeverity.Info,
                        step.EntityName,
                        $"Batch boundary reached after {performancePolicy.BatchSize} operation(s).",
                        "Continue with next batch to reduce lock pressure.");
                }
            }

            checkpoint.IsCompleted = true;
            checkpoint.HasFailed = false;
            checkpoint.FailureCategory = string.Empty;
            checkpoint.FailureReason = string.Empty;
            checkpoint.ElapsedMilliseconds += runStopwatch.ElapsedMilliseconds;
            checkpoint.UpdatedOnUtc = DateTime.UtcNow;
            PersistExecutionCheckpoint(checkpoint);

            result.Success = true;
            result.Message = $"Migration plan executed successfully. Token: {checkpoint.ExecutionToken}";
            RecordDiagnostic(checkpoint.ExecutionToken, checkpoint.CorrelationId, "exec-complete", MigrationDiagnosticSeverity.Info, string.Empty, result.Message, "Execution completed without blocking failures.");
            RecordExecutionFinished(plan, checkpoint, success: true, notes: result.Message);
            return result;
        }

        public MigrationExecutionResult ResumeMigrationPlan(string executionToken, MigrationExecutionPolicy policy = null, IProgress<PassedArgs> progress = null)
        {
            policy ??= new MigrationExecutionPolicy();
            if (string.IsNullOrWhiteSpace(executionToken))
            {
                return new MigrationExecutionResult
                {
                    Success = false,
                    Message = "Execution token is required to resume."
                };
            }

            if (!ExecutionCheckpoints.TryGetValue(executionToken.Trim(), out var checkpoint))
            {
                return new MigrationExecutionResult
                {
                    Success = false,
                    Message = $"No checkpoint found for execution token '{executionToken}'."
                };
            }

            if (checkpoint.IsCompleted)
            {
                return new MigrationExecutionResult
                {
                    Success = true,
                    ExecutionToken = checkpoint.ExecutionToken,
                    Checkpoint = checkpoint,
                    ResumedFromCheckpoint = true,
                    Message = "Execution checkpoint is already completed."
                };
            }

            if (!ExecutionPlans.TryGetValue(checkpoint.ExecutionToken, out var plan))
            {
                plan = BuildPlanFromCheckpoint(checkpoint);
                ExecutionPlans[checkpoint.ExecutionToken] = plan;
            }

            var result = ExecuteMigrationPlan(plan, policy, checkpoint.ExecutionToken, progress);
            result.ResumedFromCheckpoint = true;
            return result;
        }

        public MigrationExecutionCheckpoint GetExecutionCheckpoint(string executionToken)
        {
            if (string.IsNullOrWhiteSpace(executionToken))
                return null;

            ExecutionCheckpoints.TryGetValue(executionToken.Trim(), out var checkpoint);
            return checkpoint;
        }

        private static MigrationExecutionCheckpoint BuildNewCheckpoint(MigrationPlanArtifact plan, string token)
        {
            return new MigrationExecutionCheckpoint
            {
                ExecutionToken = token,
                CorrelationId = Guid.NewGuid().ToString("N"),
                PlanId = plan.PlanId,
                PlanHash = plan.PlanHash,
                StartedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                Steps = BuildExecutionSteps(plan.Operations)
            };
        }

        private static List<MigrationExecutionStep> BuildExecutionSteps(IReadOnlyList<MigrationPlanOperation> operations)
        {
            var steps = new List<MigrationExecutionStep>();
            if (operations == null || operations.Count == 0)
                return steps;

            var previousByEntity = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < operations.Count; index++)
            {
                var operation = operations[index];
                if (operation == null)
                    continue;

                var sequence = index + 1;
                var entityName = operation.EntityName ?? string.Empty;
                var step = new MigrationExecutionStep
                {
                    Sequence = sequence,
                    StepId = $"step-{sequence}",
                    EntityName = entityName,
                    EntityTypeName = operation.EntityTypeName ?? string.Empty,
                    OperationKind = operation.Kind,
                    MissingColumns = operation.MissingColumns?.ToList() ?? new List<string>(),
                    Status = operation.Kind == MigrationPlanOperationKind.UpToDate
                        ? MigrationExecutionStepStatus.Skipped
                        : MigrationExecutionStepStatus.Pending
                };

                if (!string.IsNullOrWhiteSpace(entityName) && previousByEntity.TryGetValue(entityName, out var previous))
                    step.DependsOn.Add(previous);
                if (!string.IsNullOrWhiteSpace(entityName))
                    previousByEntity[entityName] = sequence;

                steps.Add(step);
            }

            return steps;
        }

        private IErrorsInfo ExecuteStep(MigrationExecutionStep step)
        {
            if (step == null)
                return CreateErrorsInfo(Errors.Failed, "Execution step is null.");

            if (step.OperationKind == MigrationPlanOperationKind.UpToDate)
                return CreateErrorsInfo(Errors.Ok, "Step skipped: entity already up to date.");

            if (step.OperationKind == MigrationPlanOperationKind.Error)
                return CreateErrorsInfo(Errors.Failed, "Step is marked as plan error and cannot be executed.");

            var entityType = ResolveType(step.EntityTypeName);
            var desired = entityType != null ? TryGetEntityStructure(entityType) : null;
            if (desired != null && !string.IsNullOrWhiteSpace(step.EntityName))
                desired.EntityName = step.EntityName;

            switch (step.OperationKind)
            {
                case MigrationPlanOperationKind.CreateEntity:
                    if (desired == null)
                        return CreateErrorsInfo(Errors.Failed, $"Cannot resolve entity metadata for '{step.EntityName}'.");
                    return CreateEntity(desired);

                case MigrationPlanOperationKind.AddMissingColumns:
                    if (desired == null)
                        return CreateErrorsInfo(Errors.Failed, $"Cannot resolve entity metadata for '{step.EntityName}'.");
                    if (step.MissingColumns == null || step.MissingColumns.Count == 0)
                        return CreateErrorsInfo(Errors.Warning, $"No missing columns recorded for '{step.EntityName}'.");

                    var failures = new List<string>();
                    foreach (var columnName in step.MissingColumns)
                    {
                        var field = desired.Fields?.FirstOrDefault(candidate =>
                            candidate != null &&
                            string.Equals(candidate.FieldName, columnName, StringComparison.OrdinalIgnoreCase));
                        if (field == null)
                        {
                            failures.Add($"Column metadata not found for '{columnName}'.");
                            continue;
                        }

                        var addResult = AddColumn(desired, field);
                        if (!IsStepSuccess(addResult))
                            failures.Add($"{columnName}: {addResult?.Message}");
                    }

                    if (failures.Count > 0)
                        return CreateErrorsInfo(Errors.Failed, string.Join("; ", failures));
                    return CreateErrorsInfo(Errors.Ok, $"Added {step.MissingColumns.Count} column(s) to '{step.EntityName}'.");

                default:
                    return CreateErrorsInfo(Errors.Failed, $"Operation '{step.OperationKind}' is not yet supported by execution orchestration.");
            }
        }

        private static bool IsStepSuccess(IErrorsInfo stepResult)
        {
            var flag = stepResult?.Flag.ToString() ?? string.Empty;
            return flag.Equals("Ok", StringComparison.OrdinalIgnoreCase) ||
                   flag.Equals("Warning", StringComparison.OrdinalIgnoreCase);
        }

        private static string ClassifyFailure(string message, MigrationExecutionPolicy policy)
        {
            var text = message ?? string.Empty;
            if (ContainsMarker(text, policy.HardFailMarkers))
                return "hard";
            if (ContainsMarker(text, policy.TransientErrorMarkers))
                return "transient";
            return "normal";
        }

        private static bool ContainsMarker(string text, IEnumerable<string> markers)
        {
            if (string.IsNullOrWhiteSpace(text) || markers == null)
                return false;

            foreach (var marker in markers.Where(item => !string.IsNullOrWhiteSpace(item)))
            {
                if (text.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private MigrationPlanArtifact BuildPlanFromCheckpoint(MigrationExecutionCheckpoint checkpoint)
        {
            var plan = new MigrationPlanArtifact
            {
                PlanId = checkpoint.PlanId,
                PlanHash = checkpoint.PlanHash,
                DataSourceName = MigrateDataSource?.DatasourceName ?? string.Empty,
                DataSourceType = MigrateDataSource?.DatasourceType ?? DataSourceType.Unknown,
                DataSourceCategory = MigrateDataSource?.Category ?? DatasourceCategory.NONE
            };

            foreach (var step in checkpoint.Steps.OrderBy(item => item.Sequence))
            {
                plan.Operations.Add(new MigrationPlanOperation
                {
                    EntityName = step.EntityName,
                    EntityTypeName = step.EntityTypeName,
                    Kind = step.OperationKind,
                    MissingColumns = step.MissingColumns?.ToList() ?? new List<string>(),
                    Note = step.Message
                });
            }

            return plan;
        }

        private void PersistExecutionCheckpoint(MigrationExecutionCheckpoint checkpoint)
        {
            try
            {
                checkpoint.UpdatedOnUtc = DateTime.UtcNow;
                ExecutionCheckpoints[checkpoint.ExecutionToken] = checkpoint;

                var configEditor = _editor?.ConfigEditor as ConfigEditor;
                if (configEditor == null)
                    return;

                var snapshot = JsonSerializer.Serialize(checkpoint);
                var record = new MigrationRecord
                {
                    MigrationId = checkpoint.ExecutionToken,
                    Name = "ExecuteMigrationPlan.Checkpoint",
                    AppliedOnUtc = DateTime.UtcNow,
                    Success = checkpoint.IsCompleted && !checkpoint.HasFailed,
                    Notes = snapshot,
                    Steps = checkpoint.Steps.Select(step => new MigrationStep
                    {
                        Operation = step.OperationKind.ToString(),
                        EntityName = step.EntityName,
                        ColumnName = step.MissingColumns.Count > 0 ? string.Join(",", step.MissingColumns) : string.Empty,
                        Success = step.Status == MigrationExecutionStepStatus.Completed || step.Status == MigrationExecutionStepStatus.Skipped,
                        Message = $"{step.Status} | attempts={step.AttemptCount} | elapsedMs={step.ElapsedMilliseconds} | {step.Message}",
                        Sql = string.Empty
                    }).ToList()
                };

                configEditor.AppendMigrationRecord(
                    MigrateDataSource?.DatasourceName ?? string.Empty,
                    MigrateDataSource?.DatasourceType ?? DataSourceType.Unknown,
                    record);
            }
            catch (Exception ex)
            {
                _editor?.AddLogMessage("Beep", $"Failed to persist migration checkpoint: {ex.Message}", DateTime.Now, 0, null, Errors.Warning);
            }
        }
    }
}
