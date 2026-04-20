using System;
using Microsoft.Extensions.DependencyInjection;
using TheTechIdea.Beep.Services.Audit;
using TheTechIdea.Beep.Services.Logging;
using TheTechIdea.Beep.Services.Telemetry;
using TheTechIdea.Beep.Services.Telemetry.Presets;
using TheTechIdea.Beep.Services.Telemetry.Retention;
using TheTechIdea.Beep.Services.Telemetry.Sinks.Platform;

namespace TheTechIdea.Beep.Services
{
    /// <summary>
    /// .NET MAUI registration helpers (Android / iOS / Mac Catalyst /
    /// Windows). Mobile flash storage is constrained, so the v1
    /// budgets from <see cref="PlatformBudgets"/> are deliberately
    /// modest and rotation is more aggressive than on desktop.
    /// </summary>
    /// <remarks>
    /// The library never references <c>Microsoft.Maui.Storage</c>
    /// directly so the core stays cross-target. Callers pass a
    /// delegate that returns
    /// <c>Microsoft.Maui.Storage.FileSystem.AppDataDirectory</c>:
    /// <code>
    /// services.AddBeepLoggingForMaui(() => Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "MyApp");
    /// </code>
    /// </remarks>
    public static class BeepServiceMauiExtensions
    {
        /// <summary>
        /// Registers the Beep logging feature with MAUI-friendly
        /// defaults backed by <see cref="MauiAppDataSink"/>.
        /// </summary>
        public static IServiceCollection AddBeepLoggingForMaui(
            this IServiceCollection services,
            Func<string> appDataDirectoryProvider,
            string appName = PlatformPaths.DefaultAppName,
            Action<BeepLoggingOptions> tweak = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (appDataDirectoryProvider is null)
            {
                throw new ArgumentNullException(nameof(appDataDirectoryProvider),
                    "MAUI logging requires a delegate returning FileSystem.AppDataDirectory.");
            }

            return services.AddBeepLogging(opt =>
            {
                opt.Enabled = true;
                opt.MinLevel = BeepLogLevel.Information;
                opt.QueueCapacity = PlatformBudgets.MauiQueueCapacity;
                opt.BackpressureMode = BackpressureMode.DropOldest;
                opt.StorageBudgetBytes = PlatformBudgets.MauiLogBytes;
                opt.Budget = new StorageBudget
                {
                    MaxTotalBytes = PlatformBudgets.MauiLogBytes,
                    OnBreach = BudgetBreachAction.DeleteOldest,
                    // Mobile CPU is more constrained than disk; skip
                    // compression so flush latency stays low.
                    CompressOnRotate = false
                };
                opt.EnableRetentionSweeper = true;
                opt.Sinks.Add(MauiAppDataSink.Create(
                    appDataDirectoryProvider: appDataDirectoryProvider,
                    appName: appName,
                    subfolder: "logs",
                    prefix: "beep",
                    maxFileBytes: PlatformBudgets.MauiMaxFileBytes,
                    name: "maui-log"));

                tweak?.Invoke(opt);
            });
        }

        /// <summary>
        /// Registers the Beep audit feature with MAUI-friendly defaults.
        /// </summary>
        public static IServiceCollection AddBeepAuditForMaui(
            this IServiceCollection services,
            Func<string> appDataDirectoryProvider,
            string appName = PlatformPaths.DefaultAppName,
            Action<BeepAuditOptions> tweak = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (appDataDirectoryProvider is null)
            {
                throw new ArgumentNullException(nameof(appDataDirectoryProvider),
                    "MAUI audit requires a delegate returning FileSystem.AppDataDirectory.");
            }

            return services.AddBeepAudit(opt =>
            {
                opt.Enabled = true;
                opt.QueueCapacity = PlatformBudgets.MauiQueueCapacity;
                opt.BackpressureMode = BackpressureMode.Block;
                opt.StorageBudgetBytes = PlatformBudgets.MauiAuditBytes;
                opt.Budget = new StorageBudget
                {
                    MaxTotalBytes = PlatformBudgets.MauiAuditBytes,
                    OnBreach = BudgetBreachAction.BlockNewWrites,
                    CompressOnRotate = false
                };
                opt.HashChain = true;
                opt.Sinks.Add(MauiAppDataSink.Create(
                    appDataDirectoryProvider: appDataDirectoryProvider,
                    appName: appName,
                    subfolder: "audit",
                    prefix: "audit",
                    maxFileBytes: PlatformBudgets.MauiMaxFileBytes,
                    name: "maui-audit"));

                tweak?.Invoke(opt);
            });
        }
    }
}
