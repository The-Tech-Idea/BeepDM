using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    /// <summary>
    /// Broker-agnostic publish contract.
    /// Implementations must be safe for concurrent use.
    /// </summary>
    public interface IEventProducer<TPayload>
    {
        /// <summary>Publishes a single event envelope directly (at-least-once path).</summary>
        Task<PublishResult> PublishAsync(
            EventEnvelope<TPayload> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>Enqueues an event into the transactional outbox for reliable delivery.</summary>
        Task<PublishResult> PublishViaOutboxAsync(
            EventEnvelope<TPayload> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>Batch publish — brokers should use a single broker transaction when supported.</summary>
        Task<IReadOnlyList<PublishResult>> PublishBatchAsync(
            IEnumerable<EventEnvelope<TPayload>> envelopes,
            CancellationToken cancellationToken = default);
    }

    /// <summary>Result of a single publish attempt.</summary>
    public sealed class PublishResult
    {
        public bool Success { get; init; }
        public string EventId { get; init; }
        public string FailureReason { get; init; }
        public bool IsDuplicate { get; init; }

        public static PublishResult Ok(string eventId) => new() { Success = true, EventId = eventId };
        public static PublishResult Fail(string eventId, string reason) => new() { Success = false, EventId = eventId, FailureReason = reason };
        public static PublishResult Duplicate(string eventId) => new() { Success = true, EventId = eventId, IsDuplicate = true };
    }
}
