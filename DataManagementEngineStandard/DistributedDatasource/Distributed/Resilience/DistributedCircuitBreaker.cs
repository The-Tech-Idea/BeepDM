using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed.Resilience
{
    /// <summary>
    /// Distribution-tier circuit breaker keyed by shard id. Thin
    /// wrapper over <see cref="CircuitBreaker"/> (Phase 2 of the Proxy
    /// tier) that adds per-shard isolation and an
    /// <see cref="IShardHealthMonitor"/>-compatible surface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The per-node <see cref="CircuitBreaker"/> inside
    /// <see cref="IProxyCluster"/> is never bypassed — this breaker
    /// only adds a second guard rail at the distribution tier so the
    /// executors can trip a whole shard off the routing set even when
    /// the cluster itself reports healthy (the "returns 200s but
    /// semantically wrong" scenario).
    /// </para>
    /// <para>
    /// Configuration is driven by
    /// <see cref="ShardDownPolicyOptions.CircuitFailureThreshold"/>,
    /// <see cref="ShardDownPolicyOptions.CircuitResetTimeout"/>, and
    /// <see cref="ShardDownPolicyOptions.CircuitSuccessThreshold"/>.
    /// A failure threshold of <c>0</c> disables the breaker — every
    /// call is allowed through and the underlying
    /// <see cref="CircuitBreaker"/> is never instantiated.
    /// </para>
    /// </remarks>
    public sealed class DistributedCircuitBreaker
    {
        private readonly ShardDownPolicyOptions _options;
        private readonly ConcurrentDictionary<string, CircuitBreaker> _breakers;

        /// <summary>Creates a new distributed circuit breaker.</summary>
        /// <param name="options">Policy options carrying the breaker knobs. Required.</param>
        public DistributedCircuitBreaker(ShardDownPolicyOptions options)
        {
            _options  = options ?? throw new ArgumentNullException(nameof(options));
            _breakers = new ConcurrentDictionary<string, CircuitBreaker>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>True when the configured failure threshold disables the breaker.</summary>
        public bool IsDisabled => _options.CircuitFailureThreshold <= 0;

        /// <summary>
        /// Returns <c>true</c> when a call targeting
        /// <paramref name="shardId"/> may proceed.
        /// </summary>
        public bool CanExecute(string shardId)
        {
            if (IsDisabled || string.IsNullOrWhiteSpace(shardId)) return true;
            if (!_breakers.TryGetValue(shardId, out var breaker)) return true;
            return breaker.CanExecute();
        }

        /// <summary>Records a successful call against <paramref name="shardId"/>.</summary>
        public void RecordSuccess(string shardId)
        {
            if (IsDisabled || string.IsNullOrWhiteSpace(shardId)) return;
            if (_breakers.TryGetValue(shardId, out var breaker))
            {
                breaker.RecordSuccess();
            }
        }

        /// <summary>Records a failed call against <paramref name="shardId"/> with optional severity.</summary>
        public void RecordFailure(string shardId, ProxyErrorSeverity severity = ProxyErrorSeverity.Medium)
        {
            if (IsDisabled || string.IsNullOrWhiteSpace(shardId)) return;
            var breaker = _breakers.GetOrAdd(shardId, _ => NewBreaker());
            breaker.RecordFailure(severity);
        }

        /// <summary>Resets the breaker state for <paramref name="shardId"/> if any.</summary>
        public void Reset(string shardId)
        {
            if (string.IsNullOrWhiteSpace(shardId)) return;
            if (_breakers.TryGetValue(shardId, out var breaker))
            {
                breaker.Reset();
            }
        }

        /// <summary>Returns the current state of the breaker for <paramref name="shardId"/>, or <see cref="CircuitBreakerState.Closed"/> when no breaker exists.</summary>
        public CircuitBreakerState GetState(string shardId)
        {
            if (IsDisabled || string.IsNullOrWhiteSpace(shardId)) return CircuitBreakerState.Closed;
            return _breakers.TryGetValue(shardId, out var breaker)
                ? breaker.State
                : CircuitBreakerState.Closed;
        }

        /// <summary>Snapshot of breaker states keyed by shard id.</summary>
        public IReadOnlyDictionary<string, CircuitBreakerState> GetAllStates()
        {
            var copy = new Dictionary<string, CircuitBreakerState>(
                _breakers.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var kv in _breakers)
            {
                copy[kv.Key] = kv.Value.State;
            }
            return copy;
        }

        private CircuitBreaker NewBreaker()
            => new CircuitBreaker(
                failureThreshold: _options.CircuitFailureThreshold,
                resetTimeout:     _options.CircuitResetTimeout,
                successThreshold: Math.Max(1, _options.CircuitSuccessThreshold));
    }
}
