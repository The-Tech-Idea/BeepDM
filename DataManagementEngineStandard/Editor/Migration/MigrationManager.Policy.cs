using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Editor.Migration
{
    public partial class MigrationManager
    {
        public MigrationPolicyEvaluation EvaluateMigrationPlanPolicy(MigrationPlanArtifact plan, MigrationPolicyOptions options = null)
        {
            var effectiveOptions = options ?? new MigrationPolicyOptions();
            var evaluation = new MigrationPolicyEvaluation
            {
                EnvironmentTier = effectiveOptions.EnvironmentTier,
                IsProtectedEnvironment = IsProtectedEnvironment(effectiveOptions.EnvironmentTier),
                Decision = MigrationPolicyDecision.Pass
            };

            if (plan == null)
            {
                evaluation.Decision = MigrationPolicyDecision.Block;
                evaluation.Findings.Add(new MigrationPolicyFinding
                {
                    RuleId = "policy-plan-null",
                    Decision = MigrationPolicyDecision.Block,
                    Message = "Migration plan is null.",
                    Recommendation = "Generate a plan before running policy evaluation."
                });
                return evaluation;
            }

            foreach (var operation in plan.Operations)
            {
                EvaluateOperationPolicy(operation, effectiveOptions, evaluation);
            }

            // Cross-entity policy: check FK patterns that require visibility
            // beyond a single operation (self-referencing FKs, excessive FKs
            // per entity, FK referencing an entity not in the plan).
            EvaluateCrossEntityPolicy(plan, evaluation);

            EvaluateProviderCapabilityPolicy(plan, evaluation);

            if (evaluation.HasBlockingFindings)
                evaluation.Decision = MigrationPolicyDecision.Block;
            else if (evaluation.Findings.Exists(finding => finding.Decision == MigrationPolicyDecision.Warn))
                evaluation.Decision = MigrationPolicyDecision.Warn;
            else
                evaluation.Decision = MigrationPolicyDecision.Pass;

            return evaluation;
        }

        private void EvaluateOperationPolicy(MigrationPlanOperation operation, MigrationPolicyOptions options, MigrationPolicyEvaluation evaluation)
        {
            if (operation == null)
                return;

            var entityName = operation.EntityName ?? string.Empty;

            if (operation.Kind == MigrationPlanOperationKind.Error)
            {
                evaluation.Findings.Add(new MigrationPolicyFinding
                {
                    RuleId = "policy-plan-operation-error",
                    Decision = MigrationPolicyDecision.Block,
                    Message = $"Plan operation for '{entityName}' is in error state and cannot be applied.",
                    Recommendation = "Fix planning errors before attempting migration.",
                    EntityName = entityName,
                    OperationKind = operation.Kind,
                    RiskLevel = operation.RiskLevel
                });
                return;
            }

            var approvalProvided = HasApprovalOverride(options);
            var requiresHighRiskApproval = operation.RiskLevel == MigrationPlanRiskLevel.High && options.RequireApprovalForHighRisk;
            var requiresCriticalRiskApproval = operation.RiskLevel == MigrationPlanRiskLevel.Critical && options.RequireApprovalForCriticalRisk;

            if ((requiresHighRiskApproval || requiresCriticalRiskApproval) && !approvalProvided)
            {
                evaluation.RequiresManualApproval = true;
                evaluation.Findings.Add(new MigrationPolicyFinding
                {
                    RuleId = "policy-high-risk-approval-required",
                    Decision = MigrationPolicyDecision.Block,
                    Message = $"Operation '{operation.Kind}' on '{entityName}' is {operation.RiskLevel} risk and requires approval metadata.",
                    Recommendation = "Provide both Approver and OverrideReason in MigrationPolicyOptions.",
                    EntityName = entityName,
                    OperationKind = operation.Kind,
                    RiskLevel = operation.RiskLevel
                });
            }

            var destructive = operation.IsDestructive || IsDestructiveKind(operation.Kind);
            if (destructive && evaluation.IsProtectedEnvironment && options.BlockDestructiveInProtectedEnvironments)
            {
                if (options.AllowDestructiveOverrideInProtectedEnvironments && approvalProvided)
                {
                    evaluation.RequiresManualApproval = true;
                    evaluation.Findings.Add(new MigrationPolicyFinding
                    {
                        RuleId = "policy-destructive-protected-override",
                        Decision = MigrationPolicyDecision.Warn,
                        Message = $"Destructive operation '{operation.Kind}' on '{entityName}' is allowed via explicit protected-environment override.",
                        Recommendation = "Validate backup/rollback readiness and change ticket linkage before apply.",
                        EntityName = entityName,
                        OperationKind = operation.Kind,
                        RiskLevel = operation.RiskLevel
                    });
                }
                else
                {
                    evaluation.Findings.Add(new MigrationPolicyFinding
                    {
                        RuleId = "policy-destructive-protected-blocked",
                        Decision = MigrationPolicyDecision.Block,
                        Message = $"Destructive operation '{operation.Kind}' on '{entityName}' is blocked in protected environments.",
                        Recommendation = "Use non-destructive alternatives or provide explicit override policy with approver and reason.",
                        EntityName = entityName,
                        OperationKind = operation.Kind,
                        RiskLevel = operation.RiskLevel
                    });
                }
            }

            if ((operation.IsTypeNarrowing || operation.HasNullabilityTightening) && !approvalProvided)
            {
                evaluation.RequiresManualApproval = true;
                evaluation.Findings.Add(new MigrationPolicyFinding
                {
                    RuleId = "policy-type-nullability-approval-required",
                    Decision = MigrationPolicyDecision.Block,
                    Message = $"Operation '{operation.Kind}' on '{entityName}' includes type narrowing/nullability tightening and requires approval.",
                    Recommendation = "Provide Approver and OverrideReason and validate impact on existing data.",
                    EntityName = entityName,
                    OperationKind = operation.Kind,
                    RiskLevel = operation.RiskLevel
                });
            }

            // FK / Index operations must carry the constraint or index name
            // in TargetName so the executor and rollback can operate on the
            // right object. A missing TargetName means the DDL step lacks a
            // name and will fail at execution time (or produce an unnamed
            // constraint that cannot be targeted for drops).
            var isRelationalOp =
                operation.Kind == MigrationPlanOperationKind.AddForeignKey ||
                operation.Kind == MigrationPlanOperationKind.DropForeignKey ||
                operation.Kind == MigrationPlanOperationKind.CreateIndex ||
                operation.Kind == MigrationPlanOperationKind.DropIndex;

            if (isRelationalOp && string.IsNullOrWhiteSpace(operation.TargetName))
            {
                evaluation.Findings.Add(new MigrationPolicyFinding
                {
                    RuleId = "policy-fk-index-missing-target-name",
                    Decision = MigrationPolicyDecision.Block,
                    Message = $"Operation '{operation.Kind}' on '{entityName}' is missing the constraint/index name (TargetName). The plan must supply a name via RalationName on the RelationShipKeys or IndexName on the EntityIndex.",
                    Recommendation = "Populate RalationName (for FK) or IndexName (for Index) on the EntityStructure before building the plan, or supply the name on the MigrationPlanOperation.TargetName property.",
                    EntityName = entityName,
                    OperationKind = operation.Kind,
                    RiskLevel = operation.RiskLevel
                });
            }
        }

        /// <summary>
        /// Cross-entity FK policy checks that require visibility across
        /// multiple operations.
        /// </summary>
        private static void EvaluateCrossEntityPolicy(MigrationPlanArtifact plan, MigrationPolicyEvaluation evaluation)
        {
            var fkOps = plan?.Operations?.Where(op =>
                op != null && op.Kind == MigrationPlanOperationKind.AddForeignKey).ToList();
            if (fkOps == null || fkOps.Count == 0)
                return;

            var planEntityNames = new HashSet<string>(
                plan.Operations
                    .Where(op => op != null && !string.IsNullOrWhiteSpace(op.EntityName))
                    .Select(op => op.EntityName),
                StringComparer.OrdinalIgnoreCase);

            foreach (var fkOp in fkOps)
            {
                if (string.IsNullOrWhiteSpace(fkOp.TargetName) ||
                    string.IsNullOrWhiteSpace(fkOp.EntityName))
                    continue;

                // Self-referencing FK detection via FK name pattern:
                // FK_{EntityName}_{ReferencedEntity}_{Column}
                var segments = fkOp.TargetName.Split('_');
                if (segments.Length >= 3 &&
                    string.Equals(segments[1], fkOp.EntityName, StringComparison.OrdinalIgnoreCase))
                {
                    evaluation.Findings.Add(new MigrationPolicyFinding
                    {
                        RuleId = "policy-fk-self-referencing",
                        Decision = MigrationPolicyDecision.Warn,
                        Message = $"Self-referencing FK '{fkOp.TargetName}' on '{fkOp.EntityName}' creates a hierarchical dependency. Ensure data is inserted parent-before-child.",
                        Recommendation = "Consider deferring FK enforcement until after data load.",
                        EntityName = fkOp.EntityName,
                        OperationKind = fkOp.Kind,
                        RiskLevel = fkOp.RiskLevel
                    });
                }

                // FK referencing entity not in plan — the DDL will fail if
                // the referenced table does not already exist in the DB.
                if (segments.Length >= 3)
                {
                    var refEntity = segments[2];
                    if (!planEntityNames.Contains(refEntity))
                    {
                        evaluation.Findings.Add(new MigrationPolicyFinding
                        {
                            RuleId = "policy-fk-reference-not-in-plan",
                            Decision = MigrationPolicyDecision.Block,
                            Message = $"AddForeignKey '{fkOp.TargetName}' on '{fkOp.EntityName}' references '{refEntity}' which is not in this plan and may not exist in the DB.",
                            Recommendation = "Verify the referenced table exists before execution.",
                            EntityName = fkOp.EntityName,
                            OperationKind = fkOp.Kind,
                            RiskLevel = fkOp.RiskLevel
                        });
                    }
                }
            }

            // Excessive FKs per entity (threshold: 8)
            var fkCountsByEntity = fkOps
                .GroupBy(op => op.EntityName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 8);
            foreach (var group in fkCountsByEntity)
            {
                evaluation.Findings.Add(new MigrationPolicyFinding
                {
                    RuleId = "policy-excessive-foreign-keys",
                    Decision = MigrationPolicyDecision.Warn,
                    Message = $"Entity '{group.Key}' has {group.Count()} FK operations (>8). Excessive FKs slow writes and complicate rollback.",
                    Recommendation = "Review FK surface; consider vertical partitioning or application-level checks.",
                    EntityName = group.Key,
                    OperationKind = MigrationPlanOperationKind.AddForeignKey,
                    RiskLevel = MigrationPlanRiskLevel.Medium
                });
            }
        }

        private static bool IsProtectedEnvironment(MigrationEnvironmentTier tier)
        {
            return tier == MigrationEnvironmentTier.Staging || tier == MigrationEnvironmentTier.Production;
        }

        private static bool IsDestructiveKind(MigrationPlanOperationKind kind)
        {
            return kind == MigrationPlanOperationKind.DropEntity ||
                   kind == MigrationPlanOperationKind.DropColumn ||
                   kind == MigrationPlanOperationKind.TruncateEntity ||
                   kind == MigrationPlanOperationKind.DropForeignKey ||
                   kind == MigrationPlanOperationKind.DropIndex;
        }

        private static bool HasApprovalOverride(MigrationPolicyOptions options)
        {
            return !string.IsNullOrWhiteSpace(options?.Approver) &&
                   !string.IsNullOrWhiteSpace(options?.OverrideReason);
        }

        private static void EvaluateProviderCapabilityPolicy(MigrationPlanArtifact plan, MigrationPolicyEvaluation evaluation)
        {
            var profile = plan?.ProviderCapabilities;
            if (profile == null)
                return;

            if (!string.IsNullOrWhiteSpace(profile.PortabilityWarning))
            {
                evaluation.Findings.Add(new MigrationPolicyFinding
                {
                    RuleId = "provider-portability-warning",
                    Decision = MigrationPolicyDecision.Warn,
                    Message = profile.PortabilityWarning,
                    Recommendation = "Re-plan migrations per provider and validate helper behavior against the target runtime."
                });
            }

            foreach (var operation in plan.Operations)
            {
                if (operation == null)
                    continue;

                if (operation.Kind == MigrationPlanOperationKind.AddMissingColumns &&
                    !profile.SupportsAlterColumn &&
                    (operation.FallbackTasks == null || operation.FallbackTasks.Count == 0))
                {
                    evaluation.Findings.Add(new MigrationPolicyFinding
                    {
                        RuleId = "provider-fallback-missing-add-columns",
                        Decision = MigrationPolicyDecision.Block,
                        Message = $"Operation '{operation.Kind}' for '{operation.EntityName}' requires fallback tasks because provider alter support is limited.",
                        Recommendation = "Add explicit fallback tasks (copy-table/swap flow) before apply.",
                        EntityName = operation.EntityName,
                        OperationKind = operation.Kind,
                        RiskLevel = operation.RiskLevel
                    });
                }

                if ((operation.Kind == MigrationPlanOperationKind.AddForeignKey ||
                     operation.Kind == MigrationPlanOperationKind.DropForeignKey) &&
                    !profile.SupportsForeignKeys &&
                    (operation.FallbackTasks == null || operation.FallbackTasks.Count == 0))
                {
                    evaluation.Findings.Add(new MigrationPolicyFinding
                    {
                        RuleId = "provider-fallback-missing-foreign-key",
                        Decision = MigrationPolicyDecision.Block,
                        Message = $"Operation '{operation.Kind}' for '{operation.EntityName}' cannot run because the provider does not support foreign keys.",
                        Recommendation = "Either target an RDBMS provider that supports foreign keys or add explicit fallback tasks (manual SQL via operator runbook) before apply.",
                        EntityName = operation.EntityName,
                        OperationKind = operation.Kind,
                        RiskLevel = operation.RiskLevel
                    });
                }

                if ((operation.Kind == MigrationPlanOperationKind.CreateIndex ||
                     operation.Kind == MigrationPlanOperationKind.DropIndex) &&
                    !profile.SupportsIndexes &&
                    (operation.FallbackTasks == null || operation.FallbackTasks.Count == 0))
                {
                    evaluation.Findings.Add(new MigrationPolicyFinding
                    {
                        RuleId = "provider-fallback-missing-index",
                        Decision = MigrationPolicyDecision.Block,
                        Message = $"Operation '{operation.Kind}' for '{operation.EntityName}' cannot run because the provider does not support index DDL.",
                        Recommendation = "Either target a provider that supports indexes or add explicit fallback tasks (defer to next deploy, no-op on the target engine) before apply.",
                        EntityName = operation.EntityName,
                        OperationKind = operation.Kind,
                        RiskLevel = operation.RiskLevel
                    });
                }
            }
        }

        private static MigrationPolicyEvaluation BuildPolicyEvaluationFromReadiness(MigrationReadinessReport report)
        {
            var evaluation = new MigrationPolicyEvaluation
            {
                EnvironmentTier = MigrationEnvironmentTier.Development,
                IsProtectedEnvironment = false,
                Decision = MigrationPolicyDecision.Pass
            };

            if (report == null)
                return evaluation;

            foreach (var issue in report.Issues)
            {
                if (issue.Severity == MigrationIssueSeverity.Error)
                {
                    evaluation.Findings.Add(new MigrationPolicyFinding
                    {
                        RuleId = $"readiness-{issue.Code}",
                        Decision = MigrationPolicyDecision.Block,
                        Message = issue.Message,
                        Recommendation = issue.Recommendation,
                        EntityName = issue.EntityName
                    });
                }
                else if (issue.Severity == MigrationIssueSeverity.Warning)
                {
                    evaluation.Findings.Add(new MigrationPolicyFinding
                    {
                        RuleId = $"readiness-{issue.Code}",
                        Decision = MigrationPolicyDecision.Warn,
                        Message = issue.Message,
                        Recommendation = issue.Recommendation,
                        EntityName = issue.EntityName
                    });
                }
            }

            if (evaluation.Findings.Exists(finding => finding.Decision == MigrationPolicyDecision.Block))
                evaluation.Decision = MigrationPolicyDecision.Block;
            else if (evaluation.Findings.Exists(finding => finding.Decision == MigrationPolicyDecision.Warn))
                evaluation.Decision = MigrationPolicyDecision.Warn;

            return evaluation;
        }
    }
}
