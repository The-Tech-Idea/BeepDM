# Phase 12 — Distributed Remote Transport: Cross-Machine ProxyDataSource

**Status:** In Progress  
**Priority:** P1 (enterprise scaling requirement)  
**Root:** `DataManagementEngineStandard/Proxy/Remote/`

---

## Problem Statement

`ProxyCluster` today only routes between `IProxyDataSource` objects **in the same process**.  
In an enterprise deployment (web farm, microservices, Kubernetes) the proxy tier needs to:

1. Run `ProxyDataSource` workers on N dedicated machines (sidecar or standalone)
2. Have a single `ProxyCluster` coordinator (or one per app-server) route requests to those workers
3. Share circuit-breaker state across coordinators via Redis (done in Phase 11.5)
4. Fail over at the transport level when a worker machine is unreachable

**Without this, horizontal scaling stops at process boundaries.**

---

## Architecture

```
                 App Server (Machine A)
                 ┌────────────────────────┐
  HTTP Clients → │  ProxyCluster          │
                 │    ProxyNode["w1"]     │──► HttpProxyTransport ──► Machine B :5100
                 │    ProxyNode["w2"]     │──► HttpProxyTransport ──► Machine C :5100
                 │    ProxyNode["w3"]     │──► HttpProxyTransport ──► Machine D :5100
                 └────────────────────────┘          │
                          │                          ▼
                  Redis (shared circuit state)   Worker Machine B
                                                 ┌───────────────────────────────┐
                                                 │  ProxyRemoteServer (ASP.NET)  │
                                                 │    ProxyDataSource            │
                                                 │      ├─ SQL Server            │
                                                 │      └─ Replica               │
                                                 └───────────────────────────────┘
```

---

## Component Inventory

| File | Side | Role |
|------|------|------|
| `Remote/ProxyRemoteProtocol.cs` | Both | Shared request/response DTOs — the wire contract |
| `Remote/IProxyTransport.cs` | Client | Pluggable transport abstraction |
| `Remote/HttpProxyTransport.cs` | Client | HTTP/JSON transport (no extra NuGet) |
| `Remote/RemoteProxyDataSource.cs` | Client | Implements `IProxyDataSource`, routes via `IProxyTransport` |
| `Remote/ProxyRemoteRequestDispatcher.cs` | Server | Framework-agnostic handler; maps DTOs → `IDataSource` calls |

---

## Wire Protocol (HTTP/JSON)

```
POST <worker-base-url>/proxy/execute
Content-Type: application/json

{
  "correlationId": "a1b2c3",
  "operation": "GetEntity",
  "entityName": "Orders",
  "filtersJson": "[{\"FieldName\":\"CustomerId\",\"FilterValue\":\"42\"}]"
}

→ 200 OK
{
  "success": true,
  "dataJson": "[{\"OrderId\":1,\"CustomerId\":42,...}]",
  "typeHint": "System.Collections.Generic.List`1",
  "elapsedMs": 12,
  "correlationId": "a1b2c3"
}
```

### Supported operations (ProxyRemoteOperations constants)
| Constant | Maps to |
|---|---|
| `ping` | Health liveness check |
| `Openconnection` | `IDataSource.Openconnection()` |
| `Closeconnection` | `IDataSource.Closeconnection()` |
| `GetEntity` | `IDataSource.GetEntity(name, filters)` |
| `InsertRecord` | `IDataSource.InsertRecord(name, record)` |
| `UpdateRecord` | `IDataSource.UpdateRecord(name, record)` |
| `DeleteRecord` | `IDataSource.DeleteRecord(name, record)` |
| `RunQuery` | `IDataSource.RunQuery(sql)` |
| `ExecuteSQL` | `IDataSource.ExecuteSQL(sql, args)` |
| `BeginTransaction` | `IDataSource.BeginTransaction(args)` |
| `EndTransaction` | `IDataSource.EndTransaction(args)` |
| `Commit` | `IDataSource.Commit(args)` |
| `GetEntitiesNames` | `IDataSource.GetEntitiesNames()` |
| `GetEntityStructure` | `IDataSource.GetEntityStructure(name, refresh)` |
| `ApplyPolicy` | `IProxyDataSource.ApplyPolicy(policy)` |
| `GetMetrics` | `IProxyDataSource.GetMetrics()` |
| `GetSloSnapshot` | `IProxyDataSource.GetSloSnapshot(dsName)` |
| `AddDataSource` | `IProxyDataSource.AddDataSource(name, weight)` |
| `RemoveDataSource` | `IProxyDataSource.RemoveDataSource(name)` |
| `SetRole` | `IProxyDataSource.SetRole(name, role)` |

---

## Transport Abstraction

`IProxyTransport` is the only isolation boundary:
- Swap `HttpProxyTransport` for a gRPC transport without touching `RemoteProxyDataSource`
- Add message signing / mTLS inside the transport without changing business logic
- Mock the transport for unit tests with zero infrastructure

---

## Server Hosting (ASP.NET Core Minimal API example)

```csharp
// Program.cs on Worker Machine B
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IDMEEditor>(sp => /* configure editor */);

var app = builder.Build();
var dispatcher = new ProxyRemoteRequestDispatcher(
    app.Services.GetRequiredService<IDMEEditor>());

app.MapPost("/proxy/execute", async (HttpContext ctx) =>
{
    var req = await System.Text.Json.JsonSerializer
        .DeserializeAsync<ProxyRemoteRequest>(ctx.Request.Body);
    var resp = await dispatcher.DispatchAsync(req, ctx.RequestAborted);
    await ctx.Response.WriteAsJsonAsync(resp);
});

app.Run("http://0.0.0.0:5100");
```

---

## Adding a Remote Node to ProxyCluster

```csharp
// Coordinator (Machine A)
var transport = new HttpProxyTransport("http://machine-b:5100", TimeSpan.FromSeconds(10));
var remoteProxy = new RemoteProxyDataSource(transport, "worker-b", editor);

var node = new ProxyNode("worker-b", remoteProxy, weight: 2,
    role: ProxyDataSourceRole.Primary);
cluster.AddNode(node);
```

---

## Security Recommendations

- Use HTTPS with valid certificates in production
- Add bearer-token or API-key header in `HttpProxyTransport` constructor
- Use mTLS for zero-trust deployments (provide `HttpClientHandler` with client cert)
- Use `ProxyLogRedactor` to strip credentials from all logged payloads

---

## Transport Alternatives (future phases)

| Transport | NuGet required | Notes |
|---|---|---|
| HTTP/JSON (this phase) | None (System.Net.Http) | Ships now, lowest friction |
| gRPC | `Grpc.AspNetCore`, `Google.Protobuf` | Best throughput, streaming support |
| NATS / RabbitMQ | `NATS.Net` / `RabbitMQ.Client` | For async / event-driven patterns |
| Named Pipes | None | Single-host cross-process only |

---

## Failure modes and mitigations

| Failure | Detection | Mitigation |
|---|---|---|
| Worker unreachable | Probe returns `false` / transport timeout | ProxyCluster marks node dead, reroutes |
| Slow worker | `HttpProxyTransport` timeout per call | Hedging (Phase 11.8) fires shadow request to another node |
| Worker overloaded | Rate-limit response (429 from worker) | `TryAcquireNodeSlotAsync` returns false, reroutes |
| Coordinator restart | Stateless — reconnects on next request | Redis holds shared circuit state |
| Split-brain circuit state | — | `RedisCircuitStateStore` shared across all coordinators |
