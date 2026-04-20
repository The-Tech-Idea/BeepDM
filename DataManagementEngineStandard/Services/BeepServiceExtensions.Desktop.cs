using System;
using Microsoft.Extensions.DependencyInjection;
using TheTechIdea.Beep.Services.Audit;
using TheTechIdea.Beep.Services.Logging;
using TheTechIdea.Beep.Services.Telemetry;
using TheTechIdea.Beep.Services.Telemetry.Presets;
using TheTechIdea.Beep.Services.Telemetry.Retention;
using TheTechIdea.Beep.Services.Telemetry.Sinks;

namespace TheTechIdea.Beep.Services
{
    /// <summary>
    /// Desktop registration helpers (WinForms / WPF / Console / Avalonia).
    /// They lock the v1 storage budgets from
    /// <see cref="PlatformBudgets"/>, point file sinks at the per-user
    /// <c>LocalApplicationData</c> path resolved by
    /// <see cref="PlatformPaths"/>, and then defer to
    /// <see cref="BeepServiceLoggingExtensions.AddBeepLogging"/> /
    /// <see cref="BeepServiceAuditExtensions.AddBeepAudit"/> so every
    /// downstream wiring (sinks, enrichers, redactors, retention,
    /// diagnostics) keeps a single canonical entry point.
    /// </summary>
    public static class BeepServiceDesktopExtensions
    {
        /// <summary>
        /// Registers the Beep logging feature with desktop-friendly
        /// defaults. The caller can still override every option via
        /// <paramref name="tweak"/>; preset values are applied first
        /// so the tweak callback always wins.
        /// </summary>
        public static IServiceCollection AddBeepLoggingForDesktop(
            this IServiceCollection services,
            string appName = PlatformPaths.DefaultAppName,
            Action<BeepLoggingOptions> tweak = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddBeepLogging(opt =>
            {
                opt.Enabled = true;
                opt.MinLevel = BeepLogLevel.Information;
                opt.QueueCapacity = PlatformBudgets.DesktopQueueCapacity;
                opt.BackpressureMode = BackpressureMode.DropOldest;
                opt.StorageBudgetBytes = PlatformBudgets.DesktopLogBytes;
                opt.Budget = new StorageBudget
                {
                    MaxTotalBytes = PlatformBudgets.DesktopLogBytes,
                    OnBreach = BudgetBreachAction.DeleteOldest,
                    CompressOnRotate = true
                };
                opt.EnableRetentionSweeper = true;
                opt.Sinks.Add(new FileRollingSink(
                    directory: PlatformPaths.LogsDir(appName),
                    prefix: "beep",
                    maxFileBytes: PlatformBudgets.DesktopMaxFileBytes,
                    name: "desktop-file"));

                tweak?.Invoke(opt);
            });
        }

        /// <summary>
        /// Registers the Beep audit feature with desktop-friendly
        /// defaults. Audit defaults to <see cref="BackpressureMode.Block"/>
        /// and <see cref="BudgetBreachAction.BlockNewWrites"/> because
        /// audit is lossless.
        /// </summary>
        public static IServiceCollection AddBeepAuditForDesktop(
            this IServiceCollection services,
            string appName = PlatformPaths.DefaultAppName,
            Action<BeepAuditOptions> tweak = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddBeepAudit(opt =>
            {
                opt.Enabled = true;
                opt.QueueCapacity = PlatformBudgets.DesktopQueueCapacity;
                opt.BackpressureMode = BackpressureMode.Block;
                opt.StorageBudgetBytes = PlatformBudgets.DesktopAuditBytes;
                opt.Budget = new StorageBudget
                {
                    MaxTotalBytes = PlatformBudgets.DesktopAuditBytes,
                    OnBreach = BudgetBreachAction.BlockNewWrites,
                    CompressOnRotate = true
                };
                opt.HashChain = true;
                opt.Sinks.Add(new FileRollingSink(
                    directory: PlatformPaths.AuditDir(appName),
                    prefix: "audit",
                    maxFileBytes: PlatformBudgets.DesktopMaxFileBytes,
                    name: "desktop-audit-file"));

                tweak?.Invoke(opt);
            });
        }
    }
}
