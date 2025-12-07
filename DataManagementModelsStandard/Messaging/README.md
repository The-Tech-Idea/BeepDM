# Messaging Models Standard

This folder contains the standard models and interfaces for messaging within the BeepDM framework.

## Overview

The messaging models provide a consistent, transport-agnostic interface for message-based communication across different messaging platforms (Kafka, RabbitMQ, MassTransit, Azure Service Bus, etc.).

## Core Components

### Interfaces

- **`IMessageDataSource<TMessage, TConfig>`**: Standard interface for all messaging data sources
  - Provides methods for sending, receiving, acknowledging, and managing messages
  - Generic to support different message and configuration types

### Models

- **`GenericMessage`**: Standard message envelope
  - Contains payload, metadata, and standard properties
  - Includes helper properties for common metadata keys
  - Supports validation and serialization

- **`StreamConfig`**: Configuration for message streams/queues
  - Defines stream name, message type, consumer type, and transport-specific options

### Producers and Consumers

- **`IGenericProducer<TMessage>`** / **`GenericProducer<TMessage>`**: Generic producer interface and implementation
- **`IGenericConsumer<TMessage>`** / **`GenericConsumer<TMessage>`**: Generic consumer interface and implementation

## Standards

For detailed messaging standards, best practices, and conventions, see:

📖 **[MESSAGE_STANDARDS.md](./MESSAGE_STANDARDS.md)**

This document covers:
- Message envelope structure
- Required and recommended metadata keys
- Message versioning strategies
- Error handling patterns
- Serialization standards
- Message routing conventions
- Correlation patterns
- Dead letter queue handling
- And more...

## Quick Start

### Creating a Standard Message

```csharp
var message = new GenericMessage
{
    MessageId = Guid.NewGuid().ToString(),
    EntityName = "orders.created.v1",
    Timestamp = DateTime.UtcNow,
    Payload = orderData,
    Priority = 100
};

// Set standard metadata using helper properties
message.MessageType = typeof(OrderCreatedEvent).AssemblyQualifiedName;
message.MessageVersion = "1.0.0";
message.Source = "OrderService";
message.ContentType = "application/json";
message.CorrelationId = correlationId;

// Validate before sending
if (!message.IsValid())
{
    var errors = message.GetValidationErrors();
    // Handle validation errors
}
```

### Using IMessageDataSource

```csharp
// Initialize
var config = new StreamConfig
{
    EntityName = "orders.created.v1",
    MessageType = typeof(OrderCreatedEvent).AssemblyQualifiedName,
    MessageCategory = "Event"
};

dataSource.Initialize(config);

// Send message
await dataSource.SendMessageAsync("orders.created.v1", message, cancellationToken);

// Subscribe
await dataSource.SubscribeAsync("orders.created.v1", async (msg) =>
{
    // Process message
    await ProcessMessageAsync(msg);
    
    // Acknowledge
    await dataSource.AcknowledgeMessageAsync("orders.created.v1", msg, cancellationToken);
}, cancellationToken);
```

## Implementation Status

| Data Source | IMessageDataSource | Status |
|-------------|-------------------|--------|
| KafkaDataSource | ✅ | Complete |
| RabbitMQDataSource | ✅ | Complete |
| MassTransitDataSource | ⚠️ | Needs Implementation |

## Namespace

All messaging models are in the `TheTechIdea.Beep.Messaging` namespace.

## Related Documentation

- [Messaging Data Sources README](../../../BeepDataSources/Messaging/README.md) - Implementation details for messaging data sources
- [MassTransit Enhancement Plan](../../../BeepDataSources/Messaging/MassTransitDataSource/ENHANCEMENT_PLAN.md) - Plan for MassTransitDataSource improvements

