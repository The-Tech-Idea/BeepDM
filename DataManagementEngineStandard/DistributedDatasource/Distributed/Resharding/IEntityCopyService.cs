using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Proxy;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// Bulk-copies rows from one shard to another in paged batches
    /// with durable <see cref="CopyCheckpoint"/> semantics. Consumed
    /// by <see cref="ReshardingService"/> during
    /// <see cref="DualWriteState.DualWrite"/> to bring the target
    /// placement up to parity before cutover.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The service is decoupled from the concrete datasource — it
    /// consumes the already-resolved <see cref="IProxyCluster"/>
    /// endpoints so unit tests can supply stubs. All calls are
    /// cancellable; a cancelled run leaves the latest checkpoint in
    /// place so a resume call can pick up where it left off.
    /// </para>
    /// <para>
    /// Row shape is whatever the source shard returns from
    /// <see cref="IProxyCluster.GetEntity(string, List{AppFilter}, int, int)"/>
    /// — typically <c>IDictionary</c> or a POCO. The service hands
    /// each row verbatim to the target's
    /// <see cref="IProxyCluster.InsertEntity"/>; callers wanting to
    /// transform rows (e.g. decrypt, mask) should compose a decorator
    /// around the source cluster.
    /// </para>
    /// </remarks>
    public interface IEntityCopyService
    {
        /// <summary>Active copy options. Never <c>null</c>.</summary>
        EntityCopyOptions Options { get; }

        /// <summary>Active checkpoint store. Never <c>null</c>.</summary>
        IEntityCopyCheckpointStore CheckpointStore { get; }

        /// <summary>
        /// Copies every row matching <paramref name="filter"/> from
        /// <paramref name="source"/> to <paramref name="target"/>.
        /// Resumes from the last checkpoint when one exists.
        /// </summary>
        /// <param name="reshardId">Governing reshard id. Required.</param>
        /// <param name="entityName">Logical entity to copy. Required.</param>
        /// <param name="fromShardId">Source shard id (used for checkpoint identity). Required.</param>
        /// <param name="toShardId">Target shard id (used for checkpoint identity). Required.</param>
        /// <param name="source">Source proxy cluster. Required.</param>
        /// <param name="target">Target proxy cluster. Required.</param>
        /// <param name="filter">Optional filter applied to the source read; <c>null</c> copies every row.</param>
        /// <param name="progress">Optional progress callback invoked once per page with a cumulative row count.</param>
        /// <param name="cancellationToken">Cancellation token; cancelling stops the loop at the next page boundary.</param>
        Task<CopyResult> CopyAsync(
            string                 reshardId,
            string                 entityName,
            string                 fromShardId,
            string                 toShardId,
            IProxyCluster          source,
            IProxyCluster          target,
            List<AppFilter>        filter            = null,
            IProgress<CopyProgress> progress          = null,
            CancellationToken      cancellationToken = default);
    }

    /// <summary>Progress snapshot emitted by <see cref="IEntityCopyService.CopyAsync"/>.</summary>
    public sealed class CopyProgress
    {
        /// <summary>Initialises a progress snapshot.</summary>
        public CopyProgress(string entityName, string fromShardId, string toShardId, long rowsCopied, long? totalRows)
        {
            EntityName  = entityName  ?? string.Empty;
            FromShardId = fromShardId ?? string.Empty;
            ToShardId   = toShardId   ?? string.Empty;
            RowsCopied  = rowsCopied;
            TotalRows   = totalRows;
        }

        /// <summary>Entity being copied.</summary>
        public string EntityName { get; }

        /// <summary>Source shard.</summary>
        public string FromShardId { get; }

        /// <summary>Target shard.</summary>
        public string ToShardId { get; }

        /// <summary>Rows copied so far (cumulative, includes restart credit).</summary>
        public long RowsCopied { get; }

        /// <summary>Total rows when known, otherwise <c>null</c>.</summary>
        public long? TotalRows { get; }
    }
}
