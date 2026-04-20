using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace TheTechIdea.Beep.Distributed.Audit
{
    /// <summary>
    /// Durable <see cref="IDistributedAuditSink"/> that appends
    /// audit events as JSON-lines to a rolling daily file:
    /// <c>{directory}/distributed-audit-YYYYMMDD.jsonl</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementation notes:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///   Writes happen from a bounded background queue so the
    ///   hot path never blocks on disk I/O.
    ///   </description></item>
    ///   <item><description>
    ///   <see cref="Proxy.ProxyLogRedactor"/> is applied to the
    ///   <see cref="DistributedAuditEvent.PartitionKey"/>,
    ///   <see cref="DistributedAuditEvent.Message"/>, and
    ///   <see cref="DistributedAuditEvent.Error"/> fields so
    ///   secrets embedded in SQL / exceptions never land on disk.
    ///   </description></item>
    ///   <item><description>
    ///   The sink owns a background writer thread that is torn
    ///   down deterministically by <see cref="Dispose"/>.
    ///   </description></item>
    /// </list>
    /// </remarks>
    public sealed class FileDistributedAuditSink : IDistributedAuditSink, IDisposable
    {
        private readonly string                     _directory;
        private readonly BlockingCollection<string> _queue;
        private readonly Thread                     _writer;
        private readonly Action<Exception>          _onWriterError;
        private readonly bool                       _redactSensitiveFields;
        private readonly string                     _filePrefix;
        private volatile bool                       _disposed;

        /// <summary>Creates a new sink.</summary>
        /// <param name="directory">Target folder (created if absent).</param>
        /// <param name="queueCapacity">Max pending events (default 10 000).</param>
        /// <param name="onWriterError">Optional callback for I/O errors.</param>
        /// <param name="redactSensitiveFields">When <c>true</c> (default) routes the partition key, message, and error through <see cref="Proxy.ProxyLogRedactor"/>.</param>
        /// <param name="filePrefix">Base name prefix (default <c>distributed-audit-</c>).</param>
        public FileDistributedAuditSink(
            string             directory,
            int                queueCapacity          = 10_000,
            Action<Exception>  onWriterError          = null,
            bool               redactSensitiveFields  = true,
            string             filePrefix             = "distributed-audit-")
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
            Directory.CreateDirectory(directory);

            _queue                 = new BlockingCollection<string>(boundedCapacity: Math.Max(16, queueCapacity));
            _onWriterError         = onWriterError;
            _redactSensitiveFields = redactSensitiveFields;
            _filePrefix            = string.IsNullOrWhiteSpace(filePrefix) ? "distributed-audit-" : filePrefix;

            _writer = new Thread(DrainLoop)
            {
                IsBackground = true,
                Name         = "DistributedAuditWriter"
            };
            _writer.Start();
        }

        /// <inheritdoc/>
        public void Write(DistributedAuditEvent auditEvent)
        {
            if (_disposed || auditEvent == null) return;
            try
            {
                var line = Serialize(auditEvent);
                if (!_queue.TryAdd(line))
                {
                    // Queue full — drop silently to protect the hot path.
                }
            }
            catch (Exception ex)
            {
                SafeReportError(ex);
            }
        }

        /// <summary>Flushes and disposes the background writer.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _queue.CompleteAdding(); } catch { /* ignore */ }
            try { _writer.Join(TimeSpan.FromSeconds(5)); } catch { /* ignore */ }
            try { _queue.Dispose(); } catch { /* ignore */ }
        }

        private string Serialize(DistributedAuditEvent e)
        {
            var dict = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["kind"]          = e.Kind.ToString(),
                ["ts"]            = e.TimestampUtc.ToString("O"),
                ["correlationId"] = e.CorrelationId,
                ["entity"]        = e.EntityName,
                ["mode"]          = e.Mode,
                ["operation"]     = e.Operation,
                ["shardIds"]      = e.ShardIds,
                ["principal"]     = e.Principal,
                ["partitionKey"]  = _redactSensitiveFields
                                        ? Proxy.ProxyLogRedactor.Redact(e.PartitionKey)
                                        : e.PartitionKey,
                ["message"]       = _redactSensitiveFields
                                        ? Proxy.ProxyLogRedactor.Redact(e.Message)
                                        : e.Message,
            };

            if (e.Error != null)
            {
                dict["error"] = _redactSensitiveFields
                    ? Proxy.ProxyLogRedactor.RedactException(e.Error)
                    : e.Error.Message;
            }

            if (e.Tags != null && e.Tags.Count > 0)
            {
                dict["tags"] = e.Tags;
            }

            return JsonSerializer.Serialize(dict);
        }

        private void DrainLoop()
        {
            try
            {
                foreach (var line in _queue.GetConsumingEnumerable())
                {
                    try
                    {
                        var path = GetCurrentFilePath();
                        File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        SafeReportError(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                SafeReportError(ex);
            }
        }

        private string GetCurrentFilePath()
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            return Path.Combine(_directory, _filePrefix + today + ".jsonl");
        }

        private void SafeReportError(Exception ex)
        {
            try { _onWriterError?.Invoke(ex); } catch { /* never throw */ }
        }
    }
}
