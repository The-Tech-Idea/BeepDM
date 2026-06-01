# Proxy DataSource Guide

## Overview

`ProxyDataSource` is an advanced adaptive wrapper around multiple concrete `IDataSource` instances that adds automatic failover, circuit breaker protection, health checking, load balancing, and caching.

## Architecture

```
                +--------------------+
 Client Calls → |   ProxyDataSource  | → Selects Healthy Target (LB + Circuit + Health)
                +----------+---------+
                           |
          +----------------+------------------+
          |                |                  |
      IDataSource A    IDataSource B     IDataSource C
          |                |                  |
      (RDBSource)      (RDBSource)        (Other)
```

## Core Capabilities

| Capability | Mechanism |
|------------|-----------|
| Failover | Iterates candidates; skips unhealthy / open circuit; raises `OnFailover` |
| Circuit Breaking | `CircuitBreaker` per data source (Closed → Open → HalfOpen) |
| Health Check | Timer invoking `PerformHealthCheck` with 5s bounded probe |
| Load Balancing | Weighted random + tie break by lower `AverageResponseTime` + fewer `TotalRequests` |
| Retry Policy | `RetryPolicy(Func<Task<bool>>)` + exponential backoff |
| Caching | Concurrent dictionary of `CacheEntry` keyed by entity + filter signature |
| Metrics | `DataSourceMetrics` (atomic counters + rolling average latency) |
| Connection Pooling | Bounded queue (`MaxPoolSize`) per data source + idle expiry |

## Key Classes

| Class | Role |
|-------|------|
| `ProxyDataSource` | Orchestrator & facade implementing `IDataSource` |
| `CircuitBreaker` | Failure threshold tracking with reset timeout |
| `DataSourceMetrics` | Thread-safe counters & timing stats |
| `PooledConnection` | Wrapper for pooled `IDataSource` instance |
| `ProxyDataSourceOptions` | Tuning & feature flags |
| `CacheEntry` | Cached entity result container |

## Usage

```csharp
// Create proxy with multiple backends
var proxy = new ProxyDataSource(editor, new List<string>{"PrimaryDB","Replica1","Replica2"});

// Query (automatically selects healthy target)
var list = proxy.GetEntity("Customers", new List<AppFilter>{ 
    new AppFilter{ FieldName="Country", Operator="=", FilterValue="'USA'"}
});

// Cached query
var cached = proxy.GetEntityWithCache("Orders", null, TimeSpan.FromMinutes(2));

// Get metrics
var metrics = proxy.GetMetrics();
```

## Failover Event

```csharp
proxy.OnFailover += (s,e)=> 
    logger.LogInformation($"Failover {e.FromDataSource} -> {e.ToDataSource}");
```

## Tuning Options

- `MaxRetries`
- `RetryDelayMilliseconds`
- `HealthCheckIntervalMilliseconds`
- `FailureThreshold`
- `CircuitResetTimeout`
- `EnableCaching`
- `DefaultCacheExpiration`
- `EnableLoadBalancing`

## File Locations

- `DataManagementEngineStandard/Proxy/ProxyDataSource.cs`
- `DataManagementEngineStandard/Proxy/Remote/`

## Related Documentation

- [Core Architecture](CoreArchitecture.md)
- [Data Source Implementation](HowToCreateNewDataSource.md)
- [Distributed DataSource](Docs/DistributedDataSource.md) (if available)
