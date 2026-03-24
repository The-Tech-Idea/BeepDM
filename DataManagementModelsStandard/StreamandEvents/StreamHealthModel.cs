using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.StreamandEvents
{
    public enum StreamHealthStatus { Healthy, Degraded, Unhealthy, Unknown }

    /// <summary>Health snapshot for a topic/consumer-group pair.</summary>
    public sealed class StreamHealthReport
    {
        public string Topic { get; init; }
        public string ConsumerGroup { get; init; }
        public StreamHealthStatus Status { get; init; }
        public long ConsumerLag { get; init; }
        public double MessagesPerSecond { get; init; }
        public double ErrorRatePercent { get; init; }
        public double HandlerLatencyP99Ms { get; init; }
        public DateTime EvaluatedAt { get; init; } = DateTime.UtcNow;
        public IReadOnlyList<string> Issues { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// SLO definition for a single stream consumer group.
    /// Violation triggers an alert.
    /// </summary>
    public sealed class StreamSloProfile
    {
        public string ProfileName { get; init; }
        public string Topic { get; init; }
        public string ConsumerGroup { get; init; }

        /// <summary>Max acceptable consumer lag in number of messages.</summary>
        public long MaxLagMessages { get; init; } = 1000;

        /// <summary>Max acceptable handler P99 latency in milliseconds.</summary>
        public double MaxHandlerLatencyP99Ms { get; init; } = 5000;

        /// <summary>Max acceptable error rate as a percentage (0-100).</summary>
        public double MaxErrorRatePercent { get; init; } = 1.0;

        /// <summary>Min acceptable throughput in messages/second. Null = not enforced.</summary>
        public double? MinThroughputMps { get; init; }

        /// <summary>Alert rule key for external rule engine evaluation.</summary>
        public string AlertRuleKey { get; init; }
    }

    /// <summary>Telemetry counters accumulated per run or window.</summary>
    public sealed class StreamMetrics
    {
        public string Topic { get; init; }
        public string ConsumerGroup { get; init; }
        public long MessagesPublished { get; init; }
        public long MessagesConsumed { get; init; }
        public long MessagesRejected { get; init; }
        public long MessagesDeadLettered { get; init; }
        public long MessagesDuplicate { get; init; }
        public double ThroughputMps { get; init; }
        public double HandlerLatencyP99Ms { get; init; }
        public long ChannelDepth { get; init; }
        public TimeSpan BackpressureDuration { get; init; }
        public DateTime WindowStart { get; init; }
        public DateTime WindowEnd { get; init; }
    }

    /// <summary>Fired when an SLO is breached.</summary>
    public sealed class StreamAlertRecord
    {
        public string AlertId { get; init; } = Guid.NewGuid().ToString();
        public string Topic { get; init; }
        public string ConsumerGroup { get; init; }
        public string SloProfileName { get; init; }
        public string Severity { get; init; }
        public string Reason { get; init; }
        public string RemediationHint { get; init; }
        public DateTime EmittedAt { get; init; } = DateTime.UtcNow;
        public string Status { get; set; } = "Open"; // Open | Acknowledged | Resolved
    }
}
