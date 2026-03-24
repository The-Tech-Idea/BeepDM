using System;

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
        }

        private static bool IsProtectedEnvironment(MigrationEnvironmentTier tier)
        {
            return tier == MigrationEnvironmentTier.Staging || tier == MigrationEnvironmentTier.Production;
        }

        private static bool IsDestructiveKind(MigrationPlanOperationKind kind)
        {
            return kind == MigrationPlanOperationKind.DropEntity ||
                   kind == MigrationPlanOperationKind.DropColumn ||
                   kind == MigrationPlanOperationKind.TruncateEntity;
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
