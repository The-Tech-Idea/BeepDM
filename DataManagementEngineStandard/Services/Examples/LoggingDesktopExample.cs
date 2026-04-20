using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TheTechIdea.Beep.Services.Logging;
using TheTechIdea.Beep.Services.Telemetry;
using TheTechIdea.Beep.Services.Telemetry.Context;
using TheTechIdea.Beep.Services.Telemetry.Redaction;

namespace TheTechIdea.Beep.Services.Examples
{
    /// <summary>
    /// End-to-end sample of <c>AddBeepLoggingForDesktop</c> on a console
    /// or WinForms host. Intended to be copy-pasted into a new project's
    /// <c>Program.cs</c>; nothing here references desktop-specific
    /// frameworks so the file compiles inside the engine assembly.
    /// </summary>
    /// <remarks>
    /// Demonstrates the four elements every desktop sample app should
    /// show:
    /// <list type="number">
    ///   <item>DI registration with platform defaults.</item>
    ///   <item>A redaction preset attached to the options.</item>
    ///   <item>A correlation scope wrapping a unit of work.</item>
    ///   <item>A periodic flush followed by clean shutdown.</item>
    /// </list>
    /// </remarks>
    public static class LoggingDesktopExample
    {
        /// <summary>Suggested entry point name for the sample.</summary>
        public const string SampleAppName = "BeepLoggingDesktopSample";

        /// <summary>
        /// Builds a configured <see cref="IServiceProvider"/> with the
        /// desktop logging preset, runs <see cref="ExecuteAsync"/>, and
        /// performs a clean shutdown via <see cref="DisposeAsync"/>.
        /// </summary>
        public static async Task RunAsync(CancellationToken cancellationToken = default)
        {
            ServiceCollection services = new ServiceCollection();
            services.AddBeepLoggingForDesktop(SampleAppName, opt =>
            {
                opt.MinLevel = BeepLogLevel.Information;
                foreach (IRedactor redactor in DefaultRedactionPresets.LogsBalanced())
                {
                    opt.Redactors.Add(redactor);
                }
            });

            ServiceProvider provider = services.BuildServiceProvider();
            await using (provider.ConfigureAwait(false))
            {
                IBeepLog log = provider.GetRequiredService<IBeepLog>();
                await ExecuteAsync(log, cancellationToken).ConfigureAwait(false);
                await log.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Emits a representative workload: an info entry, a correlation
        /// scope around a "process order" work item, an error with
        /// exception, and a property bag containing a value the
        /// redaction preset will mask.
        /// </summary>
        public static async Task ExecuteAsync(IBeepLog log, CancellationToken cancellationToken = default)
        {
            if (log is null) { throw new ArgumentNullException(nameof(log)); }

            log.Info("Desktop sample starting", new { SampleAppName });

            using (BeepActivityScope.Begin("Order.Submit", new Dictionary<string, object>
            {
                ["orderId"] = "ORD-1042",
                ["channel"] = "desktop"
            }))
            {
                log.Info("Validating cart", new { itemCount = 3 });
                await Task.Delay(20, cancellationToken).ConfigureAwait(false);

                Dictionary<string, object> sensitive = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["password"] = "p@ssw0rd",
                    ["amount"] = 199.95m
                };
                log.Info("Submitting order", sensitive);

                try
                {
                    throw new InvalidOperationException("Inventory check failed");
                }
                catch (InvalidOperationException ex)
                {
                    log.Error("Order rejected", ex, new { orderId = "ORD-1042" });
                }
            }

            log.Info("Desktop sample finished");
        }
    }
}
