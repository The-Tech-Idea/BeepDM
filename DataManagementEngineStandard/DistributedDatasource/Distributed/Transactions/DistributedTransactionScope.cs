using System;
using System.Collections.Generic;
using System.Threading;

namespace TheTechIdea.Beep.Distributed.Transactions
{
    /// <summary>
    /// Opaque token returned by
    /// <see cref="IDistributedTransactionCoordinator.Begin"/>. Carries
    /// every piece of state the coordinator needs to later commit or
    /// roll back the scope: correlation id, involved shards, chosen
    /// strategy, status, log handle, and saga history.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The scope is intentionally mutable behind an internal API —
    /// callers only ever see the public read-only view. The
    /// coordinator uses the <c>Internal*</c> methods on this class
    /// (via the shared <c>internal</c> namespace) to advance the
    /// lifecycle; outside callers must go through the coordinator.
    /// </para>
    /// <para>
    /// Instances are thread-safe for status and log mutation;
    /// concurrent <c>Commit</c>/<c>Rollback</c> on the same scope is
    /// rejected by the coordinator with an <c>InvalidOperationException</c>.
    /// </para>
    /// </remarks>
    public sealed class DistributedTransactionScope
    {
        private readonly object  _gate = new object();
        private int              _status;
        private readonly List<string> _sagaCompletedSteps = new List<string>();

        /// <summary>Creates a new scope. Normally only called by the coordinator.</summary>
        public DistributedTransactionScope(
            string                  correlationId,
            TransactionStrategy     strategy,
            IReadOnlyList<string>   shardIds,
            string                  label,
            DateTime                openedUtc)
        {
            if (string.IsNullOrWhiteSpace(correlationId))
                throw new ArgumentException("CorrelationId cannot be null or whitespace.", nameof(correlationId));
            if (shardIds == null) throw new ArgumentNullException(nameof(shardIds));

            CorrelationId = correlationId;
            Strategy      = strategy;
            ShardIds      = shardIds;
            Label         = label ?? string.Empty;
            OpenedUtc     = openedUtc;
            _status       = (int)DistributedTransactionStatus.Active;
        }

        /// <summary>Correlation id — used as the scope identity and in log entries.</summary>
        public string CorrelationId { get; }

        /// <summary>Strategy captured at <see cref="IDistributedTransactionCoordinator.Begin"/>.</summary>
        public TransactionStrategy Strategy { get; }

        /// <summary>Shards the scope enlists. Snapshot taken at begin time.</summary>
        public IReadOnlyList<string> ShardIds { get; }

        /// <summary>Optional human-readable label (e.g. <c>"Orders.CreateShipment"</c>).</summary>
        public string Label { get; }

        /// <summary>UTC timestamp the scope opened.</summary>
        public DateTime OpenedUtc { get; }

        /// <summary>UTC timestamp the scope reached a terminal state, if any.</summary>
        public DateTime? ClosedUtc { get; private set; }

        /// <summary>Current status; read atomically.</summary>
        public DistributedTransactionStatus Status
            => (DistributedTransactionStatus)Volatile.Read(ref _status);

        /// <summary>Human-readable description of the last transition (for logs / errors).</summary>
        public string LastTransitionReason { get; private set; }

        /// <summary><c>true</c> when <see cref="Status"/> is a terminal outcome.</summary>
        public bool IsClosed
        {
            get
            {
                var s = Status;
                return s == DistributedTransactionStatus.Committed
                    || s == DistributedTransactionStatus.Aborted
                    || s == DistributedTransactionStatus.InDoubt;
            }
        }

        /// <summary>Names of saga forward steps that completed — consumed by the compensation runner.</summary>
        public IReadOnlyList<string> SagaCompletedSteps
        {
            get
            {
                lock (_gate) return _sagaCompletedSteps.ToArray();
            }
        }

        // ── Internal lifecycle hooks ──────────────────────────────────────

        /// <summary>
        /// Attempts an atomic status transition. Returns <c>false</c>
        /// when the scope is already in another state — the caller is
        /// expected to surface a descriptive error.
        /// </summary>
        internal bool TryTransition(
            DistributedTransactionStatus expected,
            DistributedTransactionStatus next,
            string                       reason)
        {
            int current = Interlocked.CompareExchange(
                ref _status,
                (int)next,
                (int)expected);

            if (current != (int)expected) return false;

            LastTransitionReason = reason ?? string.Empty;
            if (next == DistributedTransactionStatus.Committed
             || next == DistributedTransactionStatus.Aborted
             || next == DistributedTransactionStatus.InDoubt)
            {
                ClosedUtc = DateTime.UtcNow;
            }
            return true;
        }

        /// <summary>
        /// Forces a status update regardless of the previous value.
        /// Only used when the coordinator needs to recover from an
        /// unexpected state (logged by the caller).
        /// </summary>
        internal void ForceStatus(DistributedTransactionStatus next, string reason)
        {
            Interlocked.Exchange(ref _status, (int)next);
            LastTransitionReason = reason ?? string.Empty;
            if (next == DistributedTransactionStatus.Committed
             || next == DistributedTransactionStatus.Aborted
             || next == DistributedTransactionStatus.InDoubt)
            {
                ClosedUtc = DateTime.UtcNow;
            }
        }

        /// <summary>Records a completed saga forward step name for the compensation runner.</summary>
        internal void RecordSagaForward(string stepName)
        {
            if (string.IsNullOrWhiteSpace(stepName)) return;
            lock (_gate) _sagaCompletedSteps.Add(stepName);
        }

        /// <inheritdoc/>
        public override string ToString()
            => $"Scope(corr={CorrelationId}; strat={Strategy}; shards={ShardIds.Count}; status={Status})";
    }
}
