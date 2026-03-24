using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Stream Tier ──────────────────────────────────────────────────────────────

    /// <summary>Operational tier for capacity planning and admission control.</summary>
    public enum StreamTier
    {
        /// <summary>Business-critical streams. Strictest latency and durability budgets.</summary>
        Critical,
        /// <summary>Standard throughput streams. Balanced latency and durability.</summary>
        Standard,
        /// <summary>High-volume, higher-latency acceptable bulk streams.</summary>
        Bulk,
        /// <summary>High-frequency telemetry/metrics streams; lossy delivery acceptable.</summary>
        Telemetry
    }

    /// <summary>
    /// Capacity envelope for a <see cref="StreamTier"/>. Used for admission control
    /// and channel/worker sizing decisions at runtime.
    /// </summary>
    public sealed record StreamTierProfile
    {
        public required StreamTier Tier                   { get; init; }
        public required int        MaxMessagesPerSecond   { get; init; }
        public required int        MaxPayloadBytes        { get; init; }
        public required int        ChannelCapacity        { get; init; }
        public required int        MaxConcurrentHandlers  { get; init; }
        public required TimeSpan   MaxHandlerTimeout      { get; init; }
        public required TimeSpan   MaxE2ELatency          { get; init; }
    }

    // ── Benchmark ────────────────────────────────────────────────────────────────

    /// <summary>Performance target for a specific topic used in CI regression checks.</summary>
    public sealed record StreamBenchmarkTarget
    {
        public required string     Topic                    { get; init; }
        public required StreamTier Tier                    { get; init; }
        public required long       TargetThroughputPerSec  { get; init; }
        public required TimeSpan   TargetP99Latency        { get; init; }
        public required double     MaxErrorRatePercent     { get; init; }
    }

    // ── Chaos ────────────────────────────────────────────────────────────────────

    /// <summary>The system boundary targeted by a chaos scenario.</summary>
    public enum ChaosTarget
    {
        BrokerDisconnect,
        PartitionLoss,
        HandlerTimeout,
        NetworkLatency,
        HandlerException,
        OutboxLag
    }

    /// <summary>
    /// Definition of a chaos validation scenario.
    /// Run these during pre-rollout readiness checks or CI chaos stages.
    /// </summary>
    public sealed record ChaosScenario
    {
        public required string     ScenarioId   { get; init; }
        public required string     Description  { get; init; }
        public required ChaosTarget Target      { get; init; }
        public required TimeSpan   Duration     { get; init; }
        public IDictionary<string, string> Parameters { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    /// <summary>Outcome of executing a <see cref="ChaosScenario"/>.</summary>
    public sealed record ChaosResult
    {
        public required string   ScenarioId                  { get; init; }
        public required bool     PassedGracefulDegradation   { get; init; }
        public required bool     RecoveredWithinSlo          { get; init; }
        public required TimeSpan TimeToRecovery              { get; init; }
        public required int      MessagesLost                { get; init; }
        public required int      DuplicatesDetected          { get; init; }
        public string?           Notes                       { get; init; }
    }
}
