using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Retry-topic configuration
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Describes a single retry level (e.g. "retry-1", "retry-2").</summary>
    public sealed class RetryTopicLevel
    {
        /// <summary>Suffix appended to the base topic name to build the retry-topic name.</summary>
        public string SuffixName { get; init; } = string.Empty;

        /// <summary>How long the consumer should wait before reprocessing from this level.</summary>
        public TimeSpan Delay { get; init; }

        /// <summary>Maximum number of delivery attempts permitted at this level.</summary>
        public int MaxAttempts { get; init; } = 1;

        /// <summary>Optional consumer-group suffix for this level (isolates retry consumers).</summary>
        public string? ConsumerGroupSuffix { get; init; }
    }

    /// <summary>
    /// Full retry-topic configuration for a single originating topic.
    /// Describes all retry levels and the dead-letter queue (DLQ) topic.
    /// </summary>
    public sealed class RetryTopicConfig
    {
        /// <summary>Original topic that the retry chain is built for.</summary>
        public string OriginalTopic { get; init; } = string.Empty;

        /// <summary>Ordered list of retry levels (level 0 = first retry, etc.).</summary>
        public IReadOnlyList<RetryTopicLevel> Levels { get; init; } = Array.Empty<RetryTopicLevel>();

        /// <summary>Fully-qualified DLQ topic name for permanently-failed messages.</summary>
        public string DlqTopicName { get; init; } = string.Empty;

        /// <summary>Optional prefix prepended to <see cref="Levels"/> suffix names (defaults to <see cref="OriginalTopic"/>).</summary>
        public string? RetryTopicPrefix { get; init; }

        /// <summary>Total number of delivery attempts across all levels.</summary>
        public int TotalMaxAttempts => Levels.Sum(l => l.MaxAttempts);

        /// <summary>Builds the fully-qualified retry-topic name for the given level.</summary>
        public string GetRetryTopicName(RetryTopicLevel level)
        {
            var prefix = RetryTopicPrefix ?? OriginalTopic;
            return $"{prefix}.{level.SuffixName}";
        }

        /// <summary>Returns the DLQ topic name (exists for symmetry with <see cref="GetRetryTopicName"/>).</summary>
        public string GetDlqTopicName() => DlqTopicName;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Dead-letter envelope
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Wraps the original event with failure metadata before it is published to the DLQ topic.
    /// </summary>
    public sealed class DlqEnvelope<T>
    {
        /// <summary>Original topic from which the event was consumed.</summary>
        public string OriginalTopic { get; init; } = string.Empty;

        /// <summary>Partition on which the original event was received.</summary>
        public int OriginalPartition { get; init; }

        /// <summary>Offset of the original event.</summary>
        public long OriginalOffset { get; init; }

        /// <summary>Total number of delivery attempts (including the final failing attempt).</summary>
        public int RetryCount { get; init; }

        /// <summary>Human-readable description of the last failure.</summary>
        public string FailureReason { get; init; } = string.Empty;

        /// <summary>Timestamp of the most recent failure.</summary>
        public DateTimeOffset LastFailureAt { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>The <c>EventId</c> of the original event envelope.</summary>
        public string OriginalEventId { get; init; } = string.Empty;

        /// <summary>The <c>EventType</c> of the original event envelope.</summary>
        public string OriginalEventType { get; init; } = string.Empty;

        /// <summary>The original deserialized payload.</summary>
        public T OriginalPayload { get; init; } = default!;

        /// <summary>Headers present on the original broker message.</summary>
        public IReadOnlyDictionary<string, string> OriginalHeaders { get; init; }
            = new Dictionary<string, string>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DLQ routing policy
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Governs when a message should be routed to the DLQ vs. a retry topic.
    /// </summary>
    public sealed class DlqRoutingPolicy
    {
        /// <summary>The main topic this policy applies to.</summary>
        public string TopicName { get; init; } = string.Empty;

        /// <summary>Maximum total delivery attempts before the DLQ is used.</summary>
        public int MaxRetries { get; init; }

        /// <summary>Fully-qualified DLQ topic name.</summary>
        public string DlqTopicName { get; init; } = string.Empty;

        /// <summary>Optional retry-topic configuration for tiered retry.</summary>
        public RetryTopicConfig? RetryTopicConfig { get; init; }

        /// <summary>
        /// Exception type full names (e.g. <c>"System.ArgumentException"</c>) that must skip
        /// retry and go directly to the DLQ regardless of <see cref="MaxRetries"/>.
        /// </summary>
        public IReadOnlyList<string> PermanentErrorTypes { get; init; } = Array.Empty<string>();

        /// <summary>Returns <c>true</c> when <paramref name="exception"/> represents an unrecoverable error.</summary>
        public bool IsPermanentError(Exception exception)
        {
            if (exception is null) return false;
            var typeName = exception.GetType().FullName ?? exception.GetType().Name;
            return PermanentErrorTypes.Contains(typeName, StringComparer.Ordinal);
        }
    }
}
