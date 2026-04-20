using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// Default in-memory <see cref="IEntityCopyCheckpointStore"/>.
    /// Keeps checkpoints in a thread-safe dictionary keyed by the
    /// composite key returned by <see cref="CopyCheckpoint.CompositeKey"/>.
    /// Data is lost on process restart — Phase 13 swaps this for a
    /// <c>ConfigEditor</c>-backed store.
    /// </summary>
    public sealed class InMemoryCopyCheckpointStore : IEntityCopyCheckpointStore
    {
        private readonly ConcurrentDictionary<string, CopyCheckpoint> _store
            = new ConcurrentDictionary<string, CopyCheckpoint>(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc/>
        public void Save(CopyCheckpoint checkpoint)
        {
            if (checkpoint == null) throw new ArgumentNullException(nameof(checkpoint));
            _store[checkpoint.CompositeKey()] = checkpoint;
        }

        /// <inheritdoc/>
        public CopyCheckpoint Load(string reshardId, string entityName, string fromShardId, string toShardId)
        {
            var key = CopyCheckpoint.BuildKey(reshardId, entityName, fromShardId, toShardId);
            return _store.TryGetValue(key, out var cp) ? cp : null;
        }

        /// <inheritdoc/>
        public void RemoveAll(string reshardId)
        {
            if (string.IsNullOrWhiteSpace(reshardId)) return;
            var prefix = reshardId + "|";
            var drop   = _store.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var k in drop) _store.TryRemove(k, out _);
        }

        /// <inheritdoc/>
        public IReadOnlyList<CopyCheckpoint> ListByReshard(string reshardId)
        {
            if (string.IsNullOrWhiteSpace(reshardId)) return Array.Empty<CopyCheckpoint>();
            var prefix = reshardId + "|";
            return _store
                .Where(kv => kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(kv => kv.Value)
                .ToArray();
        }
    }
}
