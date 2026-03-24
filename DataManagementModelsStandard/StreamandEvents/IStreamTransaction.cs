using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Enums ─────────────────────────────────────────────────────────────────

    /// <summary>Lifecycle states of a streaming transaction.</summary>
    public enum TransactionState
    {
        /// <summary>No transaction is active on this producer.</summary>
        Inactive,

        /// <summary>Broker-side transaction resources are being initialised.</summary>
        Initializing,

        /// <summary>Transaction is open and accepts new messages/offset commits.</summary>
        Active,

        /// <summary>Commit is in progress.</summary>
        Committing,

        /// <summary>Abort is in progress.</summary>
        Aborting,

        /// <summary>Transaction was successfully committed.</summary>
        Committed,

        /// <summary>Transaction was successfully aborted (rolled back).</summary>
        Aborted,

        /// <summary>Transaction encountered an unrecoverable error; the producer must be re-initialised.</summary>
        Failed
    }

    /// <summary>Controls which messages are visible to consuming applications.</summary>
    public enum TransactionIsolationLevel
    {
        /// <summary>Consumers see all messages, including those from in-progress or aborted transactions.</summary>
        ReadUncommitted,

        /// <summary>Consumers only see messages from committed transactions.</summary>
        ReadCommitted
    }

    // ── Options ───────────────────────────────────────────────────────────────

    /// <summary>Configuration for initialising exactly-once semantics on a producer.</summary>
    public sealed class TransactionOptions
    {
        /// <summary>
        /// Stable, globally unique identifier for the transactional producer.
        /// The broker fences duplicate producers sharing the same ID.
        /// </summary>
        public string TransactionalId { get; init; }

        /// <summary>Maximum time the broker waits before proactively aborting an open transaction.</summary>
        public TimeSpan TransactionTimeout { get; init; } = TimeSpan.FromSeconds(60);

        /// <summary>Controls which messages downstream consumers see.</summary>
        public TransactionIsolationLevel IsolationLevel { get; init; } = TransactionIsolationLevel.ReadCommitted;

        /// <summary>Maximum number of in-flight produce requests allowed before back-pressure kicks in.</summary>
        public int MaxInflightRequests { get; init; } = 5;
    }

    // ── Offset commit record ──────────────────────────────────────────────────

    /// <summary>
    /// Encapsulates the offset that a consumer wishes to commit atomically within a transaction.
    /// This is required for the consume-transform-produce (Kafka Streams-style) pattern.
    /// </summary>
    public sealed record TransactionalOffsetCommit(
        string Topic,
        int    Partition,
        long   Offset,
        string ConsumerGroupId);

    // ── Transaction handle ────────────────────────────────────────────────────

    /// <summary>
    /// A handle to an open streaming transaction.
    /// Dispose (via <see cref="IAsyncDisposable"/>) will abort if not yet committed.
    /// </summary>
    public interface IStreamTransaction : IAsyncDisposable
    {
        /// <summary>Unique identifier assigned to this transaction instance.</summary>
        string TransactionId { get; }

        /// <summary>Current lifecycle state.</summary>
        TransactionState State { get; }

        /// <summary>Begins the transaction on the broker. Must be called before publishing.</summary>
        Task BeginAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Atomically commits all messages and offset commits accumulated since <see cref="BeginAsync"/>.
        /// </summary>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>Rolls back all messages and offset commits in the current transaction.</summary>
        Task AbortAsync(CancellationToken cancellationToken = default);
    }

    // ── Transactional producer ────────────────────────────────────────────────

    /// <summary>
    /// Exactly-once producer that participates in <see cref="IStreamTransaction"/> boundaries.
    /// </summary>
    /// <typeparam name="TPayload">Payload type for events published through this producer.</typeparam>
    public interface ITransactionalProducer<TPayload>
    {
        /// <summary>
        /// Registers the transactional ID with the broker and fences any prior epoch.
        /// Must be called once before opening the first transaction.
        /// </summary>
        Task InitTransactionsAsync(TransactionOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes <paramref name="payload"/> to <paramref name="topic"/> within
        /// the supplied open <paramref name="transaction"/>.
        /// </summary>
        Task PublishInTransactionAsync(
            IStreamTransaction transaction,
            string             topic,
            TPayload           payload,
            EventHeaders       headers            = null,
            CancellationToken  cancellationToken  = default);

        /// <summary>
        /// Atomically sends consumer offsets to the broker as part of the transaction,
        /// enabling the consume-transform-produce EOS pattern.
        /// </summary>
        Task SendConsumerOffsetsToTransactionAsync(
            IStreamTransaction                         transaction,
            IEnumerable<TransactionalOffsetCommit>     offsets,
            CancellationToken                          cancellationToken = default);

        /// <summary><c>true</c> when transactions have been initialised and the producer is ready.</summary>
        bool IsTransactionReady { get; }
    }

    // ── Exception ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Thrown when a transactional producer is fenced by the broker because a newer epoch of the
    /// same <c>TransactionalId</c> has been registered — indicating a split-brain scenario.
    /// </summary>
    [Serializable]
    public sealed class TransactionFencedException : Exception
    {
        public string TransactionalId { get; }

        public TransactionFencedException(string transactionalId)
            : base($"Producer with transactional ID '{transactionalId}' was fenced by the broker. " +
                   "A newer epoch is active. Stop duplicated producer instances before retrying.")
        {
            TransactionalId = transactionalId;
        }

        public TransactionFencedException(string transactionalId, Exception inner)
            : base($"Producer with transactional ID '{transactionalId}' was fenced by the broker.", inner)
        {
            TransactionalId = transactionalId;
        }

#pragma warning disable SYSLIB0051
        private TransactionFencedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            TransactionalId = info.GetString(nameof(TransactionalId));
        }
#pragma warning restore SYSLIB0051
    }
}
