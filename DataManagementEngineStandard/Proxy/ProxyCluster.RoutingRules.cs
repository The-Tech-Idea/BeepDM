using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Proxy
{
    /// <summary>
    /// ProxyCluster — query routing rules + traffic-split partition (Phase 11.7).
    /// Evaluates declarative <see cref="ProxyRoutingRule"/> and <see cref="TrafficSplitRule"/>
    /// to override the default router for specific traffic patterns.
    /// </summary>
    public partial class ProxyCluster
    {
        // ── RNG for traffic split percentage rolls ────────────────────────
        private static readonly ThreadLocal<Random> _splitRng
            = new(() => new Random(Guid.NewGuid().GetHashCode()));

        // ── Sorted rule cache (rebuilt in ApplyClusterPolicy) ────────────
        private IReadOnlyList<ProxyRoutingRule> _sortedRoutingRules
            = Array.Empty<ProxyRoutingRule>();

        // ─────────────────────────────────────────────────────────────────
        //  Traffic split — canary percentage selection
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Checks whether the current request falls into a canary traffic-split bucket.
        /// Returns the split target <see cref="ProxyNode"/> or null if no split fired.
        /// </summary>
        private ProxyNode? TrySelectSplitNode(
            IReadOnlyList<ProxyNode> live,
            bool isWrite)
        {
            foreach (var rule in _clusterPolicy.TrafficSplits)
            {
                // Filter by operation scope
                if (rule.OperationScope == ProxySplitScope.ReadsOnly  && isWrite)  continue;
                if (rule.OperationScope == ProxySplitScope.WritesOnly && !isWrite) continue;

                if (_splitRng.Value!.Next(100) < rule.WeightPercent)
                {
                    var canary = live.FirstOrDefault(n => n.NodeId == rule.TargetNodeId);
                    if (canary is not null) return canary;
                }
            }
            return null;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Routing rule evaluation — first matching rule wins
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates sorted routing rules against the current operation.
        /// Returns the rule-selected node, or null if no rule matches.
        /// </summary>
        private ProxyNode? TrySelectFromRules(
            IReadOnlyList<ProxyNode> live,
            string operationName,
            string? entityHint)
        {
            foreach (var rule in _sortedRoutingRules)
            {
                bool opMatch = rule.OperationPattern is null
                    || Regex.IsMatch(operationName, rule.OperationPattern,
                                     RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                bool entityMatch = rule.EntityPattern is null
                    || (entityHint is not null
                        && Regex.IsMatch(entityHint, rule.EntityPattern,
                                         RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));

                if (!opMatch || !entityMatch) continue;

                // Rule matches — fixed target first
                if (rule.TargetNodeId is not null)
                {
                    var target = live.FirstOrDefault(n => n.NodeId == rule.TargetNodeId);
                    if (target is not null) return target;
                    // Target is dead — fall through to strategy
                }

                return rule.OverrideStrategy switch
                {
                    ProxyRoutingOverrideStrategy.RouteToReplica =>
                        live.FirstOrDefault(n => n.NodeRole == ProxyDataSourceRole.Replica)
                        ?? live.FirstOrDefault(),

                    ProxyRoutingOverrideStrategy.RouteToPrimary =>
                        live.FirstOrDefault(n => n.NodeRole == ProxyDataSourceRole.Primary)
                        ?? live.FirstOrDefault(),

                    _ => live.FirstOrDefault()   // RouteToAny
                };
            }
            return null;
        }

        // ─────────────────────────────────────────────────────────────────
        //  SelectNode overload that accepts operation + entity hints
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Extended node selection that evaluates entity-affinity, traffic splits and
        /// routing rules before falling back to the cluster's default router strategy.
        /// </summary>
        internal ProxyNode SelectNode(
            string  operationName,
            string? entityHint = null,
            ProxyExecutionContext? ctx = null)
        {
            bool isWrite = IsWriteOperation(operationName);

            var live = GetLiveNodes(isWrite);

            if (live.Count == 0)
                throw new InvalidOperationException(
                    $"ProxyCluster '{DatasourceName}': no live nodes available for '{operationName}'.");

            // 1. Entity-affinity map (Phase 11.6)
            if (_clusterPolicy.EntityAffinity is not null && entityHint is not null)
            {
                var affinityNodeId = _clusterPolicy.EntityAffinity.Resolve(entityHint);
                if (affinityNodeId is not null)
                {
                    var pinned = live.FirstOrDefault(n => n.NodeId == affinityNodeId);
                    if (pinned is not null) return pinned;

                    // Pinned node is unavailable — apply fallback policy
                    switch (_clusterPolicy.AffinityFallback)
                    {
                        case EntityAffinityFallback.ThrowException:
                            throw new InvalidOperationException(
                                $"ProxyCluster: affinity node '{affinityNodeId}' for entity '{entityHint}' is unavailable.");
                        case EntityAffinityFallback.WaitForOwner:
                            // Spin until the node comes back or we run out of patience
                            var deadline = DateTime.UtcNow.AddMilliseconds(
                                _clusterPolicy.NodeProbeIntervalMs * 2);
                            while (DateTime.UtcNow < deadline)
                            {
                                System.Threading.Thread.Sleep(50);
                                if (_nodes.TryGetValue(affinityNodeId, out var candidate)
                                    && candidate.IsAlive && !candidate.IsDraining)
                                    return candidate;
                            }
                            // Fall through to RouteToAny after timeout
                            break;
                        // RouteToAny: continue to normal selection below
                    }
                }
            }

            // 2. Traffic split (canary) — Phase 11.7
            var splitNode = TrySelectSplitNode(live, isWrite);
            if (splitNode is not null) return splitNode;

            // 3. Routing rules — Phase 11.7
            var ruleNode = TrySelectFromRules(live, operationName, entityHint);
            if (ruleNode is not null) return ruleNode;

            // 4. Session affinity (already handled in SelectNode(bool))
            if (_clusterPolicy.EnableNodeAffinity && ctx?.SessionKey is not null)
            {
                if (_affinityMap.TryGetValue(ctx.SessionKey, out var entry))
                {
                    var sticky = live.FirstOrDefault(n => n.NodeId == entry.NodeId);
                    if (sticky is not null)
                    {
                        _affinityMap[ctx.SessionKey] = (entry.NodeId, DateTime.UtcNow);
                        return sticky;
                    }
                }
            }

            // 5. Slow-start + default router
            if (_clusterPolicy.SlowStartDurationMs > 0)
                ApplySlowStartWeights(live);

            var chosen = _nodeRouter.SelectBest(live, ctx);

            // Record session affinity
            if (_clusterPolicy.EnableNodeAffinity && ctx?.SessionKey is not null)
                _affinityMap[ctx.SessionKey] = (chosen.NodeId, DateTime.UtcNow);

            return chosen;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Returns live, non-draining, non-ejected nodes.  Writes filter to primaries.</summary>
        internal List<ProxyNode> GetLiveNodes(bool writesOnly = false)
        {
            var now  = DateTime.UtcNow;
            var live = _nodes.Values
                .Where(n => n.IsAlive
                         && !n.IsDraining
                         && (n.OutlierEjectedUntil is null || n.OutlierEjectedUntil < now))
                .Where(n => !writesOnly || n.NodeRole == ProxyDataSourceRole.Primary)
                .ToList();

            // Fallback: no primaries for write → any live node
            if (live.Count == 0 && writesOnly)
                live = _nodes.Values
                    .Where(n => n.IsAlive && !n.IsDraining
                             && (n.OutlierEjectedUntil is null || n.OutlierEjectedUntil < now))
                    .ToList();

            return live;
        }

        /// <summary>
        /// Rebuild the sorted routing-rule cache.
        /// Called from <see cref="ApplyClusterPolicy"/> whenever policy changes.
        /// </summary>
        private void RebuildRoutingRuleCache()
        {
            _sortedRoutingRules = _clusterPolicy.RoutingRules
                .OrderByDescending(r => r.Priority)
                .ToList();
        }
    }
}
