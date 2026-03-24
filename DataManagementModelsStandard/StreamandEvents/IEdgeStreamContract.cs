using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    /// <summary>
    /// Server-to-client edge streaming contract.
    /// <para>
    /// Compatible with SignalR hub streaming methods: hub methods may return
    /// <see cref="IAsyncEnumerable{T}"/> directly, and SignalR natively supports
    /// streaming the sequence to connected clients.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The payload type pushed to the client.</typeparam>
    public interface IEdgeStreamServer<T>
    {
        /// <summary>
        /// Starts a server-to-client push stream on the named topic.
        /// <paramref name="cancellationToken"/> carries the client disconnect / unsubscribe signal.
        /// Callers (SignalR hub or gRPC handler) must honour cancellation and stop the iteration
        /// promptly when it fires.
        /// </summary>
        IAsyncEnumerable<EventEnvelope<T>> StreamToClientAsync(
            string topic,
            string consumerGroup,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Client-to-server upload streaming contract.
    /// <para>
    /// Compatible with SignalR client-to-server streaming methods: hub methods may accept
    /// <see cref="IAsyncEnumerable{T}"/> as a parameter.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The payload type uploaded by the client.</typeparam>
    public interface IEdgeStreamClient<T>
    {
        /// <summary>
        /// Receives a client-originated event stream.
        /// <paramref name="cancellationToken"/> carries the upload-abort signal.
        /// Implementations must complete (not abandon) the consumer loop on cancellation.
        /// </summary>
        Task StreamFromClientAsync(
            IAsyncEnumerable<EventEnvelope<T>> payload,
            CancellationToken cancellationToken = default);
    }

    /// <summary>Bidirectional edge stream — combines server push and client upload on the same contract.</summary>
    /// <typeparam name="TUpstream">Payload type flowing from client to server.</typeparam>
    /// <typeparam name="TDownstream">Payload type flowing from server to client.</typeparam>
    public interface IBidirectionalEdgeStream<TUpstream, TDownstream>
        : IEdgeStreamClient<TUpstream>, IEdgeStreamServer<TDownstream>
    {
    }

    /// <summary>Metadata for a live edge stream session.</summary>
    public sealed record EdgeStreamSession
    {
        public required string        SessionId      { get; init; }
        public required string        Topic          { get; init; }
        public required string        ClientId       { get; init; }
        public required string        ConsumerGroup  { get; init; }
        public DateTimeOffset         ConnectedAt    { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset?        DisconnectedAt { get; init; }
        public bool                   IsActive       => DisconnectedAt is null;
    }
}
