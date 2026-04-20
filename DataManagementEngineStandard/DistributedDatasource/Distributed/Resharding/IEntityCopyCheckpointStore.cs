using System.Collections.Generic;

namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// Pluggable store for <see cref="CopyCheckpoint"/> records.
    /// Default implementation is in-memory; Phase 13 backs this with
    /// a <c>ConfigEditor</c>-persisted store so reshard progress
    /// survives a process restart.
    /// </summary>
    public interface IEntityCopyCheckpointStore
    {
        /// <summary>Persists or replaces a checkpoint.</summary>
        void Save(CopyCheckpoint checkpoint);

        /// <summary>
        /// Loads the last checkpoint for the supplied composite key,
        /// or <c>null</c> when none exists.
        /// </summary>
        CopyCheckpoint Load(string reshardId, string entityName, string fromShardId, string toShardId);

        /// <summary>Removes every checkpoint belonging to <paramref name="reshardId"/>.</summary>
        void RemoveAll(string reshardId);

        /// <summary>Returns every checkpoint that belongs to <paramref name="reshardId"/>.</summary>
        IReadOnlyList<CopyCheckpoint> ListByReshard(string reshardId);
    }
}
