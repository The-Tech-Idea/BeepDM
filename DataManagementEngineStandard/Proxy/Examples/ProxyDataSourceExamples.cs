// ──────────────────────────────────────────────────────────────────────────────
//  ProxyDataSourceExamples.cs
//  Runnable examples showing how to create and use a ProxyDataSource.
//
//  Every public static method is a self-contained scenario.
//  Methods intentionally do not throw — they log outcomes via editor.
// ──────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Proxy.Examples
{
    /// <summary>
    /// Demonstrates the full <see cref="ProxyDataSource"/> API surface.
    /// </summary>
    public static class ProxyDataSourceExamples
    {
        // ─────────────────────────────────────────────────────────────────────
        //  1.  Basic construction + read
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Create a <see cref="ProxyDataSource"/> over two pre-registered SQLite
        /// datasources (primary + replica), open the connection, and read rows.
        /// </summary>
        public static void BasicReadExample(IDMEEditor editor)
        {
            var proxy = new ProxyDataSource(
                dmeEditor       : editor,
                dataSourceNames : new List<string> { "orders-primary", "orders-replica" });

            proxy.Openconnection();

            // Read operation — routed to the healthiest source by policy
            var rows = proxy.GetEntity("Orders", new List<AppFilter>
            {
                new AppFilter { FieldName = "Status", Operator = "=", FilterValue = "Open" }
            });

            foreach (var row in rows)
                Console.WriteLine(row);

            proxy.Closeconnection();
            proxy.Dispose();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  2.  Policy configuration
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Apply a custom <see cref="ProxyPolicy"/> at construction time:
        /// weighted latency routing, 3 retries, 30-second circuit-breaker reset,
        /// with in-process caching enabled for 60 seconds.
        /// </summary>
        public static void PolicyExample(IDMEEditor editor)
        {
            var policy = new ProxyPolicy
            {
                RoutingStrategy = ProxyRoutingStrategy.WeightedLatency,
                Resilience = new ProxyResilienceProfile
                {
                    ProfileType          = ProxyResilienceProfileType.Custom,
                    MaxRetries           = 3,
                    RetryBaseDelayMs     = 200,
                    FailureThreshold     = 5,
                    CircuitResetTimeout  = TimeSpan.FromSeconds(30)
                },
                Cache = new ProxyCacheProfile
                {
                    Enabled           = true,
                    DefaultExpiration = TimeSpan.FromSeconds(60)
                }
            };

            var proxy = new ProxyDataSource(
                dmeEditor       : editor,
                dataSourceNames : new List<string> { "products-db1", "products-db2" },
                policy          : policy);

            proxy.Openconnection();

            var result = proxy.RunQuery("SELECT COUNT(*) FROM Products");
            Console.WriteLine($"Row count: {result}");

            proxy.Dispose();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  3.  Read / write routing
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads are routed to replicas; writes are sent exclusively to the primary.
        /// </summary>
        public static void ReadWriteRoutingExample(IDMEEditor editor)
        {
            var proxy = new ProxyDataSource(
                dmeEditor       : editor,
                dataSourceNames : new List<string> { "sales-primary", "sales-replica-1", "sales-replica-2" });

            proxy.Openconnection();

            // Read — may land on any healthy node
            var items = proxy.GetEntity("SaleItems", null);

            // Write — guaranteed to go to primary-role source
            var newOrder = new { OrderId = Guid.NewGuid(), Total = 99.99 };
            proxy.InsertEntity("Orders", newOrder);

            proxy.Dispose();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  4.  Caching
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Cache an entity for 5 minutes.  The second call returns the cached copy.
        /// </summary>
        public static void CachingExample(IDMEEditor editor)
        {
            var proxy = new ProxyDataSource(
                dmeEditor       : editor,
                dataSourceNames : new List<string> { "catalog-db" },
                policy          : new ProxyPolicy
                {
                    Cache = new ProxyCacheProfile { Enabled = true, DefaultExpiration = TimeSpan.FromMinutes(5) }
                });

            proxy.Openconnection();

            // First call: hits the database
            var products = proxy.GetEntityWithCache("Products", null, TimeSpan.FromMinutes(5));
            Console.WriteLine("From DB");

            // Second call within 5 min: served from in-process cache
            var productsAgain = proxy.GetEntityWithCache("Products", null, TimeSpan.FromMinutes(5));
            Console.WriteLine("From cache");

            // Explicit invalidation (e.g. after an update)
            proxy.InvalidateCache("Products");

            proxy.Dispose();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  5.  Watchdog auto-failover
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// The watchdog probes backends every 5 s; after 2 consecutive failures it
        /// marks the node unhealthy and promotes a replica to primary automatically.
        /// </summary>
        public static void WatchdogExample(IDMEEditor editor)
        {
            var proxy = new ProxyDataSource(
                dmeEditor       : editor,
                dataSourceNames : new List<string> { "hr-primary", "hr-standby" });

            proxy.WatchdogIntervalMs       = 5_000;
            proxy.WatchdogProbeTimeoutMs   = 2_000;
            proxy.WatchdogFailureThreshold = 2;
            proxy.WatchdogRecoveryThreshold = 3;

            proxy.OnRolePromoted += (s, e) =>
                Console.WriteLine($"[Watchdog] '{e.DataSourceName}' promoted to {e.NewRole}");

            proxy.OnRoleDemoted += (s, e) =>
                Console.WriteLine($"[Watchdog] '{e.DataSourceName}' demoted to {e.NewRole}");

            proxy.StartWatchdog();
            proxy.Openconnection();

            // ... your application logic runs here ...
            Thread.Sleep(60_000);  // watchdog probes in background

            proxy.StopWatchdog();
            proxy.Dispose();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  6.  Audit sink
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Attach a file-based audit sink — every operation is appended to
        /// an NDJSON file in the specified directory.
        /// </summary>
        public static void AuditSinkExample(IDMEEditor editor)
        {
            var proxy = new ProxyDataSource(
                dmeEditor       : editor,
                dataSourceNames : new List<string> { "finance-db" });

            proxy.AuditSink = new FileProxyAuditSink(".");
            proxy.Openconnection();

            proxy.InsertEntity("Transactions", new { Amount = 500m, Currency = "USD" });
            proxy.UpdateEntity("Accounts",     new { Balance = 1500m, AccountId = 42  });

            // Audit entries are flushed automatically on dispose
            proxy.Dispose();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  7.  Transactions
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Wrap a multi-step write in a distributed transaction.
        /// </summary>
        public static void TransactionExample(IDMEEditor editor)
        {
            var proxy = new ProxyDataSource(
                dmeEditor       : editor,
                dataSourceNames : new List<string> { "accounting-db" });

            proxy.Openconnection();

            var args = new PassedArgs();
            proxy.BeginTransaction(args);
            try
            {
                proxy.InsertEntity("Ledger", new { Debit = 200m, Account = "Cash" });
                proxy.InsertEntity("Ledger", new { Credit = 200m, Account = "Revenue" });
                proxy.Commit(args);
                Console.WriteLine("Transaction committed.");
            }
            catch
            {
                proxy.EndTransaction(args);   // rolls back
                Console.WriteLine("Transaction rolled back.");
                throw;
            }
            finally
            {
                proxy.Dispose();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  8.  Fan-out / scatter-gather reads
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Execute the same query against multiple nodes.  Filter by date on each
        /// datasource separately and merge the results in the caller.
        /// </summary>
        public static void FanOutExample(IDMEEditor editor)
        {
            var shardNames = new List<string> { "events-shard-us", "events-shard-eu", "events-shard-ap" };

            var results = new List<object>();
            foreach (var shardName in shardNames)
            {
                var shard = new ProxyDataSource(editor, new List<string> { shardName });
                shard.Openconnection();
                var rows = shard.GetEntity("Events", new List<AppFilter>
                {
                    new AppFilter { FieldName = "Date", Operator = ">=", FilterValue = "2024-01-01" }
                });
                results.AddRange(rows);
                shard.Dispose();
            }

            Console.WriteLine($"Total events from all shards: {results.Count}");
        }


        // ─────────────────────────────────────────────────────────────────────
        //  9.  Metrics & SLO
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Retrieve per-datasource latency, error rate, and SLO compliance.
        /// </summary>
        public static void MetricsExample(IDMEEditor editor)
        {
            var proxy = new ProxyDataSource(
                dmeEditor       : editor,
                dataSourceNames : new List<string> { "wh-primary", "wh-replica" });

            proxy.Openconnection();

            // Run some queries first so metrics are populated
            for (int i = 0; i < 20; i++)
                proxy.GetEntity("Products", null);

            var metrics = proxy.GetMetrics();
            foreach (var (name, m) in metrics)
                Console.WriteLine($"  {name}: avg={m.AverageResponseTime:F1}ms  errors={m.FailedRequests}");

            proxy.Dispose();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  10. Role management
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Manually promote a replica to primary (e.g. for planned maintenance).
        /// </summary>
        public static void RoleManagementExample(IDMEEditor editor)
        {
            var proxy = new ProxyDataSource(
                dmeEditor       : editor,
                dataSourceNames : new List<string> { "inv-primary", "inv-replica" });

            proxy.Openconnection();
            Console.WriteLine("Before maintenance:");
            PrintRoles(proxy);

            // Promote replica, demote old primary
            proxy.SetRole("inv-replica", ProxyDataSourceRole.Primary);
            proxy.SetRole("inv-primary", ProxyDataSourceRole.Replica);

            Console.WriteLine("After maintenance:");
            PrintRoles(proxy);

            proxy.Dispose();

            static void PrintRoles(ProxyDataSource p)
            {
                foreach (var name in p.EntitiesNames)
                    Console.WriteLine($"  {name}");
            }
        }

    }
}
