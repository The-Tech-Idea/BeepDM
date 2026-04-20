using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Proxy;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// Default <see cref="IEntityCopyService"/> — pages rows out of
    /// the source cluster with
    /// <see cref="IProxyCluster.GetEntity(string, List{AppFilter}, int, int)"/>
    /// and inserts each row into the target via
    /// <see cref="IProxyCluster.InsertEntity(string, object)"/>. Persists
    /// checkpoints between pages so a restart can resume mid-reshard.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Idempotency: the copy loop relies on the target shard's
    /// conflict-handling behaviour (upsert / unique-constraint
    /// tolerance). The service records the last copied page number
    /// in the checkpoint; on resume it reads the checkpoint,
    /// advances <c>pageNumber</c> past the recorded value, and
    /// continues.
    /// </para>
    /// <para>
    /// Throttling: when
    /// <see cref="EntityCopyOptions.MaxCopyRowsPerSecond"/> is
    /// positive, the loop sleeps between batches to stay under the
    /// target rate. The calculation is intentionally simple — a
    /// linear sleep based on the page size — which is accurate
    /// enough for operational throttling while keeping the
    /// implementation free of timers.
    /// </para>
    /// </remarks>
    public sealed class EntityCopyService : IEntityCopyService
    {
        /// <summary>Initialises a new copy service.</summary>
        /// <param name="options">Copy tuning knobs; defaults used when <c>null</c>.</param>
        /// <param name="checkpointStore">Checkpoint store; an <see cref="InMemoryCopyCheckpointStore"/> is used when <c>null</c>.</param>
        public EntityCopyService(
            EntityCopyOptions          options         = null,
            IEntityCopyCheckpointStore checkpointStore = null)
        {
            Options         = options         ?? new EntityCopyOptions();
            CheckpointStore = checkpointStore ?? new InMemoryCopyCheckpointStore();
        }

        /// <inheritdoc/>
        public EntityCopyOptions Options { get; }

        /// <inheritdoc/>
        public IEntityCopyCheckpointStore CheckpointStore { get; }

        /// <inheritdoc/>
        public async Task<CopyResult> CopyAsync(
            string                  reshardId,
            string                  entityName,
            string                  fromShardId,
            string                  toShardId,
            IProxyCluster           source,
            IProxyCluster           target,
            List<AppFilter>         filter            = null,
            IProgress<CopyProgress> progress          = null,
            CancellationToken       cancellationToken = default)
        {
            ValidateCopyArgs(reshardId, entityName, fromShardId, toShardId, source, target);

            var sw        = Stopwatch.StartNew();
            var existing  = CheckpointStore.Load(reshardId, entityName, fromShardId, toShardId);
            int pageSize  = Math.Max(1, Options.CopyBatchSize);
            int pageIndex = ResumePageFrom(existing, pageSize);
            long rowsSoFar= existing?.RowsCopied ?? 0L;

            var lastCheckpointAt = DateTime.UtcNow;

            try
            {
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return Cancelled(reshardId, entityName, fromShardId, toShardId, rowsSoFar, sw.Elapsed);
                    }

                    var (rows, eof) = await ReadPageAsync(
                        source, entityName, filter, pageIndex, pageSize, cancellationToken).ConfigureAwait(false);

                    if (rows.Count == 0 || eof)
                    {
                        PersistCheckpoint(
                            reshardId, entityName, fromShardId, toShardId,
                            lastCopiedKey: existing?.LastCopiedKey, rowsCopied: rowsSoFar, isComplete: true);
                        return Success(reshardId, entityName, fromShardId, toShardId, rowsSoFar, sw.Elapsed);
                    }

                    InsertPageOrThrow(target, entityName, rows);
                    rowsSoFar += rows.Count;
                    progress?.Report(new CopyProgress(entityName, fromShardId, toShardId, rowsSoFar, totalRows: null));

                    if (ShouldPersistCheckpoint(ref lastCheckpointAt))
                    {
                        PersistCheckpoint(
                            reshardId, entityName, fromShardId, toShardId,
                            lastCopiedKey: pageIndex.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            rowsCopied:    rowsSoFar,
                            isComplete:    false);
                    }

                    await ThrottleIfNeededAsync(rows.Count, cancellationToken).ConfigureAwait(false);

                    // Advance; a short page indicates EOF on the next iteration.
                    pageIndex++;
                    if (rows.Count < pageSize)
                    {
                        PersistCheckpoint(
                            reshardId, entityName, fromShardId, toShardId,
                            lastCopiedKey: pageIndex.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            rowsCopied:    rowsSoFar,
                            isComplete:    true);
                        return Success(reshardId, entityName, fromShardId, toShardId, rowsSoFar, sw.Elapsed);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return Cancelled(reshardId, entityName, fromShardId, toShardId, rowsSoFar, sw.Elapsed);
            }
            catch (Exception ex)
            {
                return Failed(reshardId, entityName, fromShardId, toShardId, rowsSoFar, sw.Elapsed, ex);
            }
        }

        // ── Private helpers ───────────────────────────────────────────────

        private static void ValidateCopyArgs(
            string        reshardId,
            string        entityName,
            string        fromShardId,
            string        toShardId,
            IProxyCluster source,
            IProxyCluster target)
        {
            if (string.IsNullOrWhiteSpace(reshardId))
                throw new ArgumentException("Reshard id required.", nameof(reshardId));
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name required.", nameof(entityName));
            if (string.IsNullOrWhiteSpace(fromShardId))
                throw new ArgumentException("Source shard required.", nameof(fromShardId));
            if (string.IsNullOrWhiteSpace(toShardId))
                throw new ArgumentException("Target shard required.", nameof(toShardId));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (ReferenceEquals(source, target))
                throw new ArgumentException("Source and target clusters must differ for a copy.", nameof(target));
        }

        private static int ResumePageFrom(CopyCheckpoint existing, int pageSize)
        {
            if (existing == null || existing.IsComplete) return 1;
            if (!int.TryParse(existing.LastCopiedKey,
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var page) || page < 1)
            {
                return 1;
            }
            return page;
        }

        private async Task<(List<object> rows, bool eof)> ReadPageAsync(
            IProxyCluster     source,
            string            entityName,
            List<AppFilter>   filter,
            int               pageNumber,
            int               pageSize,
            CancellationToken cancellationToken)
        {
            int attempt = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var paged = source.GetEntity(entityName, filter, pageNumber, pageSize);
                    var rows  = ExtractRows(paged);
                    bool eof  = rows.Count == 0 || (paged != null && !paged.HasNextPage);
                    return (rows, eof);
                }
                catch when (++attempt < Math.Max(1, Options.PageRetryCount))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static List<object> ExtractRows(PagedResult paged)
        {
            if (paged == null || paged.Data == null) return new List<object>();

            if (paged.Data is IEnumerable<object> typed) return typed.ToList();
            if (paged.Data is IEnumerable enumerable)
            {
                var list = new List<object>();
                foreach (var item in enumerable)
                    if (item != null) list.Add(item);
                return list;
            }
            return new List<object> { paged.Data };
        }

        private static void InsertPageOrThrow(IProxyCluster target, string entityName, List<object> rows)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var result = target.InsertEntity(entityName, rows[i]);
                if (result != null && result.Flag == Errors.Failed)
                {
                    throw new InvalidOperationException(
                        $"Copy insert failed on target for entity '{entityName}': {result.Message}");
                }
            }
        }

        private bool ShouldPersistCheckpoint(ref DateTime lastAt)
        {
            var now    = DateTime.UtcNow;
            var window = Options.CheckpointInterval;
            if (window <= TimeSpan.Zero) { lastAt = now; return true; }
            if (now - lastAt < window)   return false;
            lastAt = now;
            return true;
        }

        private void PersistCheckpoint(
            string reshardId,
            string entityName,
            string fromShardId,
            string toShardId,
            string lastCopiedKey,
            long   rowsCopied,
            bool   isComplete)
        {
            CheckpointStore.Save(new CopyCheckpoint(
                reshardId:     reshardId,
                entityName:    entityName,
                fromShardId:   fromShardId,
                toShardId:     toShardId,
                lastCopiedKey: lastCopiedKey,
                rowsCopied:    rowsCopied,
                isComplete:    isComplete));
        }

        private async Task ThrottleIfNeededAsync(int rowsJustCopied, CancellationToken cancellationToken)
        {
            int rate = Options.MaxCopyRowsPerSecond;
            if (rate <= 0 || rowsJustCopied <= 0) return;
            double seconds = (double)rowsJustCopied / rate;
            if (seconds <= 0.001) return;
            await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken).ConfigureAwait(false);
        }

        private static CopyResult Success(string reshardId, string entityName, string from, string to, long rows, TimeSpan elapsed)
            => new CopyResult(reshardId, entityName, from, to, rows, elapsed, cancelled: false, error: null);

        private static CopyResult Cancelled(string reshardId, string entityName, string from, string to, long rows, TimeSpan elapsed)
            => new CopyResult(reshardId, entityName, from, to, rows, elapsed, cancelled: true, error: null);

        private static CopyResult Failed(string reshardId, string entityName, string from, string to, long rows, TimeSpan elapsed, Exception ex)
            => new CopyResult(reshardId, entityName, from, to, rows, elapsed, cancelled: false, error: ex);
    }
}
