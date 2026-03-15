using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Interfaces
{
    /// <summary>
    /// Triggers pipeline runs on a schedule or external event.
    /// Built-in implementations: CronScheduler, FileWatchScheduler, ManualScheduler.
    /// Custom schedulers are plugins decorated with <see cref="TheTechIdea.Beep.Pipelines.Attributes.PipelinePluginAttribute"/>.
    /// </summary>
    public interface IPipelineScheduler : IPipelinePlugin
    {
        /// <summary>Raised when the trigger condition fires.</summary>
        event EventHandler<PipelineTriggerArgs> Triggered;

        /// <summary>Start watching / waiting for the trigger condition.</summary>
        Task StartAsync(CancellationToken token);

        /// <summary>Stop the scheduler gracefully.</summary>
        Task StopAsync();
    }
}
