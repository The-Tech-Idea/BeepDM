using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace TheTechIdea.Beep.Proxy
{
    // ─────────────────────────────────────────────────────────────────────────
    //  IProxyAuditSink  — swappable audit write destination
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Receives a completed <see cref="ProxyAuditEntry"/> after every proxied operation.
    /// Implement this interface to persist entries to a database, append-only log,
    /// or telemetry pipeline.
    /// </summary>
    public interface IProxyAuditSink
    {
        /// <summary>
        /// Called synchronously on the data path after the operation completes.
        /// Implementations <strong>must not throw</strong> and must complete quickly.
        /// Off-load slow I/O to a background queue (see <see cref="FileProxyAuditSink"/>).
        /// </summary>
        void Write(ProxyAuditEntry entry);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  NullProxyAuditSink  — default no-op
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// No-op default. Replace with a real sink via the constructor or
    /// <see cref="IProxyDataSource.AuditSink"/> at runtime to enable auditing.
    /// </summary>
    public sealed class NullProxyAuditSink : IProxyAuditSink
    {
        /// <summary>Shared singleton — allocates nothing.</summary>
        public static readonly NullProxyAuditSink Instance = new();
        private NullProxyAuditSink() { }
        /// <inheritdoc/>
        public void Write(ProxyAuditEntry entry) { }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  FileProxyAuditSink  — rolling daily JSON-lines file
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Appends entries as JSON-lines to a rolling daily file:
    /// <c>{directory}/proxy-audit-YYYYMMDD.jsonl</c>.
    /// Thread-safe; uses a bounded background queue so the data path is never blocked.
    /// </summary>
    public sealed class FileProxyAuditSink : IProxyAuditSink, IDisposable
    {
        private readonly string _directory;
        private readonly BlockingCollection<string> _queue;
        private readonly Thread _writer;
        private bool _disposed;

        /// <param name="directory">Folder where daily audit files are written. Created if absent.</param>
        /// <param name="queueCapacity">Max pending entries before Write() starts dropping (default 10 000).</param>
        public FileProxyAuditSink(string directory, int queueCapacity = 10_000)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
            Directory.CreateDirectory(directory);
            _queue  = new BlockingCollection<string>(boundedCapacity: queueCapacity);
            _writer = new Thread(DrainLoop)
            {
                IsBackground = true,
                Name         = "ProxyAuditWriter"
            };
            _writer.Start();
        }

        /// <inheritdoc/>
        public void Write(ProxyAuditEntry entry)
        {
            if (_disposed) return;
            try
            {
                var line = JsonSerializer.Serialize(entry);
                _queue.TryAdd(line);   // non-blocking; drops silently if at capacity
            }
            catch { /* never throw on data path */ }
        }

        private void DrainLoop()
        {
            try
            {
                foreach (var line in _queue.GetConsumingEnumerable())
                {
                    try
                    {
                        var path = Path.Combine(_directory, $"proxy-audit-{DateTime.UtcNow:yyyyMMdd}.jsonl");
                        File.AppendAllText(path, line + Environment.NewLine);
                    }
                    catch { /* ignore individual write errors */ }
                }
            }
            catch (OperationCanceledException) { }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _queue.CompleteAdding();
            _writer.Join(TimeSpan.FromSeconds(5));
            _queue.Dispose();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ProxyAuditEntry  — one immutable record per proxied operation
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Immutable record describing a single proxied operation: which datasource was
    /// selected, whether it succeeded, how long it took, and a trace of all attempts.
    /// </summary>
    public sealed class ProxyAuditEntry
    {
        /// <summary>Short identifier shared across all log and audit entries for one operation invocation.</summary>
        public string CorrelationId    { get; init; }

        /// <summary>Name of the proxied operation (e.g. "GetData", "InsertRecord").</summary>
        public string OperationName    { get; init; }

        /// <summary>The datasource name that delivered the final successful result, or null on failure.</summary>
        public string SelectedSource   { get; init; }

        /// <summary>True when the operation returned a successful result to the caller.</summary>
        public bool   Succeeded        { get; init; }

        /// <summary>Total number of individual datasource attempts including retries.</summary>
        public int    TotalAttempts    { get; init; }

        /// <summary>Wall-clock duration from operation start to completion.</summary>
        public long   ElapsedMs        { get; init; }

        /// <summary>Redacted failure reason, or null on success.</summary>
        public string FailureReason    { get; init; }

        /// <summary>UTC timestamp when the operation started.</summary>
        public DateTime OccurredAtUtc  { get; init; } = DateTime.UtcNow;

        /// <summary>Operation safety classification used to determine retry eligibility.</summary>
        public ProxyOperationSafety Safety { get; init; }

        /// <summary>
        /// Per-attempt breakdown. Included for writes and read failures; may be empty
        /// on fast single-attempt read success to minimise allocation.
        /// </summary>
        public List<ProxyAttemptRecord> Attempts { get; init; }

        /// <summary>
        /// When write fan-out is active, lists the datasource names that acknowledged the write.
        /// Empty for single-primary writes.
        /// </summary>
        public List<string> FanOutSucceeded { get; init; }
    }
}
