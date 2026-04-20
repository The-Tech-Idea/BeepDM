using System;

namespace TheTechIdea.Beep.Distributed.Transactions
{
    /// <summary>
    /// Immutable record of a single event captured by an
    /// <see cref="IDistributedTransactionLog"/>. V1 stores these
    /// in memory; Phase 13 (durable log) serialises them to disk.
    /// </summary>
    public sealed class TransactionLogEntry
    {
        /// <summary>Creates a new log entry.</summary>
        public TransactionLogEntry(
            string              correlationId,
            TransactionLogKind  kind,
            string              shardId,
            string              message,
            Exception           error,
            DateTime            timestampUtc)
        {
            if (string.IsNullOrWhiteSpace(correlationId))
                throw new ArgumentException(
                    "CorrelationId cannot be null or whitespace.", nameof(correlationId));

            CorrelationId = correlationId;
            Kind          = kind;
            ShardId       = shardId ?? string.Empty;
            Message       = message ?? string.Empty;
            Error         = error;
            TimestampUtc  = timestampUtc;
        }

        /// <summary>Scope correlation id the entry belongs to.</summary>
        public string CorrelationId { get; }

        /// <summary>Kind of event.</summary>
        public TransactionLogKind Kind { get; }

        /// <summary>Shard id the event targeted, or empty for coordinator-level events.</summary>
        public string ShardId { get; }

        /// <summary>Human-readable message for diagnostics.</summary>
        public string Message { get; }

        /// <summary>Exception captured when the event is a failure, or <c>null</c>.</summary>
        public Exception Error { get; }

        /// <summary>UTC timestamp the event was recorded.</summary>
        public DateTime TimestampUtc { get; }

        /// <summary>Factory that timestamps at call time.</summary>
        public static TransactionLogEntry Now(
            string              correlationId,
            TransactionLogKind  kind,
            string              shardId = null,
            string              message = null,
            Exception           error   = null)
            => new TransactionLogEntry(
                correlationId, kind, shardId, message, error, DateTime.UtcNow);

        /// <inheritdoc/>
        public override string ToString()
            => $"[{TimestampUtc:O}] {CorrelationId} {Kind} shard={ShardId} :: {Message}"
                + (Error != null ? " -- " + Error.Message : string.Empty);
    }
}
