using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    /// <summary>
    /// Broker-agnostic consume contract.
    /// The inner <c>IAsyncEnumerable</c> stream supports cooperative cancellation and rebalance-safe commits.
    /// </summary>
    public interface IEventConsumer<TPayload>
    {
        /// <summary>
        /// Streams received envelopes.
        /// The consumer is responsible for calling <see cref="IAckContext.AckAsync"/> after
        /// successful processing to advance the commit offset.
        /// </summary>
        IAsyncEnumerable<ReceivedEvent<TPayload>> ConsumeAsync(
            string topic,
            ConsumerGroupPolicy groupPolicy,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// An event received from the broker together with its ack handle.
    /// </summary>
    public sealed class ReceivedEvent<TPayload>
    {
        public EventEnvelope<TPayload> Envelope { get; init; }
        public IAckContext AckContext { get; init; }
        public PartitionAssignment Partition { get; init; }
    }

    /// <summary>Carry-safe partition assignment snapshot.</summary>
    public sealed class PartitionAssignment
    {
        public string Topic { get; init; }
        public int PartitionId { get; init; }
        public long Offset { get; init; }
    }

    /// <summary>
    /// Acknowledgement handle returned with every received event.
    /// Commit happens only after the handler confirms success.
    /// </summary>
    public interface IAckContext
    {
        Task AckAsync(CancellationToken cancellationToken = default);
        Task NackAsync(bool requeue = true, CancellationToken cancellationToken = default);
    }
}
