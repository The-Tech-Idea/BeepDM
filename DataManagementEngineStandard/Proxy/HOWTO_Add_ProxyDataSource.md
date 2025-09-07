# How-To: Add and Use ProxyDataSource

This guide shows how to wrap multiple existing `IDataSource` registrations with the resilient `ProxyDataSource` for automatic failover, load balancing, circuit breaking, caching and metrics.

## 1. Prerequisites
- Your concrete data sources (e.g. `RDBSource`) are already created & registered in `IDMEEditor` via its factory/registration mechanism.
- Names used below (e.g. `PrimaryDB`, `Replica1`) exactly match `DatasourceName` of those registrations.

## 2. Basic Construction
```csharp
var sourceNames = new List<string>{ "PrimaryDB", "Replica1", "Replica2" };
var proxy = new ProxyDataSource(dmeEditor, sourceNames);
```

Optionally pass tuning overrides:
```csharp
var proxy = new ProxyDataSource(dmeEditor, sourceNames, maxRetries:5, retryDelay:750, healthCheckInterval:15000);
```

## 3. Replacing Direct Usage
Before:
```csharp
var customers = dmeEditor.GetDataSource("PrimaryDB").GetEntity("Customers", null);
```
After:
```csharp
var customers = proxy.GetEntity("Customers", null); // auto failover if needed
```
All `IDataSource` methods are proxied: CRUD, scripts, scalar, pagination, transactions, etc.

## 4. Using Caching
```csharp
// Cached fetch (default expiration = options.DefaultCacheExpiration)
var orders = proxy.GetEntityWithCache("Orders", null);

// Custom expiration
var ordersShort = proxy.GetEntityWithCache("Orders", null, TimeSpan.FromMinutes(2));

// Invalidate
proxy.InvalidateCache("Orders");
proxy.InvalidateCache(); // all
```

## 5. Observing Failovers
```csharp
proxy.OnFailover += (s,e)=>
    dmeEditor.AddLogMessage($"Failover {e.FromDataSource} -> {e.ToDataSource}");
```

## 6. Inspecting Metrics
```csharp
var metrics = proxy.GetMetrics();
foreach(var kv in metrics)
{
    Console.WriteLine($"{kv.Key}: total={kv.Value.TotalRequests} ok={kv.Value.SuccessfulRequests} fail={kv.Value.FailedRequests} avgMs={kv.Value.AverageResponseTime}");
}
```

## 7. Transactions (Caveat)
Transactions are attempted on the current selected underlying source. If a failure occurs mid‑transaction and failover happens, the new source will NOT share the previous transactional context. Design long-running multi-entity units of work to pin to a single source (or add a higher-level transaction coordinator).

## 8. Adding / Removing Sources at Runtime
```csharp
proxy.AddDataSource("Replica3", weight:2); // increase selection probability
proxy.RemoveDataSource("Replica1");
```

## 9. Load Balancing Weights
Higher weight = more selection probability when healthy. Update `_dataSourceWeights` via `AddDataSource` or adapt code to expose a setter.

## 10. Circuit Breakers
Each source has its own `CircuitBreaker` (threshold + reset timeout). After enough consecutive failures, the circuit opens and traffic is skipped until timeout elapses (half‑open probe).

## 11. Retry Logic
- Transient exceptions: `TimeoutException`, `IOException`
- Backoff: linear in `RetryPolicy`, exponential in `ExecuteWithPolicy`
Extend by editing `ShouldRetry` to include additional exception categories.

## 12. Connection Pooling
The proxy keeps a small queue of reusable underlying `IDataSource` instances per name (`MaxPoolSize=10`, idle expiry = 5 min). Adjust constants in source if needed.

## 13. Disposal
Always dispose when done:
```csharp
proxy.Dispose();
```
This stops the health timer and closes pooled connections.

## 14. Debugging Tips
- Enable verbose logging inside `RecordFailure` / `RecordSuccess` for latency tuning.
- Dump metrics periodically to confirm balancing behaviour.
- If all circuits open, proxy will fallback to trying every source; ensure at least one can recover.

## 15. Extending the Proxy
Ideas:
- Plug in centralized cache (Redis)
- Add jitter to backoff
- Promote winners based on p95 latency decay
- Export metrics to Prometheus adapter

---
With these steps you can integrate high resilience multi-source data access without changing consumer code that already targets `IDataSource`.
