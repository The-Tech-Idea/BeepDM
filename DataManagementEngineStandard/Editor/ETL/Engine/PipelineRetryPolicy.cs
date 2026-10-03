using System;
using System.Threading.Tasks;
using System.Threading;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Engine
{
    /// <summary>
    /// Exponential-backoff retry policy with jitter.
    /// Used by <see cref="PipelineEngine"/> when calling sink write operations.
    /// </summary>
    public class PipelineRetryPolicy
    {
        private readonly int    _maxRetries;
        private readonly int    _baseDelayMs;
        private readonly double _backoffFactor;

        /// <param name="maxRetries">Number of retry attempts after the first failure (default 3).</param>
        /// <param name="baseDelayMs">Initial delay in milliseconds (default 500).</param>
        /// <param name="backoffFactor">Delay multiplier per retry (default 2.0 → 500 ms, 1 s, 2 s).</param>
        public PipelineRetryPolicy(int maxRetries = 3, int baseDelayMs = 500, double backoffFactor = 2.0)
        {
            _maxRetries    = maxRetries;
            _baseDelayMs   = baseDelayMs;
            _backoffFactor = backoffFactor;
        }

        /// <summary>
        /// Execute <paramref name="operation"/> retrying on any exception up to
        /// <see cref="_maxRetries"/> times with exponential backoff + ±20 % jitter.
        /// The last failure is re-thrown if all retries are exhausted.
        /// </summary>
        public Task ExecuteAsync(Func<Task> operation) => ExecuteAsync(operation, CancellationToken.None);

        public async Task ExecuteAsync(Func<Task> operation, CancellationToken token)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    await operation().ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (attempt < _maxRetries && IsRetryable(ex))
                {
                    int delay = (int)(_baseDelayMs * Math.Pow(_backoffFactor, attempt));
                    // ±20 % jitter
                    delay += Random.Shared.Next(-(delay / 5), delay / 5 + 1);
                    delay  = Math.Max(delay, 0);
                    await Task.Delay(delay, token).ConfigureAwait(false);
                    attempt++;
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="operation"/> returning <typeparamref name="T"/>,
        /// retrying on failure using the same policy as <see cref="ExecuteAsync(Func{Task})"/>.
        /// </summary>
        public Task<T> ExecuteAsync<T>(Func<Task<T>> operation) => ExecuteAsync(operation, CancellationToken.None);

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken token)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    return await operation().ConfigureAwait(false);
                }
                catch (Exception ex) when (attempt < _maxRetries && IsRetryable(ex))
                {
                    int delay = (int)(_baseDelayMs * Math.Pow(_backoffFactor, attempt));
                    delay += Random.Shared.Next(-(delay / 5), delay / 5 + 1);
                    delay  = Math.Max(delay, 0);
                    await Task.Delay(delay, token).ConfigureAwait(false);
                    attempt++;
                }
            }
        }
        private static bool IsRetryable(Exception exception)
            => exception is not OperationCanceledException &&
               (exception is not PipelineWriteException write || write.CanRetry);
    }
}
