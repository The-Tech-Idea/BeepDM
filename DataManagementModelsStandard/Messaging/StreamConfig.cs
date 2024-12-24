using System.Collections.Generic;

namespace TheTechIdea.Beep.Messaging
{
    /// <summary>
    /// Configuration settings for a message stream or queue.
    /// </summary>
    public class StreamConfig
    {
        /// <summary>
        /// The name of the entity/stream.
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        /// The type of consumer for this stream (e.g., subscriber, listener).
        /// </summary>
        public string ConsumerType { get; set; }

        /// <summary>
        /// The message category (Command, Event, Request, Response).
        /// </summary>
        public string MessageCategory { get; set; }

        /// <summary>
        /// The type of exchange (for systems like RabbitMQ).
        /// </summary>
        public string ExchangeType { get; set; }

        /// <summary>
        /// The partition key (for systems like Kafka).
        /// </summary>
        public string PartitionKey { get; set; }

        /// <summary>
        /// Retention policy for the stream (e.g., time-based, size-based).
        /// </summary>
        public string RetentionPolicy { get; set; }

        /// <summary>
        /// Additional options specific to the messaging system.
        /// </summary>
        public Dictionary<string, object> AdditionalOptions { get; set; } = new Dictionary<string, object>();
    }
}
