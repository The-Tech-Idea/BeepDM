using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Distributed.Transactions
{
    /// <summary>
    /// V1 implementation of <see cref="IDistributedTransactionLog"/>:
    /// an in-process concurrent map of correlation id → list of log
    /// entries. Intended for diagnostics and unit tests — Phase 13
    /// introduces a durable file-based log for crash recovery.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The log is deliberately simple: append is lock-free against
    /// other scopes (different keys), and synchronised within a
    /// single correlation id to keep <see cref="Read"/> stable.
    /// No eviction policy is enforced — callers are expected to
    /// <see cref="Close"/> terminated scopes.
    /// </para>
    /// </remarks>
    public sealed class InMemoryTransactionLog : IDistributedTransactionLog
    {
        private readonly ConcurrentDictionary<string, List<TransactionLogEntry>> _byScope
            = new ConcurrentDictionary<string, List<TransactionLogEntry>>(StringComparer.Ordinal);

        private readonly ConcurrentDictionary<string, List<TransactionLogEntry>> _closed
            = new ConcurrentDictionary<string, List<TransactionLogEntry>>(StringComparer.Ordinal);

        /// <inheritdoc/>
        public void Append(TransactionLogEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            var list = _byScope.GetOrAdd(
                entry.CorrelationId,
                _ => new List<TransactionLogEntry>());

            lock (list) list.Add(entry);
        }

        /// <inheritdoc/>
        public IReadOnlyList<TransactionLogEntry> Read(string correlationId)
        {
            if (string.IsNullOrWhiteSpace(correlationId)) return Array.Empty<TransactionLogEntry>();

            if (_byScope.TryGetValue(correlationId, out var live))
            {
                lock (live) return live.ToArray();
            }
            if (_closed.TryGetValue(correlationId, out var closed))
            {
                lock (closed) return closed.ToArray();
            }
            return Array.Empty<TransactionLogEntry>();
        }

        /// <inheritdoc/>
        public void Close(string correlationId)
        {
            if (string.IsNullOrWhiteSpace(correlationId)) return;

            if (_byScope.TryRemove(correlationId, out var entries))
            {
                _closed[correlationId] = entries;
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<string> OpenCorrelationIds()
            => _byScope.Keys.ToArray();
    }
}
