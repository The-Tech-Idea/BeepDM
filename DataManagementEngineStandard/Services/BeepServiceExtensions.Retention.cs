using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TheTechIdea.Beep.Services.Telemetry.Retention;

namespace TheTechIdea.Beep.Services
{
    /// <summary>
    /// Registration helpers for the Phase 04 retention sweeper and
    /// budget enforcer. These are kept distinct from the logging /
    /// audit registrations so non-hosted callers never pay for a
    /// background timer they did not ask for.
    /// </summary>
    /// <remarks>
    /// The enforcer is registered as a singleton so multiple call sites
    /// (file sinks, audit producers, the hosted service, ad-hoc admin
    /// tools) share the same scope registry. Calling
    /// <see cref="AddBeepRetentionSweeper"/> more than once is a no-op
    /// after the first registration.
    /// </remarks>
    public static class BeepServiceRetentionExtensions
    {
        /// <summary>
        /// Registers <see cref="IBudgetEnforcer"/> as a singleton
        /// (<see cref="DefaultBudgetEnforcer"/>) without scheduling the
        /// hosted sweeper. Use when callers want to enforce on demand
        /// (e.g. in unit tests, on app shutdown, or from a CLI).
        /// </summary>
        public static IServiceCollection AddBeepBudgetEnforcer(this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton<IBudgetEnforcer, DefaultBudgetEnforcer>();
            return services;
        }

        /// <summary>
        /// Registers <see cref="IBudgetEnforcer"/> and schedules
        /// <see cref="RetentionSweeperHostedService"/> on the host's
        /// <c>IHostedService</c> pipeline.
        /// </summary>
        /// <param name="services">The service collection to extend.</param>
        /// <param name="sweepInterval">
        /// Cadence between sweeps. Defaults to
        /// <see cref="RetentionSweeperHostedService.DefaultSweepInterval"/>
        /// when omitted.
        /// </param>
        public static IServiceCollection AddBeepRetentionSweeper(
            this IServiceCollection services,
            TimeSpan? sweepInterval = null)
        {
            services.AddBeepBudgetEnforcer();

            services.AddSingleton<IHostedService>(sp =>
            {
                IBudgetEnforcer enforcer = sp.GetRequiredService<IBudgetEnforcer>();
                return new RetentionSweeperHostedService(enforcer, sweepInterval);
            });

            return services;
        }
    }
}
