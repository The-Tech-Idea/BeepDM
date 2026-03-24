// ──────────────────────────────────────────────────────────────────────────────
//  ProxyClusterExamples.cs
//  Runnable examples showing how to create and use a ProxyCluster.
//
//  ProxyCluster is the cluster-tier orchestrator: it manages a pool of
//  ProxyNode objects, each backed by an IProxyDataSource (local or remote).
//  It extends everything ProxyDataSource provides with node management,
//  entity-affinity routing, traffic splits, hedging, scatter-gather,
//  fault injection, and distributed remote nodes (Phase 12).
// ──────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Proxy.Remote;

namespace TheTechIdea.Beep.Proxy.Examples
{
    /// <summary>
    /// Demonstrates the <see cref="ProxyCluster"/> API surface.
    /// All examples use the current constructor signature:
    ///   new ProxyCluster(IDMEEditor editor, string clusterName, ProxyPolicy policy = null)
    /// </summary>
    public static class ProxyClusterExamples
    {
        // ─────────────────────────────────────────────────────────────────────
        //  1.  Basic cluster construction
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Build a cluster from three local datasources, open, query, close.
        /// Constructor: new ProxyCluster(IDMEEditor editor, string clusterName, ProxyPolicy policy = null)
        /// </summary>
        public static void BasicClusterExample(IDMEEditor editor)
        {
            var proxy1 = editor.GetDataSource("orders-db1") as IProxyDataSource
                         ?? throw new InvalidOperationException("orders-db1 not found");
            var proxy2 = editor.GetDataSource("orders-db2") as IProxyDataSource
                         ?? throw new InvalidOperationException("orders-db2 not found");
            var proxy3 = editor.GetDataSource("orders-db3") as IProxyDataSource
                         ?? throw new InvalidOperationException("orders-db3 not found");

            var cluster = new ProxyCluster(editor, "orders-cluster");
            cluster.AddNode(new ProxyNode("n1", proxy1, weight: 5, role: ProxyDataSourceRole.Primary));
            cluster.AddNode(new ProxyNode("n2", proxy2, weight: 3, role: ProxyDataSourceRole.Replica));
            cluster.AddNode(new ProxyNode("n3", proxy3, weight: 2, role: ProxyDataSourceRole.Replica));

            cluster.Openconnection();

            var rows = cluster.GetEntity("Orders", new List<AppFilter>
            {
                new AppFilter { FieldName = "Status", Operator = "=", FilterValue = "Pending" }
            });

            foreach (var r in rows) Console.WriteLine(r);

            cluster.Dispose();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  2.  Cluster policy fan-out
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// <see cref="ProxyCluster.ApplyClusterPolicy"/> pushes the same
        /// <see cref="ProxyPolicy"/> down to every registered node.
        /// </summary>
        public static void ClusterPolicyFanOutExample(IDMEEditor editor)
        {
            var cluster = BuildThreeNodeCluster(editor);

            var policy = new ProxyPolicy
            {
                NodeRoutingStrategy = ProxyNodeRoutingStrategy.LeastConnections,
                Resilience = new ProxyResilienceProfile
                {
                    MaxRetries         = 3,
                    RetryBaseDelayMs   = 100,
                    FailureThreshold   = 5,
                    CircuitResetTimeout = TimeSpan.FromSeconds(20)
                },
                Cache = new ProxyCacheProfile
                {
                    Enabled           = true,
                    DefaultExpiration = TimeSpan.FromMinutes(2)
                }
            };

            cluster.ApplyClusterPolicy(policy);
            Console.WriteLine("Policy applied to all nodes.");

            cluster.Dispose();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  3.  Drain + rolling restart
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Drain one node at a time before restarting it — zero downtime.
        /// </summary>
        public static async Task RollingRestartExample(IDMEEditor editor)
        {
            var cluster = BuildThreeNodeCluster(editor);
            cluster.Openconnection();

            var nodes = cluster.GetNodes();
            foreach (var node in nodes)
            {
                Console.WriteLine($"Draining {node.NodeId}...");
                await cluster.DrainNodeAsync(node.NodeId, timeoutMs: 30_000);

                // Restart the backing datasource via the node's Proxy property
                node.Proxy.Closeconnection();
                await Task.Delay(500);
                node.Proxy.Openconnection();

                // Return to the routing pool by re-adding (drain is set via DrainNode internals)
                Console.WriteLine($"{node.NodeId} back online.");
            }

            cluster.Dispose();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  4.  Entity-affinity routing
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Pin specific entity types to dedicated nodes via <see cref="EntityAffinityMap"/>
        /// in the cluster <see cref="ProxyPolicy"/>.
        /// </summary>
        public static void EntityAffinityExample(IDMEEditor editor)
        {
            var affinity = new EntityAffinityMap();
            affinity.MapEntity("Invoices",    "n1");
            affinity.MapEntity("Payments",    "n1");
            affinity.MapEntity("CreditNotes", "n1");
            affinity.MapEntity("Products",    "n2");
            affinity.MapEntity("Categories",  "n2");

            var policy = new ProxyPolicy { EntityAffinity = affinity };
            var cluster = BuildThreeNodeCluster(editor, policy);

            cluster.Openconnection();

            var invoices = cluster.GetEntity("Invoices", null);
            Console.WriteLine($"Invoices from affinity node: {Count(invoices)}");

            cluster.Dispose();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  5.  Traffic split (canary / A/B test)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Route 10% of read traffic to a canary node via <see cref="TrafficSplitRule"/>.
        /// </summary>
        public static void TrafficSplitExample(IDMEEditor editor)
        {
            var policy = new ProxyPolicy
            {
                TrafficSplits = new[]
                {
                    new TrafficSplitRule
                    {
                        TargetNodeId   = "n3",
                        WeightPercent  = 10,
                        OperationScope = ProxySplitScope.ReadsOnly
                    }
                }
            };

            var cluster = BuildThreeNodeCluster(editor, policy);
            cluster.Openconnection();

            for (int i = 0; i < 100; i++)
                cluster.GetEntity("Recommendations", null);

            var metrics = cluster.GetClusterMetrics();
            foreach (var (id, m) in metrics)
                Console.WriteLine($"  {id}: {m.TotalRequests} reqs");

            cluster.Dispose();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  6.  Hedging
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// If a node has not responded within 100 ms, the cluster fires the same
        /// request on a second node and returns whichever answers first.
        /// Hedging is enabled via <see cref="ProxyPolicy.EnableHedging"/>.
        /// </summary>
        public static async Task HedgingExample(IDMEEditor editor)
        {
            var policy = new ProxyPolicy
            {
                EnableHedging      = true,
                HedgingThresholdMs = 100,
                MaxHedgeRequests   = 2
            };

            var cluster = BuildThreeNodeCluster(editor, policy);
            cluster.Openconnection();

            // GetEntityAsync triggers hedging when the first node is slow
            var result = await cluster.GetEntityAsync("UserProfiles", null);

            Console.WriteLine($"Got {Count(result)} profiles (hedged).");
            cluster.Dispose();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  7.  Scatter-gather (fan-out reads to all nodes)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Set <see cref="ProxyReadMode.ScatterGather"/> to broadcast every read to all
        /// live nodes in parallel and merge results automatically.
        /// </summary>
        public static async Task ScatterGatherExample(IDMEEditor editor)
        {
            var policy  = new ProxyPolicy { ReadMode = ProxyReadMode.ScatterGather };
            var cluster = BuildThreeNodeCluster(editor, policy);
            cluster.Openconnection();

            // With ReadMode = ScatterGather, GetEntityAsync fans out to all live nodes
            var allRows = await cluster.GetEntityAsync("AuditLog", new List<AppFilter>
            {
                new AppFilter { FieldName = "CreatedAt", Operator = ">=", FilterValue = "2024-01-01" }
            });

            Console.WriteLine($"Total audit rows from all nodes: {Count(allRows)}");
            cluster.Dispose();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  8.  Fault injection (testing / chaos engineering)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Inject 20% error rate and 150 ms delay on node n2 to verify failover.
        /// Configure via <see cref="FaultInjectionPolicy"/> in the cluster policy.
        /// Only use in test/staging environments.
        /// </summary>
        public static void FaultInjectionExample(IDMEEditor editor)
        {
            var policy = new ProxyPolicy
            {
                FaultInjection = new FaultInjectionPolicy
                {
                    ErrorRate    = 0.20,
                    DelayRate    = 0.20,
                    DelayMs      = 150,
                    TargetNodeId = "n2"
                }
            };

            var cluster = BuildThreeNodeCluster(editor, policy);
            cluster.Openconnection();

            int errors = 0;
            for (int i = 0; i < 50; i++)
            {
                try   { cluster.GetEntity("Products", null); }
                catch { errors++; }
            }

            Console.WriteLine($"Observed {errors} errors with ~20% injection rate on n2.");
            cluster.Dispose();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  9.  Distributed remote nodes
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Build a cluster where each node runs on a different machine.
        /// Workers expose HTTP endpoints; this coordinator communicates via
        /// <see cref="HttpProxyTransport"/> + <see cref="RemoteProxyDataSource"/>.
        /// </summary>
        public static void DistributedRemoteNodesExample(IDMEEditor editor)
        {
            var transportA = new HttpProxyTransport("http://worker-a:5100", TimeSpan.FromSeconds(10));
            var transportB = new HttpProxyTransport("http://worker-b:5100", TimeSpan.FromSeconds(10));
            var transportC = new HttpProxyTransport("http://worker-c:5100", TimeSpan.FromSeconds(10));

            var remoteA = new RemoteProxyDataSource(transportA, "worker-a", editor);
            var remoteB = new RemoteProxyDataSource(transportB, "worker-b", editor);
            var remoteC = new RemoteProxyDataSource(transportC, "worker-c", editor);

            var cluster = new ProxyCluster(editor, "global-cluster");
            cluster.AddNode(new ProxyNode("worker-a", remoteA, weight: 5, role: ProxyDataSourceRole.Primary));
            cluster.AddNode(new ProxyNode("worker-b", remoteB, weight: 3, role: ProxyDataSourceRole.Replica));
            cluster.AddNode(new ProxyNode("worker-c", remoteC, weight: 2, role: ProxyDataSourceRole.Replica));

            cluster.ApplyClusterPolicy(new ProxyPolicy
            {
                NodeRoutingStrategy = ProxyNodeRoutingStrategy.WeightedRoundRobin,
                Resilience          = new ProxyResilienceProfile { MaxRetries = 3 }
            });

            cluster.Openconnection();

            var customers = cluster.GetEntity("Customers", null);
            Console.WriteLine($"Customers from distributed cluster: {Count(customers)}");

            cluster.Dispose();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  10. Aggregate metrics
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Collect per-node <see cref="DataSourceMetrics"/> from the entire cluster.
        /// </summary>
        public static void ClusterMetricsExample(IDMEEditor editor)
        {
            var cluster = BuildThreeNodeCluster(editor);
            cluster.Openconnection();

            for (int i = 0; i < 50; i++)
                cluster.GetEntity("Inventory", null);

            var metrics = cluster.GetClusterMetrics();
            Console.WriteLine("=== Cluster Metrics ===");
            foreach (var (nodeId, m) in metrics)
                Console.WriteLine(
                    $"  {nodeId,-12} total={m.TotalRequests,5}  " +
                    $"failed={m.FailedRequests,3}  avg={m.AverageResponseTime,6:F1}ms");

            var slos = cluster.GetClusterSloSnapshots();
            Console.WriteLine("=== SLO Snapshots ===");
            foreach (var s in slos)
                Console.WriteLine($"  {s.DataSourceName,-12} p95={s.P95LatencyMs:F1}ms  err={s.ErrorRatePercent:F1}%");

            cluster.Dispose();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  11. Node events
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Subscribe to cluster-level events to integrate with alerting.
        /// </summary>
        public static void ClusterEventsExample(IDMEEditor editor)
        {
            var cluster = BuildThreeNodeCluster(editor);

            cluster.OnNodeDown     += (s, e) => Console.WriteLine($"[ALERT] Node DOWN:    {e.NodeId} — {e.Reason}");
            cluster.OnNodeRestored += (s, e) => Console.WriteLine($"[INFO]  Node UP:      {e.NodeId}");
            cluster.OnNodePromoted += (s, e) => Console.WriteLine($"[INFO]  Promoted:     {e.DataSourceName} → {e.NewRole}");
            cluster.OnNodeDemoted  += (s, e) => Console.WriteLine($"[INFO]  Demoted:      {e.DataSourceName} → {e.NewRole}");
            cluster.OnFailover     += (s, e) =>
                Console.WriteLine($"[WARN]  Failover: {e.FromDataSource} → {e.ToDataSource}");

            cluster.Openconnection();

            // ... your application logic ...

            cluster.Dispose();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Factory helpers
        // ─────────────────────────────────────────────────────────────────────

        private static ProxyCluster BuildThreeNodeCluster(
            IDMEEditor editor, ProxyPolicy policy = null)
        {
            IProxyDataSource MakeNode(string dsName) =>
                new ProxyDataSource(
                    dmeEditor       : editor,
                    dataSourceNames : new List<string> { dsName });

            var cluster = new ProxyCluster(editor, "example-cluster", policy);
            cluster.AddNode(new ProxyNode("n1", MakeNode("db-node-1"), weight: 5, role: ProxyDataSourceRole.Primary));
            cluster.AddNode(new ProxyNode("n2", MakeNode("db-node-2"), weight: 3, role: ProxyDataSourceRole.Replica));
            cluster.AddNode(new ProxyNode("n3", MakeNode("db-node-3"), weight: 2, role: ProxyDataSourceRole.Replica));
            return cluster;
        }

        private static int Count(IEnumerable<object> seq)
        {
            int n = 0;
            if (seq != null) foreach (var _ in seq) n++;
            return n;
        }
    }
}

