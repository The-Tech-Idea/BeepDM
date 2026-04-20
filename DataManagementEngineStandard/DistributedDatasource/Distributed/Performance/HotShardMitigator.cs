using System;
using System.Collections.Generic;
using System.Threading;
using TheTechIdea.Beep.Distributed.Observability;

namespace TheTechIdea.Beep.Distributed.Performance
{
    /// <summary>
    /// Maintains the set of shards currently flagged "hot" by the
    /// Phase 13 <see cref="HotShardDetector"/> and answers the
    /// executor's question "may I still dispatch this read to shard
    /// X?". Hot shards are read-shed only for replicated / broadcast
    /// placements; single-shard (sharded) reads are never shed so no
    /// partitioned data gets orphaned.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The mitigator subscribes to
    /// <see cref="IDistributedMetricsAggregator.OnHotShardDetected"/>
    /// and maintains a cooldown per shard so a single flap does not
    /// yank the shard in and out. When no aggregator is attached the
    /// mitigator is a no-op (<see cref="ShouldShedRead(string, bool)"/>
    /// returns <c>false</c>).
    /// </para>
    /// <para>
    /// Callers use <see cref="Enable"/>/<see cref="Disable"/> to
    /// bounce the mitigator without re-subscribing to events, and
    /// <see cref="Clear"/> to wipe the hot-shard set after a plan
    /// swap.
    /// </para>
    /// </remarks>
    public sealed class HotShardMitigator
    {
        private readonly Dictionary<string, DateTime> _hotUntilUtc;
        private readonly object                       _lock;
        private readonly TimeSpan                     _cooldown;
        private bool                                  _enabled;

        /// <summary>Creates a new mitigator.</summary>
        /// <param name="enabled">Initial enabled state.</param>
        /// <param name="cooldown">How long a shard stays shed-eligible after the last hot event.</param>
        public HotShardMitigator(bool enabled, TimeSpan cooldown)
        {
            _enabled     = enabled;
            _cooldown    = cooldown > TimeSpan.Zero ? cooldown : TimeSpan.FromSeconds(30);
            _hotUntilUtc = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            _lock        = new object();
        }

        /// <summary>True when the mitigator currently sheds reads.</summary>
        public bool IsEnabled => Volatile.Read(ref _enabled);

        /// <summary>Number of shards currently flagged hot.</summary>
        public int HotShardCount
        {
            get { lock (_lock) return _hotUntilUtc.Count; }
        }

        /// <summary>Re-enables read shedding (keeps the accumulated hot set).</summary>
        public void Enable()  => Volatile.Write(ref _enabled, true);

        /// <summary>Disables read shedding without clearing the hot set.</summary>
        public void Disable() => Volatile.Write(ref _enabled, false);

        /// <summary>Drops every hot-shard entry (used after a plan swap).</summary>
        public void Clear()
        {
            lock (_lock) { _hotUntilUtc.Clear(); }
        }

        /// <summary>
        /// Subscribes to the supplied aggregator's hot-shard event.
        /// Subsequent detections flag the shard for the configured
        /// cooldown window.
        /// </summary>
        public void Attach(IDistributedMetricsAggregator aggregator)
        {
            if (aggregator == null) return;
            aggregator.OnHotShardDetected += OnHotShardDetected;
        }

        /// <summary>Reverses a prior <see cref="Attach"/>.</summary>
        public void Detach(IDistributedMetricsAggregator aggregator)
        {
            if (aggregator == null) return;
            aggregator.OnHotShardDetected -= OnHotShardDetected;
        }

        /// <summary>
        /// Returns <c>true</c> when <paramref name="shardId"/> is
        /// currently hot AND the caller is allowed to shed
        /// (<paramref name="canShed"/>).
        /// </summary>
        public bool ShouldShedRead(string shardId, bool canShed)
        {
            if (!IsEnabled || !canShed || string.IsNullOrWhiteSpace(shardId)) return false;

            lock (_lock)
            {
                if (!_hotUntilUtc.TryGetValue(shardId, out var until)) return false;
                if (DateTime.UtcNow >= until)
                {
                    _hotUntilUtc.Remove(shardId);
                    return false;
                }
                return true;
            }
        }

        private void OnHotShardDetected(object sender, HotShardEventArgs e)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.ShardId)) return;
            var until = DateTime.UtcNow.Add(_cooldown);
            lock (_lock) { _hotUntilUtc[e.ShardId] = until; }
        }
    }
}
