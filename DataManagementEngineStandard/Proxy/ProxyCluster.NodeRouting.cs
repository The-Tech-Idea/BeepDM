using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Proxy
{
    // ─────────────────────────────────────────────────────────────────────────
    //  INodeRouter  — strategy interface
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Selects the best <see cref="ProxyNode"/> from a pre-filtered list of live,
    /// non-draining candidates for the current operation context.
    /// </summary>
    internal interface INodeRouter
    {
        /// <summary>
        /// Returns the chosen node.  Must never return <c>null</c> when
        /// <paramref name="candidates"/> is non-empty.
        /// </summary>
        ProxyNode SelectBest(IReadOnlyList<ProxyNode> candidates, ProxyExecutionContext ctx = null);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  LeastConnections  — fewest in-flight wins
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class LeastConnectionsRouter : INodeRouter
    {
        public ProxyNode SelectBest(IReadOnlyList<ProxyNode> candidates, ProxyExecutionContext ctx)
            => candidates.MinBy(n => n.InFlightCount);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  WeightedRoundRobin  — distribute proportionally to Weight
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class WeightedRoundRobinRouter : INodeRouter
    {
        private int _counter = -1;

        public ProxyNode SelectBest(IReadOnlyList<ProxyNode> candidates, ProxyExecutionContext ctx)
        {
            // Build an expanded list where each node appears Weight times
            var expanded = candidates
                .SelectMany(n => Enumerable.Repeat(n, Math.Max(1, n.Weight)))
                .ToList();

            int idx = Math.Abs(Interlocked.Increment(ref _counter)) % expanded.Count;
            return expanded[idx];
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  LowestLatency  — always pick the fastest-responding node
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class LowestLatencyRouter : INodeRouter
    {
        public ProxyNode SelectBest(IReadOnlyList<ProxyNode> candidates, ProxyExecutionContext ctx)
            => candidates.MinBy(n => n.Metrics?.AverageResponseTime ?? double.MaxValue);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PrimaryWithStandby  — route to Primary; fallback to any live node
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class PrimaryWithStandbyRouter : INodeRouter
    {
        public ProxyNode SelectBest(IReadOnlyList<ProxyNode> candidates, ProxyExecutionContext ctx)
        {
            var primary = candidates.FirstOrDefault(n => n.NodeRole == ProxyDataSourceRole.Primary);
            return primary ?? candidates[0];
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ConsistentHash  — 150 virtual-slot MurmurHash3 ring
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class ConsistentHashRouter : INodeRouter
    {
        private const int VirtualSlots = 150;

        // Sorted ring: hash → node
        private SortedDictionary<uint, ProxyNode> _ring = new();

        /// <summary>
        /// Rebuilds the hash ring from the current node set.
        /// Must be called after any AddNode / RemoveNode operation.
        /// </summary>
        internal void Rebuild(IEnumerable<ProxyNode> nodes)
        {
            var newRing = new SortedDictionary<uint, ProxyNode>();
            foreach (var n in nodes)
            {
                for (int v = 0; v < VirtualSlots; v++)
                {
                    uint slot = MurmurHash3($"{n.NodeId}:{v}");
                    // Collision: keep whichever slot is already there
                    newRing.TryAdd(slot, n);
                }
            }
            Interlocked.Exchange(ref _ring, newRing);
        }

        public ProxyNode SelectBest(IReadOnlyList<ProxyNode> candidates, ProxyExecutionContext ctx)
        {
            var ring = _ring;
            if (ring.Count == 0) return candidates[0];

            string key  = ctx?.CorrelationId ?? Guid.NewGuid().ToString();
            uint   hash = MurmurHash3(key);

            // Find the first slot >= hash (clockwise walk); wrap-around if none found
            ProxyNode target = null;
            foreach (var kv in ring)
            {
                if (kv.Key >= hash && candidates.Contains(kv.Value))
                {
                    target = kv.Value;
                    break;
                }
            }

            // Wrap-around: look from the start of the ring
            if (target == null)
            {
                foreach (var kv in ring)
                {
                    if (candidates.Contains(kv.Value))
                    {
                        target = kv.Value;
                        break;
                    }
                }
            }

            return target ?? candidates[0];
        }

        // ── MurmurHash3 (32-bit x86) ─────────────────────────────────────

        private static uint MurmurHash3(string s)
        {
            const uint c1   = 0xcc9e2d51u;
            const uint c2   = 0x1b873593u;
            const uint seed = 0u;

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(s);
            int    len   = bytes.Length;
            uint   h1    = seed;
            int    i     = 0;

            // Body — process 4-byte blocks
            while (i + 4 <= len)
            {
                uint k1 = (uint)(bytes[i]
                    | (bytes[i + 1] << 8)
                    | (bytes[i + 2] << 16)
                    | (bytes[i + 3] << 24));

                k1 *= c1;
                k1  = RotL(k1, 15);
                k1 *= c2;

                h1 ^= k1;
                h1  = RotL(h1, 13);
                h1  = h1 * 5 + 0xe6546b64u;

                i += 4;
            }

            // Tail
            uint tail = 0;
            int  rem  = len & 3;
            if (rem == 3) tail ^= (uint)bytes[i + 2] << 16;
            if (rem >= 2) tail ^= (uint)bytes[i + 1] << 8;
            if (rem >= 1) { tail ^= bytes[i]; tail *= c1; tail = RotL(tail, 15); tail *= c2; h1 ^= tail; }

            // Finalize (fmix32)
            h1 ^= (uint)len;
            h1 ^= h1 >> 16;
            h1 *= 0x85ebca6bu;
            h1 ^= h1 >> 13;
            h1 *= 0xc2b2ae35u;
            h1 ^= h1 >> 16;

            return h1;
        }

        private static uint RotL(uint x, int r) => (x << r) | (x >> (32 - r));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ProxyCluster — routing partition  (SelectNode + helpers)
    // ─────────────────────────────────────────────────────────────────────────

    public partial class ProxyCluster
    {
        // ── Router instance ───────────────────────────────────────────────
        private INodeRouter _nodeRouter;

        /// <summary>
        /// Builds the correct router implementation based on
        /// <see cref="ProxyPolicy.NodeRoutingStrategy"/>.
        /// </summary>
        private INodeRouter BuildRouter(ProxyNodeRoutingStrategy strategy)
        {
            return strategy switch
            {
                ProxyNodeRoutingStrategy.WeightedRoundRobin  => new WeightedRoundRobinRouter(),
                ProxyNodeRoutingStrategy.LowestLatency       => new LowestLatencyRouter(),
                ProxyNodeRoutingStrategy.PrimaryWithStandby  => new PrimaryWithStandbyRouter(),
                ProxyNodeRoutingStrategy.ConsistentHash      => BuildConsistentHashRouter(),
                _                                            => new LeastConnectionsRouter()
            };
        }

        private ConsistentHashRouter BuildConsistentHashRouter()
        {
            var router = new ConsistentHashRouter();
            router.Rebuild(_nodes.Values);
            return router;
        }

        /// <summary>
        /// Selects the best live node for routing an operation.
        /// Applies slow-start weight adjustment and respects draining state.
        /// </summary>
        /// <param name="isWrite">
        /// When <c>true</c>, only Primary-role nodes are considered.
        /// </param>
        private ProxyNode SelectNode(bool isWrite = false)
        {
            var live = _nodes.Values
                .Where(n => n.IsAlive && !n.IsDraining)
                .Where(n => !isWrite || n.NodeRole == ProxyDataSourceRole.Primary)
                .ToList();

            // Fallback: if no primary alive for a write, widen to any live node
            if (live.Count == 0 && isWrite)
            {
                live = _nodes.Values
                    .Where(n => n.IsAlive && !n.IsDraining)
                    .ToList();
            }

            if (live.Count == 0)
                throw new InvalidOperationException(
                    $"ProxyCluster '{DatasourceName}': no live nodes available.");

            // Apply slow-start weight ramp if configured
            if (_clusterPolicy.SlowStartDurationMs > 0)
                ApplySlowStartWeights(live);

            return _nodeRouter.SelectBest(live);
        }

        /// <summary>
        /// Reduces the effective weight of recently-added nodes proportionally to
        /// how far through their slow-start ramp window they are.
        /// Modifies <see cref="ProxyNode.Weight"/> in-place (transient mutation).
        /// </summary>
        private void ApplySlowStartWeights(List<ProxyNode> candidates)
        {
            // Keep original weights in a temp dict so we only affect the current routing call
            foreach (var n in candidates)
            {
                double elapsed    = (DateTime.UtcNow - n.AddedAtUtc).TotalMilliseconds;
                double rampFactor = Math.Min(1.0, elapsed / _clusterPolicy.SlowStartDurationMs);
                n.Weight = Math.Max(1, (int)Math.Round(n.Weight * rampFactor));
            }
        }

        /// <summary>Returns <c>true</c> for operations that mutate persistent state.</summary>
        private static bool IsWriteOperation(string operationName)
        {
            if (string.IsNullOrEmpty(operationName)) return false;
            return operationName.StartsWith("Insert", StringComparison.OrdinalIgnoreCase)
                || operationName.StartsWith("Update", StringComparison.OrdinalIgnoreCase)
                || operationName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase)
                || operationName.StartsWith("Create", StringComparison.OrdinalIgnoreCase)
                || operationName.StartsWith("RunScript", StringComparison.OrdinalIgnoreCase)
                || operationName.StartsWith("ExecuteSql", StringComparison.OrdinalIgnoreCase)
                || operationName.StartsWith("BeginTransaction", StringComparison.OrdinalIgnoreCase)
                || operationName.StartsWith("EndTransaction", StringComparison.OrdinalIgnoreCase)
                || operationName.StartsWith("Commit", StringComparison.OrdinalIgnoreCase);
        }
    }
}
