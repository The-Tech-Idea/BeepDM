using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    /// <summary>
    /// Broker-adapter abstraction. Specific brokers (Kafka, RabbitMQ, etc.) implement this.
    /// The engine references only this interface; broker details stay in adapter assemblies.
    /// </summary>
    public interface IBrokerAdapter
    {
        string BrokerType { get; }

        Task<PublishResult> PublishAsync(BrokerPublishRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PublishResult>> PublishBatchAsync(IEnumerable<BrokerPublishRequest> requests, CancellationToken cancellationToken = default);

        IAsyncEnumerable<BrokerMessage> SubscribeAsync(BrokerSubscribeRequest request, CancellationToken cancellationToken = default);

        Task AckAsync(BrokerAckRequest request, CancellationToken cancellationToken = default);
        Task NackAsync(BrokerAckRequest request, bool requeue = true, CancellationToken cancellationToken = default);

        Task<bool> TopicExistsAsync(string topic, CancellationToken cancellationToken = default);
        Task EnsureTopicAsync(TopicDescriptor descriptor, CancellationToken cancellationToken = default);
        Task DeleteTopicAsync(string topic, CancellationToken cancellationToken = default);

        Task ConnectAsync(CancellationToken cancellationToken = default);
        Task DisconnectAsync(CancellationToken cancellationToken = default);
    }

    public sealed class BrokerPublishRequest
    {
        public string Topic { get; init; }
        public string EventId { get; init; }
        public string IdempotencyKey { get; init; }
        public string PartitionKey { get; init; }
        public byte[] PayloadBytes { get; init; }
        public string ContentType { get; init; }
        public IReadOnlyDictionary<string, string> Headers { get; init; }
    }

    public sealed class BrokerSubscribeRequest
    {
        public string Topic { get; init; }
        public string ConsumerGroupId { get; init; }
        public bool AutoCommit { get; init; }
        public int MaxConcurrency { get; init; } = 1;
    }

    public sealed class BrokerMessage
    {
        public string Topic { get; init; }
        public int Partition { get; init; }
        public long Offset { get; init; }
        public string EventId { get; init; }
        public byte[] PayloadBytes { get; init; }
        public string ContentType { get; init; }
        public IReadOnlyDictionary<string, string> Headers { get; init; }
        public object AckHandle { get; init; }
    }

    public sealed class BrokerAckRequest
    {
        public string Topic { get; init; }
        public int Partition { get; init; }
        public long Offset { get; init; }
        public object AckHandle { get; init; }
    }
}
