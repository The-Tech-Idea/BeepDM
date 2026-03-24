using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Proxy
{
    public partial class ProxyDataSource
    {
        // ── Shared Random instance — thread-safe in .NET 6+ via Random.Shared ──────────
        // For compatibility with older targets we use a ThreadLocal instance.
        private static readonly ThreadLocal<Random> _threadRandom =
            new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));

        // ── Async-local correlation ID ────────────────────────────────────────────────
        private static readonly AsyncLocal<string> _correlationIdContext = new AsyncLocal<string>();

        internal static string CurrentCorrelationId
        {
            get => _correlationIdContext.Value ?? "(none)";
            set => _correlationIdContext.Value = value;
        }

        // ─────────────────────────────────────────────────────────────────────────────
        //  Core policy execution — read-safe  (Phase 4)
        //  May retry transient errors freely across multiple candidates.
        // ─────────────────────────────────────────────────────────────────────────────

        private (bool Success, T Result) ExecuteReadWithPolicy<T>(
            string operationName,
            Func<IDataSource, T> operation,
            Func<T, bool> successPredicate = null)
        {
            var ctx = new ProxyExecutionContext(safety: ProxyOperationSafety.ReadSafe);
            CurrentCorrelationId = ctx.CorrelationId;

            T lastResult = default;
            Exception lastEx = null;

            foreach (var dsName in SelectCandidates())
            {
                var ds = GetPooledConnection(dsName);
                if (ds == null) continue;

                for (int attempt = 1; attempt <= _policy.Resilience.MaxRetries; attempt++)
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        lastResult = operation(ds);
                        sw.Stop();
                        var success = successPredicate == null || successPredicate(lastResult);
                        if (success)
                        {
                            RecordSuccess(dsName, sw.Elapsed);
                            ReturnConnection(dsName, ds);
                            RecordLatency(dsName, sw.ElapsedMilliseconds);
                            ctx.Attempts.Add(new ProxyAttemptRecord
                            {
                                DataSourceName = dsName, AttemptNumber = attempt,
                                Success = true, Duration = sw.Elapsed
                            });
                            _auditSink.Write(new ProxyAuditEntry
                            {
                                CorrelationId  = ctx.CorrelationId,
                                OperationName  = operationName,
                                SelectedSource = dsName,
                                Succeeded      = true,
                                TotalAttempts  = ctx.Attempts.Count,
                                ElapsedMs      = sw.ElapsedMilliseconds,
                                Safety         = ctx.OperationSafety,
                                Attempts       = ctx.Attempts
                            });
                            return (true, lastResult);
                        }

                        LogSafe($"[{ctx.CorrelationId}] {operationName}: unsuccessful result on {dsName} (attempt {attempt}).");
                        SafeFailover(operationName);
                        break;
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        lastEx = ex;
                        var (category, severity) = ProxyErrorClassifier.Classify(ex);
                        RecordFailure(dsName, severity);
                        ctx.Attempts.Add(new ProxyAttemptRecord
                        {
                            DataSourceName = dsName, AttemptNumber = attempt,
                            Success = false, Duration = sw.Elapsed,
                            ErrorMessage = ex.Message, ErrorCategory = category
                        });

                        if (!ProxyErrorClassifier.IsRetryEligible(category))
                        {
                            LogSafe($"[{ctx.CorrelationId}] {operationName}: non-retryable error on {dsName}.", ex);
                            throw;
                        }

                        // P1 fix: Saturation → add proportional delay before retry
                        // to reduce load on an overwhelmed backend instead of hammering it.
                        if (category == ProxyErrorCategory.Saturation)
                        {
                            LogSafe($"[{ctx.CorrelationId}] {operationName}: saturation on {dsName} — backing off before retry.");
                            if (attempt < _policy.Resilience.MaxRetries)
                                DelayWithBackoff(attempt * 2);   // double the back-off under saturation
                            continue;
                        }

                        LogSafe($"[{ctx.CorrelationId}] {operationName}: transient error on {dsName} attempt {attempt}/{_policy.Resilience.MaxRetries}.", ex);

                        if (attempt < _policy.Resilience.MaxRetries)
                            DelayWithBackoff(attempt);
                    }
                }
            }

            _auditSink.Write(new ProxyAuditEntry
            {
                CorrelationId  = ctx.CorrelationId,
                OperationName  = operationName,
                Succeeded      = false,
                TotalAttempts  = ctx.Attempts.Count,
                ElapsedMs      = (long)(DateTime.UtcNow - ctx.StartedAt).TotalMilliseconds,
                FailureReason  = lastEx == null ? null : ProxyLogRedactor.Redact(lastEx.Message),
                Safety         = ctx.OperationSafety,
                Attempts       = ctx.Attempts
            });

            if (lastEx != null)
                throw new AggregateException($"[{ctx.CorrelationId}] All retry attempts failed for {operationName}", lastEx);

            return (false, lastResult);
        }

        private async Task<(bool Success, T Result)> ExecuteReadWithPolicyAsync<T>(
            string operationName,
            Func<IDataSource, Task<T>> operation,
            Func<T, bool> successPredicate = null,
            CancellationToken cancellationToken = default)
        {
            var ctx = new ProxyExecutionContext(safety: ProxyOperationSafety.ReadSafe);
            CurrentCorrelationId = ctx.CorrelationId;

            T lastResult = default;
            Exception lastEx = null;

            foreach (var dsName in SelectCandidates())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var ds = GetPooledConnection(dsName);
                if (ds == null) continue;

                for (int attempt = 1; attempt <= _policy.Resilience.MaxRetries; attempt++)
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        lastResult = await operation(ds).ConfigureAwait(false);
                        sw.Stop();
                        var success = successPredicate == null || successPredicate(lastResult);
                        if (success)
                        {
                            RecordSuccess(dsName, sw.Elapsed);
                            ReturnConnection(dsName, ds);
                            RecordLatency(dsName, sw.ElapsedMilliseconds);
                            return (true, lastResult);
                        }

                        SafeFailover(operationName);
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        lastEx = ex;
                        var (category, severity) = ProxyErrorClassifier.Classify(ex);
                        RecordFailure(dsName, severity);

                        if (!ProxyErrorClassifier.IsRetryEligible(category)) throw;

                        LogSafe($"[{ctx.CorrelationId}] {operationName}: transient error on {dsName} attempt {attempt}.", ex);

                        if (attempt < _policy.Resilience.MaxRetries)
                            await DelayWithBackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            _auditSink.Write(new ProxyAuditEntry
            {
                CorrelationId  = ctx.CorrelationId,
                OperationName  = operationName,
                Succeeded      = false,
                TotalAttempts  = ctx.Attempts.Count,
                ElapsedMs      = (long)(DateTime.UtcNow - ctx.StartedAt).TotalMilliseconds,
                FailureReason  = lastEx == null ? null : ProxyLogRedactor.Redact(lastEx.Message),
                Safety         = ctx.OperationSafety,
                Attempts       = ctx.Attempts
            });

            if (lastEx != null)
                throw new AggregateException($"[{ctx.CorrelationId}] All retry attempts failed for {operationName}", lastEx);

            return (false, lastResult);
        }

        // ─────────────────────────────────────────────────────────────────────────────
        //  Core policy execution — write  (Phase 4)
        //  NonIdempotent: single execute, no retry.
        //  IdempotentWrite: retry with same idempotency key on transient errors only.
        // ─────────────────────────────────────────────────────────────────────────────

        private (bool Success, T Result) ExecuteWriteWithPolicy<T>(
            string operationName,
            Func<IDataSource, T> operation,
            Func<T, bool> successPredicate = null,
            ProxyOperationSafety safety = ProxyOperationSafety.NonIdempotentWrite,
            string idempotencyKey = null)
        {
            int maxAttempts = safety == ProxyOperationSafety.IdempotentWrite
                ? _policy.Resilience.MaxRetries
                : 1;

            var ctx = new ProxyExecutionContext(safety: safety);
            CurrentCorrelationId = ctx.CorrelationId;

            // ── P2-13: Fan-out branch ───────────────────────────────────────
            if (_policy.WriteMode == ProxyWriteMode.FanOut ||
                _policy.WriteMode == ProxyWriteMode.QuorumWrite)
            {
                var fanTask = ExecuteFanOutWriteAsync<T>(
                    operationName,
                    ds => Task.FromResult(operation(ds)),
                    successPredicate,
                    ctx,
                    CancellationToken.None);
                fanTask.GetAwaiter().GetResult();
                var (fanOk, fanResult, fanSucceeded) = fanTask.Result;
                _auditSink.Write(new ProxyAuditEntry
                {
                    CorrelationId   = ctx.CorrelationId,
                    OperationName   = operationName,
                    Succeeded       = fanOk,
                    TotalAttempts   = ctx.Attempts.Count,
                    ElapsedMs       = (long)(DateTime.UtcNow - ctx.StartedAt).TotalMilliseconds,
                    Safety          = ctx.OperationSafety,
                    Attempts        = ctx.Attempts,
                    FanOutSucceeded = fanSucceeded
                });
                return (fanOk, fanResult);
            }

            var writeDsName = System.Linq.Enumerable.FirstOrDefault(SelectWriteCandidates()) ?? System.Linq.Enumerable.FirstOrDefault(_dataSourceNames);
            if (writeDsName == null)
                throw new InvalidOperationException($"[{ctx.CorrelationId}] No data source available for {operationName}.");
            var ds = GetPooledConnection(writeDsName);
            if (ds == null)
                throw new InvalidOperationException($"[{ctx.CorrelationId}] No active data source for {operationName}.");

            string dsName = writeDsName;
            T lastResult = default;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    lastResult = operation(ds);
                    sw.Stop();
                    var success = successPredicate == null || successPredicate(lastResult);
                    if (success)
                    {
                        RecordSuccess(dsName, sw.Elapsed);
                        RecordLatency(dsName, sw.ElapsedMilliseconds);
                        ctx.Attempts.Add(new ProxyAttemptRecord
                        {
                            DataSourceName = dsName, AttemptNumber = attempt,
                            Success = true, Duration = sw.Elapsed
                        });
                        _auditSink.Write(new ProxyAuditEntry
                        {
                            CorrelationId  = ctx.CorrelationId,
                            OperationName  = operationName,
                            SelectedSource = dsName,
                            Succeeded      = true,
                            TotalAttempts  = ctx.Attempts.Count,
                            ElapsedMs      = sw.ElapsedMilliseconds,
                            Safety         = ctx.OperationSafety,
                            Attempts       = ctx.Attempts
                        });
                        return (true, lastResult);
                    }

                    LogSafe($"[{ctx.CorrelationId}] {operationName}: operation returned unsuccessful result on {dsName}.");
                    _auditSink.Write(new ProxyAuditEntry
                    {
                        CorrelationId  = ctx.CorrelationId,
                        OperationName  = operationName,
                        SelectedSource = dsName,
                        Succeeded      = false,
                        TotalAttempts  = ctx.Attempts.Count,
                        ElapsedMs      = sw.ElapsedMilliseconds,
                        Safety         = ctx.OperationSafety,
                        Attempts       = ctx.Attempts
                    });
                    return (false, lastResult);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    var (category, severity) = ProxyErrorClassifier.Classify(ex);
                    RecordFailure(dsName, severity);
                    ctx.Attempts.Add(new ProxyAttemptRecord
                    {
                        DataSourceName = dsName, AttemptNumber = attempt,
                        Success = false, Duration = sw.Elapsed,
                        ErrorMessage = ex.Message, ErrorCategory = category
                    });

                    LogSafe($"[{ctx.CorrelationId}] {operationName}: {category} error on {dsName} attempt {attempt}.", ex);

                    bool canRetry = safety == ProxyOperationSafety.IdempotentWrite &&
                                    ProxyErrorClassifier.IsRetryEligible(category) &&
                                    attempt < maxAttempts;

                    if (!canRetry) throw;

                    DelayWithBackoff(attempt);
                }
            }

            _auditSink.Write(new ProxyAuditEntry
            {
                CorrelationId  = ctx.CorrelationId,
                OperationName  = operationName,
                SelectedSource = dsName,
                Succeeded      = false,
                TotalAttempts  = ctx.Attempts.Count,
                ElapsedMs      = (long)(DateTime.UtcNow - ctx.StartedAt).TotalMilliseconds,
                Safety         = ctx.OperationSafety,
                Attempts       = ctx.Attempts
            });
            return (false, lastResult);
        }

        // ─────────────────────────────────────────────────────────────────────────────
        //  Back-compat wrappers (delegate to typed policy methods)
        // ─────────────────────────────────────────────────────────────────────────────

        private (bool Success, T Result) ExecuteWithRetry<T>(
            string operationName,
            Func<IDataSource, T> operation,
            Func<T, bool> successPredicate = null,
            bool failoverOnException = true,
            bool failoverOnUnsuccessful = true)
            => ExecuteReadWithPolicy(operationName, operation, successPredicate);

        private async Task<(bool Success, T Result)> ExecuteWithRetryAsync<T>(
            string operationName,
            Func<IDataSource, Task<T>> operation,
            Func<T, bool> successPredicate = null,
            bool failoverOnException = true,
            bool failoverOnUnsuccessful = true)
            => await ExecuteReadWithPolicyAsync(operationName, operation, successPredicate).ConfigureAwait(false);

        // ─────────────────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────────────────

        private void SafeFailover(string operationName)
        {
            try { Failover(); }
            catch (Exception ex)
            { LogSafe($"Failover during '{operationName}' failed.", ex); }
        }

        // ── P1-9: PII-safe logging helpers ────────────────────────────────
        private void LogSafe(string message)
        {
            var text = _policy.EnableLogRedaction
                ? ProxyLogRedactor.Redact(message)
                : message;
            _dmeEditor.AddLogMessage(text);
        }

        private void LogSafe(string message, Exception ex)
        {
            if (_policy.EnableLogRedaction)
            {
                var safeMsg = ProxyLogRedactor.Redact(message);
                var safeEx  = ProxyLogRedactor.RedactException(ex);
                _dmeEditor.AddLogMessage($"{safeMsg} — {safeEx}");
            }
            else
            {
                _dmeEditor.AddLogMessage($"{message} — {ex.Message}");
            }
        }

        /// <summary>Exponential backoff with optional jitter. Fully synchronous (no Task.Wait).</summary>
        private void DelayWithBackoff(int attempt)
        {
            int delayMs = ComputeBackoffMs(attempt);
            Thread.Sleep(delayMs);
        }

        private Task DelayWithBackoffAsync(int attempt, CancellationToken ct = default)
        {
            int delayMs = ComputeBackoffMs(attempt);
            return Task.Delay(delayMs, ct);
        }

        private int ComputeBackoffMs(int attempt)
        {
            if (!_policy.Resilience.UseExponentialBackoff)
                return _policy.Resilience.RetryBaseDelayMs;

            // Exponential: base * 2^(attempt-1), capped at max
            int exponential = _policy.Resilience.RetryBaseDelayMs * (1 << (attempt - 1));
            int capped = Math.Min(exponential, _policy.Resilience.RetryMaxDelayMs);

            if (_policy.Resilience.UseJitter)
            {
                int jitter = _threadRandom.Value.Next(0, capped / 4 + 1);
                capped = capped + jitter;
            }

            return capped;
        }
    }
}

