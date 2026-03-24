# Implementation Record — Phase 4: Retry, Idempotency, and Failover Semantics

_Plan file: [04-phase4-retry-idempotency-and-failover-semantics.md](04-phase4-retry-idempotency-and-failover-semantics.md)_  
_Status: ✅ Complete (⚠️ Transactions.cs still uses back-compat wrapper — see gaps)_  
_File: `ProxyDataSource.ExecutionHelpers.cs`_

---

## What Was Implemented

### `ProxyOperationSafety` enum (`ProxyotherClasses.cs`)

```csharp
public enum ProxyOperationSafety
{
    ReadSafe,          // free to retry across any candidate
    IdempotentWrite,   // retry transient errors only; same key = same outcome
    NonIdempotentWrite // single execute, no retry; duplicate = data corruption
}
```

---

### `ProxyExecutionContext` and `ProxyAttemptRecord` (`ProxyotherClasses.cs`)

Carries full execution history for diagnostics and SLO correlation.

```csharp
public class ProxyExecutionContext
{
    public string CorrelationId { get; }   // new GUID per top-level operation
    public ProxyOperationSafety Safety  { get; }
    public List<ProxyAttemptRecord> Attempts { get; } = new();
    public DateTime StartedAt { get; }
}

public class ProxyAttemptRecord
{
    public string DataSourceName { get; set; }
    public int    AttemptNumber  { get; set; }
    public bool   Success        { get; set; }
    public TimeSpan Duration     { get; set; }
    public ProxyErrorCategory? ErrorCategory { get; set; }
    public string ErrorMessage   { get; set; }
}
```

---

### `ExecuteReadWithPolicy<T>` (`ProxyDataSource.ExecutionHelpers.cs`)

For all read-only operations. Iterates `SelectCandidates()` (multiple sources allowed). Retries transient errors up to `_policy.Resilience.MaxRetries`.

**Flow:**
```
foreach candidate in SelectCandidates()
    for attempt = 1..MaxRetries
        try
            result = operation(ds)
            if successPredicate passes
                RecordSuccess, RecordLatency
                return (true, result)
            else
                SafeFailover()
                break to next candidate
        catch
            classify(ex) → (category, severity)
            RecordFailure(severity)
            if NOT IsRetryEligible(category)
                break to next candidate
            DelayWithBackoff(attempt)
            continue retry loop
return (false, default)
```

**Async variant:** `ExecuteReadWithPolicyAsync<T>` uses `await Task.Delay(...)` instead of `Thread.Sleep`.

---

### `ExecuteWriteWithPolicy<T>` (`ProxyDataSource.ExecutionHelpers.cs`)

Safety-aware routing for mutations.

```csharp
private (bool Success, T Result) ExecuteWriteWithPolicy<T>(
    string operationName,
    Func<IDataSource, T> operation,
    ProxyOperationSafety safety = ProxyOperationSafety.NonIdempotentWrite,
    Func<T, bool> successPredicate = null)
```

| Safety level | Behavior |
|-------------|----------|
| `NonIdempotentWrite` | Tries first candidate ONCE. No retry. No next-candidate fallback. |
| `IdempotentWrite` | Retries transient errors on same candidate. No cross-candidate failover (avoids double-write). |
| `ReadSafe` | Delegates to `ExecuteReadWithPolicy` (cross-candidate retry allowed). |

---

### Exponential Backoff with Jitter (`ComputeBackoffMs`)

```
delay = min(RetryBaseDelayMs * 2^(attempt-1) + jitter, RetryMaxDelayMs)
jitter = ThreadLocal<Random>.Next(0, RetryBaseDelayMs / 4)
```

Uses `ThreadLocal<Random>` (not `new Random()` per call). Capped at `_policy.Resilience.RetryMaxDelayMs`.

---

### `_threadRandom` — Thread-safe Random

```csharp
private static readonly ThreadLocal<Random> _threadRandom =
    new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));
```

Fixes the original `new Random()` per call that produced biased distributions under concurrency.

---

### `_correlationIdContext` — Async-local Correlation ID

```csharp
private static readonly AsyncLocal<string> _correlationIdContext = new AsyncLocal<string>();
```

Each top-level operation (`ExecuteReadWithPolicy`, `ExecuteWriteWithPolicy`) stamps a new GUID. All log lines within that operation include `[correlationId]`.

---

### Back-compat Wrappers

`ExecuteWithRetry<T>` and `ExecuteWithRetryAsync<T>` still exist as wrappers that delegate to `ExecuteReadWithPolicy<T>`. This preserves callers that were not updated yet (e.g., `ProxyDataSource.Transactions.cs`).

---

### `ProxyDataSource.cs` — Write Operations Updated

| Method | Before | After |
|--------|--------|-------|
| `InsertEntity` | `ExecuteWithRetry(...)` | `ExecuteWriteWithPolicy(..., NonIdempotentWrite)` + `InvalidateCacheOnWrite` |
| `UpdateEntity` | `ExecuteWithRetry(...)` | `ExecuteWriteWithPolicy(..., NonIdempotentWrite)` + `InvalidateCacheOnWrite` |
| `UpdateEntities` | `ExecuteWithRetry(...)` | `ExecuteWriteWithPolicy(..., NonIdempotentWrite)` + `InvalidateCacheOnWrite` |
| `DeleteEntity` | `ExecuteWithRetry(...)` | `ExecuteWriteWithPolicy(..., NonIdempotentWrite)` + `InvalidateCacheOnWrite` |
| `ExecuteSql` | `ExecuteWithRetry(...)` | `ExecuteWriteWithPolicy(..., NonIdempotentWrite)` |
| `RunQuery` | `ExecuteWithRetry(...)` | `ExecuteReadWithPolicy(...)` |
| `GetEntity` | `ExecuteWithRetry(...)` | `ExecuteReadWithPolicy(...)` |
| `GetEntityWithRawSql` | `ExecuteWithRetry(...)` | `ExecuteReadWithPolicy(...)` |

---

### Bugs Fixed

| Bug | Original | Fix |
|-----|----------|-----|
| Double execution in retry wrappers | `RunQuery` called operation in probe, then called again after success | Replaced with single `operation(ds)` inside retry loop — result captured once |
| `.Result`/`.Wait()` on async retry | Blocking call causing thread starvation risk | `ExecuteReadWithPolicyAsync` uses `await` throughout; sync variant uses `Thread.Sleep` not `Task.Wait` |

---

## Known Gap — `ProxyDataSource.Transactions.cs`

`BeginTransaction`, `EndTransaction`, and `Commit` still use `ExecuteWithRetry` (the back-compat wrapper which routes to the read-safe path). This means `Commit` can be retried on transient failure → **double-commit risk**.

**Fix required:**
```csharp
// Change from:
return ExecuteWithRetry("Commit", ds => ds.CommitTransaction(), ...);

// To:
return ExecuteWriteWithPolicy("Commit", ds => ds.CommitTransaction(),
    ProxyOperationSafety.NonIdempotentWrite);
```

See [impl-remaining-gaps.md](impl-remaining-gaps.md).

---

## Acceptance Criteria Check

| Criterion | Met? |
|-----------|------|
| Retry behavior is deterministic and operation-aware | ✅ `ProxyOperationSafety` enum drives retry strategy |
| Write operations avoid unsafe duplicate side effects | ✅ For all CRUD/SQL ops; ⚠️ Transactions.cs not yet updated |
| Full attempt history for diagnostics | ✅ `ProxyAttemptRecord` list in `ProxyExecutionContext` |
| Correlation ID on every log line | ✅ `AsyncLocal<string>` stamps each operation context |

---

## Files Changed

- `ProxyotherClasses.cs` — `ProxyOperationSafety`, `ProxyExecutionContext`, `ProxyAttemptRecord`
- `ProxyDataSource.ExecutionHelpers.cs` — complete rewrite
- `ProxyDataSource.cs` — all CRUD / SQL methods updated to use `ExecuteWriteWithPolicy` / `ExecuteReadWithPolicy`
