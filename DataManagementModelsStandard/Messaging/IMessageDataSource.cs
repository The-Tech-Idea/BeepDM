using System;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Messaging
{
    public interface IMessageDataSource<TMessage, TConfig>
        where TMessage : class
        where TConfig : class
    {
        /// <summary>
        /// Initializes the data source with the specified configuration.
        /// </summary>
        void Initialize(TConfig config);

        /// <summary>
        /// Sends a message to the specified stream.
        /// </summary>
        Task SendMessageAsync(string streamName, TMessage message, CancellationToken cancellationToken);

        /// <summary>
        /// Subscribes to a specific stream to receive messages.
        /// </summary>
        Task SubscribeAsync(string streamName, Func<TMessage, Task> onMessageReceived, CancellationToken cancellationToken);

        /// <summary>
        /// Acknowledges that a message has been successfully processed.
        /// </summary>
        Task AcknowledgeMessageAsync(string streamName, TMessage message, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves a message without committing its acknowledgment (peek functionality).
        /// </summary>
        Task<TMessage> PeekMessageAsync(string streamName, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves metadata about a stream (e.g., queue depth, message count).
        /// </summary>
        Task<object> GetStreamMetadataAsync(string streamName, CancellationToken cancellationToken);

        /// <summary>
        /// Disconnects and cleans up resources.
        /// </summary>
        void Disconnect();
    }
}
