using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor.Mapping.Models;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor.Mapping
{
    /// <summary>
    /// Phase 10 — Rollout governance partial. Adds promotion-evaluation helpers
    /// on top of the existing mapping manager core.
    /// </summary>
    public partial class MappingManager
    {
        /// <summary>
        /// Compare the supplied <paramref name="snapshot"/> against
        /// <paramref name="thresholds"/> and return a promotion decision for
        /// <paramref name="current"/>. Promotion walks the wave enum forward by
        /// one step when allowed; never skips a wave.
        /// </summary>
        public static MappingRolloutDecision EvaluateRolloutGovernance(
            MappingRolloutWave current,
            MappingKpiSnapshot snapshot,
            MappingKpiThresholds thresholds = null)
        {
            thresholds ??= new MappingKpiThresholds();
            snapshot ??= new MappingKpiSnapshot();

            var blockers = new List<string>();

            if (snapshot.AutoMatchCoveragePct < thresholds.MinAutoMatchCoverage)
            {
                blockers.Add(
                    $"Auto-match coverage {snapshot.AutoMatchCoveragePct}% " +
                    $"< threshold {thresholds.MinAutoMatchCoverage}%.");
            }
            if (snapshot.QualityScoreAverage < thresholds.MinQualityScore)
            {
                blockers.Add(
                    $"Quality score average {snapshot.QualityScoreAverage} " +
                    $"< threshold {thresholds.MinQualityScore} ({thresholds.MinBand}).");
            }
            if (snapshot.DriftEntryCount > thresholds.MaxDriftEntries)
            {
                blockers.Add(
                    $"Drift entries {snapshot.DriftEntryCount} " +
                    $"> threshold {thresholds.MaxDriftEntries}.");
            }

            var recommended = blockers.Count == 0 ? NextWave(current) : current;
            return new MappingRolloutDecision
            {
                CurrentWave = current,
                RecommendedWave = recommended,
                Promote = blockers.Count == 0 && recommended != current,
                BlockerReasons = blockers,
                MeasuredKpis = snapshot,
                Thresholds = thresholds
            };
        }

        /// <summary>
        /// Build a <see cref="MappingKpiSnapshot"/> from an <see cref="EntityDataMap"/>
        /// by computing the per-field auto-match coverage and a quality score
        /// from the available diagnostics. Used by the governance evaluation
        /// above and by external dashboards.
        /// </summary>
        public static MappingKpiSnapshot BuildKpiSnapshot(EntityDataMap_DTL map)
        {
            if (map == null) return new MappingKpiSnapshot();

            int total = map.EntityFields?.Count ?? 0;
            int mapped = map.FieldMapping?.Count ?? 0;
            int coverage = total == 0 ? 0 : (int)Math.Round(mapped * 100.0 / total);

            // Heuristic: a flat 70 for a freshly-authored mapping, plus
            // 1 point per covered field (capped at 100). This gives the
            // governance gate something to chew on until the Mapping UI
            // reports a real quality score per <c>MappingQualityBand</c>.
            int quality = Math.Min(100, 70 + Math.Min(mapped, 30));

            return new MappingKpiSnapshot
            {
                AutoMatchCoveragePct = coverage,
                QualityScoreAverage  = quality,
                DriftEntryCount     = 0,
                TopIssueCode        = string.Empty
            };
        }

        private static MappingRolloutWave NextWave(MappingRolloutWave current) => current switch
        {
            MappingRolloutWave.Draft             => MappingRolloutWave.Canary,
            MappingRolloutWave.Canary            => MappingRolloutWave.EarlyAdopter,
            MappingRolloutWave.EarlyAdopter      => MappingRolloutWave.GeneralAvailability,
            MappingRolloutWave.GeneralAvailability => MappingRolloutWave.Deprecation,
            MappingRolloutWave.Deprecation       => MappingRolloutWave.Deprecation,
            _ => MappingRolloutWave.Draft
        };
    }

    /// <summary>
    /// Output of <see cref="MappingManager.EvaluateRolloutGovernance"/>: the
    /// recommended wave, the measured KPIs, and the blocker reasons when
    /// promotion is denied.
    /// </summary>
    public sealed class MappingRolloutDecision
    {
        public MappingRolloutWave CurrentWave    { get; init; }
        public MappingRolloutWave RecommendedWave { get; init; }
        public bool Promote                   { get; init; }
        public IReadOnlyList<string> BlockerReasons { get; init; } = new List<string>();
        public MappingKpiSnapshot MeasuredKpis { get; init; } = new();
        public MappingKpiThresholds Thresholds { get; init; } = new();
    }
}
