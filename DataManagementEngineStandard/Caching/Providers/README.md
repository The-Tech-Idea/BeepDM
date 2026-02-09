# Caching Providers

## Purpose
This folder contains cache-provider implementations used by cache-aware data sources and helper services. Each provider exposes a consistent async lifecycle for key management and expiry handling.

## Key Files
- `SimpleCacheProvider.cs`: Minimal provider for straightforward in-memory key/value scenarios.
- `InMemoryCacheProvider.cs`: In-memory provider tuned for app-local cache use.
- `MemoryCacheProvider.cs`: `MemoryCache`-style provider with expiration controls.
- `RedisCacheProvider.cs`: Distributed cache provider for cross-process cache sharing.
- `HybridCacheProvider.cs`: Combines local and distributed behavior for latency plus consistency.

## Runtime Flow
1. Higher-level services select a provider from configuration.
2. Runtime code uses common async operations (`ExistsAsync`, `RemoveAsync`, `ClearAsync`, `RefreshAsync`).
3. Provider-specific cleanup runs through `Dispose` during shutdown.

## Extension Guidelines
- Keep method behavior consistent across providers, especially clear and refresh semantics.
- Do not hide connectivity errors; surface them through normal BeepDM error channels.
- Prefer cancellation-aware operations for distributed providers.

## Testing Focus
- Expiry refresh behavior and TTL drift.
- Pattern-based clear behavior in large keyspaces.
- Disposal correctness under concurrent cache usage.
