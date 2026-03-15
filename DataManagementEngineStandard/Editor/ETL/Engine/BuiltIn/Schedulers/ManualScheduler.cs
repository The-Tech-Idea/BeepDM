using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Attributes;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Engine.BuiltIn.Schedulers
{
    /// <summary>
    /// A no-op scheduler that never fires on its own.
    /// Pipelines using this scheduler can only be triggered programmatically via
    /// <c>SchedulerHost.TriggerManualAsync</c> or by calling <see cref="FireAsync"/>.
    ///
    /// Config parameters:
    /// <list type="bullet">
    ///   <item><c>PipelineId</c> — pipeline to trigger (injected by SchedulerHost)</item>
    /// </list>
    /// </summary>
    [PipelinePlugin(
        "beep.schedule.manual",
        "Manual Scheduler",
        PipelinePluginType.Scheduler,
        Category = "Schedule",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class ManualScheduler : IPipelineScheduler
    {
        public string PluginId    => "beep.schedule.manual";
        public string DisplayName => "Manual Scheduler";
        public string Description => "A no-op scheduler — fires only on explicit demand.";

        private string _pipelineId = string.Empty;

        public event EventHandler<PipelineTriggerArgs>? Triggered;

        // ── IPipelinePlugin ────────────────────────────────────────────────────

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef { Name = "PipelineId", Type = ParamType.String, IsRequired = true }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("PipelineId", out var p))
                _pipelineId = p?.ToString() ?? "";
        }

        // ── IPipelineScheduler ─────────────────────────────────────────────────

        public Task StartAsync(CancellationToken token) => Task.CompletedTask;
        public Task StopAsync()                         => Task.CompletedTask;

        // ── Programmatic trigger ───────────────────────────────────────────────

        /// <summary>
        /// Fires the <see cref="Triggered"/> event immediately with optional override parameters.
        /// Can be called directly when the SchedulerHost is not managing this scheduler.
        /// </summary>
        public Task FireAsync(IReadOnlyDictionary<string, object>? parameters = null)
        {
            Triggered?.Invoke(this, new PipelineTriggerArgs
            {
                PipelineId    = _pipelineId,
                TriggerSource = "manual",
                Parameters    = parameters ?? new Dictionary<string, object>()
            });
            return Task.CompletedTask;
        }
    }
}
