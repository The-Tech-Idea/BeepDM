# Redis Helper

## Purpose
Redis-specific helper logic bridging key/value and in-memory structures with BeepDM helper contracts. Supports caching, pub/sub, streams, and data structure operations.

## Key Files
- `RedisHelper.cs`: Redis helper implementing `IDataSourceHelper` for key-value operations, TTL management, pub/sub messaging, and data type translation.

## Features
- Key-value CRUD operations
- TTL/expiry management (EXPIRE, PEXPIRE)
- Pub/sub messaging support
- Stream operations (XADD, XREAD)
- Transactions via Lua scripts (EVAL)
- Capability signaling: `SupportsTTL`, `SupportsPublishSubscribe`, `SupportsIdentity` (INCR)

## Usage Notes
- Keep key naming and namespace patterns consistent
- Reflect limited relational support via capability checks
- Align type conversion with Redis storage representations

## Related Documentation
- [Datasource Types Reference](../../../Help/datasource-types-reference.html)
- [Caching API](../../../Help/caching-api.html)
