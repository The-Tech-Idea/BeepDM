# Caching Guide

## Overview

BeepDM provides a comprehensive, high-performance caching solution with support for multiple cache providers, advanced features, and production-ready optimizations.

## Cache Providers

| Provider | Best For | Features |
|----------|----------|----------|
| **SimpleCacheProvider** | General-purpose, small to medium apps | Fast in-memory, automatic cleanup, statistics |
| **MemoryCacheProvider** | Memory-constrained environments | Size-based eviction, LRU, memory monitoring |
| **RedisCacheProvider** | Distributed applications | Persistence, clustering, advanced data structures |
| **HybridCacheProvider** | High-performance multi-tier apps | L1 (local) + L2 (distributed) strategy |

## Quick Start

```csharp
// Initialize with default settings
CacheManager.Initialize();

// Store data with expiration
await CacheManager.SetAsync("user:123", userData, TimeSpan.FromMinutes(30));

// Retrieve data
var user = await CacheManager.GetAsync<UserData>("user:123");

// GetOrCreate pattern
var expensiveData = await CacheManager.GetOrCreateAsync("expensive:key", async () =>
{
    return await ComputeExpensiveOperation();
}, TimeSpan.FromHours(1));
```

## Advanced Configuration

```csharp
var config = new CacheConfiguration
{
    DefaultExpiry = TimeSpan.FromMinutes(30),
    MaxItems = 10000,
    EnableStatistics = true,
    KeyPrefix = "myapp:",
    EnableCompression = true,
    CompressionThreshold = 1024
};

// Use Redis as primary, InMemory as fallback
CacheManager.Initialize(config, CacheProviderType.Redis, CacheProviderType.InMemory);
```

## Batch Operations

```csharp
// Set multiple items at once
var items = new Dictionary<string, object>
{
    ["key1"] = value1,
    ["key2"] = value2
};
await CacheManager.SetManyAsync(items, TimeSpan.FromMinutes(15));

// Get multiple items
var results = await CacheManager.GetManyAsync<object>(items.Keys);
```

## Tagged Caching

```csharp
// Tag items for grouped operations
await CacheManager.SetWithTagsAsync("item:1", data, new[] { "user", "recent" });

// Remove all items with a tag
await CacheManager.RemoveByTagAsync("user");
```

## Distributed Locking

```csharp
if (await CacheManager.TryAcquireLockAsync("resource", "unique-id", TimeSpan.FromMinutes(1)))
{
    try
    {
        // Critical section
    }
    finally
    {
        await CacheManager.ReleaseLockAsync("resource", "unique-id");
    }
}
```

## Cache Warming

```csharp
var warmupData = new Dictionary<string, Func<Task<object>>>
{
    ["popular:item"] = () => LoadPopularItem(),
    ["config:settings"] = () => LoadConfiguration()
};

await CacheManager.WarmCacheAsync(warmupData);
```

## Monitoring

```csharp
// Statistics
var stats = CacheManager.GetStatistics();
Console.WriteLine($"Hit Ratio: {stats.CombinedHitRatio:F2}%");
Console.WriteLine($"Memory Usage: {stats.TotalMemoryUsage / 1024 / 1024} MB");

// Health checks
var health = await CacheManager.CheckHealthAsync();
foreach (var provider in new[] { health.PrimaryProviderHealth, health.FallbackProviderHealth })
{
    Console.WriteLine($"{provider?.ProviderName}: {provider?.IsHealthy}");
}
```

## Cached DataSource

```csharp
// Wrap an existing datasource with caching
var cachedDs = new CachedMemoryDataSource(underlyingDataSource, cacheManager);

// Or use InMemoryCacheDataSource
var inMemoryDs = new InMemoryCacheDataSource(connectionProperties, editor);
```

## File Locations

- `DataManagementEngineStandard/Caching/CacheManager.cs`
- `DataManagementEngineStandard/Caching/Providers/`
- `DataManagementEngineStandard/Caching/DataSources/CachedMemoryDataSource.cs`
- `DataManagementEngineStandard/Caching/DataSources/InMemoryCacheDataSource.cs`

## Related Documentation

- [Core Architecture](CoreArchitecture.md)
- [Data Source Implementation](HowToCreateNewDataSource.md)
