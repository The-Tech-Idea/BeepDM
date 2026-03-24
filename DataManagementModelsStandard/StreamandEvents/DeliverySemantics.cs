using System;

namespace TheTechIdea.Beep.StreamandEvents
{
    /// <summary>Delivery guarantee level declared per topic or subscription.</summary>
    public enum DeliverySemantics
    {
        /// <summary>Message may be lost (fire-and-forget telemetry).</summary>
        AtMostOnce = 0,

        /// <summary>Message delivered at least once; duplicates must be handled by consumer.</summary>
        AtLeastOnce = 1,

        /// <summary>Exactly-once outcome through idempotent consumer + transactional outbox.</summary>
        ExactlyOnce = 2
    }

    /// <summary>When the consumer commits the offset / acks the message.</summary>
    public enum AckStrategy
    {
        /// <summary>Ack before handler runs (at-most-once).</summary>
        BeforeHandler,

        /// <summary>Ack after handler completes successfully (at-least-once).</summary>
        AfterHandler,

        /// <summary>Ack is triggered explicitly by the handler (exactly-once path).</summary>
        Manual
    }

    /// <summary>
    /// Full delivery semantics profile attached to a topic or consumer group.
    /// Declares what guarantees exist and how ordering is managed.
    /// </summary>
    public sealed class DeliverySemanticsProfile
    {
        public DeliverySemantics Semantics { get; init; } = DeliverySemantics.AtLeastOnce;
        public AckStrategy AckStrategy { get; init; } = AckStrategy.AfterHandler;

        /// <summary>Whether per-key ordering is required.</summary>
        public bool OrderingRequired { get; init; }

        /// <summary>Partition key strategy: "Key", "RoundRobin", "Sticky".</summary>
        public string PartitioningStrategy { get; init; } = "Key";

        /// <summary>Idempotency TTL for the dedup window. Null = no dedup.</summary>
        public TimeSpan? IdempotencyWindow { get; init; }

        /// <summary>Whether replaying events (e.g. from DLQ) is safe to apply again.</summary>
        public bool ReplaySafe { get; init; }
    }

    /// <summary>Controls how the retry engine backs off between attempts.</summary>
    public enum BackoffMode { Fixed, Linear, Exponential, Jitter }

    /// <summary>
    /// Retry policy for a consumer or producer — model-only, no implementation coupling.
    /// </summary>
    public sealed class StreamRetryPolicy
    {
        public int MaxAttempts { get; init; } = 3;
        public TimeSpan BaseDelay { get; init; } = TimeSpan.FromSeconds(1);
        public BackoffMode BackoffMode { get; init; } = BackoffMode.Exponential;
        public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(60);

        /// <summary>Exception type names that must NOT be retried (permanent failures).</summary>
        public string[] NonRetryableExceptions { get; init; } = Array.Empty<string>();

        /// <summary>Route to dead-letter topic after all retries exhausted.</summary>
        public bool SendToDeadLetterOnExhaustion { get; init; } = true;
    }
}
