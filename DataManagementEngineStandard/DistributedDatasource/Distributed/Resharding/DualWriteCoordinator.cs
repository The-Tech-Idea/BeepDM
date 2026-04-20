using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// Default in-memory <see cref="IDualWriteCoordinator"/>. Stores
    /// one window per entity in a concurrent dictionary so the
    /// router's read/write hot path pays only a dictionary lookup
    /// when no migration is active.
    /// </summary>
    /// <remarks>
    /// Persistence is out of scope for v1 — if the process restarts
    /// mid-reshard the coordinator starts empty and the
    /// <see cref="ReshardingService"/> is responsible for replaying
    /// the last checkpoint. Phase 13 extends this with a persisted
    /// window store backed by <c>ConfigEditor</c>.
    /// </remarks>
    public sealed class DualWriteCoordinator : IDualWriteCoordinator
    {
        private readonly ConcurrentDictionary<string, DualWriteWindow> _windows
            = new ConcurrentDictionary<string, DualWriteWindow>(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc/>
        public void Register(DualWriteWindow window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            if (!_windows.TryAdd(window.EntityName, window))
            {
                throw new InvalidOperationException(
                    $"A dual-write window is already registered for entity '{window.EntityName}'. " +
                    "Complete or cancel the active window before starting a new migration.");
            }
        }

        /// <inheritdoc/>
        public void Unregister(string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName)) return;
            _windows.TryRemove(entityName, out _);
        }

        /// <inheritdoc/>
        public DualWriteWindow TryGetWindow(string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName)) return null;
            return _windows.TryGetValue(entityName, out var window) ? window : null;
        }

        /// <inheritdoc/>
        public IReadOnlyList<DualWriteWindow> Snapshot()
            => _windows.Values.ToArray();

        /// <inheritdoc/>
        public IReadOnlyList<string> GetAdditionalWriteShardIds(string entityName)
        {
            var window = TryGetWindow(entityName);
            if (window == null || !window.IsWriteDualHit) return Array.Empty<string>();

            // Union returns the full fan-out set; the caller is expected to
            // de-duplicate against the baseline decision's ShardIds.
            return window.UnionShardIds();
        }
    }
}
