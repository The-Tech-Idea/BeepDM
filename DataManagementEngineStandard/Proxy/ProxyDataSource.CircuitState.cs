using System;
using System.Collections.Concurrent;

namespace TheTechIdea.Beep.Proxy
{
    // ─────────────────────────────────────────────────────────────────────────
    //  ICircuitStateStore  — swappable circuit-state backend
    //
    //  Allows the proxy to be wired to a distributed state store (e.g. Redis,
    //  SQL, in-memory) without changing any other ProxyDataSource code.
    //  The default (InProcessCircuitStateStore) wraps the existing per-process
    //  ConcurrentDictionary<string, CircuitBreaker>.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Abstraction over the circuit-breaker state storage layer.
    /// Implement this interface to replace the default in-process store with a
    /// distributed backend (e.g. Redis, SQL Server, Azure Table Storage).
    /// </summary>
    public interface ICircuitStateStore
    {
        /// <summary>
        /// Returns <c>true</c> when the circuit for <paramref name="dsName"/> allows
        /// a call to proceed (Closed or HalfOpen within reset window).
        /// </summary>
        bool CanExecute(string dsName);

        /// <summary>Records a successful call result. May close an open circuit.</summary>
        void RecordSuccess(string dsName);

        /// <summary>
        /// Records a failed call result. Severity weights the failure accumulation:
        /// <list type="bullet">
        ///   <item>Critical — immediately breaches the failure threshold.</item>
        ///   <item>High     — counts as two failures.</item>
        ///   <item>Medium / Low — counts as one failure.</item>
        /// </list>
        /// </summary>
        void RecordFailure(string dsName, ProxyErrorSeverity severity = ProxyErrorSeverity.Medium);

        /// <summary>
        /// Registers (or reinitialises) the circuit entry for a datasource.
        /// Called when a datasource is added to the proxy or when policy is applied.
        /// </summary>
        void Initialize(string dsName, int failureThreshold, TimeSpan resetTimeout, int successThreshold = 2);

        /// <summary>
        /// Removes the circuit entry for a datasource.
        /// Called when a datasource is removed from the proxy.
        /// </summary>
        void Remove(string dsName);

        /// <summary>Returns the current <see cref="CircuitBreakerState"/>, or Closed if not tracked.</summary>
        CircuitBreakerState GetState(string dsName);

        /// <summary>Manually trips the circuit to Open (e.g. for maintenance windows).</summary>
        void ForceOpen(string dsName);

        /// <summary>Manually resets the circuit to Closed (e.g. after manual recovery).</summary>
        void Reset(string dsName);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  InProcessCircuitStateStore  — default in-process implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Default in-process circuit-state store backed by
    /// <see cref="ConcurrentDictionary{TKey,TValue}"/>. Thread-safe; suitable for
    /// single-machine deployments. Replace with a distributed implementation for
    /// multi-replica or multi-region deployments.
    /// </summary>
    public sealed class InProcessCircuitStateStore : ICircuitStateStore
    {
        private readonly ConcurrentDictionary<string, CircuitBreaker> _breakers = new();

        // Cache the config per-entry so Initialize can recreate with same params when needed
        private readonly ConcurrentDictionary<string, (int Threshold, TimeSpan Timeout, int SuccessThreshold)> _configs = new();

        /// <inheritdoc/>
        public bool CanExecute(string dsName)
            // If not tracked, default to allowing execution
            => !_breakers.TryGetValue(dsName, out var b) || b.CanExecute();

        /// <inheritdoc/>
        public void RecordSuccess(string dsName)
        {
            if (_breakers.TryGetValue(dsName, out var b))
                b.RecordSuccess();
        }

        /// <inheritdoc/>
        public void RecordFailure(string dsName, ProxyErrorSeverity severity = ProxyErrorSeverity.Medium)
        {
            if (_breakers.TryGetValue(dsName, out var b))
                b.RecordFailure(severity);
        }

        /// <inheritdoc/>
        public void Initialize(string dsName, int failureThreshold, TimeSpan resetTimeout, int successThreshold = 2)
        {
            _configs[dsName] = (failureThreshold, resetTimeout, successThreshold);
            _breakers[dsName] = new CircuitBreaker(failureThreshold, resetTimeout, successThreshold);
        }

        /// <inheritdoc/>
        public void Remove(string dsName)
        {
            _breakers.TryRemove(dsName, out _);
            _configs.TryRemove(dsName, out _);
        }

        /// <inheritdoc/>
        public CircuitBreakerState GetState(string dsName)
            => _breakers.TryGetValue(dsName, out var b) ? b.State : CircuitBreakerState.Closed;

        /// <inheritdoc/>
        public void ForceOpen(string dsName)
        {
            // A Critical failure immediately breaches the threshold (sets count = threshold),
            // which trips the circuit to Open from any state (Closed or HalfOpen).
            if (_breakers.TryGetValue(dsName, out var b))
                b.RecordFailure(ProxyErrorSeverity.Critical);
        }

        /// <inheritdoc/>
        public void Reset(string dsName)
        {
            if (_breakers.TryGetValue(dsName, out var b))
                b.Reset();
        }
    }
}
