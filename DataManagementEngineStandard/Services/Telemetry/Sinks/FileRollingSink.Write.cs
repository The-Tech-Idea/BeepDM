using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks
{
    /// <summary>
    /// Append + flush half of <see cref="FileRollingSink"/>. All disk IO
    /// runs under <c>_writeGate</c> so a single sink instance may be
    /// safely shared by the pipeline drain loop and ad-hoc flush calls.
    /// </summary>
    public sealed partial class FileRollingSink
    {
        /// <inheritdoc />
        public Task WriteBatchAsync(IReadOnlyList<TelemetryEnvelope> batch, CancellationToken cancellationToken)
        {
            if (batch is null || batch.Count == 0)
            {
                return Task.CompletedTask;
            }

            lock (_writeGate)
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    return Task.CompletedTask;
                }

                try
                {
                    EnsureOpenUnderLock();
                }
                catch (Exception ex)
                {
                    MarkUnhealthy(ex);
                    return Task.CompletedTask;
                }

                for (int i = 0; i < batch.Count; i++)
                {
                    TelemetryEnvelope env = batch[i];
                    if (env is null)
                    {
                        continue;
                    }

                    byte[] line;
                    try
                    {
                        line = NdjsonSerializer.SerializeLine(env);
                    }
                    catch (Exception ex)
                    {
                        MarkUnhealthy(ex);
                        continue;
                    }

                    try
                    {
                        _stream.Write(line, 0, line.Length);
                        _currentBytes += line.Length;
                        Interlocked.Increment(ref _writtenCount);
                        RecordSuccess();
                    }
                    catch (Exception ex)
                    {
                        MarkUnhealthy(ex);
                        // After an IO failure, drop the open handle so the next
                        // batch attempts a fresh open instead of writing to a
                        // potentially-corrupt stream.
                        CloseUnderLock(reasonSuffix: "io-error");
                        return Task.CompletedTask;
                    }

                    if (ShouldRollUnderLock())
                    {
                        RollUnderLock(reasonSuffix: _currentBytes >= _maxFileBytes ? "size" : "time");
                    }
                }

                FlushUnderLock();
            }

            return Task.CompletedTask;
        }

        private void EnsureOpenUnderLock()
        {
            if (_stream is not null)
            {
                return;
            }

            string fileName = string.Concat(_prefix, "-", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"), _extension);
            string path = Path.Combine(_directory, fileName);

            FileStream fs = new FileStream(
                path,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 8192,
                useAsync: false);

            _stream = fs;
            _currentPath = path;
            _currentOpenedUtc = DateTime.UtcNow;
            _currentBytes = fs.Length;
            Volatile.Write(ref _healthy, true);
        }

        private void FlushUnderLock()
        {
            if (_stream is null)
            {
                return;
            }
            try
            {
                _stream.Flush(flushToDisk: false);
            }
            catch (Exception ex)
            {
                MarkUnhealthy(ex);
            }
        }

        private void MarkUnhealthy(Exception ex)
        {
            Volatile.Write(ref _healthy, false);
            Volatile.Write(ref _lastError, ex.Message);
            RecordError();
        }
    }
}
