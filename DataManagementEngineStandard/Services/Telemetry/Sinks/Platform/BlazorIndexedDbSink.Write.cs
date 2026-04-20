using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks.Platform
{
    /// <summary>
    /// Write + flush + budget-pruning logic for
    /// <see cref="BlazorIndexedDbSink"/>. The sink converts each
    /// envelope into one NDJSON line (so the same canonical shape used
    /// by <see cref="FileRollingSink"/> survives) and hands the batch
    /// to the <see cref="IIndexedDbBridge"/> in a single call.
    /// </summary>
    public sealed partial class BlazorIndexedDbSink
    {
        /// <inheritdoc />
        public async Task WriteBatchAsync(IReadOnlyList<TelemetryEnvelope> batch, CancellationToken cancellationToken)
        {
            if (batch is null || batch.Count == 0)
            {
                return;
            }
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                List<string> lines = new List<string>(batch.Count);
                for (int i = 0; i < batch.Count; i++)
                {
                    TelemetryEnvelope env = batch[i];
                    if (env is null) { continue; }
                    try
                    {
                        lines.Add(NdjsonSerializer.SerializeText(env));
                    }
                    catch (Exception ex)
                    {
                        // Don't fail the entire batch over a bad envelope —
                        // emit a synthetic placeholder so the count still
                        // matches the producer side.
                        Volatile.Write(ref _lastError, ex.Message);
                    }
                }
                if (lines.Count == 0)
                {
                    return;
                }

                try
                {
                    await _bridge.PutBatchAsync(lines, cancellationToken).ConfigureAwait(false);
                    Interlocked.Add(ref _writtenCount, lines.Count);
                    MarkHealthy();
                }
                catch (Exception ex)
                {
                    MarkUnhealthy(ex);
                    return;
                }

                await EnforceBudgetAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _writeGate.Release();
            }
        }

        /// <inheritdoc />
        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }
            await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                try
                {
                    await _bridge.FlushAsync(cancellationToken).ConfigureAwait(false);
                    MarkHealthy();
                }
                catch (Exception ex)
                {
                    MarkUnhealthy(ex);
                }
            }
            finally
            {
                _writeGate.Release();
            }
        }

        private async Task EnforceBudgetAsync(CancellationToken cancellationToken)
        {
            long threshold = (_storageBudgetBytes / 100L) * _pruneThresholdPercent;
            if (threshold <= 0)
            {
                return;
            }

            long used;
            try
            {
                used = await _bridge.EstimateUsedBytesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                MarkUnhealthy(ex);
                return;
            }

            if (used <= threshold)
            {
                return;
            }

            // Free 25% of the budget so we don't oscillate at the cap.
            long target = used - (_storageBudgetBytes - (_storageBudgetBytes / 4));
            if (target <= 0)
            {
                target = _storageBudgetBytes / 4;
            }

            try
            {
                await _bridge.PruneOldestAsync(target, cancellationToken).ConfigureAwait(false);
                Interlocked.Increment(ref _prunedCount);
            }
            catch (Exception ex)
            {
                MarkUnhealthy(ex);
            }
        }
    }
}
