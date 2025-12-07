using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;

namespace TheTechIdea.Beep.Messaging
{
    /// <summary>
    /// Helper class for creating and validating messages according to messaging standards.
    /// </summary>
    public static class MessageStandardsHelper
    {
        #region Standard JSON Serialization Options

        /// <summary>
        /// Default JSON serialization options following messaging standards.
        /// </summary>
        public static readonly JsonSerializerOptions DefaultJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        #endregion

        #region Message Creation

        /// <summary>
        /// Creates a standard message with all required metadata.
        /// </summary>
        /// <param name="entityName">The stream/queue/topic name.</param>
        /// <param name="payload">The message payload object.</param>
        /// <param name="source">The source application/service name.</param>
        /// <param name="messageVersion">The semantic version (default: "1.0.0").</param>
        /// <param name="priority">Message priority 0-255 (default: 100).</param>
        /// <param name="correlationId">Optional correlation ID for request/response.</param>
        /// <param name="causationId">Optional causation ID for event sourcing.</param>
        /// <returns>A properly configured GenericMessage.</returns>
        public static GenericMessage CreateStandardMessage(
            string entityName,
            object payload,
            string source,
            string messageVersion = "1.0.0",
            int? priority = 100,
            string correlationId = null,
            string causationId = null)
        {
            if (string.IsNullOrEmpty(entityName))
                throw new ArgumentException("EntityName is required", nameof(entityName));
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source is required", nameof(source));

            var message = new GenericMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                EntityName = entityName,
                Timestamp = DateTime.UtcNow,
                Payload = payload,
                Priority = priority ?? 100
            };

            // Set required metadata
            message.MessageType = payload.GetType().AssemblyQualifiedName;
            message.MessageVersion = messageVersion;
            message.Source = source;
            message.ContentType = "application/json";
            message.Encoding = "utf-8";

            // Set optional metadata
            if (!string.IsNullOrEmpty(correlationId))
                message.CorrelationId = correlationId;

            if (!string.IsNullOrEmpty(causationId))
                message.CausationId = causationId;

            return message;
        }

        /// <summary>
        /// Creates a correlated message (for request/response or event sourcing).
        /// </summary>
        /// <param name="originalMessage">The original message to correlate with.</param>
        /// <param name="newPayload">The payload for the new message.</param>
        /// <param name="newEntityName">The entity name for the new message.</param>
        /// <param name="source">The source application/service name.</param>
        /// <returns>A new message correlated with the original.</returns>
        public static GenericMessage CreateCorrelatedMessage(
            GenericMessage originalMessage,
            object newPayload,
            string newEntityName,
            string source)
        {
            if (originalMessage == null)
                throw new ArgumentNullException(nameof(originalMessage));
            if (newPayload == null)
                throw new ArgumentNullException(nameof(newPayload));
            if (string.IsNullOrEmpty(newEntityName))
                throw new ArgumentException("NewEntityName is required", nameof(newEntityName));
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source is required", nameof(source));

            var message = CreateStandardMessage(
                newEntityName,
                newPayload,
                source,
                originalMessage.MessageVersion,
                originalMessage.Priority,
                originalMessage.CorrelationId ?? originalMessage.MessageId,
                originalMessage.MessageId
            );

            return message;
        }

        #endregion

        #region Message Validation

        /// <summary>
        /// Validates a message according to messaging standards.
        /// </summary>
        /// <param name="message">The message to validate.</param>
        /// <returns>Validation result with errors if any.</returns>
        public static MessageValidationResult ValidateMessage(GenericMessage message)
        {
            var result = new MessageValidationResult();

            if (message == null)
            {
                result.Errors.Add("Message is null");
                return result;
            }

            // Core validation
            if (string.IsNullOrEmpty(message.MessageId))
                result.Errors.Add("MessageId is required");

            if (string.IsNullOrEmpty(message.EntityName))
                result.Errors.Add("EntityName is required");

            if (message.Payload == null)
                result.Errors.Add("Payload is required");

            // Metadata validation
            if (string.IsNullOrEmpty(message.MessageType))
                result.Errors.Add("MessageType is required in metadata");

            if (string.IsNullOrEmpty(message.MessageVersion))
                result.Errors.Add("MessageVersion is required in metadata");

            if (string.IsNullOrEmpty(message.Source))
                result.Errors.Add("Source is required in metadata");

            if (string.IsNullOrEmpty(message.ContentType))
                result.Errors.Add("ContentType is required in metadata");

            // Priority validation
            if (message.Priority.HasValue && (message.Priority < 0 || message.Priority > 255))
                result.Errors.Add("Priority must be between 0 and 255");

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        /// <summary>
        /// Ensures a message has all required metadata, adding defaults if missing.
        /// </summary>
        /// <param name="message">The message to ensure standards compliance.</param>
        /// <param name="defaultSource">Default source if not set.</param>
        /// <param name="defaultVersion">Default version if not set.</param>
        /// <returns>The message with ensured metadata.</returns>
        public static GenericMessage EnsureMessageStandards(
            GenericMessage message,
            string defaultSource = "Unknown",
            string defaultVersion = "1.0.0")
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            // Ensure MessageId
            if (string.IsNullOrEmpty(message.MessageId))
                message.MessageId = Guid.NewGuid().ToString();

            // Ensure Timestamp
            if (message.Timestamp == default(DateTime))
                message.Timestamp = DateTime.UtcNow;

            // Ensure required metadata
            if (string.IsNullOrEmpty(message.MessageType) && message.Payload != null)
                message.MessageType = message.Payload.GetType().AssemblyQualifiedName;

            if (string.IsNullOrEmpty(message.MessageVersion))
                message.MessageVersion = defaultVersion;

            if (string.IsNullOrEmpty(message.Source))
                message.Source = defaultSource;

            if (string.IsNullOrEmpty(message.ContentType))
                message.ContentType = "application/json";

            if (string.IsNullOrEmpty(message.Encoding))
                message.Encoding = "utf-8";

            // Ensure Priority
            if (!message.Priority.HasValue)
                message.Priority = 100;

            return message;
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Serializes a message payload to JSON using standard options.
        /// </summary>
        /// <param name="payload">The payload to serialize.</param>
        /// <returns>JSON string representation.</returns>
        public static string SerializePayload(object payload)
        {
            if (payload == null) return null;
            return JsonSerializer.Serialize(payload, DefaultJsonOptions);
        }

        /// <summary>
        /// Deserializes JSON to a typed object using standard options.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        public static T DeserializePayload<T>(string json)
        {
            if (string.IsNullOrEmpty(json)) return default(T);
            return JsonSerializer.Deserialize<T>(json, DefaultJsonOptions);
        }

        /// <summary>
        /// Deserializes JSON to an object of the specified type using standard options.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="type">The target type.</param>
        /// <returns>The deserialized object.</returns>
        public static object DeserializePayload(string json, Type type)
        {
            if (string.IsNullOrEmpty(json) || type == null) return null;
            return JsonSerializer.Deserialize(json, type, DefaultJsonOptions);
        }

        /// <summary>
        /// Deserializes a message payload using the MessageType from metadata.
        /// </summary>
        /// <param name="message">The message containing the payload and MessageType.</param>
        /// <returns>The deserialized payload object.</returns>
        public static object DeserializeMessagePayload(GenericMessage message)
        {
            if (message == null || message.Payload == null)
                return null;

            // If payload is already an object, return it
            if (!(message.Payload is string))
                return message.Payload;

            // Try to deserialize from MessageType
            var messageType = ResolveMessageType(message.MessageType);
            if (messageType != null)
            {
                return DeserializePayload(message.Payload.ToString(), messageType);
            }

            // Fallback: return as string
            return message.Payload;
        }

        #endregion

        #region Type Resolution

        /// <summary>
        /// Resolves a message type from a fully qualified type name.
        /// </summary>
        /// <param name="messageType">The fully qualified type name.</param>
        /// <returns>The resolved Type, or null if not found.</returns>
        public static Type ResolveMessageType(string messageType)
        {
            if (string.IsNullOrEmpty(messageType))
                return typeof(object);

            return Type.GetType(messageType, throwOnError: false) ?? typeof(object);
        }

        #endregion

        #region Partition Key

        /// <summary>
        /// Gets the partition key for a message (for Kafka or similar systems).
        /// Priority: Explicit PartitionKey > CorrelationId > EntityName + MessageId prefix
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>The partition key.</returns>
        public static string GetPartitionKey(GenericMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            // Priority 1: Explicit partition key
            if (!string.IsNullOrEmpty(message.PartitionKey))
                return message.PartitionKey;

            // Priority 2: Correlation ID (for request/response)
            if (!string.IsNullOrEmpty(message.CorrelationId))
                return message.CorrelationId;

            // Priority 3: EntityName + MessageId prefix
            var prefix = message.MessageId?.Length >= 8 
                ? message.MessageId.Substring(0, 8) 
                : message.MessageId ?? "default";
            
            return $"{message.EntityName}-{prefix}";
        }

        #endregion

        #region Error Handling

        /// <summary>
        /// Updates message metadata with error information.
        /// </summary>
        /// <param name="message">The message that failed.</param>
        /// <param name="exception">The exception that occurred.</param>
        public static void SetErrorMessage(GenericMessage message, Exception exception)
        {
            if (message == null || exception == null) return;

            message.ErrorCode = exception.GetType().Name;
            message.ErrorMessage = exception.Message;
            message.RetryCount = (message.RetryCount ?? 0) + 1;
            message.Metadata["LastErrorAt"] = DateTime.UtcNow.ToString("O");
            message.Metadata["StackTrace"] = exception.StackTrace;
        }

        /// <summary>
        /// Clears error information from message metadata.
        /// </summary>
        /// <param name="message">The message to clear errors from.</param>
        public static void ClearErrorMessage(GenericMessage message)
        {
            if (message == null) return;

            message.ErrorCode = null;
            message.ErrorMessage = null;
            message.Metadata?.Remove("LastErrorAt");
            message.Metadata?.Remove("StackTrace");
        }

        #endregion

        #region Version Compatibility

        /// <summary>
        /// Checks if two message versions are compatible (same major version).
        /// </summary>
        /// <param name="messageVersion">The message version.</param>
        /// <param name="consumerVersion">The consumer version.</param>
        /// <returns>True if compatible, false otherwise.</returns>
        public static bool IsVersionCompatible(string messageVersion, string consumerVersion)
        {
            if (string.IsNullOrEmpty(messageVersion) || string.IsNullOrEmpty(consumerVersion))
                return false;

            var msgMajor = GetMajorVersion(messageVersion);
            var consMajor = GetMajorVersion(consumerVersion);
            return msgMajor == consMajor;
        }

        /// <summary>
        /// Gets the major version number from a semantic version string.
        /// </summary>
        /// <param name="version">The semantic version (e.g., "1.0.0").</param>
        /// <returns>The major version number, or 1 if parsing fails.</returns>
        public static int GetMajorVersion(string version)
        {
            if (string.IsNullOrEmpty(version)) return 1;
            var parts = version.Split('.');
            return int.TryParse(parts[0], out var major) ? major : 1;
        }

        #endregion
    }

    /// <summary>
    /// Result of message validation.
    /// </summary>
    public class MessageValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }
}

