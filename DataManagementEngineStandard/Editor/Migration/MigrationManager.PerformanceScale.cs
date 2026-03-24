using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Editor.Migration
{
    public partial class MigrationManager
    {
        public MigrationPerformancePlan BuildPerformancePlan(MigrationPlanArtifact plan, MigrationPerformancePolicy policy = null)
        {
            var performancePlan = new MigrationPerformancePlan
            {
                GeneratedOnUtc = DateTime.UtcNow,
                PlanId = plan?.PlanId ?? string.Empty,
                PlanHash = plan?.PlanHash ?? string.Empty,
                Policy = policy ?? CreateDefaultPerformancePolicy(plan)
            };

            if (plan == null || plan.Operations == null || plan.Operations.Count == 0)
                return performancePlan;

            foreach (var operation in plan.Operations.Where(item => item != null))
            {
                var annotation = BuildOperationScaleAnnotation(operation, plan.ProviderCapabilities);
                performancePlan.OperationAnnotations.Add(annotation);
            }

            var totalEstimatedSeconds = performancePlan.OperationAnnotations.Sum(item => Math.Max(0, item.EstimatedRuntimeSeconds));
            var estimatedWindowMinutes = Math.Max(1, (int)Math.Ceiling(totalEstimatedSeconds / 60.0));
            performancePlan.Kpis = new MigrationPerformanceKpi
            {
                PlannedMigrationWindowMinutes = Math.Max(estimatedWindowMinutes, 15),
                MaxAllowedLockWaitMilliseconds = Math.Min(performancePlan.Policy.LockTimeoutMilliseconds, 10000),
                TargetOperationsPerMinute = Math.Max(5, (int)Math.Ceiling(plan.Operations.Count / Math.Max(1, estimatedWindowMinutes / 5.0)))
            };

            var requiresMaintenance = performancePlan.OperationAnnotations.Any(item => item.WindowMode == MigrationExecutionWindowMode.MaintenanceWindowRequired);
            if (requiresMaintenance)
            {
                performancePlan.MaintenanceWindowGuidance.Add("At least one operation requires maintenance-window execution due to lock/runtime impact.");
                performancePlan.MaintenanceWindowGuidance.Add("Use throttled mode and batch execution for production safety.");
            }
            else
            {
                performancePlan.MaintenanceWindowGuidance.Add("Operations are suitable for online-preferred execution with standard monitoring.");
            }

            performancePlan.TimeoutGuidance.Add($"Lock timeout policy: {performancePlan.Policy.LockTimeoutMilliseconds} ms.");
            performancePlan.TimeoutGuidance.Add($"Batch size policy: {performancePlan.Policy.BatchSize} operations per batch.");
            performancePlan.TimeoutGuidance.Add(performancePlan.Policy.EnableThrottledMode
                ? $"Throttling enabled at {performancePlan.Policy.ThrottleDelayMilliseconds} ms between operations."
                : "Throttling disabled.");

            return performancePlan;
        }

        private static MigrationPerformancePolicy CreateDefaultPerformancePolicy(MigrationPlanArtifact plan)
        {
            var hasHighRisk = plan?.Operations?.Any(operation =>
                operation != null &&
                (operation.RiskLevel == MigrationPlanRiskLevel.High ||
                 operation.RiskLevel == MigrationPlanRiskLevel.Critical ||
                 operation.IsDestructive)) == true;

            return new MigrationPerformancePolicy
            {
                BatchSize = hasHighRisk ? 5 : 15,
                ThrottleDelayMilliseconds = hasHighRisk ? 250 : 0,
                LockTimeoutMilliseconds = hasHighRisk ? 15000 : 30000,
                EnableThrottledMode = hasHighRisk,
                PreferredWindowMode = hasHighRisk
                    ? MigrationExecutionWindowMode.MaintenanceWindowRequired
                    : MigrationExecutionWindowMode.OnlinePreferred
            };
        }

        private static MigrationOperationScaleAnnotation BuildOperationScaleAnnotation(MigrationPlanOperation operation, MigrationProviderCapabilityProfile profile)
        {
            var lockScore = EstimateLockImpactScore(operation, profile);
            var runtimeSeconds = EstimateRuntimeSeconds(operation, lockScore);
            var windowMode = lockScore >= 7 || (profile?.RequiresOfflineWindowForSchemaChanges ?? false)
                ? MigrationExecutionWindowMode.MaintenanceWindowRequired
                : MigrationExecutionWindowMode.OnlinePreferred;

            return new MigrationOperationScaleAnnotation
            {
                EntityName = operation.EntityName,
                OperationKind = operation.Kind,
                WindowMode = windowMode,
                EstimatedRuntimeSeconds = runtimeSeconds,
                EstimatedLockImpactScore = lockScore,
                Note = windowMode == MigrationExecutionWindowMode.MaintenanceWindowRequired
                    ? "High lock/runtime impact; run in controlled maintenance window."
                    : "Suitable for online-preferred execution with normal monitoring."
            };
        }

        private static int EstimateLockImpactScore(MigrationPlanOperation operation, MigrationProviderCapabilityProfile profile)
        {
            var score = 1;

            if (operation == null)
                return score;

            if (operation.RiskLevel == MigrationPlanRiskLevel.Medium)
                score += 2;
            else if (operation.RiskLevel == MigrationPlanRiskLevel.High)
                score += 4;
            else if (operation.RiskLevel == MigrationPlanRiskLevel.Critical)
                score += 6;

            if (operation.IsDestructive)
                score += 2;

            if (operation.Kind == MigrationPlanOperationKind.AddMissingColumns)
                score += Math.Min(3, operation.MissingColumns.Count / 2);

            if (profile != null && !profile.SupportsTransactionalDdl)
                score += 1;
            if (profile?.RequiresOfflineWindowForSchemaChanges == true)
                score += 2;

            return Math.Min(10, Math.Max(1, score));
        }

        private static int EstimateRuntimeSeconds(MigrationPlanOperation operation, int lockImpactScore)
        {
            var baseline = operation?.Kind == MigrationPlanOperationKind.CreateEntity ? 8 :
                           operation?.Kind == MigrationPlanOperationKind.AddMissingColumns ? 12 :
                           operation?.Kind == MigrationPlanOperationKind.UpToDate ? 1 : 15;

            var columnFactor = operation?.MissingColumns?.Count ?? 0;
            return Math.Max(1, baseline + (lockImpactScore * 2) + columnFactor);
        }
    }
}
