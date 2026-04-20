using System;
using System.IO;
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
    /// ASP.NET Core / server-side registration helpers. They lock the
    /// v1 web budgets from <see cref="PlatformBudgets"/>, point file
    /// sinks at <c>{contentRoot}/logs</c> (or an explicit path the
    /// caller supplies for read-only container deploys), and use a
    /// larger queue capacity than the desktop preset because web
    /// hosts typically run higher-throughput pipelines.
    /// </summary>
    /// <remarks>
    /// The library deliberately does not reference
    /// <c>Microsoft.AspNetCore.Hosting</c> so it builds against pure
    /// netN TFMs. Callers that have an <c>IWebHostEnvironment</c>
    /// pass <c>env.ContentRootPath</c> explicitly:
    /// <code>
    /// services.AddBeepLoggingForWeb(builder.Environment.ContentRootPath, "MyApp");
    /// </code>
    /// </remarks>
    public static class BeepServiceWebExtensions
    {
        /// <summary>
        /// Registers the Beep logging feature with web-friendly defaults.
        /// </summary>
        /// <param name="services">Service collection to extend.</param>
        /// <param name="contentRootPath">
        /// Writable per-app root (typically <c>IWebHostEnvironment.ContentRootPath</c>).
        /// Pass <c>null</c> to fall back to <see cref="PlatformPaths.LogsDir(string,string)"/>.
        /// </param>
        /// <param name="appName">App name for the per-user fallback path.</param>
        /// <param name="tweak">Optional override callback.</param>
        public static IServiceCollection AddBeepLoggingForWeb(
            this IServiceCollection services,
            string contentRootPath = null,
            string appName = PlatformPaths.DefaultAppName,
            Action<BeepLoggingOptions> tweak = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            string directory = ResolveDirectory(contentRootPath, appName, "logs");

            return services.AddBeepLogging(opt =>
            {
                opt.Enabled = true;
                opt.MinLevel = BeepLogLevel.Information;
                opt.QueueCapacity = PlatformBudgets.WebQueueCapacity;
                opt.BackpressureMode = BackpressureMode.DropOldest;
                opt.StorageBudgetBytes = PlatformBudgets.WebLogBytes;
                opt.Budget = new StorageBudget
                {
                    MaxTotalBytes = PlatformBudgets.WebLogBytes,
                    OnBreach = BudgetBreachAction.DeleteOldest,
                    CompressOnRotate = true
                };
                opt.EnableRetentionSweeper = true;
                opt.Sinks.Add(new FileRollingSink(
                    directory: directory,
                    prefix: "beep",
                    maxFileBytes: PlatformBudgets.WebMaxFileBytes,
                    name: "web-file"));

                tweak?.Invoke(opt);
            });
        }

        /// <summary>
        /// Registers the Beep audit feature with web-friendly defaults.
        /// </summary>
        public static IServiceCollection AddBeepAuditForWeb(
            this IServiceCollection services,
            string contentRootPath = null,
            string appName = PlatformPaths.DefaultAppName,
            Action<BeepAuditOptions> tweak = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            string directory = ResolveDirectory(contentRootPath, appName, "audit");

            return services.AddBeepAudit(opt =>
            {
                opt.Enabled = true;
                opt.QueueCapacity = PlatformBudgets.WebQueueCapacity;
                opt.BackpressureMode = BackpressureMode.Block;
                opt.StorageBudgetBytes = PlatformBudgets.WebAuditBytes;
                opt.Budget = new StorageBudget
                {
                    MaxTotalBytes = PlatformBudgets.WebAuditBytes,
                    OnBreach = BudgetBreachAction.BlockNewWrites,
                    CompressOnRotate = true
                };
                opt.HashChain = true;
                opt.Sinks.Add(new FileRollingSink(
                    directory: directory,
                    prefix: "audit",
                    maxFileBytes: PlatformBudgets.WebMaxFileBytes,
                    name: "web-audit-file"));

                tweak?.Invoke(opt);
            });
        }

        private static string ResolveDirectory(string contentRootPath, string appName, string subfolder)
        {
            if (string.IsNullOrWhiteSpace(contentRootPath))
            {
                return subfolder == "audit"
                    ? PlatformPaths.AuditDir(appName)
                    : PlatformPaths.LogsDir(appName);
            }
            string full = Path.Combine(contentRootPath, subfolder);
            try
            {
                if (!Directory.Exists(full))
                {
                    Directory.CreateDirectory(full);
                }
            }
            catch
            {
                // Container deploys may forbid creation under ContentRoot.
                // Fall back to the per-user path so the sink can still
                // open a writable file on first write.
                return subfolder == "audit"
                    ? PlatformPaths.AuditDir(appName)
                    : PlatformPaths.LogsDir(appName);
            }
            return full;
        }
    }
}
