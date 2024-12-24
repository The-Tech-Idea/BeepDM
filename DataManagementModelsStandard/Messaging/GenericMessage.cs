using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;

namespace TheTechIdea.Beep.Messaging
{
    /// <summary>
    /// Represents a generic message with dynamic payload and metadata.
    /// </summary>
    public class GenericMessage : INotifyPropertyChanged
    {
        private string _entityName;
        private object _payload;
        private Dictionary<string, string> _metadata;
        /// <summary>
        /// RabbitMQ-specific delivery tag for acknowledging the message.
        /// </summary>
        public ulong? DeliveryTag { get; set; }
        /// <summary>
        /// The name of the entity/stream associated with the message.
        /// </summary>
        public string EntityName
        {
            get => _entityName;
            set
            {
                if (_entityName != value)
                {
                    _entityName = value;
                    OnPropertyChanged(nameof(EntityName));
                }
            }
        }

        /// <summary>
        /// The payload of the message. Can be any object or serialized data.
        /// </summary>
        public object Payload
        {
            get => _payload;
            set
            {
                if (_payload != value)
                {
                    _payload = value;
                    OnPropertyChanged(nameof(Payload));
                }
            }
        }

        /// <summary>
        /// Additional metadata for the message (e.g., headers, correlation IDs).
        /// </summary>
        public Dictionary<string, string> Metadata
        {
            get => _metadata ??= new Dictionary<string, string>();
            set
            {
                if (_metadata != value)
                {
                    _metadata = value;
                    OnPropertyChanged(nameof(Metadata));
                }
            }
        }
       

        public bool IsValidPriority(int maxPriority)
        {
            return Priority.HasValue && Priority.Value >= 0 && Priority.Value <= maxPriority;
        }

        /// <summary>
        /// The timestamp when the message was created.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// A unique identifier for the message.
        /// </summary>
        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The priority of the message (optional).
        /// </summary>
        public int? Priority { get; set; }

        /// <summary>
        /// Converts the payload to JSON for serialization.
        /// </summary>
        /// <returns>The JSON representation of the payload.</returns>
        public string SerializePayload()
        {
            return Payload != null ? JsonSerializer.Serialize(Payload) : null;
        }
        public string SerializePayload(JsonSerializerOptions options)
        {
            return Payload != null ? JsonSerializer.Serialize(Payload, options) : null;
        }

        /// <summary>
        /// Deserializes JSON into the payload.
        /// </summary>
        /// <typeparam name="T">The target type for deserialization.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        public void DeserializePayload<T>(string json)
        {
            Payload = JsonSerializer.Deserialize<T>(json);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Clones the current message to create a new instance with the same data.
        /// </summary>
        public GenericMessage Clone()
        {
            return new GenericMessage
            {
                EntityName = this.EntityName,
                Payload = this.Payload,
                Metadata = new Dictionary<string, string>(this.Metadata),
                Timestamp = this.Timestamp,
                MessageId = this.MessageId,
                Priority = this.Priority
            };
        }
        public override bool Equals(object obj)
        {
            return obj is GenericMessage other &&
                   EntityName == other.EntityName &&
                   MessageId == other.MessageId &&
                   Timestamp == other.Timestamp;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EntityName, MessageId, Timestamp);
        }

    }
}
