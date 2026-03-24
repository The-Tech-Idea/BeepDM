using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace TheTechIdea.Beep.Editor.Migration
{
    public partial class MigrationManager
    {
        public MigrationCiValidationReport ValidatePlanForCi(MigrationPlanArtifact plan, MigrationPolicyOptions options = null)
        {
            var report = new MigrationCiValidationReport
            {
                GeneratedOnUtc = DateTime.UtcNow,
                PlanId = plan?.PlanId ?? string.Empty,
                PlanHash = plan?.PlanHash ?? string.Empty
            };

            if (plan == null)
            {
                report.Gates.Add(new MigrationCiGateResult
                {
                    Gate = "plan-lint",
                    Decision = MigrationPolicyDecision.Block,
                    Message = "Plan is null."
                });
                report.CanMerge = false;
                return report;
            }

            // Plan lint gate
            var lintDecision = plan.Operations.Any(operation => operation == null || string.IsNullOrWhiteSpace(operation.EntityName))
                ? MigrationPolicyDecision.Block
                : MigrationPolicyDecision.Pass;
            report.Gates.Add(new MigrationCiGateResult
            {
                Gate = "plan-lint",
                Decision = lintDecision,
                Message = lintDecision == MigrationPolicyDecision.Pass
                    ? "Plan lint checks passed."
                    : "Plan contains invalid or unnamed operations."
            });

            // Policy gate
            var policy = EvaluateMigrationPlanPolicy(plan, options);
            if (policy.Decision == MigrationPolicyDecision.Block)
                RecordPolicyBlockCount();
            report.Gates.Add(new MigrationCiGateResult
            {
                Gate = "policy-check",
                Decision = policy.Decision,
                Message = $"Policy decision: {policy.Decision}."
            });

            // Dry-run gate
            plan.DryRunReport ??= GenerateDryRunReport(plan);
            var dryRunDecision = plan.DryRunReport.HasBlockingIssues
                ? MigrationPolicyDecision.Block
                : MigrationPolicyDecision.Pass;
            report.Gates.Add(new MigrationCiGateResult
            {
                Gate = "dry-run-validation",
                Decision = dryRunDecision,
                Message = dryRunDecision == MigrationPolicyDecision.Pass
                    ? "Dry-run validation passed."
                    : "Dry-run produced blocking diagnostics."
            });

            // Portability warnings gate
            var portabilityDecision = string.IsNullOrWhiteSpace(plan.ProviderCapabilities?.PortabilityWarning)
                ? MigrationPolicyDecision.Pass
                : MigrationPolicyDecision.Warn;
            report.Gates.Add(new MigrationCiGateResult
            {
                Gate = "portability-warning",
                Decision = portabilityDecision,
                Message = portabilityDecision == MigrationPolicyDecision.Pass
                    ? "No provider portability warning."
                    : plan.ProviderCapabilities.PortabilityWarning
            });

            report.CanMerge = report.Gates.All(gate => gate.Decision != MigrationPolicyDecision.Block);
            plan.CiValidationReport = report;
            return report;
        }

        public string BuildMigrationPlanDiff(MigrationPlanArtifact previousPlan, MigrationPlanArtifact currentPlan)
        {
            if (previousPlan == null && currentPlan == null)
                return "No plans to diff.";
            if (previousPlan == null)
                return "Previous plan is null; current plan is treated as full addition.";
            if (currentPlan == null)
                return "Current plan is null; previous plan is treated as fully removed.";

            var before = previousPlan.Operations
                .Where(operation => operation != null)
                .Select(ToOperationSignature)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var after = currentPlan.Operations
                .Where(operation => operation != null)
                .Select(ToOperationSignature)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var added = after.Except(before, StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToList();
            var removed = before.Except(after, StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToList();

            var builder = new StringBuilder();
            builder.AppendLine($"Plan Diff: {previousPlan.PlanId} -> {currentPlan.PlanId}");
            builder.AppendLine($"Added operations: {added.Count}");
            foreach (var item in added)
                builder.AppendLine($"+ {item}");
            builder.AppendLine($"Removed operations: {removed.Count}");
            foreach (var item in removed)
                builder.AppendLine($"- {item}");
            return builder.ToString();
        }

        public MigrationDevExArtifactBundle ExportMigrationArtifacts(MigrationPlanArtifact plan, MigrationCiValidationReport ciReport = null)
        {
            var bundle = new MigrationDevExArtifactBundle
            {
                GeneratedOnUtc = DateTime.UtcNow,
                PlanId = plan?.PlanId ?? string.Empty,
                PlanHash = plan?.PlanHash ?? string.Empty
            };

            if (plan == null)
                return bundle;

            ciReport ??= plan.CiValidationReport ?? ValidatePlanForCi(plan);
            plan.DryRunReport ??= GenerateDryRunReport(plan);

            bundle.PlanJson = JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true });
            bundle.DryRunJson = JsonSerializer.Serialize(plan.DryRunReport, new JsonSerializerOptions { WriteIndented = true });
            bundle.CiValidationJson = JsonSerializer.Serialize(ciReport, new JsonSerializerOptions { WriteIndented = true });
            bundle.ApprovalReportMarkdown = BuildApprovalReportMarkdown(plan, ciReport);
            return bundle;
        }

        private static string ToOperationSignature(MigrationPlanOperation operation)
        {
            var missing = operation.MissingColumns == null || operation.MissingColumns.Count == 0
                ? string.Empty
                : string.Join(",", operation.MissingColumns.OrderBy(value => value, StringComparer.OrdinalIgnoreCase));
            return $"{operation.EntityName}|{operation.Kind}|{operation.RiskLevel}|{missing}";
        }

        private static string BuildApprovalReportMarkdown(MigrationPlanArtifact plan, MigrationCiValidationReport ciReport)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"# Migration Approval Report - {plan.PlanId}");
            builder.AppendLine();
            builder.AppendLine($"- Plan Hash: `{plan.PlanHash}`");
            builder.AppendLine($"- Datasource: `{plan.DataSourceName}` (`{plan.DataSourceType}`)");
            builder.AppendLine($"- Pending Operations: `{plan.PendingOperationCount}`");
            builder.AppendLine($"- CI Can Merge: `{ciReport.CanMerge}`");
            builder.AppendLine();
            builder.AppendLine("## CI Gates");
            foreach (var gate in ciReport.Gates)
            {
                builder.AppendLine($"- `{gate.Gate}`: `{gate.Decision}` - {gate.Message}");
            }
            builder.AppendLine();
            builder.AppendLine("## Operations");
            foreach (var operation in plan.Operations.Where(item => item != null))
            {
                builder.AppendLine($"- `{operation.EntityName}` `{operation.Kind}` `{operation.RiskLevel}`");
            }
            return builder.ToString();
        }
    }
}
