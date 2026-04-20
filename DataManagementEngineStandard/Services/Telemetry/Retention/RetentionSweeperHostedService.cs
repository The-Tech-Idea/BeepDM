using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace TheTechIdea.Beep.Services.Telemetry.Retention
{
    /// <summary>
    /// Long-running background service that periodically calls
    /// <see cref="IBudgetEnforcer.EnforceAllAsync"/> on every registered
    /// scope. Strictly opt-in: registration helpers add this service only
    /// when the operator turns on the retention sweeper.
    /// </summary>
    /// <remarks>
    /// The service uses <see cref="PeriodicTimer"/> for a low-allocation
    /// cadence and tolerates individual scope failures: a thrown
    /// exception is swallowed (the enforcer reports it through
    /// <see cref="IBudgetEnforcer.Swept"/>) so a single bad directory
    /// cannot take the host process down.
    /// </remarks>
    public sealed class RetentionSweeperHostedService : BackgroundService
    {
        /// <summary>Default cadence between sweeps (5 minutes).</summary>
        public static readonly TimeSpan DefaultSweepInterval = TimeSpan.FromMinutes(5);

        private readonly IBudgetEnforcer _enforcer;
        private readonly TimeSpan _sweepInterval;

        /// <summary>Creates the sweeper with an explicit interval.</summary>
        public RetentionSweeperHostedService(IBudgetEnforcer enforcer, TimeSpan? sweepInterval = null)
        {
            _enforcer = enforcer ?? throw new ArgumentNullException(nameof(enforcer));
            _sweepInterval = sweepInterval is { Ticks: > 0 } v ? v : DefaultSweepInterval;
        }

        /// <summary>Configured cadence between sweeps.</summary>
        public TimeSpan SweepInterval => _sweepInterval;

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Run a sweep immediately on startup so any pre-existing files
            // from a prior process get accounted for before the first
            // periodic tick.
            await SafeEnforceAllAsync(stoppingToken).ConfigureAwait(false);

            using PeriodicTimer timer = new PeriodicTimer(_sweepInterval);
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                {
                    await SafeEnforceAllAsync(stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // expected on shutdown
            }
        }

        private async Task SafeEnforceAllAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _enforcer.EnforceAllAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Per-scope errors are surfaced via the enforcer's Swept event.
                // We deliberately swallow so the timer keeps ticking.
            }
        }
    }
}
