using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Telemetry
{
    /// <summary>
    /// Long-running drain loop owned by <see cref="TelemetryPipeline"/>.
    /// Reads envelopes off <see cref="BoundedChannelQueue"/> in batches and
    /// fans them out to every registered <see cref="ITelemetrySink"/>.
    /// </summary>
    /// <remarks>
    /// One <see cref="BatchWriter"/> instance per pipeline. The drain loop
    /// runs on a dedicated <see cref="Task"/> so producers never block on
    /// sink IO. Per-sink exceptions are caught here so a single failing sink
    /// can never starve the others or crash the pipeline.
    /// </remarks>
    internal sealed class BatchWriter : IAsyncDisposable
    {
        private readonly BoundedChannelQueue _queue;
        private readonly IReadOnlyList<ITelemetrySink> _sinks;
        private readonly TimeSpan _flushInterval;
        private readonly int _maxBatchSize;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _drainTask;
        private readonly Action<string, Exception> _onSinkError;
        private readonly Action<long> _onFlushLatency;
        private long _sinkErrorCount;

        public BatchWriter(
            BoundedChannelQueue queue,
            IReadOnlyList<ITelemetrySink> sinks,
            TimeSpan flushInterval,
            int maxBatchSize = 256,
            Action<string, Exception> onSinkError = null,
            Action<long> onFlushLatency = null)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _sinks = sinks ?? Array.Empty<ITelemetrySink>();
            _flushInterval = flushInterval;
            _maxBatchSize = Math.Max(1, maxBatchSize);
            _onSinkError = onSinkError;
            _onFlushLatency = onFlushLatency;
            _drainTask = Task.Run(DrainLoopAsync);
        }

        /// <summary>Total per-sink exceptions caught since startup.</summary>
        public long SinkErrorCount => Interlocked.Read(ref _sinkErrorCount);

        /// <summary>
        /// Drains any remaining envelopes and asks every sink to flush.
        /// Bounded by the supplied <paramref name="timeout"/>.
        /// </summary>
        public async Task FlushAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (timeout > TimeSpan.Zero)
            {
                linked.CancelAfter(timeout);
            }

            List<TelemetryEnvelope> batch = new List<TelemetryEnvelope>(_maxBatchSize);
            ChannelReader<TelemetryEnvelope> reader = _queue.Reader;

            while (!linked.IsCancellationRequested && reader.TryRead(out TelemetryEnvelope envelope))
            {
                batch.Add(envelope);
                if (batch.Count >= _maxBatchSize)
                {
                    await DispatchAsync(batch, linked.Token).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await DispatchAsync(batch, linked.Token).ConfigureAwait(false);
            }

            foreach (ITelemetrySink sink in _sinks)
            {
                try
                {
                    await sink.FlushAsync(linked.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _sinkErrorCount);
                    InvokeSinkError(sink?.Name, ex);
                }
            }
        }

        private async Task DrainLoopAsync()
        {
            CancellationToken ct = _cts.Token;
            ChannelReader<TelemetryEnvelope> reader = _queue.Reader;
            List<TelemetryEnvelope> batch = new List<TelemetryEnvelope>(_maxBatchSize);

            try
            {
                while (await reader.WaitToReadAsync(ct).ConfigureAwait(false))
                {
                    batch.Clear();

                    using CancellationTokenSource window = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    window.CancelAfter(_flushInterval);

                    while (batch.Count < _maxBatchSize)
                    {
                        if (reader.TryRead(out TelemetryEnvelope envelope))
                        {
                            batch.Add(envelope);
                            continue;
                        }

                        if (batch.Count == 0)
                        {
                            try
                            {
                                if (!await reader.WaitToReadAsync(window.Token).ConfigureAwait(false))
                                {
                                    break;
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }
                            continue;
                        }

                        // Have a partial batch; ship it now rather than wait further.
                        break;
                    }

                    if (batch.Count > 0)
                    {
                        await DispatchAsync(batch, ct).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown via DisposeAsync.
            }
            catch
            {
                // Last-ditch swallow so a bug in the loop never kills the host.
                Interlocked.Increment(ref _sinkErrorCount);
            }
        }

        private async Task DispatchAsync(IReadOnlyList<TelemetryEnvelope> batch, CancellationToken ct)
        {
            long startTicks = _onFlushLatency is null ? 0L : Stopwatch.GetTimestamp();
            for (int i = 0; i < _sinks.Count; i++)
            {
                ITelemetrySink sink = _sinks[i];
                try
                {
                    await sink.WriteBatchAsync(batch, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _sinkErrorCount);
                    InvokeSinkError(sink?.Name, ex);
                    // Per-sink isolation: keep going so other sinks still receive the batch.
                }
            }
            if (_onFlushLatency is not null)
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - startTicks) * 1000L / Stopwatch.Frequency;
                try
                {
                    _onFlushLatency(elapsedMs);
                }
                catch
                {
                    // observers are best-effort
                }
            }
        }

        private void InvokeSinkError(string sinkName, Exception ex)
        {
            Action<string, Exception> handler = _onSinkError;
            if (handler is null)
            {
                return;
            }
            try
            {
                handler(sinkName, ex);
            }
            catch
            {
                // observers are best-effort
            }
        }

        public async ValueTask DisposeAsync()
        {
            _queue.Complete();
            try
            {
                _cts.Cancel();
            }
            catch
            {
                // ignored
            }

            try
            {
                await _drainTask.ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }

            _cts.Dispose();

            foreach (ITelemetrySink sink in _sinks)
            {
                try
                {
                    await sink.DisposeAsync().ConfigureAwait(false);
                }
                catch
                {
                    Interlocked.Increment(ref _sinkErrorCount);
                }
            }
        }
    }
}
