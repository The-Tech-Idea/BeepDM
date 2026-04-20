using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using TheTechIdea.Beep.Distributed.Events;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed.Resilience
{
    /// <summary>
    /// Default <see cref="IShardHealthMonitor"/> implementation. Uses a
    /// lightweight <see cref="Timer"/> to poll each shard's
    /// <see cref="IProxyCluster.ConnectionStatus"/> and aggregates
    /// hot-path failure counts reported via
    /// <see cref="RecordFailure(string, Exception, string)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The monitor is deliberately additive on top of each cluster's
    /// internal health machinery (<see cref="IProxyDataSource.GetMetrics"/>,
    /// per-node watchdog, per-node circuit breaker). It never tries to
    /// reproduce those checks — it simply observes the cluster's
    /// top-level state and trusts the Proxy tier for the finer-grained
    /// decisions.
    /// </para>
    /// <para>
    /// A shard is flipped to unhealthy when its consecutive failure
    /// counter reaches
    /// <see cref="ShardDownPolicyOptions.CircuitFailureThreshold"/>.
    /// A single successful observation (polling tick or hot-path call)
    /// flips it back, mirroring the
    /// <see cref="ShardDownPolicyOptions.CircuitSuccessThreshold"/> >= 1
    /// contract of the underlying <see cref="CircuitBreaker"/>.
    /// </para>
    /// </remarks>
    public sealed class ShardHealthMonitor : IShardHealthMonitor
    {
        private readonly Func<IReadOnlyDictionary<string, IProxyCluster>> _resolveShards;
        private readonly ShardDownPolicyOptions                           _options;
        private readonly TimeSpan                                         _pollInterval;
        private readonly ConcurrentDictionary<string, ShardHealthState>   _states;
        private readonly object                                           _lifecycleGate = new object();

        private Timer   _timer;
        private int     _disposed;
        private bool    _running;

        /// <summary>Creates a new health monitor.</summary>
        /// <param name="resolveShards">Delegate returning the live shard map. Required.</param>
        /// <param name="options">Shard-down policy options. Required.</param>
        /// <param name="pollInterval">Polling interval for the background loop. Defaults to 5 seconds.</param>
        /// <exception cref="ArgumentNullException"><paramref name="resolveShards"/> or <paramref name="options"/> is <c>null</c>.</exception>
        public ShardHealthMonitor(
            Func<IReadOnlyDictionary<string, IProxyCluster>> resolveShards,
            ShardDownPolicyOptions                           options,
            TimeSpan?                                        pollInterval = null)
        {
            _resolveShards = resolveShards ?? throw new ArgumentNullException(nameof(resolveShards));
            _options       = options       ?? throw new ArgumentNullException(nameof(options));
            _pollInterval  = pollInterval.GetValueOrDefault(TimeSpan.FromSeconds(5));
            _states        = new ConcurrentDictionary<string, ShardHealthState>(StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public event EventHandler<ShardDownEventArgs>      OnShardDown;

        /// <inheritdoc/>
        public event EventHandler<ShardRestoredEventArgs>  OnShardRestored;

        /// <inheritdoc/>
        public void Start()
        {
            if (Volatile.Read(ref _disposed) == 1) return;
            lock (_lifecycleGate)
            {
                if (_running) return;
                _running = true;
                _timer   = new Timer(OnTick, state: null,
                                     dueTime: _pollInterval,
                                     period:  _pollInterval);
            }
        }

        /// <inheritdoc/>
        public void Stop()
        {
            lock (_lifecycleGate)
            {
                if (!_running) return;
                _running = false;
                _timer?.Dispose();
                _timer = null;
            }
        }

        /// <inheritdoc/>
        public ShardHealthSnapshot GetSnapshot(string shardId)
        {
            if (string.IsNullOrWhiteSpace(shardId)) return null;
            return _states.TryGetValue(shardId, out var state)
                ? state.ToSnapshot()
                : null;
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, ShardHealthSnapshot> GetAllSnapshots()
        {
            var copy = new Dictionary<string, ShardHealthSnapshot>(
                _states.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var kv in _states)
            {
                copy[kv.Key] = kv.Value.ToSnapshot();
            }
            return copy;
        }

        /// <inheritdoc/>
        public bool IsShardHealthy(string shardId)
        {
            if (string.IsNullOrWhiteSpace(shardId)) return true;
            return !_states.TryGetValue(shardId, out var state) || state.IsHealthy;
        }

        /// <inheritdoc/>
        public double GetHealthyShardRatio()
        {
            int total = 0;
            int healthy = 0;
            foreach (var kv in _states)
            {
                total++;
                if (kv.Value.IsHealthy) healthy++;
            }
            return total == 0 ? 0d : (double)healthy / total;
        }

        /// <inheritdoc/>
        public void RecordSuccess(string shardId, double latencyMs = 0)
        {
            if (string.IsNullOrWhiteSpace(shardId) || Volatile.Read(ref _disposed) == 1) return;

            var state = _states.GetOrAdd(shardId, id => new ShardHealthState(id));
            ShardRestoredEventArgs recoveredArgs = null;
            lock (state.Gate)
            {
                state.ConsecutiveFailures = 0;
                state.LastLatencyMs       = latencyMs < 0 ? 0 : latencyMs;
                state.LastCheckedUtc      = DateTime.UtcNow;
                state.LastSuccessUtc      = state.LastCheckedUtc;

                if (!state.IsHealthy)
                {
                    state.IsHealthy = true;
                    var downtime = state.LastCheckedUtc - state.LastTransitionUtc;
                    state.LastTransitionUtc = state.LastCheckedUtc;
                    state.Reason            = "success";
                    recoveredArgs           = new ShardRestoredEventArgs(
                        shardId:  shardId,
                        reason:   "consecutive-success",
                        downtime: downtime);
                }
            }
            if (recoveredArgs != null) SafeInvoke(OnShardRestored, recoveredArgs);
        }

        /// <inheritdoc/>
        public void RecordFailure(string shardId, Exception error, string reason = null)
        {
            if (string.IsNullOrWhiteSpace(shardId) || Volatile.Read(ref _disposed) == 1) return;

            var state = _states.GetOrAdd(shardId, id => new ShardHealthState(id));
            ShardDownEventArgs downArgs = null;
            lock (state.Gate)
            {
                state.ConsecutiveFailures++;
                state.LastCheckedUtc = DateTime.UtcNow;
                state.Reason         = reason ?? error?.Message ?? "failure";
                if (state.FirstErrorInStreak == null)
                {
                    state.FirstErrorInStreak = error;
                }

                int threshold = _options.CircuitFailureThreshold > 0
                    ? _options.CircuitFailureThreshold
                    : int.MaxValue;

                if (state.IsHealthy && state.ConsecutiveFailures >= threshold)
                {
                    state.IsHealthy         = false;
                    state.LastTransitionUtc = state.LastCheckedUtc;
                    downArgs = new ShardDownEventArgs(
                        shardId:             shardId,
                        reason:              state.Reason,
                        consecutiveFailures: state.ConsecutiveFailures,
                        firstError:          state.FirstErrorInStreak);
                }
            }
            if (downArgs != null) SafeInvoke(OnShardDown, downArgs);
        }

        /// <inheritdoc/>
        public void Forget(string shardId)
        {
            if (string.IsNullOrWhiteSpace(shardId)) return;
            _states.TryRemove(shardId, out _);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            Stop();
            _states.Clear();
        }

        // ── Polling loop ──────────────────────────────────────────────────

        private void OnTick(object _)
        {
            if (Volatile.Read(ref _disposed) == 1) return;
            IReadOnlyDictionary<string, IProxyCluster> shards;
            try
            {
                shards = _resolveShards() ?? new Dictionary<string, IProxyCluster>();
            }
            catch
            {
                return; // caller delegate misbehaved; next tick will retry
            }

            foreach (var kv in shards)
            {
                ProbeShard(kv.Key, kv.Value);
            }
        }

        private void ProbeShard(string shardId, IProxyCluster cluster)
        {
            if (cluster == null)
            {
                RecordFailure(shardId, error: null, reason: "cluster-null");
                return;
            }

            try
            {
                var status = cluster.ConnectionStatus;
                if (status == ConnectionState.Open || status == ConnectionState.Connecting)
                {
                    RecordSuccess(shardId);
                }
                else
                {
                    RecordFailure(
                        shardId: shardId,
                        error:   null,
                        reason:  $"cluster-state:{status}");
                }
            }
            catch (Exception ex)
            {
                RecordFailure(shardId, ex, "probe-threw");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static void SafeInvoke<T>(EventHandler<T> handler, T args) where T : EventArgs
        {
            if (handler == null) return;
            try
            {
                handler(null, args);
            }
            catch
            {
                // Monitor must never throw from the hot path.
            }
        }

        // ── Internal mutable state ────────────────────────────────────────

        private sealed class ShardHealthState
        {
            internal readonly object Gate = new object();

            internal ShardHealthState(string shardId)
            {
                ShardId           = shardId;
                IsHealthy         = true;
                LastCheckedUtc    = DateTime.UtcNow;
                LastTransitionUtc = DateTime.UtcNow;
                Reason            = "newly-registered";
            }

            internal string   ShardId             { get; }
            internal bool     IsHealthy;
            internal int      ConsecutiveFailures;
            internal double   LastLatencyMs;
            internal DateTime LastCheckedUtc;
            internal DateTime? LastSuccessUtc;
            internal DateTime LastTransitionUtc;
            internal string   Reason;
            internal Exception FirstErrorInStreak;

            internal ShardHealthSnapshot ToSnapshot()
            {
                lock (Gate)
                {
                    return new ShardHealthSnapshot(
                        shardId:             ShardId,
                        isHealthy:           IsHealthy,
                        consecutiveFailures: ConsecutiveFailures,
                        averageLatencyMs:    LastLatencyMs,
                        lastCheckedUtc:      LastCheckedUtc,
                        lastSuccessUtc:      LastSuccessUtc,
                        reason:              Reason);
                }
            }
        }

        // Helpers for read-only enumeration of healthy/unhealthy ids.
        internal (IReadOnlyList<string> healthy, IReadOnlyList<string> unhealthy) ClassifyKnownShards()
        {
            var healthy   = new List<string>();
            var unhealthy = new List<string>();
            foreach (var kv in _states.ToArray())
            {
                if (kv.Value.IsHealthy) healthy.Add(kv.Key);
                else unhealthy.Add(kv.Key);
            }
            healthy.Sort(StringComparer.OrdinalIgnoreCase);
            unhealthy.Sort(StringComparer.OrdinalIgnoreCase);
            return (healthy, unhealthy);
        }
    }
}
