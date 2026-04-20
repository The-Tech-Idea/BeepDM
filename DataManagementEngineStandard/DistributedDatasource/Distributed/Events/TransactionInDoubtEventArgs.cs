using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Distributed.Transactions;

namespace TheTechIdea.Beep.Distributed.Events
{
    /// <summary>
    /// Event payload raised when a two-phase commit scope enters
    /// <see cref="DistributedTransactionStatus.InDoubt"/>: every
    /// shard voted <c>PrepareOk</c>, but at least one shard failed
    /// the commit round. The operator (or Phase 13's automatic
    /// reconciler) must decide how to resolve the orphaned state.
    /// </summary>
    public sealed class TransactionInDoubtEventArgs : EventArgs
    {
        /// <summary>Creates a new event payload.</summary>
        public TransactionInDoubtEventArgs(
            string                              correlationId,
            IReadOnlyList<string>               committedShards,
            IReadOnlyList<string>               failedShards,
            Exception                           firstCommitError,
            DateTime                            timestampUtc)
        {
            if (string.IsNullOrWhiteSpace(correlationId))
                throw new ArgumentException(
                    "CorrelationId cannot be null or whitespace.", nameof(correlationId));

            CorrelationId    = correlationId;
            CommittedShards  = committedShards ?? Array.Empty<string>();
            FailedShards     = failedShards    ?? Array.Empty<string>();
            FirstCommitError = firstCommitError;
            TimestampUtc     = timestampUtc;
        }

        /// <summary>Correlation id of the in-doubt scope.</summary>
        public string CorrelationId { get; }

        /// <summary>Shards that successfully acked the commit round.</summary>
        public IReadOnlyList<string> CommittedShards { get; }

        /// <summary>Shards that failed the commit round (the in-doubt side).</summary>
        public IReadOnlyList<string> FailedShards { get; }

        /// <summary>First exception captured during the commit round, if any.</summary>
        public Exception FirstCommitError { get; }

        /// <summary>UTC timestamp when the coordinator entered the in-doubt state.</summary>
        public DateTime TimestampUtc { get; }

        /// <inheritdoc/>
        public override string ToString()
            => $"InDoubt(corr={CorrelationId}; committed={CommittedShards.Count}; failed={FailedShards.Count}"
                + (FirstCommitError != null ? $"; firstError={FirstCommitError.Message}" : string.Empty) + ")";

        /// <summary>Returns a pipe-delimited snapshot of failed shards for log messages.</summary>
        public string FailedShardsAsCsv() => string.Join("|", FailedShards);

        /// <summary>Returns a pipe-delimited snapshot of committed shards for log messages.</summary>
        public string CommittedShardsAsCsv() => string.Join("|", CommittedShards);

        /// <summary>Enumerates every shard that participated in the commit phase.</summary>
        public IEnumerable<string> AllShards()
            => CommittedShards.Concat(FailedShards);
    }
}
