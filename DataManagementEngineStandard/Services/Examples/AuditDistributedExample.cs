using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TheTechIdea.Beep.Services.Audit;
using TheTechIdea.Beep.Services.Audit.Bridges;
using TheTechIdea.Beep.Services.Audit.Models;
using TheTechIdea.Beep.Services.Telemetry.Context;

namespace TheTechIdea.Beep.Services.Examples
{
    /// <summary>
    /// End-to-end sample showing how a distributed-tier subsystem
    /// (resharding planner, transaction coordinator, plan-change
    /// service) wires <c>AddBeepAuditForDesktop</c> and forwards
    /// events through <see cref="DistributedAuditBridge"/>. The bridge
    /// keeps the call sites strongly typed so they do not have to
    /// build <see cref="AuditEvent"/> instances by hand.
    /// </summary>
    /// <remarks>
    /// Demonstrates:
    /// <list type="number">
    ///   <item>DI registration with a stricter audit chain segment.</item>
    ///   <item>Three distinct distributed event flows (reshard, tx,
    ///         plan-change) recorded under category
    ///         <see cref="AuditCategory.Distributed"/>.</item>
    ///   <item>An explicit failure path that emits
    ///         <see cref="AuditOutcome.Failed"/> with a reason.</item>
    ///   <item>Final flush + integrity verify.</item>
    /// </list>
    /// </remarks>
    public static class AuditDistributedExample
    {
        /// <summary>Suggested entry-point name for the sample.</summary>
        public const string SampleAppName = "BeepAuditDistributedSample";

        /// <summary>
        /// Builds a configured <see cref="IServiceProvider"/>, runs the
        /// distributed workload, and verifies chain integrity at the
        /// end of the session.
        /// </summary>
        public static async Task<bool> RunAsync(CancellationToken cancellationToken = default)
        {
            ServiceCollection services = new ServiceCollection();
            services.AddBeepAuditForDesktop(SampleAppName, opt =>
            {
                opt.HashChain = true;
            });

            ServiceProvider provider = services.BuildServiceProvider();
            await using (provider.ConfigureAwait(false))
            {
                IBeepAudit audit = provider.GetRequiredService<IBeepAudit>();
                DistributedAuditBridge bridge = new DistributedAuditBridge(audit);
                await ExecuteAsync(bridge, cancellationToken).ConfigureAwait(false);
                await audit.FlushAsync(cancellationToken).ConfigureAwait(false);
                return await audit.VerifyIntegrityAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Drives a representative distributed workload through the
        /// bridge: a successful shard split, a successful global
        /// commit, a successful plan change, and finally a failed
        /// shard split that surfaces with a reason.
        /// </summary>
        public static async Task ExecuteAsync(DistributedAuditBridge bridge, CancellationToken cancellationToken = default)
        {
            if (bridge is null) { throw new ArgumentNullException(nameof(bridge)); }

            using (BeepActivityScope.Begin("Distributed.Workload"))
            {
                await bridge.ForwardAsync(
                    component: "Reshard",
                    operation: "SplitShard",
                    outcome: AuditOutcome.Success,
                    properties: new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["shard.from"] = "S1",
                        ["shard.toA"]  = "S1A",
                        ["shard.toB"]  = "S1B"
                    },
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                await bridge.ForwardAsync(
                    component: "Tx",
                    operation: "CommitGlobal",
                    outcome: AuditOutcome.Success,
                    properties: new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["tx.id"]   = "TX-7781",
                        ["tx.size"] = 12
                    },
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                await bridge.ForwardAsync(
                    component: "Plan",
                    operation: "RewriteJoinOrder",
                    outcome: AuditOutcome.Success,
                    properties: new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["plan.queryId"]   = "Q-9001",
                        ["plan.savedCost"] = 0.42
                    },
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                await bridge.ForwardAsync(
                    component: "Reshard",
                    operation: "SplitShard",
                    outcome: AuditOutcome.Failure,
                    properties: new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["shard.from"] = "S2",
                        ["error"]      = "TargetUnreachable"
                    },
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
