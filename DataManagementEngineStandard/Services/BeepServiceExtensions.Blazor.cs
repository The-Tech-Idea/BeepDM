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
    /// Blazor WebAssembly registration helpers. They use the
    /// IndexedDB sink exclusively because the WASM sandbox cannot
    /// write to a real filesystem. The browser quota is shared with
    /// the page, so budgets are deliberately small (<see cref="PlatformBudgets.BlazorLogBytes"/> /
    /// <see cref="PlatformBudgets.BlazorAuditBytes"/>).
    /// </summary>
    /// <remarks>
    /// The host project is responsible for registering an
    /// <see cref="IIndexedDbBridge"/> implementation (typically a
    /// thin wrapper around <c>IJSRuntime.InvokeAsync</c>) before
    /// calling these helpers. The helpers will resolve the bridge
    /// from the service collection at registration time so the
    /// pipeline can build its sink.
    /// </remarks>
    public static class BeepServiceBlazorExtensions
    {
        /// <summary>
        /// Registers the Beep logging feature with Blazor WASM
        /// defaults. Requires an <see cref="IIndexedDbBridge"/>
        /// already registered in the service collection (the host
        /// project supplies a JS-interop implementation).
        /// </summary>
        public static IServiceCollection AddBeepLoggingForBlazor(
            this IServiceCollection services,
            IIndexedDbBridge bridge,
            Action<BeepLoggingOptions> tweak = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (bridge is null)
            {
                throw new ArgumentNullException(nameof(bridge),
                    "Blazor logging requires an IIndexedDbBridge supplied by the host project.");
            }

            return services.AddBeepLogging(opt =>
            {
                opt.Enabled = true;
                opt.MinLevel = BeepLogLevel.Information;
                opt.QueueCapacity = PlatformBudgets.BlazorQueueCapacity;
                opt.BackpressureMode = BackpressureMode.DropOldest;
                opt.StorageBudgetBytes = PlatformBudgets.BlazorLogBytes;
                opt.Budget = new StorageBudget
                {
                    MaxTotalBytes = PlatformBudgets.BlazorLogBytes,
                    OnBreach = BudgetBreachAction.DeleteOldest,
                    CompressOnRotate = false
                };
                // The IndexedDB sink prunes itself; the file-based
                // retention sweeper is unnecessary in WASM.
                opt.EnableRetentionSweeper = false;
                opt.Sinks.Add(new BlazorIndexedDbSink(
                    bridge: bridge,
                    storageBudgetBytes: PlatformBudgets.BlazorLogBytes,
                    name: "blazor-idb-log"));

                tweak?.Invoke(opt);
            });
        }

        /// <summary>
        /// Registers the Beep audit feature for Blazor WASM. Audit
        /// is opt-in on this host because IDB quota is shared with
        /// the page; callers must call this method explicitly.
        /// </summary>
        public static IServiceCollection AddBeepAuditForBlazor(
            this IServiceCollection services,
            IIndexedDbBridge bridge,
            Action<BeepAuditOptions> tweak = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (bridge is null)
            {
                throw new ArgumentNullException(nameof(bridge),
                    "Blazor audit requires an IIndexedDbBridge supplied by the host project.");
            }

            return services.AddBeepAudit(opt =>
            {
                opt.Enabled = true;
                opt.QueueCapacity = PlatformBudgets.BlazorQueueCapacity;
                opt.BackpressureMode = BackpressureMode.Block;
                opt.StorageBudgetBytes = PlatformBudgets.BlazorAuditBytes;
                opt.Budget = new StorageBudget
                {
                    MaxTotalBytes = PlatformBudgets.BlazorAuditBytes,
                    OnBreach = BudgetBreachAction.BlockNewWrites,
                    CompressOnRotate = false
                };
                opt.HashChain = true;
                opt.Sinks.Add(new BlazorIndexedDbSink(
                    bridge: bridge,
                    storageBudgetBytes: PlatformBudgets.BlazorAuditBytes,
                    name: "blazor-idb-audit"));

                tweak?.Invoke(opt);
            });
        }
    }
}
