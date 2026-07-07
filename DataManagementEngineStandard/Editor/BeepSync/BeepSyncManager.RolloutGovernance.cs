using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor.BeepSync;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Phase 10 — Rollout governance partial. Evaluates promotion eligibility for a
    /// sync plan based on measured KPIs and a caller-supplied threshold policy.
    /// </summary>
    public partial class BeepSyncManager
    {
        /// <summary>
        /// Compare the supplied <paramref name="measured"/> KPIs against
        /// <paramref name="thresholds"/> and return a <see cref="SyncGovernanceReport"/>
        /// describing whether the plan may be promoted. Promotion walks the wave enum
        /// forward by one step (Draft → Canary → EarlyAdopter → GA) when allowed; never
        /// skips a wave.
        /// </summary>
        public SyncGovernanceReport EvaluateRolloutGovernance(
            DataSyncSchema schema,
            SyncMetrics measured,
            SyncKpiThresholds thresholds = null)
        {
            thresholds ??= new SyncKpiThresholds();
            measured ??= new SyncMetrics();

            var blockers = new List<string>();

            if (measured.RejectRate > thresholds.MaxRejectRate)
            {
                blockers.Add(
                    $"Reject rate {measured.RejectRate:P2} exceeds threshold " +
                    $"{thresholds.MaxRejectRate:P2}.");
            }
            if (measured.ConflictRate > thresholds.MaxConflictRate)
            {
                blockers.Add(
                    $"Conflict rate {measured.ConflictRate:P2} exceeds threshold " +
                    $"{thresholds.MaxConflictRate:P2}.");
            }
            if (measured.FreshnessLagSeconds > thresholds.MaxFreshnessLagSeconds)
            {
                blockers.Add(
                    $"Freshness lag {measured.FreshnessLagSeconds:F0}s exceeds threshold " +
                    $"{thresholds.MaxFreshnessLagSeconds:F0}s.");
            }
            if (string.IsNullOrWhiteSpace(measured.SloComplianceTier) ||
                !SloTierMeets(measured.SloComplianceTier, thresholds.MinSloComplianceTier))
            {
                blockers.Add(
                    $"SLO tier '{measured.SloComplianceTier ?? "(null)"}' is below required " +
                    $"tier '{thresholds.MinSloComplianceTier}'.");
            }

            var current = schema?.CurrentWave ?? SyncRolloutWave.Draft;
            var recommended = blockers.Count == 0
                ? NextWave(current)
                : current;

            return new SyncGovernanceReport
            {
                PlanId = schema?.Id ?? string.Empty,
                CurrentWave = current,
                RecommendedWave = recommended,
                Promote = blockers.Count == 0 && recommended != current,
                BlockerReasons = blockers,
                MeasuredKpis = measured,
                Thresholds = thresholds
            };
        }

        /// <summary>
        /// Stamp <paramref name="schema"/>'s <see cref="DataSyncSchema.CurrentWave"/>
        /// with <paramref name="targetWave"/> and persist the change. Returns
        /// <see cref="IErrorsInfo"/> with the outcome.
        /// </summary>
        public IErrorsInfo PromoteWave(DataSyncSchema schema, SyncRolloutWave targetWave)
        {
            if (schema == null)
                return new ErrorsInfo { Flag = Errors.Failed, Message = "Schema is null." };
            if (schema.CurrentWave == targetWave)
                return new ErrorsInfo { Flag = Errors.Ok, Message = "Already on target wave." };

            schema.CurrentWave = targetWave;
            schema.LastModifiedAt = DateTime.UtcNow;
            return new ErrorsInfo
            {
                Flag = Errors.Ok,
                Message = $"Promoted '{schema.Id}' to {targetWave}."
            };
        }

        private static SyncRolloutWave NextWave(SyncRolloutWave current) => current switch
        {
            SyncRolloutWave.Draft             => SyncRolloutWave.Canary,
            SyncRolloutWave.Canary            => SyncRolloutWave.EarlyAdopter,
            SyncRolloutWave.EarlyAdopter      => SyncRolloutWave.GeneralAvailability,
            SyncRolloutWave.GeneralAvailability => SyncRolloutWave.Deprecation,
            SyncRolloutWave.Deprecation       => SyncRolloutWave.Deprecation,
            _                                 => SyncRolloutWave.Draft
        };

        private static bool SloTierMeets(string measured, string required)
        {
            // Higher tier is better. Ordering: Gold > Standard > Bronze.
            int Rank(string t) => t switch
            {
                "Gold"     => 3,
                "Standard" => 2,
                "Bronze"   => 1,
                _          => 0
            };
            return Rank(measured) >= Rank(required);
        }
    }
}
