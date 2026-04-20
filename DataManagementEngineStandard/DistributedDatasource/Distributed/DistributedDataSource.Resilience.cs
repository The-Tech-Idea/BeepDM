using System;
using System.Collections.Generic;
using System.Threading;
using TheTechIdea.Beep.Distributed.Events;
using TheTechIdea.Beep.Distributed.Resilience;

namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// <see cref="DistributedDataSource"/> partial — Phase 10 resilience
    /// wiring. Owns the <see cref="IShardHealthMonitor"/> and
    /// <see cref="DistributedCircuitBreaker"/> instances, forwards
    /// their events through the datasource's safe-invoke raise
    /// helpers, and exposes hot-path helpers the read / write
    /// executors use to filter unhealthy shards.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lifetime contract:
    /// </para>
    /// <list type="bullet">
    ///   <item>The monitor + circuit breaker are constructed in
    ///         <see cref="DistributedDataSource"/>'s ctor.</item>
    ///   <item>Polling is started inside
    ///         <see cref="Openconnection"/> when
    ///         <see cref="DistributedDataSourceOptions.EnableHealthMonitor"/>
    ///         is <c>true</c>.</item>
    ///   <item>Polling is stopped inside
    ///         <see cref="Closeconnection"/>.</item>
    ///   <item>The monitor itself is disposed in
    ///         <see cref="Dispose"/>.</item>
    /// </list>
    /// </remarks>
    public partial class DistributedDataSource
    {
        private IShardHealthMonitor       _healthMonitor;
        private DistributedCircuitBreaker _circuitBreaker;

        // ── Public accessors (hot-swappable) ──────────────────────────────

        /// <summary>
        /// Current health monitor. Defaults to a
        /// <see cref="ShardHealthMonitor"/> constructed from the
        /// active options; callers can replace it at runtime (e.g. a
        /// fake monitor in tests) provided the new instance respects
        /// the thread-safety contract of
        /// <see cref="IShardHealthMonitor"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">When assigned to <c>null</c>.</exception>
        public IShardHealthMonitor HealthMonitor
        {
            get { return _healthMonitor; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                SwapHealthMonitor(value);
            }
        }

        /// <summary>
        /// Current distributed circuit breaker. Exposed for tests and
        /// for operators who want to inspect breaker state without
        /// racing through the public event pipeline.
        /// </summary>
        public DistributedCircuitBreaker CircuitBreaker => _circuitBreaker;

        /// <summary>
        /// Shortcut to the active
        /// <see cref="ShardDownPolicyOptions"/> stored on
        /// <see cref="DistributedDataSourceOptions"/>. Replaced by
        /// <see cref="ApplyResilienceOptions(ShardDownPolicyOptions)"/>.
        /// </summary>
        public ShardDownPolicyOptions ResilienceOptions => _options.ShardDownPolicy;

        // ── Hot-path helpers used by executors ────────────────────────────

        /// <summary>
        /// Returns <c>true</c> when <paramref name="shardId"/> passes
        /// both the health monitor and the distributed circuit
        /// breaker. Used by scatter / fan-out paths to filter the
        /// candidate set before dispatching per-shard operations.
        /// </summary>
        internal bool IsShardHealthyForDispatch(string shardId)
        {
            if (string.IsNullOrWhiteSpace(shardId)) return false;
            return _healthMonitor.IsShardHealthy(shardId)
                && _circuitBreaker.CanExecute(shardId);
        }

        /// <summary>
        /// Filters <paramref name="shardIds"/> to the subset passing
        /// <see cref="IsShardHealthyForDispatch(string)"/>. Preserves
        /// input order and uses
        /// <see cref="StringComparer.OrdinalIgnoreCase"/> semantics.
        /// </summary>
        internal IReadOnlyList<string> FilterHealthyShards(IReadOnlyList<string> shardIds)
        {
            if (shardIds == null || shardIds.Count == 0) return Array.Empty<string>();
            var healthy = new List<string>(shardIds.Count);
            for (int i = 0; i < shardIds.Count; i++)
            {
                if (IsShardHealthyForDispatch(shardIds[i]))
                {
                    healthy.Add(shardIds[i]);
                }
            }
            return healthy;
        }

        /// <summary>
        /// Evaluates the scatter entry gate. Returns <c>true</c> when
        /// the scatter call is allowed to proceed; when <c>false</c>,
        /// populates <paramref name="reason"/> with a human-readable
        /// description and raises
        /// <see cref="IDistributedDataSource.OnDegradedMode"/>.
        /// </summary>
        /// <param name="attemptedShardIds">Shards the executor would have targeted.</param>
        /// <param name="healthyShardIds">Subset that passed
        /// <see cref="IsShardHealthyForDispatch"/>.</param>
        /// <param name="reason">Filled with a human-readable explanation when the gate fails.</param>
        internal bool PassesScatterGate(
            IReadOnlyList<string> attemptedShardIds,
            IReadOnlyList<string> healthyShardIds,
            out string            reason)
        {
            reason = string.Empty;
            var threshold = _options.MinimumHealthyShardRatio;
            if (threshold <= 0d) return true;
            if (attemptedShardIds == null || attemptedShardIds.Count == 0) return true;

            double ratio = (double)healthyShardIds.Count / attemptedShardIds.Count;
            if (ratio >= threshold) return true;

            reason =
                $"Healthy shard ratio {ratio:F2} is below the configured threshold {threshold:F2}. " +
                $"attempted={attemptedShardIds.Count}, healthy={healthyShardIds.Count}.";

            var unhealthy = BuildUnhealthyList(attemptedShardIds, healthyShardIds);
            RaiseDegradedMode(new DegradedModeEventArgs(
                healthyShardIds:   healthyShardIds,
                unhealthyShardIds: unhealthy,
                healthyRatio:      ratio,
                threshold:         threshold,
                reason:            reason));
            return false;
        }

        /// <summary>
        /// Scatter-gate wrapper that returns a ready-to-throw
        /// <see cref="DegradedShardSetException"/> when the gate rejects
        /// the call, or <c>null</c> when the scatter may proceed. Used
        /// by <see cref="ShardInvokerAdapter"/> so executors stay free
        /// of partial-class coupling.
        /// </summary>
        internal DegradedShardSetException EvaluateScatterGate(
            IReadOnlyList<string> attemptedShardIds,
            IReadOnlyList<string> healthyShardIds)
        {
            if (PassesScatterGate(attemptedShardIds, healthyShardIds, out var reason))
            {
                return null;
            }

            var unhealthy = BuildUnhealthyList(attemptedShardIds, healthyShardIds);
            double ratio  = attemptedShardIds == null || attemptedShardIds.Count == 0
                ? 0d
                : (double)(healthyShardIds?.Count ?? 0) / attemptedShardIds.Count;

            return new DegradedShardSetException(
                message:           reason,
                healthyShardIds:   healthyShardIds ?? Array.Empty<string>(),
                unhealthyShardIds: unhealthy,
                healthyRatio:      ratio,
                threshold:         _options.MinimumHealthyShardRatio);
        }

        /// <summary>
        /// Records hot-path success for <paramref name="shardId"/> in
        /// both the health monitor and the distributed circuit breaker.
        /// Called by the executors once a per-shard call completes
        /// without error.
        /// </summary>
        internal void NotifyShardCallSucceeded(string shardId, double latencyMs = 0)
        {
            if (string.IsNullOrWhiteSpace(shardId)) return;
            _healthMonitor.RecordSuccess(shardId, latencyMs);
            _circuitBreaker.RecordSuccess(shardId);
            RecordDistributedRequest(shardId, entityName: null, mode: null, latencyMs: latencyMs, succeeded: true);
        }

        /// <summary>
        /// Records hot-path failure for <paramref name="shardId"/> in
        /// both the health monitor and the distributed circuit
        /// breaker. Called by the executors whenever a per-shard call
        /// throws.
        /// </summary>
        internal void NotifyShardCallFailed(
            string                     shardId,
            Exception                  error,
            string                     reason = null,
            Proxy.ProxyErrorSeverity   severity = Proxy.ProxyErrorSeverity.Medium)
        {
            if (string.IsNullOrWhiteSpace(shardId)) return;
            _healthMonitor.RecordFailure(shardId, error, reason);
            _circuitBreaker.RecordFailure(shardId, severity);
            RecordDistributedRequest(shardId, entityName: null, mode: reason, latencyMs: 0, succeeded: false);
        }

        /// <summary>
        /// Convenience entry point for broadcast writes that need to
        /// surface a partial-broadcast event after excluding unhealthy
        /// shards. Safe to call with an empty skipped list — the event
        /// is raised only when at least one shard was skipped.
        /// </summary>
        internal void ReportPartialBroadcast(
            string                entityName,
            string                operation,
            IReadOnlyList<string> attemptedShardIds,
            IReadOnlyList<string> skippedShardIds,
            string                reason)
        {
            if (skippedShardIds == null || skippedShardIds.Count == 0) return;
            RaisePartialBroadcast(new PartialBroadcastEventArgs(
                entityName:        entityName,
                operation:         operation,
                attemptedShardIds: attemptedShardIds ?? Array.Empty<string>(),
                skippedShardIds:   skippedShardIds,
                reason:            reason));
        }

        // ── Configuration ────────────────────────────────────────────────

        /// <summary>
        /// Replaces <see cref="DistributedDataSourceOptions.ShardDownPolicy"/>
        /// and rebuilds the distributed circuit breaker. The health
        /// monitor instance is preserved so its observation history
        /// survives the reconfiguration.
        /// </summary>
        public void ApplyResilienceOptions(ShardDownPolicyOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            _options.ShardDownPolicy = options;
            _circuitBreaker = new DistributedCircuitBreaker(options);
        }

        // ── Internal wiring (called from ctor + lifecycle) ───────────────

        internal void BuildResilience()
        {
            _circuitBreaker = new DistributedCircuitBreaker(_options.ShardDownPolicy);

            var interval = _options.HealthMonitorIntervalMs > 0
                ? TimeSpan.FromMilliseconds(_options.HealthMonitorIntervalMs)
                : Timeout.InfiniteTimeSpan;

            var monitor = new ShardHealthMonitor(
                resolveShards: () => _shards,
                options:       _options.ShardDownPolicy,
                pollInterval:  interval);
            SwapHealthMonitor(monitor);
        }

        internal void StartHealthMonitor()
        {
            if (!_options.EnableHealthMonitor) return;
            if (_options.HealthMonitorIntervalMs <= 0) return;
            _healthMonitor?.Start();
        }

        internal void StopHealthMonitor()
        {
            _healthMonitor?.Stop();
        }

        internal void DisposeResilience()
        {
            try
            {
                _healthMonitor?.Dispose();
            }
            catch
            {
                // Disposal must never throw from the lifecycle path.
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private void SwapHealthMonitor(IShardHealthMonitor next)
        {
            var previous = _healthMonitor;
            if (ReferenceEquals(previous, next)) return;

            if (previous != null)
            {
                previous.OnShardDown     -= OnMonitorShardDown;
                previous.OnShardRestored -= OnMonitorShardRestored;
                try { previous.Stop(); } catch { /* best-effort */ }
                try { previous.Dispose(); } catch { /* best-effort */ }
            }

            _healthMonitor = next;
            next.OnShardDown     += OnMonitorShardDown;
            next.OnShardRestored += OnMonitorShardRestored;
        }

        private void OnMonitorShardDown(object sender, ShardDownEventArgs e)
            => RaiseShardDown(e);

        private void OnMonitorShardRestored(object sender, ShardRestoredEventArgs e)
            => RaiseShardRestored(e);

        private static IReadOnlyList<string> BuildUnhealthyList(
            IReadOnlyList<string> attempted,
            IReadOnlyList<string> healthy)
        {
            if (attempted == null || attempted.Count == 0) return Array.Empty<string>();
            if (healthy == null || healthy.Count == 0)     return attempted;

            var healthySet = new HashSet<string>(healthy, StringComparer.OrdinalIgnoreCase);
            var result     = new List<string>(attempted.Count - healthy.Count);
            for (int i = 0; i < attempted.Count; i++)
            {
                if (!healthySet.Contains(attempted[i]))
                {
                    result.Add(attempted[i]);
                }
            }
            return result;
        }
    }
}
