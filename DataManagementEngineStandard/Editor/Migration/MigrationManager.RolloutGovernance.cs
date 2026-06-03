using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    public partial class MigrationManager
    {
        public MigrationRolloutGovernanceReport EvaluateRolloutGovernance(MigrationPlanArtifact plan, MigrationRolloutGovernanceRequest request = null)
        {
            var resolvedRequest = request ?? CreateDefaultRolloutRequest(plan);
            var report = new MigrationRolloutGovernanceReport
            {
                GeneratedOnUtc = DateTime.UtcNow,
                PlanId = plan?.PlanId ?? string.Empty,
                PlanHash = plan?.PlanHash ?? string.Empty,
                Wave = resolvedRequest.Wave,
                ReviewedBy = resolvedRequest.ReviewedBy ?? string.Empty,
                Notes = resolvedRequest.Notes ?? string.Empty
            };

            if (plan == null)
            {
                report.HardStopTriggered = true;
                report.HardStopReason = "Plan is null.";
                report.CanPromote = false;
                return report;
            }

            // Plan hash stability: the hash at governance-time must match the
            // hash that was recorded in the execution checkpoint (or the plan
            // itself, for a cold-start). A mismatch means the plan was
            // regenerated or tampered with after checkpoint — the governance
            // review must re-evaluate or block.
            var recordedHash = !string.IsNullOrWhiteSpace(plan.ExecutionCheckpoint?.PlanHash)
                ? plan.ExecutionCheckpoint.PlanHash
                : plan.PlanHash;
            if (!string.IsNullOrWhiteSpace(plan.ExecutionCheckpoint?.PlanHash) &&
                !string.Equals(plan.PlanHash, recordedHash, StringComparison.Ordinal))
            {
                report.Gates.Add(new MigrationRolloutGateResult
                {
                    Gate = "plan-hash-stability",
                    Decision = MigrationPolicyDecision.Block,
                    Observed = plan.PlanHash ?? string.Empty,
                    Threshold = $"== {recordedHash}",
                    Message = "Plan hash mismatch: the plan has been modified or regenerated since the last execution checkpoint. Re-run CI and policy evaluation."
                });
                report.CanPromote = false;
                plan.RolloutGovernanceReport = report;
                return report;
            }
            report.Gates.Add(new MigrationRolloutGateResult
            {
                Gate = "plan-hash-stability",
                Decision = MigrationPolicyDecision.Pass,
                Observed = plan.PlanHash ?? string.Empty,
                Threshold = string.Empty,
                Message = "Plan hash is stable."
            });

            var snapshot = GetMigrationTelemetrySnapshot(plan.ExecutionCheckpoint?.ExecutionToken);
            var actualExecutionCount = snapshot.Metrics.ExecutionCount;
            var executionCount = Math.Max(1, actualExecutionCount);
            var kpis = new MigrationRolloutKpiSnapshot
            {
                SuccessRate = snapshot.SuccessRate,
                MeanExecutionDurationMilliseconds = snapshot.Metrics.AverageStepDurationMilliseconds,
                RollbackInvocationRate = (double)snapshot.Metrics.RollbackCount / executionCount,
                PolicyBlockRatio = snapshot.Metrics.PolicyBlockRatio
            };
            report.Kpis = kpis;

            AddWaveEligibilityGate(report, resolvedRequest);

            // When there is no execution history (first-run plan), KPI gates
            // would all pass because zero division-by-1 produces 0 values that
            // satisfy all thresholds. Instead, mark them as Warn and note that
            // no history exists so the operator can decide manually.
            if (actualExecutionCount == 0)
            {
                report.Gates.Add(new MigrationRolloutGateResult
                {
                    Gate = "kpi-no-history",
                    Decision = MigrationPolicyDecision.Warn,
                    Observed = "N/A",
                    Threshold = "N/A",
                    Message = "No prior execution history exists for this plan. KPI gates (success rate, duration, rollback, policy-block) are deferred to the operator."
                });
            }
            else
            {
                AddSuccessRateGate(report, resolvedRequest, kpis.SuccessRate);
                AddDurationGate(report, resolvedRequest, kpis.MeanExecutionDurationMilliseconds);
                AddRollbackRateGate(report, resolvedRequest, kpis.RollbackInvocationRate);
                AddPolicyBlockGate(report, resolvedRequest, kpis.PolicyBlockRatio);
            }

            EvaluateHardStopPolicy(report, resolvedRequest, plan, snapshot);
            report.CanPromote = !report.HardStopTriggered && report.Gates.All(gate => gate.Decision != MigrationPolicyDecision.Block);

            plan.RolloutGovernanceReport = report;
            RecordRolloutDecisionAudit(plan, report);
            return report;
        }

        private static MigrationRolloutGovernanceRequest CreateDefaultRolloutRequest(MigrationPlanArtifact plan)
        {
            var isRdbms = plan?.DataSourceCategory == DatasourceCategory.RDBMS;
            if (!isRdbms)
                return new MigrationRolloutGovernanceRequest
                {
                    Wave = MigrationRolloutWave.Wave1NonCritical,
                    IsCriticalDataSource = false,
                    ReviewedBy = Environment.UserName,
                    Notes = "Auto-generated governance request."
                };

            // Destructive relational ops (DropFK, DropIndex) on RDBMS are
            // Wave3Critical — they remove referential integrity or query
            // performance contracts. Additive FK/Index ops are Wave2Standard
            // — they are non-destructive and can be safely phased.
            var hasDestructiveRelOp = plan.Operations?.Any(operation =>
                operation != null &&
                (operation.Kind == MigrationPlanOperationKind.DropForeignKey ||
                 operation.Kind == MigrationPlanOperationKind.DropIndex)) ?? false;

            var hasCriticalOp = plan.Operations?.Any(operation =>
                operation != null &&
                (operation.RiskLevel == MigrationPlanRiskLevel.Critical ||
                 operation.IsDestructive)) ?? false;

            var hasAddRelOp = plan.Operations?.Any(operation =>
                operation != null &&
                (operation.Kind == MigrationPlanOperationKind.AddForeignKey ||
                 operation.Kind == MigrationPlanOperationKind.CreateIndex)) ?? false;

            var isCritical = hasCriticalOp || hasDestructiveRelOp;
            var wave = hasCriticalOp || hasDestructiveRelOp
                ? MigrationRolloutWave.Wave3Critical
                : hasAddRelOp
                    ? MigrationRolloutWave.Wave2StandardProduction
                    : MigrationRolloutWave.Wave1NonCritical;

            return new MigrationRolloutGovernanceRequest
            {
                Wave = wave,
                IsCriticalDataSource = isCritical,
                ReviewedBy = Environment.UserName,
                Notes = "Auto-generated governance request."
            };
        }

        private static void AddWaveEligibilityGate(MigrationRolloutGovernanceReport report, MigrationRolloutGovernanceRequest request)
        {
            var blocked = request.Wave == MigrationRolloutWave.Wave1NonCritical && request.IsCriticalDataSource;
            report.Gates.Add(new MigrationRolloutGateResult
            {
                Gate = "wave-eligibility",
                Decision = blocked ? MigrationPolicyDecision.Block : MigrationPolicyDecision.Pass,
                Observed = request.IsCriticalDataSource ? "critical" : "non-critical",
                Threshold = request.Wave.ToString(),
                Message = blocked
                    ? "Critical datasource cannot be promoted in Wave 1."
                    : "Datasource eligibility matches rollout wave."
            });
        }

        private static void AddSuccessRateGate(MigrationRolloutGovernanceReport report, MigrationRolloutGovernanceRequest request, double observed)
        {
            report.Gates.Add(new MigrationRolloutGateResult
            {
                Gate = "kpi-success-rate",
                Decision = observed >= request.Thresholds.MinSuccessRate ? MigrationPolicyDecision.Pass : MigrationPolicyDecision.Block,
                Observed = observed.ToString("P2"),
                Threshold = $">= {request.Thresholds.MinSuccessRate:P2}",
                Message = observed >= request.Thresholds.MinSuccessRate
                    ? "Success rate meets rollout threshold."
                    : "Success rate below rollout threshold."
            });
        }

        private static void AddDurationGate(MigrationRolloutGovernanceReport report, MigrationRolloutGovernanceRequest request, double observed)
        {
            report.Gates.Add(new MigrationRolloutGateResult
            {
                Gate = "kpi-mean-execution-duration",
                Decision = observed <= request.Thresholds.MaxMeanExecutionDurationMilliseconds ? MigrationPolicyDecision.Pass : MigrationPolicyDecision.Block,
                Observed = $"{Math.Round(observed, 2)} ms",
                Threshold = $"<= {request.Thresholds.MaxMeanExecutionDurationMilliseconds} ms",
                Message = observed <= request.Thresholds.MaxMeanExecutionDurationMilliseconds
                    ? "Mean execution duration is within threshold."
                    : "Mean execution duration exceeds threshold."
            });
        }

        private static void AddRollbackRateGate(MigrationRolloutGovernanceReport report, MigrationRolloutGovernanceRequest request, double observed)
        {
            report.Gates.Add(new MigrationRolloutGateResult
            {
                Gate = "kpi-rollback-invocation-rate",
                Decision = observed <= request.Thresholds.MaxRollbackInvocationRate ? MigrationPolicyDecision.Pass : MigrationPolicyDecision.Block,
                Observed = observed.ToString("P2"),
                Threshold = $"<= {request.Thresholds.MaxRollbackInvocationRate:P2}",
                Message = observed <= request.Thresholds.MaxRollbackInvocationRate
                    ? "Rollback invocation rate is within threshold."
                    : "Rollback invocation rate exceeds threshold."
            });
        }

        private static void AddPolicyBlockGate(MigrationRolloutGovernanceReport report, MigrationRolloutGovernanceRequest request, double observed)
        {
            report.Gates.Add(new MigrationRolloutGateResult
            {
                Gate = "kpi-policy-block-ratio",
                Decision = observed <= request.Thresholds.MaxPolicyBlockRatio ? MigrationPolicyDecision.Pass : MigrationPolicyDecision.Block,
                Observed = observed.ToString("P2"),
                Threshold = $"<= {request.Thresholds.MaxPolicyBlockRatio:P2}",
                Message = observed <= request.Thresholds.MaxPolicyBlockRatio
                    ? "Policy-block ratio is within threshold."
                    : "Policy-block ratio exceeds threshold."
            });
        }

        private static void EvaluateHardStopPolicy(
            MigrationRolloutGovernanceReport report,
            MigrationRolloutGovernanceRequest request,
            MigrationPlanArtifact plan,
            MigrationTelemetrySnapshot snapshot)
        {
            if (request.HardStopPolicy.StopOnAnyCriticalDiagnostic &&
                snapshot.Diagnostics.Any(diagnostic => diagnostic.Severity == MigrationDiagnosticSeverity.Critical))
            {
                report.HardStopTriggered = true;
                report.HardStopReason = "Critical diagnostic detected.";
                return;
            }

            if (snapshot.FailureRate > request.HardStopPolicy.MaxFailureRate)
            {
                report.HardStopTriggered = true;
                report.HardStopReason = $"Failure rate {snapshot.FailureRate:P2} exceeded hard-stop threshold {request.HardStopPolicy.MaxFailureRate:P2}.";
                return;
            }

            if (request.Wave == MigrationRolloutWave.Wave3Critical &&
                request.HardStopPolicy.StopOnAnyRollbackForCriticalWave &&
                snapshot.Metrics.RollbackCount > 0)
            {
                report.HardStopTriggered = true;
                report.HardStopReason = "Rollback detected during critical wave.";
            }
        }

        private static void RecordRolloutDecisionAudit(MigrationPlanArtifact plan, MigrationRolloutGovernanceReport report)
        {
            var audit = CreateAuditEvent(
                executionToken: plan.ExecutionCheckpoint?.ExecutionToken,
                correlationId: plan.ExecutionCheckpoint?.CorrelationId,
                planId: plan.PlanId,
                planHash: plan.PlanHash,
                eventType: "RolloutGovernanceReviewed",
                approvedBy: report.ReviewedBy,
                executedBy: Environment.UserName,
                result: report.CanPromote ? "Promoted" : "Blocked",
                notes: report.HardStopTriggered
                    ? $"hard-stop={report.HardStopReason}"
                    : $"wave={report.Wave}; gates={string.Join(",", report.Gates.Select(g => $"{g.Gate}:{g.Decision}"))}");
            AddAuditEvent(audit);
        }
    }
}
