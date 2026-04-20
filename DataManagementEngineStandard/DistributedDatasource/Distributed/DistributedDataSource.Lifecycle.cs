using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// <see cref="DistributedDataSource"/> partial — connection
    /// lifecycle (<see cref="Openconnection"/>, <see cref="Closeconnection"/>)
    /// and <see cref="IDisposable.Dispose"/> orchestration across the
    /// composed shard clusters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Openconnection"/> walks every shard cluster and
    /// requests <c>Openconnection</c>; the aggregate state is
    /// <see cref="ConnectionState.Open"/> only when all shards opened
    /// successfully, otherwise it falls back to
    /// <see cref="ConnectionState.Broken"/>. When
    /// <see cref="DistributedDataSourceOptions.EagerOpenShards"/> is
    /// <c>false</c> the method short-circuits and reports
    /// <see cref="ConnectionState.Open"/> immediately so shards are
    /// opened lazily by the executors in later phases.
    /// </para>
    /// <para>
    /// <see cref="Dispose"/> is idempotent: a second call is a no-op.
    /// Disposal walks every shard cluster, swallows any individual
    /// failure (so a misbehaving cluster cannot block the others), and
    /// surfaces the aggregated message via <see cref="IDataSource.PassEvent"/>.
    /// </para>
    /// </remarks>
    public partial class DistributedDataSource
    {
        /// <inheritdoc/>
        public ConnectionState Openconnection()
        {
            ThrowIfDisposed();

            if (!_options.EagerOpenShards)
            {
                ConnectionStatus = ConnectionState.Open;
                return ConnectionStatus;
            }

            var failures = new List<string>();
            foreach (var kv in _shards)
            {
                try
                {
                    var state = kv.Value.Openconnection();
                    if (state != ConnectionState.Open && state != ConnectionState.Connecting)
                    {
                        failures.Add($"Shard '{kv.Key}' opened in state {state}.");
                    }
                }
                catch (Exception ex)
                {
                    failures.Add($"Shard '{kv.Key}' open failed: {ex.Message}");
                }
            }

            ConnectionStatus = failures.Count == 0
                ? ConnectionState.Open
                : ConnectionState.Broken;

            if (failures.Count > 0)
            {
                RaisePassEventSafe(
                    "DistributedDataSource open partial failure: " + string.Join("; ", failures));
            }

            // Phase 10 — kick off the background health monitor. Safe
            // to call whether or not EnableHealthMonitor is set; the
            // helper short-circuits when disabled.
            StartHealthMonitor();
            return ConnectionStatus;
        }

        /// <inheritdoc/>
        public ConnectionState Closeconnection()
        {
            if (_disposed) return ConnectionState.Closed;

            // Phase 10 — pause the background health monitor so it
            // does not observe shards mid-close.
            StopHealthMonitor();

            var failures = new List<string>();
            foreach (var kv in _shards)
            {
                try
                {
                    kv.Value.Closeconnection();
                }
                catch (Exception ex)
                {
                    failures.Add($"Shard '{kv.Key}' close failed: {ex.Message}");
                }
            }
            ConnectionStatus = ConnectionState.Closed;
            if (failures.Count > 0)
            {
                RaisePassEventSafe(
                    "DistributedDataSource close partial failure: " + string.Join("; ", failures));
            }
            return ConnectionStatus;
        }

        /// <summary>
        /// Idempotent disposal that closes every shard cluster and
        /// suppresses individual disposal failures so one misbehaving
        /// cluster cannot prevent the others from being released.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // Phase 10 — tear down the health monitor before the
            // shard map is cleared so any last tick has a coherent
            // view of state.
            DisposeResilience();

            // Phase 14 — release the concurrency gate and mitigator
            // subscriptions. Rate limiter is pure heap state so GC
            // handles it; the gate owns OS handles.
            try
            {
                if (_hotShardMitigator != null && _metricsAggregator != null)
                {
                    _hotShardMitigator.Detach(_metricsAggregator);
                }
            }
            catch { /* best-effort */ }
            try { _concurrencyGate?.Dispose(); } catch { /* best-effort */ }
            _concurrencyGate = null;

            var failures = new List<string>();
            foreach (var kv in _shards.ToArray())
            {
                try
                {
                    kv.Value.Dispose();
                }
                catch (Exception ex)
                {
                    failures.Add($"Shard '{kv.Key}' dispose failed: {ex.Message}");
                }
            }
            _shards.Clear();
            ConnectionStatus = ConnectionState.Closed;

            if (failures.Count > 0)
            {
                RaisePassEventSafe(
                    "DistributedDataSource dispose partial failure: " + string.Join("; ", failures));
            }

            GC.SuppressFinalize(this);
        }
    }
}
