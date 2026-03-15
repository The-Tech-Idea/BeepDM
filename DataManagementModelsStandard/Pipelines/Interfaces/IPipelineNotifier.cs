using System.Threading;
using System.Threading.Tasks;
using ObservabilityAlertEvent = TheTechIdea.Beep.Pipelines.Observability.AlertEvent;

namespace TheTechIdea.Beep.Pipelines.Interfaces
{
    /// <summary>
    /// Sends notifications (email, webhook, log file, etc.) when an alert rule fires.
    /// Implement and decorate with <c>[PipelinePlugin(..., PipelinePluginType.Notifier)]</c>
    /// to register in the plugin registry.
    /// </summary>
    public interface IPipelineNotifier : IPipelinePlugin
    {
        /// <summary>
        /// Deliver notification for the given alert event.
        /// Implementations must not throw — log failures and return gracefully.
        /// </summary>
        Task NotifyAsync(ObservabilityAlertEvent alertEvent, CancellationToken token);
    }
}
