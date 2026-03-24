using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Rollout Strategy ─────────────────────────────────────────────────────────

    /// <summary>Progressive rollout strategy for a stream version migration.</summary>
    public enum RolloutStrategy
    {
        /// <summary>Route a fixed percentage to the new stream version for initial validation.</summary>
        Canary,
        /// <summary>Gradually increase traffic across defined wave thresholds.</summary>
        Progressive,
        /// <summary>Immediate full traffic switch with a hard cutover.</summary>
        HardCutover,
        /// <summary>New stream receives message copies; responses still come from the current stream.</summary>
        Shadow
    }

    public enum RolloutGateOperator
    {
        LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual, Equal
    }

    // ── Rollout Definition ───────────────────────────────────────────────────────

    /// <summary>
    /// A single traffic wave in a progressive rollout plan.
    /// The wave advances only when all its <see cref="Gates"/> pass their KPI thresholds.
    /// </summary>
    public sealed record RolloutWave
    {
        public required string WaveId               { get; init; }
        /// <summary>Target percentage of traffic routed to the new stream (0–100).</summary>
        public required int    TargetTrafficPercent { get; init; }
        /// <summary>Minimum stable duration at this traffic level before gates are evaluated.</summary>
        public required TimeSpan SoakDuration       { get; init; }
        public string? Description                  { get; init; }
        public IReadOnlyList<RolloutKpiGate> Gates  { get; init; } = Array.Empty<RolloutKpiGate>();
    }

    /// <summary>
    /// A measurable KPI gate that must satisfy its threshold before the rollout wave can advance.
    /// </summary>
    public sealed record RolloutKpiGate
    {
        public required string             MetricName { get; init; }
        public required double             Threshold  { get; init; }
        public required RolloutGateOperator Operator  { get; init; }
        public string?                     Description { get; init; }
    }

    /// <summary>
    /// Rollout plan for a specific topic migration.
    /// </summary>
    public sealed record StreamRolloutPlan
    {
        public required string            PlanId              { get; init; }
        public required string            Topic               { get; init; }
        public required RolloutStrategy   Strategy            { get; init; }
        public IReadOnlyList<RolloutWave> Waves               { get; init; } = Array.Empty<RolloutWave>();
        public DateTimeOffset             CreatedAt           { get; init; } = DateTimeOffset.UtcNow;
        public string?                    Owner               { get; init; }
        /// <summary>Topic to revert to if a rollback is triggered.</summary>
        public string?                    RollbackTopicName   { get; init; }
    }

    // ── Rollout Audit ────────────────────────────────────────────────────────────

    /// <summary>Immutable audit event for a rollout plan action.</summary>
    public sealed record RolloutAuditRecord
    {
        public required string        PlanId     { get; init; }
        public required string        WaveId     { get; init; }
        /// <summary>One of: "advance", "pause", "rollback", "gate:&lt;metric&gt;".</summary>
        public required string        Action     { get; init; }
        public required bool          Passed     { get; init; }
        public DateTimeOffset         OccurredAt { get; init; } = DateTimeOffset.UtcNow;
        public string?                Detail     { get; init; }
    }
}
