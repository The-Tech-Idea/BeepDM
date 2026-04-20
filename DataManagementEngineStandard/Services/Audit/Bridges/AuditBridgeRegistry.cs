using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Services.Audit.Bridges
{
    /// <summary>
    /// Centralizes registration of every audit bridge so apps don't have
    /// to wire each one separately. Invoked from
    /// <c>BeepServiceAuditExtensions.AddBeepAudit</c> based on the flags
    /// exposed by <see cref="BeepAuditOptions"/>.
    /// </summary>
    /// <remarks>
    /// Every bridge is registered as a singleton so it can be resolved by
    /// the legacy subsystems (forms manager, proxy datasource) on demand.
    /// Registration is **idempotent** (uses
    /// <see cref="ServiceCollectionDescriptorExtensions.TryAddSingleton{TService}"/>),
    /// so calling the helper from multiple test fixtures or composition
    /// roots is safe.
    /// </remarks>
    public static class AuditBridgeRegistry
    {
        /// <summary>
        /// Registers every bridge enabled by <paramref name="options"/>.
        /// Must be called after the unified <see cref="IBeepAudit"/> has
        /// already been registered.
        /// </summary>
        /// <param name="services">DI container.</param>
        /// <param name="options">Active audit options.</param>
        public static void Register(IServiceCollection services, BeepAuditOptions options)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (options is null) throw new ArgumentNullException(nameof(options));

            if (!options.Enabled)
            {
                return;
            }

            if (options.BridgeForms)
            {
                services.TryAddSingleton(sp =>
                    new FormsAuditBridge(sp.GetRequiredService<IBeepAudit>()));
            }

            if (options.BridgeProxy)
            {
                services.TryAddSingleton(sp =>
                    new ProxyAuditBridge(sp.GetRequiredService<IBeepAudit>()));

                services.TryAddSingleton<IProxyAuditSink>(sp =>
                    sp.GetRequiredService<ProxyAuditBridge>());
            }

            if (options.BridgeDistributed)
            {
                services.TryAddSingleton(sp =>
                    new DistributedAuditBridge(sp.GetRequiredService<IBeepAudit>()));
            }
        }
    }
}
