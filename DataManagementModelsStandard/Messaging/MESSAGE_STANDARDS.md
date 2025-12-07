# Messaging Standards and Best Practices

## Overview

This document defines standards and best practices for messaging within the BeepDM framework. All messaging data sources (Kafka, RabbitMQ, MassTransit, etc.) should adhere to these standards for consistency, interoperability, and maintainability.

## Table of Contents

1. [Message Envelope Standard](#message-envelope-standard)
2. [Metadata Conventions](#metadata-conventions)
3. [Message Versioning](#message-versioning)
4. [Error Handling](#error-handling)
5. [Serialization Standards](#serialization-standards)
6. [Message Routing](#message-routing)
7. [Message Correlation](#message-correlation)
8. [Priority and Ordering](#priority-and-ordering)
9. [Dead Letter Queue (DLQ)](#dead-letter-queue-dlq)
10. [Stream Configuration](#stream-configuration)

---

## Message Envelope Standard

### GenericMessage Structure

The `GenericMessage` class serves as the standard envelope for all messages. It should always include:

```csharp
public class GenericMessage
{
    // Core Identity
    public string MessageId { get; set; }           // Unique message identifier (GUID)
    public string EntityName { get; set; }         // Stream/queue/topic name
    public DateTime Timestamp { get; set; }        // UTC timestamp when message was created
    
    // Payload
    public object Payload { get; set; }            // The actual message data
    
    // Metadata (Standard Headers)
    public Dictionary<string, string> Metadata { get; set; }
    
    // Transport-Specific
    public ulong? DeliveryTag { get; set; }        // For acknowledgment (RabbitMQ)
    public int? Priority { get; set; }             // Message priority (0-255)
}
```

### Required Metadata Keys

All messages MUST include these standard metadata keys:

| Key | Type | Required | Description |
|-----|------|----------|-------------|
| `MessageType` | string | Yes | Fully qualified type name of payload |
| `MessageVersion` | string | Yes | Semantic version (e.g., "1.0.0") |
| `CorrelationId` | string | No | For request/response correlation |
| `CausationId` | string | No | ID of message that caused this one |
| `Source` | string | Yes | Application/service that created message |
| `ContentType` | string | Yes | MIME type (e.g., "application/json") |
| `Encoding` | string | No | Character encoding (default: "utf-8") |
| `RetryCount` | string | No | Number of retry attempts (for DLQ) |
| `ErrorCode` | string | No | Error code if message failed |
| `ErrorMessage` | string | No | Error message if message failed |

### Recommended Metadata Keys

| Key | Type | Description |
|-----|------|-------------|
| `TenantId` | string | Multi-tenant isolation |
| `UserId` | string | User who triggered the message |
| `SessionId` | string | Session identifier |
| `TraceId` | string | Distributed tracing ID |
| `SpanId` | string | Span identifier for tracing |
| `ExpiresAt` | string | ISO 8601 expiration timestamp |
| `ScheduledAt` | string | ISO 8601 scheduled delivery time |
| `PartitionKey` | string | For partitioning (Kafka) |
| `RoutingKey` | string | For routing (RabbitMQ) |

---

## Metadata Conventions

### Standard Metadata Helper Methods

Add these helper methods to `GenericMessage`:

```csharp
public class GenericMessage
{
    // Standard metadata accessors
    public string MessageType
    {
        get => Metadata?.GetValueOrDefault("MessageType");
        set => EnsureMetadata()["MessageType"] = value;
    }
    
    public string MessageVersion
    {
        get => Metadata?.GetValueOrDefault("MessageVersion") ?? "1.0.0";
        set => EnsureMetadata()["MessageVersion"] = value;
    }
    
    public string CorrelationId
    {
        get => Metadata?.GetValueOrDefault("CorrelationId");
        set => EnsureMetadata()["CorrelationId"] = value;
    }
    
    public string Source
    {
        get => Metadata?.GetValueOrDefault("Source");
        set => EnsureMetadata()["Source"] = value;
    }
    
    public string ContentType
    {
        get => Metadata?.GetValueOrDefault("ContentType") ?? "application/json";
        set => EnsureMetadata()["ContentType"] = value;
    }
    
    private Dictionary<string, string> EnsureMetadata()
    {
        if (Metadata == null) Metadata = new Dictionary<string, string>();
        return Metadata;
    }
    
    // Validation
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(MessageId) &&
               !string.IsNullOrEmpty(EntityName) &&
               !string.IsNullOrEmpty(MessageType) &&
               Payload != null;
    }
}
```

---

## Message Versioning

### Semantic Versioning

Messages should use semantic versioning (MAJOR.MINOR.PATCH):

- **MAJOR**: Breaking changes to message structure
- **MINOR**: New optional fields added
- **PATCH**: Bug fixes, no structural changes

### Version Compatibility

```csharp
public static class MessageVersionHelper
{
    public static bool IsCompatible(string messageVersion, string consumerVersion)
    {
        // Same major version = compatible
        var msgMajor = GetMajorVersion(messageVersion);
        var consMajor = GetMajorVersion(consumerVersion);
        return msgMajor == consMajor;
    }
    
    private static int GetMajorVersion(string version)
    {
        if (string.IsNullOrEmpty(version)) return 1;
        var parts = version.Split('.');
        return int.TryParse(parts[0], out var major) ? major : 1;
    }
}
```

### Versioning Strategy

1. **Backward Compatibility**: New versions should be backward compatible when possible
2. **Version Header**: Always include `MessageVersion` in metadata
3. **Type Evolution**: Use optional fields for new properties
4. **Deprecation**: Mark deprecated fields in metadata

---

## Error Handling

### Error Message Structure

When a message fails processing, create an error message:

```csharp
public class MessageError
{
    public string ErrorCode { get; set; }
    public string ErrorMessage { get; set; }
    public string StackTrace { get; set; }
    public DateTime ErrorTimestamp { get; set; }
    public string OriginalMessageId { get; set; }
    public int RetryCount { get; set; }
    public Dictionary<string, object> ErrorContext { get; set; }
}
```

### Retry Policy

Standard retry configuration:

```csharp
public class RetryPolicy
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(5);
    public double BackoffMultiplier { get; set; } = 2.0;
    public bool ExponentialBackoff { get; set; } = true;
    
    public TimeSpan GetDelay(int attemptNumber)
    {
        if (!ExponentialBackoff) return InitialDelay;
        
        var delay = TimeSpan.FromMilliseconds(
            InitialDelay.TotalMilliseconds * Math.Pow(BackoffMultiplier, attemptNumber - 1)
        );
        
        return delay > MaxDelay ? MaxDelay : delay;
    }
}
```

### Error Metadata

When a message fails, update metadata:

```csharp
message.Metadata["ErrorCode"] = errorCode;
message.Metadata["ErrorMessage"] = errorMessage;
message.Metadata["RetryCount"] = retryCount.ToString();
message.Metadata["LastErrorAt"] = DateTime.UtcNow.ToString("O");
```

---

## Serialization Standards

### JSON Serialization

All messages should use consistent JSON serialization:

```csharp
public static class MessageSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };
    
    public static string Serialize(object payload)
    {
        return JsonSerializer.Serialize(payload, DefaultOptions);
    }
    
    public static T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }
    
    public static object Deserialize(string json, Type type)
    {
        return JsonSerializer.Deserialize(json, type, DefaultOptions);
    }
}
```

### Content Type Mapping

| Content Type | Serialization Format |
|--------------|---------------------|
| `application/json` | JSON (default) |
| `application/xml` | XML |
| `application/avro` | Apache Avro |
| `application/protobuf` | Protocol Buffers |
| `text/plain` | Plain text |

### Type Resolution

Use `StreamConfig.MessageType` to resolve payload type:

```csharp
public static Type ResolveMessageType(string messageType)
{
    if (string.IsNullOrEmpty(messageType))
        return typeof(object);
    
    return Type.GetType(messageType, throwOnError: false) 
        ?? typeof(object);
}
```

---

## Message Routing

### Routing Keys (RabbitMQ)

Format: `{entity}.{action}.{version}`

Examples:
- `orders.created.v1`
- `users.updated.v2`
- `payments.processed.v1`

### Partition Keys (Kafka)

Use consistent partition key strategy:

```csharp
public static string GetPartitionKey(GenericMessage message)
{
    // Priority order:
    // 1. Explicit partition key in metadata
    // 2. CorrelationId (for request/response)
    // 3. EntityName + first part of MessageId
    return message.Metadata?.GetValueOrDefault("PartitionKey")
        ?? message.CorrelationId
        ?? $"{message.EntityName}-{message.MessageId.Substring(0, 8)}";
}
```

### Topic/Queue Naming

Format: `{environment}.{domain}.{entity}.{category}`

Examples:
- `prod.orders.commands`
- `dev.users.events`
- `test.payments.requests`

---

## Message Correlation

### Correlation Patterns

1. **Request/Response**: Use `CorrelationId` to match request and response
2. **Saga/Workflow**: Use `CorrelationId` to track workflow steps
3. **Event Sourcing**: Use `CausationId` to track event chain

### Correlation Helper

```csharp
public static class MessageCorrelation
{
    public static GenericMessage CreateCorrelatedMessage(
        GenericMessage originalMessage,
        object newPayload,
        string newEntityName)
    {
        return new GenericMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            EntityName = newEntityName,
            Payload = newPayload,
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>
            {
                ["CorrelationId"] = originalMessage.CorrelationId ?? originalMessage.MessageId,
                ["CausationId"] = originalMessage.MessageId,
                ["Source"] = originalMessage.Metadata?.GetValueOrDefault("Source") ?? "Unknown"
            }
        };
    }
}
```

---

## Priority and Ordering

### Priority Levels

Standard priority levels (0-255):

| Priority | Level | Use Case |
|----------|-------|----------|
| 0-63 | Low | Background processing |
| 64-127 | Normal | Standard messages (default: 100) |
| 128-191 | High | Important messages |
| 192-255 | Critical | Urgent messages |

### Ordering Guarantees

- **FIFO**: Use same partition key for ordered delivery
- **Priority Queue**: Use priority field for priority-based ordering
- **Scheduled**: Use `ScheduledAt` metadata for delayed delivery

---

## Dead Letter Queue (DLQ)

### DLQ Naming Convention

Format: `{original-queue-name}.dlq` or `{original-topic-name}-dlq`

Examples:
- `orders.created.v1.dlq`
- `users-updated-dlq`

### DLQ Message Structure

Messages sent to DLQ should include:

```csharp
public class DeadLetterMessage
{
    public GenericMessage OriginalMessage { get; set; }
    public MessageError Error { get; set; }
    public int RetryCount { get; set; }
    public DateTime FirstFailureAt { get; set; }
    public DateTime LastFailureAt { get; set; }
    public string DeadLetterReason { get; set; }
}
```

### DLQ Metadata

```csharp
message.Metadata["DeadLetterReason"] = "MaxRetriesExceeded";
message.Metadata["OriginalQueue"] = originalEntityName;
message.Metadata["FirstFailureAt"] = firstFailure.ToString("O");
message.Metadata["LastFailureAt"] = lastFailure.ToString("O");
```

---

## Stream Configuration

### StreamConfig Standards

```csharp
public class StreamConfig
{
    // Required
    public string EntityName { get; set; }              // Stream/queue/topic name
    public string MessageType { get; set; }            // Fully qualified type name
    
    // Optional but Recommended
    public string MessageCategory { get; set; }          // Command, Event, Request, Response
    public string ConsumerType { get; set; }           // Consumer group/type
    public string ExchangeType { get; set; }           // For RabbitMQ (direct, topic, fanout)
    public string PartitionKey { get; set; }           // For Kafka partitioning
    public string RetentionPolicy { get; set; }         // Retention settings
    
    // Additional Options
    public Dictionary<string, object> AdditionalOptions { get; set; }
}
```

### Message Categories

Standard categories:

- **Command**: Request to perform an action
- **Event**: Notification that something happened
- **Request**: Request for data/operation
- **Response**: Response to a request
- **Query**: Request for data (read-only)

### StreamConfig Validation

```csharp
public static class StreamConfigValidator
{
    public static ValidationResult Validate(StreamConfig config)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrEmpty(config.EntityName))
            errors.Add("EntityName is required");
        
        if (string.IsNullOrEmpty(config.MessageType))
            errors.Add("MessageType is required");
        
        // Validate MessageType can be resolved
        var messageType = Type.GetType(config.MessageType, throwOnError: false);
        if (messageType == null)
            errors.Add($"MessageType '{config.MessageType}' cannot be resolved");
        
        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
```

---

## Implementation Checklist

### For All Messaging Data Sources

- [ ] Implement `IMessageDataSource<GenericMessage, StreamConfig>`
- [ ] Use `GenericMessage` as standard envelope
- [ ] Include all required metadata keys
- [ ] Implement proper error handling with retry policies
- [ ] Support dead letter queues
- [ ] Use consistent serialization (JSON by default)
- [ ] Implement message correlation support
- [ ] Support priority-based routing
- [ ] Validate `StreamConfig` before use
- [ ] Log all message operations with correlation IDs
- [ ] Support message versioning
- [ ] Implement proper resource disposal

### Message Creation

- [ ] Always set `MessageId` (GUID)
- [ ] Always set `Timestamp` (UTC)
- [ ] Always set `MessageType` in metadata
- [ ] Always set `MessageVersion` in metadata
- [ ] Always set `Source` in metadata
- [ ] Always set `ContentType` in metadata
- [ ] Validate message before sending

### Message Consumption

- [ ] Validate message structure
- [ ] Check message version compatibility
- [ ] Handle deserialization errors gracefully
- [ ] Implement retry logic with exponential backoff
- [ ] Send failed messages to DLQ after max retries
- [ ] Log all processing errors with context
- [ ] Acknowledge messages only after successful processing

---

## Examples

### Creating a Standard Message

```csharp
var message = new GenericMessage
{
    MessageId = Guid.NewGuid().ToString(),
    EntityName = "orders.created.v1",
    Timestamp = DateTime.UtcNow,
    Payload = orderData,
    Priority = 100,
    Metadata = new Dictionary<string, string>
    {
        ["MessageType"] = typeof(OrderCreatedEvent).AssemblyQualifiedName,
        ["MessageVersion"] = "1.0.0",
        ["Source"] = "OrderService",
        ["ContentType"] = "application/json",
        ["CorrelationId"] = correlationId,
        ["UserId"] = userId
    }
};
```

### Processing a Message

```csharp
public async Task ProcessMessageAsync(GenericMessage message, CancellationToken ct)
{
    try
    {
        // Validate
        if (!message.IsValid())
            throw new InvalidMessageException("Message validation failed");
        
        // Check version compatibility
        if (!MessageVersionHelper.IsCompatible(message.MessageVersion, "1.0.0"))
            throw new VersionMismatchException("Message version incompatible");
        
        // Deserialize payload
        var payloadType = Type.GetType(message.MessageType);
        var payload = MessageSerializer.Deserialize(
            message.SerializePayload(), 
            payloadType
        );
        
        // Process
        await ProcessPayloadAsync(payload, ct);
        
        // Acknowledge
        await AcknowledgeMessageAsync(message.EntityName, message, ct);
    }
    catch (Exception ex)
    {
        // Update error metadata
        message.Metadata["ErrorCode"] = ex.GetType().Name;
        message.Metadata["ErrorMessage"] = ex.Message;
        
        // Retry or send to DLQ
        await HandleErrorAsync(message, ex, ct);
    }
}
```

---

## References

- **Interface**: `TheTechIdea.Beep.Messaging.IMessageDataSource<TMessage, TConfig>`
- **Message Model**: `TheTechIdea.Beep.Messaging.GenericMessage`
- **Config Model**: `TheTechIdea.Beep.Messaging.StreamConfig`
- **Consumer**: `TheTechIdea.Beep.Messaging.GenericConsumer<TMessage>`
- **Producer**: `TheTechIdea.Beep.Messaging.GenericProducer<TMessage>`

---

## Version History

- **1.0.0** (2024-01-XX): Initial message standards document

