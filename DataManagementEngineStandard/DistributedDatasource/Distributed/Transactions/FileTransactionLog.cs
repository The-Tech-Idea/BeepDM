using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace TheTechIdea.Beep.Distributed.Transactions
{
    /// <summary>
    /// Durable file-backed <see cref="IDistributedTransactionLog"/>
    /// introduced in Phase 13. Each entry is appended as a single
    /// JSON-line record under
    /// <c>{directory}/{correlationId}.tx.jsonl</c> with a terse
    /// <c>ClosedCorrelationIds</c> manifest used to reconstruct
    /// in-doubt scopes after a crash.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Design points:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///   Append semantics are preserved with
    ///   <see cref="FileShare.Read"/>; concurrent writers to the
    ///   same scope synchronise on the in-memory list lock.
    ///   </description></item>
    ///   <item><description>
    ///   <see cref="OpenCorrelationIds"/> is derived by scanning
    ///   the directory for <c>*.tx.jsonl</c> files not present in
    ///   the closed manifest so a process restart can re-surface
    ///   in-doubt transactions.
    ///   </description></item>
    ///   <item><description>
    ///   <see cref="Close"/> renames the scope file into the
    ///   <c>closed/</c> sub-folder so the open set stays small;
    ///   callers that need long-term retention can back up the
    ///   folder.
    ///   </description></item>
    /// </list>
    /// </remarks>
    public sealed class FileTransactionLog : IDistributedTransactionLog, IDisposable
    {
        private readonly string _directory;
        private readonly string _closedDirectory;

        private readonly ConcurrentDictionary<string, object> _scopeLocks
            = new ConcurrentDictionary<string, object>(StringComparer.Ordinal);

        private readonly Action<Exception> _onWriterError;
        private volatile bool _disposed;

        /// <summary>Creates a new durable log.</summary>
        /// <param name="directory">Target folder (created if missing).</param>
        /// <param name="onWriterError">
        /// Optional I/O error callback. Defaults to silent so the
        /// hot path is never interrupted by disk flakes.
        /// </param>
        public FileTransactionLog(
            string            directory,
            Action<Exception> onWriterError = null)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
            _closedDirectory = Path.Combine(_directory, "closed");
            Directory.CreateDirectory(_directory);
            Directory.CreateDirectory(_closedDirectory);

            _onWriterError = onWriterError;
        }

        /// <inheritdoc/>
        public void Append(TransactionLogEntry entry)
        {
            if (_disposed) return;
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (string.IsNullOrWhiteSpace(entry.CorrelationId)) return;

            var lockObj = _scopeLocks.GetOrAdd(entry.CorrelationId, _ => new object());
            try
            {
                var line = Serialize(entry);
                lock (lockObj)
                {
                    var path = GetOpenPath(entry.CorrelationId);
                    File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                SafeReportError(ex);
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<TransactionLogEntry> Read(string correlationId)
        {
            if (string.IsNullOrWhiteSpace(correlationId)) return Array.Empty<TransactionLogEntry>();

            var open = GetOpenPath(correlationId);
            var closed = GetClosedPath(correlationId);

            string path = File.Exists(open)
                ? open
                : (File.Exists(closed) ? closed : null);

            if (path == null) return Array.Empty<TransactionLogEntry>();

            var lockObj = _scopeLocks.GetOrAdd(correlationId, _ => new object());
            lock (lockObj)
            {
                if (!File.Exists(path)) return Array.Empty<TransactionLogEntry>();
                var results = new List<TransactionLogEntry>();
                foreach (var raw in File.ReadAllLines(path, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    var parsed = SafeDeserialize(raw);
                    if (parsed != null) results.Add(parsed);
                }
                return results;
            }
        }

        /// <inheritdoc/>
        public void Close(string correlationId)
        {
            if (_disposed) return;
            if (string.IsNullOrWhiteSpace(correlationId)) return;

            var lockObj = _scopeLocks.GetOrAdd(correlationId, _ => new object());
            lock (lockObj)
            {
                var open   = GetOpenPath(correlationId);
                if (!File.Exists(open)) return;

                var closed = GetClosedPath(correlationId);
                try
                {
                    if (File.Exists(closed)) File.Delete(closed);
                    File.Move(open, closed);
                }
                catch (Exception ex)
                {
                    SafeReportError(ex);
                }
            }

            _scopeLocks.TryRemove(correlationId, out _);
        }

        /// <inheritdoc/>
        public IReadOnlyList<string> OpenCorrelationIds()
        {
            if (_disposed) return Array.Empty<string>();
            try
            {
                return Directory.EnumerateFiles(_directory, "*.tx.jsonl", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Select(n => n.EndsWith(".tx", StringComparison.OrdinalIgnoreCase)
                        ? n.Substring(0, n.Length - 3)
                        : n)
                    .ToArray();
            }
            catch (Exception ex)
            {
                SafeReportError(ex);
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Convenience helper for crash-recovery code: returns every
        /// entry for every currently-open correlation id.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<TransactionLogEntry>> LoadOpenScopes()
        {
            var result = new Dictionary<string, IReadOnlyList<TransactionLogEntry>>(StringComparer.Ordinal);
            foreach (var id in OpenCorrelationIds())
            {
                result[id] = Read(id);
            }
            return result;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _disposed = true;
            _scopeLocks.Clear();
        }

        // ── Internals ────────────────────────────────────────────────────

        private string GetOpenPath(string correlationId)
            => Path.Combine(_directory,        Sanitize(correlationId) + ".tx.jsonl");

        private string GetClosedPath(string correlationId)
            => Path.Combine(_closedDirectory,  Sanitize(correlationId) + ".tx.jsonl");

        private static string Sanitize(string correlationId)
        {
            if (string.IsNullOrEmpty(correlationId)) return "__empty__";
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(correlationId.Length);
            foreach (var ch in correlationId)
                sb.Append(Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);
            return sb.ToString();
        }

        private static string Serialize(TransactionLogEntry entry)
        {
            var dict = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["ts"]            = entry.TimestampUtc.ToString("O"),
                ["correlationId"] = entry.CorrelationId,
                ["kind"]          = entry.Kind.ToString(),
                ["shardId"]       = entry.ShardId ?? string.Empty,
                ["message"]       = entry.Message ?? string.Empty,
            };
            if (entry.Error != null)
            {
                dict["error"] = Proxy.ProxyLogRedactor.RedactException(entry.Error);
            }
            return JsonSerializer.Serialize(dict);
        }

        private static TransactionLogEntry SafeDeserialize(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                string correlationId = root.TryGetProperty("correlationId", out var ceid)
                    ? ceid.GetString() : string.Empty;
                if (string.IsNullOrWhiteSpace(correlationId)) return null;

                TransactionLogKind kind = TransactionLogKind.Begin;
                if (root.TryGetProperty("kind", out var kindEl) &&
                    Enum.TryParse(kindEl.GetString(), true, out TransactionLogKind parsedKind))
                {
                    kind = parsedKind;
                }

                string shardId = root.TryGetProperty("shardId", out var s) ? s.GetString() : string.Empty;
                string message = root.TryGetProperty("message", out var m) ? m.GetString() : string.Empty;
                string errorMsg = root.TryGetProperty("error", out var e) ? e.GetString() : null;

                DateTime ts = DateTime.UtcNow;
                if (root.TryGetProperty("ts", out var tsEl) &&
                    DateTime.TryParse(tsEl.GetString(),
                                      System.Globalization.CultureInfo.InvariantCulture,
                                      System.Globalization.DateTimeStyles.AssumeUniversal
                                        | System.Globalization.DateTimeStyles.AdjustToUniversal,
                                      out var parsedTs))
                {
                    ts = parsedTs;
                }

                Exception err = errorMsg == null
                    ? null
                    : new InvalidOperationException(errorMsg);

                return new TransactionLogEntry(correlationId, kind, shardId, message, err, ts);
            }
            catch
            {
                return null;
            }
        }

        private void SafeReportError(Exception ex)
        {
            try { _onWriterError?.Invoke(ex); } catch { /* guard */ }
        }
    }
}
