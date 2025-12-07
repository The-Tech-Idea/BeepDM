# Messaging Standards - Quick Reference

## Required Metadata Keys

Every message MUST include these metadata keys:

```csharp
message.MessageType = typeof(MyPayload).AssemblyQualifiedName;  // Required
message.MessageVersion = "1.0.0";                               // Required (default: "1.0.0")
message.Source = "MyService";                                    // Required
message.ContentType = "application/json";                       // Required (default: "application/json")
```

## Optional Metadata Keys

Common optional metadata:

```csharp
message.CorrelationId = correlationId;      // For request/response correlation
message.CausationId = parentMessageId;       // For event sourcing
message.PartitionKey = partitionKey;         // For Kafka partitioning
message.RoutingKey = routingKey;             // For RabbitMQ routing
message.TenantId = tenantId;                 // Multi-tenant isolation
message.UserId = userId;                     // User context
message.TraceId = traceId;                   // Distributed tracing
```

## Message Creation Pattern

```csharp
var message = new GenericMessage
{
    MessageId = Guid.NewGuid().ToString(),
    EntityName = "orders.created.v1",
    Timestamp = DateTime.UtcNow,
    Payload = payloadObject,
    Priority = 100  // 0-255, default: 100
};

// Set required metadata
message.MessageType = typeof(OrderCreatedEvent).AssemblyQualifiedName;
message.MessageVersion = "1.0.0";
message.Source = "OrderService";
message.ContentType = "application/json";

// Validate
if (!message.IsValid())
{
    var errors = message.GetValidationErrors();
    // Handle errors
}
```

## Priority Levels

| Priority | Range | Use Case |
|----------|-------|----------|
| Low | 0-63 | Background processing |
| Normal | 64-127 | Standard messages (default: 100) |
| High | 128-191 | Important messages |
| Critical | 192-255 | Urgent messages |

## Message Categories

Standard categories for `StreamConfig.MessageCategory`:

- `Command` - Request to perform an action
- `Event` - Notification that something happened
- `Request` - Request for data/operation
- `Response` - Response to a request
- `Query` - Request for data (read-only)

## Naming Conventions

### Queue/Topic Names
Format: `{environment}.{domain}.{entity}.{category}`

Examples:
- `prod.orders.commands`
- `dev.users.events`
- `test.payments.requests`

### Routing Keys (RabbitMQ)
Format: `{entity}.{action}.{version}`

Examples:
- `orders.created.v1`
- `users.updated.v2`
- `payments.processed.v1`

### Dead Letter Queues
Format: `{original-queue-name}.dlq` or `{original-topic-name}-dlq`

Examples:
- `orders.created.v1.dlq`
- `users-updated-dlq`

## Error Handling

### Error Metadata
When a message fails, update metadata:

```csharp
message.ErrorCode = ex.GetType().Name;
message.ErrorMessage = ex.Message;
message.RetryCount = (message.RetryCount ?? 0) + 1;
```

### Retry Policy
Standard retry configuration:

```csharp
var retryPolicy = new RetryPolicy
{
    MaxRetries = 3,
    InitialDelay = TimeSpan.FromSeconds(1),
    MaxDelay = TimeSpan.FromMinutes(5),
    BackoffMultiplier = 2.0,
    ExponentialBackoff = true
};
```

## Message Correlation

### Request/Response Pattern
```csharp
// Request message
var request = new GenericMessage { /* ... */ };
request.CorrelationId = Guid.NewGuid().ToString();

// Response message
var response = new GenericMessage { /* ... */ };
response.CorrelationId = request.CorrelationId;  // Match request
response.CausationId = request.MessageId;        // Link to request
```

### Event Sourcing Pattern
```csharp
var eventMessage = new GenericMessage { /* ... */ };
eventMessage.CausationId = previousEvent.MessageId;  // Chain events
eventMessage.CorrelationId = workflowId;              // Track workflow
```

## Serialization

### Standard JSON Options
```csharp
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNameCaseInsensitive = true
};
```

### Serialize/Deserialize
```csharp
// Serialize payload
string json = message.SerializePayload();

// Deserialize payload
message.DeserializePayload<MyType>(json);
```

## Validation Checklist

Before sending a message:

- [ ] `MessageId` is set (GUID)
- [ ] `EntityName` is set (stream/queue name)
- [ ] `Timestamp` is set (UTC)
- [ ] `Payload` is not null
- [ ] `MessageType` is set in metadata
- [ ] `MessageVersion` is set in metadata
- [ ] `Source` is set in metadata
- [ ] `ContentType` is set in metadata
- [ ] Message passes `IsValid()` check

## Common Patterns

### Create Correlated Message
```csharp
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
            ["Source"] = originalMessage.Source ?? "Unknown"
        }
    };
}
```

### Get Partition Key
```csharp
public static string GetPartitionKey(GenericMessage message)
{
    return message.PartitionKey
        ?? message.CorrelationId
        ?? $"{message.EntityName}-{message.MessageId.Substring(0, 8)}";
}
```

## See Also

- [MESSAGE_STANDARDS.md](./MESSAGE_STANDARDS.md) - Complete standards document
- [README.md](./README.md) - Overview and quick start guide

