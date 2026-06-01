# Streaming Helper

## Purpose
Helper functionality for streaming-oriented data sources (Kafka, Kinesis) where append/read patterns differ from transactional stores. Supports messaging, ordering, and dead letter queue patterns.

## Key Files
- `StreamingHelper.cs`: Streaming provider helper implementing `IDataSourceHelper` for append-only operations, message ordering, and capability signaling.

## Features
- Append-only data operations
- Pub/sub messaging pattern
- Message ordering guarantees
- Dead letter queue routing
- Offset handling for replay scenarios
- Capability signaling: `SupportsStreaming`, `SupportsPublishSubscribe`, `SupportsMessageOrdering`, `SupportsDeadLetterQueue`, `SupportsAppend`

## Usage Notes
- Model operations around stream append/read semantics
- Expose unsupported random-update semantics through capability checks
- Keep ordering and offset handling predictable for replay scenarios

## Related Documentation
- [Datasource Types Reference](../../../Help/datasource-types-reference.html)
