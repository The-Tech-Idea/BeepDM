# Cache Connection Helpers

The Cache Connection Helpers provide configuration support for various caching systems and in-memory data sources in the Beep Data Management Engine.

## Overview

The `ConnectionHelper_Cache.cs` file contains connection configurations for various caching technologies, including in-memory caches, distributed caches, and specialized caching systems.

## Cache Categories

### In-Memory Caches
- **InMemoryCache**: Built-in Beep cache data source
- **CachedMemory**: Memory-based caching with compression support
- **MemoryCache**: Microsoft Extensions Memory Cache
- **L1L2Cache**: Multi-level caching system
- **HybridCache**: Combination of multiple cache providers

### Distributed Caches
- **Redis**: Popular distributed cache
- **Hazelcast**: Distributed in-memory data grid
- **Apache Ignite**: Distributed database and cache
- **NCache**: .NET distributed cache solution
- **Infinispan**: Red Hat's distributed cache

### Specialized Caches
- **Couchbase**: NoSQL database with caching capabilities  
- **EhCache**: Java-based cache (with .NET bindings)
- **Caffeine**: High-performance caching library

## Usage Examples

### 1. InMemoryCache Configuration

```csharp
// Get InMemoryCache configuration
var inMemoryConfig = ConnectionHelper.CreateInMemoryCacheConfig();

// Create connection properties
var connectionProps = new ConnectionProperties
{
    ConnectionName = "MyInMemoryCache",
    DatabaseType = DataSourceType.InMemoryCache,
    Category = DatasourceCategory.INMEMORY,
    ConnectionString = "CacheName=MyCache;MaxItems=10000;ExpiryMinutes=60;CleanupInterval=5"
};

// Add to DME Editor
dmeEditor.ConfigEditor.DataConnections.Add(connectionProps);
dmeEditor.ConfigEditor.DataDriversClasses.Add(inMemoryConfig);
```

### 2. Redis Cache Configuration

```csharp
// Get Redis configuration
var redisConfig = ConnectionHelper.CreateRedisCacheConfig();

// Create connection properties for Redis
var redisProps = new ConnectionProperties
{
    ConnectionName = "MyRedisCache",
    DatabaseType = DataSourceType.Redis,
    Category = DatasourceCategory.NOSQL,
    ConnectionString = "Host=localhost;Port=6379;Password=mypassword;Database=0;ConnectTimeout=5000",
    Host = "localhost",
    Port = "6379",
    Password = "mypassword",
    Database = "0"
};

dmeEditor.ConfigEditor.DataConnections.Add(redisProps);
dmeEditor.ConfigEditor.DataDriversClasses.Add(redisConfig);
```

### 3. Hybrid Cache Configuration

```csharp
// Get Hybrid Cache configuration
var hybridConfig = ConnectionHelper.CreateHybridCacheConfig();

// Create connection properties
var hybridProps = new ConnectionProperties
{
    ConnectionName = "MyHybridCache",
    DatabaseType = DataSourceType.InMemoryCache,
    Category = DatasourceCategory.INMEMORY,
    ConnectionString = "L1Cache=Memory;L2Cache=Redis;L1MaxItems=1000;L2MaxItems=100000"
};

dmeEditor.ConfigEditor.DataConnections.Add(hybridProps);
dmeEditor.ConfigEditor.DataDriversClasses.Add(hybridConfig);
```

## Connection String Parameters

### InMemoryCache Parameters
- `CacheName`: Name of the cache instance
- `MaxItems`: Maximum number of items to cache
- `ExpiryMinutes`: Default expiry time in minutes
- `CleanupInterval`: Cleanup interval in minutes

### Redis Parameters
- `Host`: Redis server hostname or IP
- `Port`: Redis server port (default: 6379)
- `Password`: Authentication password
- `Database`: Database number (0-15)
- `ConnectTimeout`: Connection timeout in milliseconds

### MemoryCache Parameters
- `SizeLimit`: Maximum cache size
- `CompactionPercentage`: Percentage to compact when limit reached

### Hazelcast Parameters
- `ClusterName`: Name of the Hazelcast cluster
- `Host`: Hazelcast server host
- `Port`: Hazelcast server port

## Integration with Beep Data Management

All cache configurations are automatically included when calling:

```csharp
var allConfigs = ConnectionHelper.GetAllConnectionConfigs();
```

The cache configurations are added via:

```csharp
configs.AddRange(GetCacheConfigs());
```

## Supported Cache Types

| Cache Type | DataSourceType | Category | In-Memory | Distributed |
|-----------|---------------|----------|-----------|-------------|
| InMemoryCache | InMemoryCache | INMEMORY | ? | ? |
| CachedMemory | CachedMemory | INMEMORY | ? | ? |
| MemoryCache | InMemoryCache | INMEMORY | ? | ? |
| Redis | Redis | NOSQL | ? | ? |
| Hazelcast | Hazelcast | INMEMORY | ? | ? |
| Apache Ignite | ApacheIgnite | INMEMORY | ? | ? |
| NCache | InMemoryCache | INMEMORY | ? | ? |

## Notes

- All cache configurations use `ADOType = false` as they don't use traditional ADO.NET patterns
- Most in-memory caches have `CreateLocal = true` allowing local cache creation
- Connection strings use placeholder syntax `{Parameter}` for value substitution
- Icon names follow the pattern `{cachename}.svg` for UI representation