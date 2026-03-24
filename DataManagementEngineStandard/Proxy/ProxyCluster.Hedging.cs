using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Proxy
{
    /// <summary>
    /// ProxyCluster — request hedging + outlier detection partition (Phase 11.8).
    /// </summary>
    public partial class ProxyCluster
    {
        // ─────────────────────────────────────────────────────────────────
        //  Request hedging
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Fires <paramref name="operation"/> on <paramref name="primary"/>; if it hasn't
        /// completed within <see cref="ProxyPolicy.HedgingThresholdMs"/>, also fires it on
        /// <paramref name="hedge"/> in parallel and returns whichever resolves first.
        /// The slower task is cancelled via its linked <see cref="CancellationTokenSource"/>.
        /// </summary>
        internal static async Task<T> ExecuteWithHedging<T>(
            ProxyNode               primary,
            ProxyNode               hedge,
            Func<ProxyNode, Task<T>> operation,
            int                     thresholdMs,
            CancellationToken       ct = default)
        {
            using var primaryCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            using var hedgeCts   = CancellationTokenSource.CreateLinkedTokenSource(ct);
            using var delayCts   = CancellationTokenSource.CreateLinkedTokenSource(ct);

            primary.IncrementInFlight();
            var primaryTask = operation(primary);

            // Wait for either primary to finish or hedge-delay to expire
            var hedgeDelay    = Task.Delay(thresholdMs, delayCts.Token);
            var firstComplete = await Task.WhenAny(primaryTask, hedgeDelay).ConfigureAwait(false);

            if (firstComplete == primaryTask)
            {
                // Primary answered before hedge threshold — cancel the delay and return
                delayCts.Cancel();
                primary.DecrementInFlight();
                return await primaryTask.ConfigureAwait(false);
            }

            // Hedge delay fired — issue parallel hedge request
            hedge.IncrementInFlight();
            var hedgeTask = operation(hedge);

            var winner = await Task.WhenAny(primaryTask, hedgeTask).ConfigureAwait(false);

            // Cancel the loser
            if (winner == primaryTask) hedgeCts.Cancel();
            else                       primaryCts.Cancel();

            primary.DecrementInFlight();
            hedge.DecrementInFlight();

            return await winner.ConfigureAwait(false);
        }

        // ─────────────────────────────────────────────────────────────────
        //  Outcome recording — feeds outlier detection
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Records whether an operation on <paramref name="node"/> succeeded.
        /// Updates the per-node error-rate window used by outlier detection.
        /// </summary>
        internal void RecordNodeOutcome(ProxyNode node, bool success)
        {
            node.Metrics.IncrementTotalRequests();
            if (success)
                node.Metrics.IncrementSuccessfulRequests();
            else
                node.Metrics.IncrementFailedRequests();

            var od = _clusterPolicy.OutlierDetection;
            if (od is null) return;

            // Window roll-over
            var now = DateTime.UtcNow;
            if ((now - node.OutlierWindowStart).TotalMilliseconds > od.IntervalMs)
            {
                node.OutlierWindowRequests = 0;
                node.OutlierWindowErrors   = 0;
                node.OutlierWindowStart    = now;
            }

            node.OutlierWindowRequests++;
            if (!success)
            {
                node.OutlierWindowErrors++;
                node.OutlierErrorCount++;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Outlier detection — runs on the probe timer cycle
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates every node against the <see cref="OutlierDetectionPolicy"/>.
        /// Ejects nodes that exceed the error rate or consecutive error threshold,
        /// while never ejecting more than <see cref="OutlierDetectionPolicy.MaxEjectionPercent"/>.
        /// </summary>
        private void RunOutlierDetection()
        {
            var od = _clusterPolicy.OutlierDetection;
            if (od is null) return;

            var nodes = _nodes.Values.ToList();
            int total  = nodes.Count;
            if (total == 0) return;

            int alreadyEjected = nodes.Count(n =>
                n.OutlierEjectedUntil.HasValue && n.OutlierEjectedUntil > DateTime.UtcNow);

            int maxCanEject = Math.Max(0,
                (int)Math.Floor(total * od.MaxEjectionPercent / 100.0) - alreadyEjected);

            var now = DateTime.UtcNow;
            int ejectedThisCycle = 0;

            foreach (var node in nodes)
            {
                // Restore expired ejections first
                if (node.OutlierEjectedUntil.HasValue && node.OutlierEjectedUntil <= now)
                {
                    node.OutlierEjectedUntil = null;
                    Logger?.WriteLog($"[ProxyCluster] Outlier ejection expired: node '{node.NodeId}' restored to pool.");
                }

                if (ejectedThisCycle >= maxCanEject) continue;
                if (node.OutlierEjectedUntil.HasValue) continue;   // already ejected

                bool shouldEject = false;

                // Check 1: consecutive error threshold
                if (node.OutlierErrorCount >= od.ConsecutiveErrorThreshold)
                    shouldEject = true;

                // Check 2: error rate over the analysis interval
                if (!shouldEject
                    && node.OutlierWindowRequests > 0
                    && (double)node.OutlierWindowErrors / node.OutlierWindowRequests >= od.ErrorRateThreshold)
                    shouldEject = true;

                if (!shouldEject) continue;

                // Exponential back-off ejection time
                node.OutlierEjectionCount++;
                int ejectionMs = Math.Min(
                    od.BaseEjectionTimeMs * node.OutlierEjectionCount,
                    od.MaxEjectionTimeMs);

                node.OutlierEjectedUntil  = now.AddMilliseconds(ejectionMs);
                node.OutlierErrorCount    = 0;
                node.OutlierWindowErrors  = 0;
                node.OutlierWindowRequests = 0;
                node.OutlierWindowStart   = now;
                ejectedThisCycle++;

                Logger?.WriteLog(
                    $"[ProxyCluster] Outlier ejected: node '{node.NodeId}' for {ejectionMs} ms " +
                    $"(ejection #{node.OutlierEjectionCount}).");
            }
        }
    }
}
