---
name: beepdm-retry
description: Use when writing or refactoring any retry/backoff loop in BeepDM. The shared `IRetryPipeline` primitive (`Common/Retry/`) replaces ad-hoc `for`/`while` retry loops with a uniform `RetryPlan<TResult>` configuration. Used by `BeepSyncManager.SyncDataAsync`, `MigrationManager.ExecuteMigrationPlanAsync` (per-step retry), and `WebAPIErrorHelper.ExecuteWithRetryAsync`. Documents the cases where the pipeline is NOT the right tool, so future maintainers know the boundary.
---

# beepdm-retry

`IRetryPipeline` is the **shared retry/backoff primitive** for BeepDM. Any code that needs "try, classify, maybe retry with backoff, eventually give up" should compose it instead of writing a `for (int attempt = ...; attempt <= retries; attempt++)` loop by hand.

It does NOT own telemetry, audit logging, connection-pool returns, or per-attempt side effects beyond what's in `Classify`/`OnSuccess`/`OnGiveUp`. The pipeline is a **control-flow primitive**, not a state machine.

## When to use this skill

- A function fails with transient errors and should retry with backoff.
- You need different behavior for "succeed / retry / give up" based on the error type.
- The retry budget (max attempts, base delay, backoff factor, jitter) is policy-driven.
- You're refactoring a manual `for (attempt = 1; attempt <= N; attempt++)` loop and the body is a clean "try / catch / decide" shape.

## Do NOT use this skill for

- Retry loops entangled with state (telemetry, audit, connection-pool, failover to next candidate) — see the **Worked example: when the pipeline is the wrong tool** section below.
- Cancellation-token-driven service loops (`while (!ct.IsCancellationRequested)`) — these are long-running workers, not retry loops.
- Stream-consumption loops (`while (!parser.EndOfData)`, `while (!sr.EndOfStream)`) — not retries at all.
- File I/O with three different catch types that map to three different control-flow decisions (retry / retry / silently return) — see `SetupWizard.LoadPersistedState` for the canonical example.

## File locations

`DataManagementEngineStandard/Common/Retry/`:

- `IRetryPipeline.cs` — the contract: `Task<RetryResult<TResult>> ExecuteAsync<TResult>(RetryPlan<TResult> plan, CancellationToken token = default)`
- `RetryPipeline.cs` — default implementation; exposes `static readonly RetryPipeline Instance` (stateless, thread-safe; share it).
- `RetryPlan.cs` — the config object: `MaxAttempts`, `Run`, `Classify`, `Backoff`, optional `BeforeAttempt`, `OnSuccess`, `OnGiveUp`.
- `RetryAttemptContext.cs` — per-attempt context: `Attempt` (1-indexed), `LastResult`, `LastError` (`IErrorsInfo?`), `FailureMessage`.
- `RetryResult.cs` — return shape: `Value` (only set on Succeed), `AttemptsUsed`, `FinalDecision` (`Succeed` / `GiveUp`), `FailureMessage`, `TotalElapsed`.
- `RetryDecision.cs` — `Succeed`, `Retry`, `GiveUp`.

## API surface

```csharp
public interface IRetryPipeline
{
    Task<RetryResult<TResult>> ExecuteAsync<TResult>(
        RetryPlan<TResult> plan,
        CancellationToken token = default);
}

public sealed class RetryPlan<TResult>
{
    public int MaxAttempts { get; init; }                              // default 1
    public Func<RetryAttemptContext<TResult>, CancellationToken, Task<TResult>>? BeforeAttempt { get; init; }
    public Func<RetryAttemptContext<TResult>, CancellationToken, Task<TResult>>  Run           { get; init; } = null!;
    public Func<RetryAttemptContext<TResult>, RetryDecision>                    Classify      { get; init; } = null!;
    public Func<int, TimeSpan>                                                  Backoff       { get; init; } = null!;
    public Func<RetryAttemptContext<TResult>, TResult, CancellationToken, Task>? OnSuccess   { get; init; }
    public Func<RetryAttemptContext<TResult>, TResult, RetryDecision, CancellationToken, Task>? OnGiveUp { get; init; }
}
```

## Pipeline contract (the load-bearing guarantees)

These are the rules every caller can rely on:

1. **`OperationCanceledException` always propagates.** The pipeline does NOT catch it. The cancellable `Task.Delay` in `Backoff` will throw it. Pass `cancellationToken` through to `Run` if your operation is cancellable.
2. **Any OTHER exception in `Run`, `Classify`, `Backoff`, or `BeforeAttempt` is caught and treated as a transient failure.** The loop continues until `MaxAttempts` is exhausted or `Classify` returns `GiveUp`. This prevents a buggy classifier from hard-failing the run.
3. **`OnSuccess` runs only after a successful `Run`.** If `OnSuccess` itself throws (non-cancellation), the throw is logged but does NOT flip the outcome — the work itself succeeded.
4. **`OnGiveUp` runs only when `Classify` returns `GiveUp` (or the loop exhausts `MaxAttempts`).** It does NOT run on successful completions.
5. **`Classify` is called even on success.** If `ctx.LastError == null`, that's your cue to return `RetryDecision.Succeed`.
6. **`Backoff` is called between retries only** — not before the first attempt, not after the last.
7. **All `await`s use `ConfigureAwait(false)`.** Safe to call from sync-over-async contexts (`.GetAwaiter().GetResult()` doesn't deadlock).
8. **`RetryPipeline.Instance` is the canonical instance.** Use it; only `new RetryPipeline()` in tests.

## Worked example 1 — `WebAPIErrorHelper.ExecuteWithRetryAsync`

This is the cleanest "try / classify / retry / give up" shape in the codebase. The `Run` body is small (check breaker, run operation), `Classify` does the analysis, and the breaker state is updated inside the hooks. Original was 38 lines; migrated version is 38 lines but uniformly structured (the line count is similar because the comments are detailed; the structural complexity is much lower).

```csharp
public async Task<T> ExecuteWithRetryAsync<T>(
    Func<Task<T>> operation,
    string operationName,
    int maxRetries = 0,
    int baseDelayMs = 0,
    CancellationToken cancellationToken = default)
{
    if (_disposed) throw new ObjectDisposedException(nameof(WebAPIErrorHelper));

    var retries = maxRetries > 0 ? maxRetries : DefaultMaxRetries;
    var delay   = baseDelayMs  > 0 ? baseDelayMs : DefaultBaseDelayMs;
    var circuitBreaker = GetOrCreateCircuitBreaker(operationName);

    // Closure-captured so we can re-throw the original exception after the pipeline gives up.
    // RetryResult only carries FailureMessage; the pipeline swallows the Exception by design.
    Exception lastException = null;

    var retryResult = await RetryPipeline.Instance.ExecuteAsync(new RetryPlan<T>
    {
        MaxAttempts = retries + 1,                                    // original was attempt <= retries

        Run = async (ctx, ct) =>
        {
            if (!circuitBreaker.CanExecute())
                throw new InvalidOperationException($"Circuit breaker is open for {operationName}");
            return await operation().ConfigureAwait(false);
        },

        Classify = ctx =>
        {
            if (ctx.LastError == null) return RetryDecision.Succeed;
            lastException     = ctx.LastError.Ex;                     // grab the original Exception
            circuitBreaker.OnFailure();                                // update on EVERY failure, not just giveup
            var info = AnalyzeError(lastException);
            _logger?.WriteLog($"Attempt {ctx.Attempt} failed for {operationName}: {info.ErrorMessage}");
            if (ctx.Attempt > retries || !info.IsRetryable) return RetryDecision.GiveUp;
            return RetryDecision.Retry;
        },

        OnSuccess = (ctx, value, ct) => { circuitBreaker.OnSuccess(); return Task.CompletedTask; },
        OnGiveUp  = (ctx, value, decision, ct) =>
        {
            _logger?.WriteLog($"Operation {operationName} failed permanently: {ctx.FailureMessage}");
            return Task.CompletedTask;
        },

        Backoff = attempt => TimeSpan.FromMilliseconds(CalculateDelay(attempt - 1, delay))
    }, cancellationToken).ConfigureAwait(false);

    if (retryResult.FinalDecision == RetryDecision.Succeed)
        return retryResult.Value;

    throw lastException
        ?? new InvalidOperationException($"All retry attempts exhausted for {operationName}");
}
```

Three things to learn from this:

- **`Classify` runs on every attempt, including successful ones.** Guard with `if (ctx.LastError == null) return RetryDecision.Succeed;`.
- **State that should update on every failure (circuit breaker counters, metrics) belongs in `Classify`, not `OnGiveUp`.** `OnGiveUp` is "log the terminal message," not "do all the per-failure bookkeeping."
- **Closure-capture is how you preserve the original `Exception` for re-throw.** The pipeline only exposes `FailureMessage` (string) on giveup; the actual `Exception` lives in `ctx.LastError.Ex` but isn't carried in `RetryResult`.

## Worked example 2 — Two patterns for the same primitive

The pipeline is generic over what "the work" is. Two callers use it for two different units of work:

**A. Per-step retry** (`MigrationManager.ExecuteMigrationPlanAsync`, in `Editor/Migration/MigrationManager.ExecutionOrchestration.cs:195`)

The migration plan has N steps. The outer `foreach` over steps is the engine's job; the pipeline runs once per step. On giveup, the outer code consults `policy.AbortOnStepFailure` to decide whether to abort the whole plan or continue to the next step (recording `result.FailedSteps.Add(step.Sequence)`). Plan-level cancellation is checked between steps via `token.ThrowIfCancellationRequested()`.

**B. Whole-run retry** (`BeepSyncManager.SyncDataAsync`, in `Editor/BeepSync/BeepSyncManager.Sync.cs:140`)

The whole sync is a single async operation (`importMgr.RunImportAsync` + optional reverse-import). The pipeline wraps the *whole run* — `Run` returns the final `IErrorsInfo`, `Classify` maps the result + failure message to a retry decision, and the post-pipeline code either continues with the successful result or records a permanent failure. The pipeline runs *one plan per sync call*, not one per chunk of work.

**Which to use:** if the "work" is naturally a list of independent units (steps, rows, files) and the caller wants per-unit bookkeeping (record this step as failed, advance to the next, etc.), use the per-step pattern. If the work is a single async operation that the caller wants to retry as a whole, use the whole-run pattern. Both are the same primitive — the difference is what's inside the `Run` callback and what the surrounding code does with the result.

## Worked example 3 — when the pipeline is the wrong tool

These are the 6 manual retry loops in the engine that were *intentionally not* migrated. They each have a "NOTE: Manual retry loop. NOT migrated to IRetryPipeline" comment at the site explaining why.

| File | What it's doing | Why the pipeline doesn't fit |
|---|---|---|
| `Proxy/ProxyDataSource.ExecutionHelpers.cs` (3 loops) | Per-attempt `RecordSuccess`/`RecordFailure` (telemetry) + `ReturnConnection` (pool) + `SafeFailover` to next candidate + saturation-aware backoff override (`DelayWithBackoff(attempt * 2)` under saturation). | The body is a state machine that interleaves per-candidate failover (foreach) with per-attempt retry (for). The pipeline models only the latter. |
| `Editor/ETL/Scheduling/SchedulerHost.cs:484` | Captures `PipelineRunResult` + a `success` flag and short-circuits with `break` on partial success. Backoff depends on `def.RetryPolicy.BaseDelayMs × BackoffFactor^(attempt-1)`. | Mid-loop `break` is not a pipeline concept; the result-capture + success-flag plumbing would force the Run lambda to be 25+ lines. |
| `SetUp/SetupWizard.cs:460` | Discriminates three exception types with different control flow: retry on `IOException`, retry on `UnauthorizedAccessException`, return-without-throw on any other (silent start-fresh). | The classifier would have to map "giveup" to "return" — pipelines model "giveup = throw/rethrow-log", not "giveup = silent return." |
| `SetUp/SetupWizard.cs:517` | Same shape as above + wrapped in a `try/finally` that deletes a temp file regardless of retry outcome. | The persist path is best-effort and swallows exceptions in the outer try; a pipeline "giveup = rethrow" semantic would change behavior. |
| `WebAPI/Helpers/WebAPIErrorHelper.cs:152` (already migrated — listed for reference) | The one clean fit. | n/a |

The rule of thumb: **if the retry loop's body is "try X; on success return; on retryable failure wait and try again; on non-retryable failure throw," the pipeline is a win. If the body does anything else — telemetry, audit, connection management, failover to a different candidate, mid-loop control flow — keep the loop, add a comment, and move on.**

## Design rules

- **Default to `RetryPipeline.Instance`.** `Instance` is stateless and thread-safe; share it.
- **For per-instance injection** (e.g. for testability), hold a `protected IRetryPipeline` field with a lazy fallback to `new RetryPipeline()` (this is the pattern `BeepSyncManager` uses at `BeepSyncManager.Core.cs:67`). Tests can set the field directly.
- **Use `IErrorsInfo` as the `TResult` if the operation has no meaningful return value.** `RetryPlan<IErrorsInfo>` is what `BeepSyncManager` and `MigrationManager` use; it's flexible enough for any failure-shape the engine emits.
- **Don't put domain logic in the `Classify` callback.** The classifier is a routing decision, not a side effect. Side effects (telemetry, breaker counters) belong in `Classify` only if they MUST run on every attempt; otherwise in `OnSuccess`/`OnGiveUp`.
- **Always pass `cancellationToken` through to `Run`.** Otherwise the pipeline's cancellable `Task.Delay` in `Backoff` is the only way to cancel, and a slow `Run` will block shutdown.
- **Capture the original `Exception` in a closure** if you need to re-throw it. The pipeline's public surface is `FailureMessage` (string), not the exception itself. See the WebAPI worked example above.
- **`Backoff` is 1-indexed by attempt.** If your existing backoff function is 0-indexed, subtract 1 in the lambda.
- **The pipeline uses `Task.Delay(delay, token)` for sleeps.** The `token` cancels the sleep mid-wait. This is different from `Thread.Sleep` in the old for-loops; if you specifically need the old behavior, override `Backoff` to call `Thread.Sleep` directly (acceptable for sync callers only).
- **If `BeforeAttempt` needs to log the upcoming delay, do NOT re-derive the delay from the policy.** The Backoff lambda and any "delay=Xms" log message must call the *same* function — otherwise they can silently drift (the log says 500ms, the actual sleep is 1000ms). The pattern in `Editor/BeepSync/BeepSyncManager.Sync.cs:348` (`ComputeBackoffMs(rp, baseDelay, attempt)`) is the canonical example: extract the formula into a method, and have the `Backoff` lambda call it. The `Backoff` lambda becomes a one-liner.
- **Set `LoggerTag`** to a short, human-readable caller name (e.g. `"Migration"`, `"BeepSync"`, `"WebAPI"`). The pipeline emits `[<tag>] attempt N/M: <message>` lines to `Debug.WriteLine` in DEBUG builds for transient retries, non-fatal hook throws, and final give-up. Stripped in Release.

## Cross-references

- See **beepdm-migration** for the per-step pipeline use in `MigrationManager.ExecuteMigrationPlanAsync` (the per-step pattern, where the outer foreach over steps owns step-to-step control flow and the pipeline handles only the per-step retry).
- For the whole-run pipeline use in `BeepSyncManager.SyncDataAsync`, see `Editor/BeepSync/BeepSyncManager.Sync.cs:140` (the `RetryPlan<IErrorsInfo>` config in that file is the canonical "wrap a single async operation" example). For the `ComputeBackoffMs` / Backoff-lambda dedup pattern, see `Editor/BeepSync/BeepSyncManager.Sync.cs:348`. There is no `beepdm-beepsync` skill — the BeepSync flow is documented inline in the source.
- See the comments at the 6 non-migrated retry-loop sites for the why-not-pipeline reasoning.
