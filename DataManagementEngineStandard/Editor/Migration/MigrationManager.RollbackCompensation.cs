using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Core;

namespace TheTechIdea.Beep.Editor.Migration
{
    public partial class MigrationManager
    {
        public MigrationCompensationPlan BuildCompensationPlan(MigrationPlanArtifact plan)
        {
            var compensation = new MigrationCompensationPlan
            {
                GeneratedOnUtc = DateTime.UtcNow,
                PlanId = plan?.PlanId ?? string.Empty,
                PlanHash = plan?.PlanHash ?? string.Empty
            };

            if (plan == null || plan.Operations == null || plan.Operations.Count == 0)
                return compensation;

            var sequence = 1;
            foreach (var operation in plan.Operations.Where(item => item != null))
            {
                var highRisk = operation.RiskLevel == MigrationPlanRiskLevel.High ||
                               operation.RiskLevel == MigrationPlanRiskLevel.Critical ||
                               operation.IsDestructive ||
                               operation.IsTypeNarrowing ||
                               operation.HasNullabilityTightening;
                if (!highRisk)
                    continue;

                var action = new MigrationCompensationAction
                {
                    ActionId = $"comp-{sequence}",
                    Sequence = sequence,
                    EntityName = operation.EntityName,
                    OperationKind = operation.Kind,
                    IsHighRisk = true,
                    IsRequiredBeforeApply = true,
                    RollbackMode = ResolveRollbackMode(operation.Kind),
                    RollbackSqlPreview = BuildRollbackSqlPreview(operation),
                    CompensationPlaybook = BuildCompensationPlaybook(operation)
                };
                compensation.Actions.Add(action);
                sequence++;
            }

            return compensation;
        }

        public MigrationRollbackReadinessReport CheckRollbackReadiness(MigrationPlanArtifact plan, bool backupConfirmed, bool restoreTestEvidenceProvided, string restoreTestEvidence = null)
        {
            var report = new MigrationRollbackReadinessReport
            {
                CheckedOnUtc = DateTime.UtcNow,
                PlanId = plan?.PlanId ?? string.Empty,
                PlanHash = plan?.PlanHash ?? string.Empty,
                BackupConfirmed = backupConfirmed,
                RestoreTestEvidenceProvided = restoreTestEvidenceProvided,
                RestoreTestEvidence = restoreTestEvidence ?? string.Empty
            };

            if (plan == null)
            {
                report.Checks.Add(new MigrationRollbackReadinessCheck
                {
                    Code = "rollback-plan-null",
                    Decision = MigrationPolicyDecision.Block,
                    Message = "Rollback readiness cannot be checked for a null plan.",
                    Recommendation = "Generate a migration plan and compensation artifacts first."
                });
                report.IsReady = false;
                return report;
            }

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
                report.Checks.Add(new MigrationRollbackReadinessCheck
                {
                    Code = "rollback-compensation-missing",
                    Decision = MigrationPolicyDecision.Block,
                    Message = "High-risk migration operations do not have compensation actions.",
                    Recommendation = "Build compensation plan artifacts before apply."
                });
            }
            else
            {
                report.Checks.Add(new MigrationRollbackReadinessCheck
                {
                    Code = "rollback-compensation-present",
                    Decision = MigrationPolicyDecision.Pass,
                    Message = hasHighRisk
                        ? "Compensation actions are attached for high-risk operations."
                        : "No high-risk operations require compensation actions.",
                    Recommendation = "Proceed with standard preflight checks."
                });
            }

            report.Checks.Add(new MigrationRollbackReadinessCheck
            {
                Code = "rollback-backup-confirmation",
                Decision = backupConfirmed ? MigrationPolicyDecision.Pass : (hasHighRisk ? MigrationPolicyDecision.Block : MigrationPolicyDecision.Warn),
                Message = backupConfirmed
                    ? "Backup/snapshot confirmation is provided."
                    : "Backup/snapshot confirmation is missing.",
                Recommendation = "Capture explicit backup or snapshot confirmation before apply."
            });

            report.Checks.Add(new MigrationRollbackReadinessCheck
            {
                Code = "rollback-restore-evidence",
                Decision = restoreTestEvidenceProvided ? MigrationPolicyDecision.Pass : (hasHighRisk ? MigrationPolicyDecision.Block : MigrationPolicyDecision.Warn),
                Message = restoreTestEvidenceProvided
                    ? "Restore test evidence is provided."
                    : "Restore test evidence is missing.",
                Recommendation = "Provide restore test run evidence from pre-production."
            });

            report.IsReady = report.Checks.All(check => check.Decision != MigrationPolicyDecision.Block);
            return report;
        }

        public MigrationRollbackResult RollbackFailedExecution(string executionToken, bool dryRun = true)
        {
            var result = new MigrationRollbackResult
            {
                ExecutionToken = executionToken ?? string.Empty,
                DryRun = dryRun
            };
            RecordRollbackCount();

            if (string.IsNullOrWhiteSpace(executionToken))
            {
                result.Success = false;
                result.Message = "Execution token is required.";
                return result;
            }

            if (!ExecutionCheckpoints.TryGetValue(executionToken.Trim(), out var checkpoint))
            {
                result.Success = false;
                result.Message = $"Checkpoint '{executionToken}' was not found.";
                RecordDiagnostic(executionToken, string.Empty, "rollback-checkpoint-missing", MigrationDiagnosticSeverity.Error, string.Empty, result.Message, "Use a valid execution token from a failed run.");
                return result;
            }

            AddAuditEvent(CreateAuditEvent(
                executionToken: checkpoint.ExecutionToken,
                correlationId: checkpoint.CorrelationId,
                planId: checkpoint.PlanId,
                planHash: checkpoint.PlanHash,
                eventType: dryRun ? "RollbackDryRunStarted" : "RollbackStarted",
                approvedBy: string.Empty,
                executedBy: Environment.UserName,
                result: "Started",
                notes: "Rollback/compensation flow started."));

            if (!ExecutionPlans.TryGetValue(executionToken.Trim(), out var plan))
            {
                plan = BuildPlanFromCheckpoint(checkpoint);
                ExecutionPlans[executionToken.Trim()] = plan;
            }

            plan.CompensationPlan ??= BuildCompensationPlan(plan);
            var actions = plan.CompensationPlan.Actions
                .OrderBy(action => action.Sequence)
                .ToList();

            if (actions.Count == 0)
            {
                result.Success = true;
                result.Message = "No compensation actions are required for this execution.";
                RecordDiagnostic(checkpoint.ExecutionToken, checkpoint.CorrelationId, "rollback-no-actions", MigrationDiagnosticSeverity.Info, string.Empty, result.Message, "No high-risk compensation actions were detected.");
                return result;
            }

            var failed = new List<string>();
            foreach (var action in actions)
            {
                if (dryRun)
                {
                    result.ExecutedActions.Add($"[dry-run] {action.ActionId} {action.RollbackMode} {action.EntityName}: {action.CompensationPlaybook}");
                    continue;
                }

                if (action.RollbackMode == MigrationRollbackMode.ReversibleDdl &&
                    action.OperationKind == MigrationPlanOperationKind.CreateEntity &&
                    !string.IsNullOrWhiteSpace(action.EntityName))
                {
                    var dropResult = DropEntity(action.EntityName);
                    if (dropResult.Flag == Errors.Ok || dropResult.Flag == Errors.Warning)
                    {
                        result.ExecutedActions.Add($"{action.ActionId} rollback executed for '{action.EntityName}'.");
                    }
                    else
                    {
                        failed.Add($"{action.ActionId}: {dropResult.Message}");
                    }
                }
                else
                {
                    result.ExecutedActions.Add($"{action.ActionId} requires manual compensation: {action.CompensationPlaybook}");
                }
            }

            result.Success = failed.Count == 0;
            result.Message = result.Success
                ? (dryRun ? "Rollback dry-run completed." : "Rollback/compensation completed.")
                : $"Rollback encountered {failed.Count} failure(s): {string.Join("; ", failed)}";
            RecordDiagnostic(
                checkpoint.ExecutionToken,
                checkpoint.CorrelationId,
                "rollback-finished",
                result.Success ? MigrationDiagnosticSeverity.Info : MigrationDiagnosticSeverity.Error,
                string.Empty,
                result.Message,
                result.Success ? "Rollback flow completed." : "Review failed compensation actions and apply manual operator playbook.");
            AddAuditEvent(CreateAuditEvent(
                executionToken: checkpoint.ExecutionToken,
                correlationId: checkpoint.CorrelationId,
                planId: checkpoint.PlanId,
                planHash: checkpoint.PlanHash,
                eventType: dryRun ? "RollbackDryRunFinished" : "RollbackFinished",
                approvedBy: string.Empty,
                executedBy: Environment.UserName,
                result: result.Success ? "Success" : "Failure",
                notes: result.Message));
            return result;
        }

        private static MigrationRollbackMode ResolveRollbackMode(MigrationPlanOperationKind kind)
        {
            return kind == MigrationPlanOperationKind.CreateEntity
                ? MigrationRollbackMode.ReversibleDdl
                : kind == MigrationPlanOperationKind.AddMissingColumns
                    ? MigrationRollbackMode.ForwardFixWithCompensation
                    : MigrationRollbackMode.ManualOnly;
        }

        private static string BuildRollbackSqlPreview(MigrationPlanOperation operation)
        {
            if (operation == null)
                return string.Empty;

            if (operation.Kind == MigrationPlanOperationKind.CreateEntity && !string.IsNullOrWhiteSpace(operation.EntityName))
                return $"DROP TABLE {operation.EntityName};";

            if (operation.Kind == MigrationPlanOperationKind.AddMissingColumns && operation.MissingColumns.Count > 0)
                return $"-- Provider-specific drop-column/forward-fix required for {operation.EntityName}: {string.Join(", ", operation.MissingColumns)}";

            return "-- Manual rollback required";
        }

        private static string BuildCompensationPlaybook(MigrationPlanOperation operation)
        {
            if (operation == null)
                return "Manual operator review required.";

            if (operation.Kind == MigrationPlanOperationKind.CreateEntity)
                return "If validation fails, drop created entity and restore metadata from backup snapshot.";

            if (operation.Kind == MigrationPlanOperationKind.AddMissingColumns)
                return "Use forward-fix migration: patch consumers, backfill values, and mark deprecated columns in a follow-up release.";

            return "Execute manual operator playbook with restore verification evidence.";
        }

        private (string rollbackOutcome, string compensationOutcome) DescribeFailureRollbackOutcomes(MigrationPlanArtifact plan, MigrationExecutionStep failedStep)
        {
            if (plan == null || failedStep == null)
                return ("Rollback unavailable.", "Compensation unavailable.");

            plan.CompensationPlan ??= BuildCompensationPlan(plan);
            var action = plan.CompensationPlan.Actions
                .FirstOrDefault(item =>
                    string.Equals(item.EntityName, failedStep.EntityName, StringComparison.OrdinalIgnoreCase) &&
                    item.OperationKind == failedStep.OperationKind);

            if (action == null)
                return ("No direct rollback action mapped.", "No compensation action mapped.");

            var rollback = action.RollbackMode == MigrationRollbackMode.ReversibleDdl
                ? $"Rollback path available (preview): {action.RollbackSqlPreview}"
                : "Strict rollback is not available for this step.";
            var compensation = $"Compensation playbook: {action.CompensationPlaybook}";
            return (rollback, compensation);
        }
    }
}
