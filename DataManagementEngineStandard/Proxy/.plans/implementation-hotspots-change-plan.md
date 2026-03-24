# ProxyDataSource Implementation Hotspots Change Plan

This document lists exact planned code changes for audited hotspots in `ProxyDataSource`.

## 1) Double execution in retry wrappers (`RunQuery`, `ExecuteSql`, `CreateEntityAs`, CRUD wrappers)

### Current risk
- Wrapper calls execute operation inside retry probe, then calls operation again after success.
- Non-idempotent operations can be applied twice.

### Exact change
- Refactor wrappers to return operation result directly from retry pipeline (no second invocation).
- Introduce generic helpers:
  - `ExecuteWithRetry<T>(Func<IDataSource, T> op, ...)`
  - `ExecuteWithRetryAsync<T>(Func<IDataSource, Task<T>> op, ...)`
- Add write-path tests proving single invocation per successful request.

## 2) Async blocking and deadlock risk (`.Result` / `.Wait()`)

### Current risk
- Many methods block on async retry calls, increasing deadlock and thread starvation risk.

### Exact change
- Convert internal retry helpers to fully async flow with sync-safe wrappers only where required by interface.
- Avoid nested `Task.Run` patterns where direct calls are sufficient.

## 3) Runtime policy drift (`MaxRetries` properties vs `_options`)

### Current risk
- Constructor updates public retry properties but `_options` remains independent in `ExecuteWithPolicy`.

### Exact change
- Establish single policy source of truth.
- Sync constructor parameters into policy model once, and remove duplicate mutable knobs or keep strict synchronization.

## 4) Health state concurrency (`_healthStatus` dictionary)

### Current risk
- Health map is plain dictionary, read/write by timer and request threads concurrently.

### Exact change
- Replace with `ConcurrentDictionary<string,bool>` or guarded access with lock.
- Add deterministic health snapshot for routing selection.

## 5) Retry/error taxonomy and idempotency

### Current risk
- `ShouldRetry` only checks a narrow exception set and does not enforce operation safety.

### Exact change
- Add explicit error classification and operation category (read/write/transactional).
- Enforce idempotency-aware retry behavior for write operations.

## 6) Routing stability and weighted selection

### Current risk
- `GetNextBalancedDataSource` uses per-call `Random` and can produce unstable distribution.

### Exact change
- Use stable random source or deterministic weighted round-robin.
- Add routing hysteresis to reduce oscillation under flapping health.

## 7) Metrics thread-safety and observability contract

### Current risk
- `AverageResponseTime` updates are non-atomic and may race.
- Logs are verbose but not consistently structured for alerting.

### Exact change
- Introduce thread-safe latency aggregation strategy.
- Emit structured telemetry envelope: datasource, operation, attempt, outcome, duration, failover reason.

## 8) Transaction wrapper semantics (`BeginTransaction`, `Commit`, `EndTransaction`)

### Current risk
- Wrappers return `Current.ErrorObject` and can lose operation-local result context.

### Exact change
- Return the exact `IErrorsInfo` from attempted operation.
- Include attempted datasource and failover context in error metadata.

## 9) Connection pool lifecycle and cleanup under load

### Current risk
- Pool cleanup and count-based checks are approximate under concurrency.

### Exact change
- Add explicit pool policy contract (max active, max idle, idle timeout enforcement).
- Harden pool metrics and overload behavior.

## 10) Dispose and background activity safety

### Current risk
- Timer is stopped but not fully disposed; background loops can race with disposal.

### Exact change
- Dispose timer deterministically and guard background operations with cancellation token.
- Ensure no post-dispose failover/health operations continue.
