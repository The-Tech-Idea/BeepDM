using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Helpers.ConnectionHelpers;

namespace TheTechIdea.Beep.Proxy
{
    /// <summary>
    /// Watchdog layer: active health probing of the write-primary on a tight cycle,
    /// automatic role promotion (Replica → Primary) when the current Primary fails,
    /// and demotion back to Replica when the original Primary recovers.
    /// Runs as a background <see cref="Task"/> and communicates via events.
    /// </summary>
    public partial class ProxyDataSource
    {
        // ── Watchdog state ────────────────────────────────────────────

        private CancellationTokenSource  _watchdogCts;
        private Task                     _watchdogTask;

        /// <summary>
        /// How often (ms) the watchdog probes the current write-primary.
        /// Defaults to a tighter cycle than the general health-check timer.
        /// </summary>
        public int WatchdogIntervalMs { get; set; } = 5_000;

        /// <summary>How many consecutive probe failures before a primary is declared dead.</summary>
        public int WatchdogFailureThreshold { get; set; } = 2;

        /// <summary>How many consecutive probe successes before a promoted primary is demoted back.</summary>
        public int WatchdogRecoveryThreshold { get; set; } = 3;

        /// <summary>
        /// Timeout in milliseconds for a single liveness probe call.
        /// Must be shorter than <see cref="WatchdogIntervalMs"/> so that a slow probe
        /// does not block the next loop iteration.
        /// Defaults to 2 000 ms (less than the 5 000 ms loop interval).
        /// </summary>
        public int WatchdogProbeTimeoutMs { get; set; } = 2_000;

        // ── Events ────────────────────────────────────────────────────

        /// <summary>Raised when the watchdog promotes a Replica to Primary role.</summary>
        public event EventHandler<RoleChangeEventArgs> OnRolePromoted;

        /// <summary>Raised when the watchdog demotes a recovered source back to its original role.</summary>
        public event EventHandler<RoleChangeEventArgs> OnRoleDemoted;

        // ── Tracking (per-source running counters) ─────────────────────

        // consecutive watchdog-level failures/successes, independent of the circuit breaker
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, int>
            _watchdogFailures  = new();
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, int>
            _watchdogSuccesses = new();

        // stores the "original" role before a watchdog promotion, so we can demote later
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, ProxyDataSourceRole>
            _originalRoles = new();

        // ── Lifecycle ─────────────────────────────────────────────────

        /// <summary>Starts the watchdog background loop.</summary>
        public void StartWatchdog()
        {
            if (_watchdogTask != null && !_watchdogTask.IsCompleted) return;

            _watchdogCts  = new CancellationTokenSource();
            _watchdogTask = Task.Run(() => WatchdogLoop(_watchdogCts.Token));
            _dmeEditor.AddLogMessage($"[Watchdog] Started (interval={WatchdogIntervalMs}ms, failThreshold={WatchdogFailureThreshold}).");
        }

        /// <summary>Stops the watchdog background loop.</summary>
        public void StopWatchdog()
        {
            _watchdogCts?.Cancel();
            try { _watchdogTask?.Wait(TimeSpan.FromSeconds(5)); } catch { /* best-effort */ }
            _watchdogTask = null;
            _watchdogCts?.Dispose();
            _watchdogCts  = null;
            _dmeEditor.AddLogMessage("[Watchdog] Stopped.");
        }

        // ── Main loop ─────────────────────────────────────────────────

        private async Task WatchdogLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(WatchdogIntervalMs, ct).ConfigureAwait(false);
                    ProbeAllNodes();
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _dmeEditor.AddLogMessage($"[Watchdog] Unexpected error: {ex.Message}");
                }
            }
        }

        // ── Node probing ──────────────────────────────────────────────

        private void ProbeAllNodes()
        {
            // Take a snapshot of names to avoid lock while iterating
            var names = new List<string>(_dataSourceNames);

            foreach (var dsName in names)
            {
                bool alive = ProbeDatasource(dsName);
                var  role  = _roles.GetValueOrDefault(dsName, ProxyDataSourceRole.Primary);

                if (!alive)
                {
                    HandleNodeFailure(dsName, role);
                }
                else
                {
                    HandleNodeRecovery(dsName, role);
                }
            }
        }

        private bool ProbeDatasource(string dsName)
        {
            try
            {
                var ds = _dmeEditor.GetDataSource(dsName);
                if (ds == null) return false;

                // Use WatchdogProbeTimeoutMs (not WatchdogIntervalMs) so a slow backend
                // cannot block the entire loop interval.
                // Task.Wait() is safe here: ProxyLivenessHelper.IsAlive is synchronous
                // and TestConnectionAsync wraps Task.FromResult — no deadlock risk.
                var task = TestConnectionHelper.TestConnectionAsync(ds, WatchdogProbeTimeoutMs);
                task.Wait();
                return task.Result.success;
            }
            catch
            {
                return false;
            }
        }

        // ── Failure handling: primary down → promote a replica ────────

        private void HandleNodeFailure(string dsName, ProxyDataSourceRole role)
        {
            int failures = _watchdogFailures.AddOrUpdate(dsName, 1, (_, v) => v + 1);
            _watchdogSuccesses[dsName] = 0;

            _dmeEditor.AddLogMessage($"[Watchdog] {dsName} probe failed ({failures}/{WatchdogFailureThreshold}).");

            if (failures < WatchdogFailureThreshold) return;

            if (role == ProxyDataSourceRole.Primary)
            {
                // Mark unhealthy to stop write/read traffic immediately
                _healthStatus[dsName] = false;

                // Find the best available replica or standby to promote
                var candidate = FindPromotionCandidate();
                if (candidate != null)
                    PromoteToWritePrimary(candidate, demotedPrimary: dsName);
                else
                    _dmeEditor.AddLogMessage("[Watchdog] Primary failed but no replica available for promotion!");
            }
        }

        // ── Recovery handling: original primary back → demote promoted ─

        private void HandleNodeRecovery(string dsName, ProxyDataSourceRole role)
        {
            _watchdogFailures[dsName] = 0;
            int successes = _watchdogSuccesses.AddOrUpdate(dsName, 1, (_, v) => v + 1);

            if (successes < WatchdogRecoveryThreshold) return;

            // Was this node the original primary before it was demoted by watchdog?
            bool wasOriginalPrimary = _originalRoles.TryGetValue(dsName, out var originalRole)
                                      && originalRole == ProxyDataSourceRole.Primary
                                      && role != ProxyDataSourceRole.Primary;   // currently not primary

            if (wasOriginalPrimary)
            {
                // Restore original primary; re-demote its temporary stand-in
                DemoteBackToOriginal(dsName);
            }
        }

        // ── Promotion / demotion ──────────────────────────────────────

        private string FindPromotionCandidate()
        {
            // Prefer Replica with best health, then Standby
            return _dataSourceNames
                .Where(n => _roles.GetValueOrDefault(n, ProxyDataSourceRole.Primary) != ProxyDataSourceRole.Primary
                         && _healthStatus.GetValueOrDefault(n, false))
                .OrderBy(n => _roles.GetValueOrDefault(n, ProxyDataSourceRole.Primary) == ProxyDataSourceRole.Replica ? 0 : 1)
                .ThenByDescending(n => _metrics.TryGetValue(n, out var m) ? m.SuccessfulRequests : 0)
                .FirstOrDefault();
        }

        private void PromoteToWritePrimary(string promoted, string demotedPrimary)
        {
            var oldRole = _roles.GetValueOrDefault(promoted, ProxyDataSourceRole.Replica);

            // Remember original roles so we can reverse later
            _originalRoles.TryAdd(demotedPrimary, ProxyDataSourceRole.Primary);
            _originalRoles.TryAdd(promoted, oldRole);

            // Perform the role swap
            _roles[demotedPrimary] = ProxyDataSourceRole.Replica;  // demote the dead primary
            _roles[promoted]       = ProxyDataSourceRole.Primary;   // promote the replica

            _dmeEditor.AddLogMessage($"[Watchdog] PROMOTED {promoted} (was {oldRole}) to Primary. Demoted {demotedPrimary}.");

            try
            {
                OnRolePromoted?.Invoke(this, new RoleChangeEventArgs
                {
                    DataSourceName = promoted,
                    OldRole        = oldRole,
                    NewRole        = ProxyDataSourceRole.Primary,
                    Reason         = $"Primary '{demotedPrimary}' failed watchdog threshold"
                });
            }
            catch { /* don't let subscriber exceptions crash the watchdog */ }
        }

        private void DemoteBackToOriginal(string originalPrimary)
        {
            // Find the currently-promoted stand-in and restore it
            var standIn = _dataSourceNames.FirstOrDefault(n =>
                _originalRoles.TryGetValue(n, out var orig)
                && orig != ProxyDataSourceRole.Primary
                && _roles.GetValueOrDefault(n) == ProxyDataSourceRole.Primary);

            _roles[originalPrimary] = ProxyDataSourceRole.Primary;

            if (standIn != null)
            {
                var standInOriginal = _originalRoles.GetValueOrDefault(standIn, ProxyDataSourceRole.Replica);
                _roles[standIn] = standInOriginal;

                _dmeEditor.AddLogMessage($"[Watchdog] RESTORED {originalPrimary} to Primary. Demoted stand-in {standIn} back to {standInOriginal}.");

                try
                {
                    OnRoleDemoted?.Invoke(this, new RoleChangeEventArgs
                    {
                        DataSourceName = standIn,
                        OldRole        = ProxyDataSourceRole.Primary,
                        NewRole        = standInOriginal,
                        Reason         = $"Original primary '{originalPrimary}' recovered"
                    });
                }
                catch { /* best-effort */ }
            }

            // Clean up tracking
            _originalRoles.TryRemove(originalPrimary, out _);
            if (standIn != null) _originalRoles.TryRemove(standIn, out _);
            _watchdogSuccesses[originalPrimary] = 0;
        }

        // ── Public status summary ─────────────────────────────────────

        /// <summary>
        /// Returns the current role and watchdog health of every registered datasource.
        /// </summary>
        public IReadOnlyList<WatchdogNodeStatus> GetWatchdogStatus()
        {
            return _dataSourceNames.Select(n => new WatchdogNodeStatus
            {
                DataSourceName       = n,
                Role                 = _roles.GetValueOrDefault(n, ProxyDataSourceRole.Primary),
                IsHealthy            = _healthStatus.GetValueOrDefault(n, false),
                WatchdogFailures     = _watchdogFailures.GetValueOrDefault(n, 0),
                WatchdogSuccesses    = _watchdogSuccesses.GetValueOrDefault(n, 0),
                IsCircuitOpen        = IsCircuitOpen(n)
            }).ToList();
        }
    }
}
