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
            var dataSourceCategory = plan.DataSourceCategory;
            foreach (var operation in plan.Operations.Where(item => item != null))
            {
                var isRelationalOp = operation.Kind == MigrationPlanOperationKind.AddForeignKey ||
                                     operation.Kind == MigrationPlanOperationKind.DropForeignKey ||
                                     operation.Kind == MigrationPlanOperationKind.CreateIndex ||
                                     operation.Kind == MigrationPlanOperationKind.DropIndex;

                // FK/Index ops are RiskLevel.Low by default but still touch the
                // relational integrity surface of a critical datasource. On
                // RDBMS targets, treat them as compensation-worthy so a partial
                // apply can drop a partially-created constraint or index. On
                // non-RDBMS targets (File/NoSQL/API/etc.) the relational
                // surface is emulated or non-existent, so we still skip them.
                var includeRelationalOps = isRelationalOp && dataSourceCategory == DatasourceCategory.RDBMS;

                var highRisk = operation.RiskLevel == MigrationPlanRiskLevel.High ||
                               operation.RiskLevel == MigrationPlanRiskLevel.Critical ||
                               operation.IsDestructive ||
                               operation.IsTypeNarrowing ||
                               operation.HasNullabilityTightening ||
                               includeRelationalOps;
                if (!highRisk)
                    continue;

                var action = new MigrationCompensationAction
                {
                    ActionId = $"comp-{sequence}",
                    Sequence = sequence,
                    EntityName = operation.EntityName,
                    OperationKind = operation.Kind,
                    IsHighRisk = operation.RiskLevel == MigrationPlanRiskLevel.High ||
                                 operation.RiskLevel == MigrationPlanRiskLevel.Critical ||
                                 operation.IsDestructive ||
                                 operation.IsTypeNarrowing ||
                                 operation.HasNullabilityTightening,
                    IsRequiredBeforeApply = true,
                    RollbackMode = ResolveRollbackMode(operation.Kind),
                    RollbackSqlPreview = BuildRollbackSqlPreview(operation),
                    CompensationPlaybook = BuildCompensationPlaybook(operation),
                    TargetName = operation.TargetName ?? string.Empty
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

            // BuildCompensationPlan includes FK/Index ops on RDBMS targets
            // even when RiskLevel is Low (they touch the relational integrity
            // surface). Surface this in the readiness check so the operator
            // isn't told "no high-risk ops" when compensation actions exist.
            var hasRelationalCompensation = plan.DataSourceCategory == DatasourceCategory.RDBMS &&
                (plan.CompensationPlan.Actions?.Any(a =>
                    a.OperationKind == MigrationPlanOperationKind.AddForeignKey ||
                    a.OperationKind == MigrationPlanOperationKind.DropForeignKey ||
                    a.OperationKind == MigrationPlanOperationKind.CreateIndex ||
                    a.OperationKind == MigrationPlanOperationKind.DropIndex) ?? false);

            if (hasRelationalCompensation && !hasHighRisk && (plan.CompensationPlan.Actions?.Count ?? 0) <= 4)
            {
                report.Checks.Add(new MigrationRollbackReadinessCheck
                {
                    Code = "rollback-relational-compensation-present",
                    Decision = MigrationPolicyDecision.Pass,
                    Message = "Relational FK/Index compensation actions exist for this RDBMS plan. These are Low-risk individually but affect referential integrity.",
                    Recommendation = "Review FK/Index rollback actions and validate they match the constraint/index names in the plan."
                });
            }

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
                    var line = $"[dry-run] {action.ActionId} {action.RollbackMode} {action.EntityName}: {action.CompensationPlaybook}";
                    if (!string.IsNullOrWhiteSpace(action.RollbackSqlPreview))
                        line += $" | sql: {action.RollbackSqlPreview}";
                    result.ExecutedActions.Add(line);
                    continue;
                }

                if (action.RollbackMode == MigrationRollbackMode.ReversibleDdl)
                {
                    IErrorsInfo dropResult = null;
                    var executed = false;

                    switch (action.OperationKind)
                    {
                        case MigrationPlanOperationKind.CreateEntity:
                            if (!string.IsNullOrWhiteSpace(action.EntityName))
                            {
                                dropResult = DropEntity(action.EntityName);
                                executed = true;
                            }
                            break;

                        case MigrationPlanOperationKind.DropForeignKey:
                        case MigrationPlanOperationKind.AddForeignKey:
                            // For both FK ops the rollback direction is "drop the
                            // constraint" (the forward op added it; the reverse
                            // removes it). We rely on the action's TargetName —
                            // which mirrors the original plan op's TargetName —
                            // rather than re-deriving from a desired structure.
                            if (!string.IsNullOrWhiteSpace(action.EntityName) &&
                                !string.IsNullOrWhiteSpace(action.TargetName))
                            {
                                dropResult = DropForeignKey(action.EntityName, action.TargetName);
                                executed = true;
                            }
                            break;

                        case MigrationPlanOperationKind.CreateIndex:
                        case MigrationPlanOperationKind.DropIndex:
                            // For both index ops the rollback direction is "drop
                            // the index". The plan op's TargetName is the actual
                            // index name (not the synthetic step-N identifier).
                            if (!string.IsNullOrWhiteSpace(action.EntityName) &&
                                !string.IsNullOrWhiteSpace(action.TargetName))
                            {
                                dropResult = DropIndex(action.EntityName, action.TargetName);
                                executed = true;
                            }
                            break;
                    }

                    if (executed && dropResult != null)
                    {
                        if (dropResult.Flag == Errors.Ok || dropResult.Flag == Errors.Warning)
                        {
                            result.ExecutedActions.Add($"{action.ActionId} rollback executed for '{action.EntityName}' [{action.OperationKind} '{action.TargetName}'].");
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
                    : kind == MigrationPlanOperationKind.AddForeignKey
                        || kind == MigrationPlanOperationKind.DropForeignKey
                        || kind == MigrationPlanOperationKind.CreateIndex
                        || kind == MigrationPlanOperationKind.DropIndex
                            ? MigrationRollbackMode.ReversibleDdl
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

            if (operation.Kind == MigrationPlanOperationKind.AddForeignKey && !string.IsNullOrWhiteSpace(operation.EntityName))
            {
                var constraint = !string.IsNullOrWhiteSpace(operation.TargetName) ? operation.TargetName : "<unnamed-FK>";
                return $"ALTER TABLE {operation.EntityName} DROP CONSTRAINT {constraint};";
            }

            if (operation.Kind == MigrationPlanOperationKind.DropForeignKey && !string.IsNullOrWhiteSpace(operation.EntityName))
            {
                var constraint = !string.IsNullOrWhiteSpace(operation.TargetName) ? operation.TargetName : "<unnamed-FK>";
                return $"ALTER TABLE {operation.EntityName} ADD CONSTRAINT {constraint} ...; -- restore from backup or original spec";
            }

            if (operation.Kind == MigrationPlanOperationKind.CreateIndex && !string.IsNullOrWhiteSpace(operation.EntityName))
            {
                var index = !string.IsNullOrWhiteSpace(operation.TargetName) ? operation.TargetName : "<unnamed-IX>";
                return $"DROP INDEX {index} ON {operation.EntityName};";
            }

            if (operation.Kind == MigrationPlanOperationKind.DropIndex && !string.IsNullOrWhiteSpace(operation.EntityName))
            {
                var index = !string.IsNullOrWhiteSpace(operation.TargetName) ? operation.TargetName : "<unnamed-IX>";
                return $"-- Provider-specific CREATE INDEX required for {operation.EntityName}; restore index '{index}' per original spec";
            }

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

            if (operation.Kind == MigrationPlanOperationKind.AddForeignKey)
            {
                var constraint = !string.IsNullOrWhiteSpace(operation.TargetName) ? operation.TargetName : "<unnamed-FK>";
                return $"If the FK constraint '{constraint}' blocks the dependent workload, defer the FK creation in a follow-up migration after data backfill completes.";
            }

            if (operation.Kind == MigrationPlanOperationKind.DropForeignKey)
            {
                var constraint = !string.IsNullOrWhiteSpace(operation.TargetName) ? operation.TargetName : "<unnamed-FK>";
                return $"Re-add the constraint '{constraint}' in a follow-up release; record cascading-impact rows in the audit trail before re-applying.";
            }

            if (operation.Kind == MigrationPlanOperationKind.CreateIndex)
            {
                var index = !string.IsNullOrWhiteSpace(operation.TargetName) ? operation.TargetName : "<unnamed-IX>";
                return $"If the index build for '{index}' fails, drop the partial index and rebuild online during off-peak hours; re-collect statistics before retrying.";
            }

            if (operation.Kind == MigrationPlanOperationKind.DropIndex)
            {
                var index = !string.IsNullOrWhiteSpace(operation.TargetName) ? operation.TargetName : "<unnamed-IX>";
                return $"Restore the index '{index}' via CREATE INDEX; capture query-plan regression evidence before and after.";
            }

            return "Execute manual operator playbook with restore verification evidence.";
        }

        private (string rollbackOutcome, string compensationOutcome) DescribeFailureRollbackOutcomes(MigrationPlanArtifact plan, MigrationExecutionStep failedStep)
        {
            if (plan == null || failedStep == null)
                return ("Rollback unavailable.", "Compensation unavailable.");

            plan.CompensationPlan ??= BuildCompensationPlan(plan);

            // For FK/Index ops a single entity can host multiple constraints or
            // indexes, so prefer matching by TargetName when it is set; otherwise
            // fall back to EntityName + OperationKind which is sufficient for
            // single-op plans.
            var isRelationalOp = failedStep.OperationKind == MigrationPlanOperationKind.AddForeignKey ||
                                 failedStep.OperationKind == MigrationPlanOperationKind.DropForeignKey ||
                                 failedStep.OperationKind == MigrationPlanOperationKind.CreateIndex ||
                                 failedStep.OperationKind == MigrationPlanOperationKind.DropIndex;

            MigrationCompensationAction action = null;
            if (isRelationalOp && !string.IsNullOrWhiteSpace(failedStep.TargetName))
            {
                action = plan.CompensationPlan.Actions.FirstOrDefault(item =>
                    string.Equals(item.EntityName, failedStep.EntityName, StringComparison.OrdinalIgnoreCase) &&
                    item.OperationKind == failedStep.OperationKind &&
                    string.Equals(item.TargetName, failedStep.TargetName, StringComparison.OrdinalIgnoreCase));
            }

            if (action == null)
            {
                action = plan.CompensationPlan.Actions.FirstOrDefault(item =>
                    string.Equals(item.EntityName, failedStep.EntityName, StringComparison.OrdinalIgnoreCase) &&
                    item.OperationKind == failedStep.OperationKind);
            }

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
