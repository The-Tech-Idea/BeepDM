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

        #region Standard Metadata Properties

        /// <summary>
        /// Gets or sets the fully qualified type name of the message payload.
        /// Required metadata key: "MessageType"
        /// </summary>
        public string MessageType
        {
            get => Metadata?.GetValueOrDefault("MessageType");
            set => EnsureMetadata()["MessageType"] = value;
        }

        /// <summary>
        /// Gets or sets the semantic version of the message (e.g., "1.0.0").
        /// Required metadata key: "MessageVersion"
        /// </summary>
        public string MessageVersion
        {
            get => Metadata?.GetValueOrDefault("MessageVersion") ?? "1.0.0";
            set => EnsureMetadata()["MessageVersion"] = value;
        }

        /// <summary>
        /// Gets or sets the correlation ID for request/response or workflow tracking.
        /// Optional metadata key: "CorrelationId"
        /// </summary>
        public string CorrelationId
        {
            get => Metadata?.GetValueOrDefault("CorrelationId");
            set => EnsureMetadata()["CorrelationId"] = value;
        }

        /// <summary>
        /// Gets or sets the causation ID (ID of message that caused this one).
        /// Optional metadata key: "CausationId"
        /// </summary>
        public string CausationId
        {
            get => Metadata?.GetValueOrDefault("CausationId");
            set => EnsureMetadata()["CausationId"] = value;
        }

        /// <summary>
        /// Gets or sets the source application/service that created the message.
        /// Required metadata key: "Source"
        /// </summary>
        public string Source
        {
            get => Metadata?.GetValueOrDefault("Source");
            set => EnsureMetadata()["Source"] = value;
        }

        /// <summary>
        /// Gets or sets the MIME content type (e.g., "application/json").
        /// Required metadata key: "ContentType"
        /// </summary>
        public string ContentType
        {
            get => Metadata?.GetValueOrDefault("ContentType") ?? "application/json";
            set => EnsureMetadata()["ContentType"] = value;
        }

        /// <summary>
        /// Gets or sets the character encoding (default: "utf-8").
        /// Optional metadata key: "Encoding"
        /// </summary>
        public string Encoding
        {
            get => Metadata?.GetValueOrDefault("Encoding") ?? "utf-8";
            set => EnsureMetadata()["Encoding"] = value;
        }

        /// <summary>
        /// Gets or sets the retry count for failed message processing.
        /// Optional metadata key: "RetryCount"
        /// </summary>
        public int? RetryCount
        {
            get
            {
                if (Metadata?.TryGetValue("RetryCount", out var value) == true &&
                    int.TryParse(value, out var count))
                    return count;
                return null;
            }
            set => EnsureMetadata()["RetryCount"] = value?.ToString();
        }

        /// <summary>
        /// Gets or sets the error code if message processing failed.
        /// Optional metadata key: "ErrorCode"
        /// </summary>
        public string ErrorCode
        {
            get => Metadata?.GetValueOrDefault("ErrorCode");
            set => EnsureMetadata()["ErrorCode"] = value;
        }

        /// <summary>
        /// Gets or sets the error message if message processing failed.
        /// Optional metadata key: "ErrorMessage"
        /// </summary>
        public string ErrorMessage
        {
            get => Metadata?.GetValueOrDefault("ErrorMessage");
            set => EnsureMetadata()["ErrorMessage"] = value;
        }

        /// <summary>
        /// Gets or sets the partition key for message routing (Kafka).
        /// Optional metadata key: "PartitionKey"
        /// </summary>
        public string PartitionKey
        {
            get => Metadata?.GetValueOrDefault("PartitionKey");
            set => EnsureMetadata()["PartitionKey"] = value;
        }

        /// <summary>
        /// Gets or sets the routing key for message routing (RabbitMQ).
        /// Optional metadata key: "RoutingKey"
        /// </summary>
        public string RoutingKey
        {
            get => Metadata?.GetValueOrDefault("RoutingKey");
            set => EnsureMetadata()["RoutingKey"] = value;
        }

        /// <summary>
        /// Gets or sets the tenant ID for multi-tenant isolation.
        /// Optional metadata key: "TenantId"
        /// </summary>
        public string TenantId
        {
            get => Metadata?.GetValueOrDefault("TenantId");
            set => EnsureMetadata()["TenantId"] = value;
        }

        /// <summary>
        /// Gets or sets the user ID who triggered the message.
        /// Optional metadata key: "UserId"
        /// </summary>
        public string UserId
        {
            get => Metadata?.GetValueOrDefault("UserId");
            set => EnsureMetadata()["UserId"] = value;
        }

        /// <summary>
        /// Gets or sets the trace ID for distributed tracing.
        /// Optional metadata key: "TraceId"
        /// </summary>
        public string TraceId
        {
            get => Metadata?.GetValueOrDefault("TraceId");
            set => EnsureMetadata()["TraceId"] = value;
        }

        /// <summary>
        /// Ensures the Metadata dictionary is initialized.
        /// </summary>
        private Dictionary<string, string> EnsureMetadata()
        {
            if (Metadata == null) Metadata = new Dictionary<string, string>();
            return Metadata;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validates that the message has all required fields.
        /// </summary>
        /// <returns>True if the message is valid, false otherwise.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(MessageId) &&
                   !string.IsNullOrEmpty(EntityName) &&
                   !string.IsNullOrEmpty(MessageType) &&
                   Payload != null;
        }

        /// <summary>
        /// Gets a list of validation errors for the message.
        /// </summary>
        /// <returns>List of validation error messages.</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(MessageId))
                errors.Add("MessageId is required");

            if (string.IsNullOrEmpty(EntityName))
                errors.Add("EntityName is required");

            if (string.IsNullOrEmpty(MessageType))
                errors.Add("MessageType is required (set via MessageType property or Metadata['MessageType'])");

            if (Payload == null)
                errors.Add("Payload is required");

            return errors;
        }

        #endregion
    }
}
