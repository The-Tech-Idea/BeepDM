using System;

namespace TheTechIdea.Beep.StreamandEvents
{
    /// <summary>
    /// Outbox record persisted atomically with a business transaction.
    /// The OutboxDispatcherService drains these into the broker.
    /// </summary>
    public sealed class OutboxRecord
    {
        public string OutboxId { get; init; } = Guid.NewGuid().ToString();

        /// <summary>Deterministic idempotency key — resend must produce same broker outcome.</summary>
        public string IdempotencyKey { get; init; }

        public string Topic { get; init; }
        public string EventType { get; init; }
        public string EventId { get; init; }

        /// <summary>Serialized envelope payload bytes.</summary>
        public byte[] PayloadBytes { get; init; }

        public string ContentType { get; init; } = "application/json";
        public string PartitionKey { get; init; }
        public string CorrelationId { get; init; }

        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public DateTime? DispatchedAt { get; set; }
        public DateTime? FailedAt { get; set; }

        public int AttemptCount { get; set; }
        public string Status { get; set; } = "Pending"; // Pending | Dispatched | Failed | Abandoned
        public string FailureReason { get; set; }
    }

    /// <summary>Persistence contract for outbox records.</summary>
    public interface IOutboxStore
    {
        System.Threading.Tasks.Task InsertAsync(OutboxRecord record, System.Threading.CancellationToken cancellationToken = default);
        System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<OutboxRecord>> FetchPendingAsync(int batchSize, System.Threading.CancellationToken cancellationToken = default);
        System.Threading.Tasks.Task MarkDispatchedAsync(string outboxId, System.Threading.CancellationToken cancellationToken = default);
        System.Threading.Tasks.Task MarkFailedAsync(string outboxId, string reason, System.Threading.CancellationToken cancellationToken = default);
        System.Threading.Tasks.Task AbandonStaleAsync(TimeSpan maxAge, System.Threading.CancellationToken cancellationToken = default);
    }
}
