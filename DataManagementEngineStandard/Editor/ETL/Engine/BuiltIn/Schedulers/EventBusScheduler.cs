using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Attributes;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;
using TheTechIdea.Beep.Pipelines.Scheduling;

namespace TheTechIdea.Beep.Pipelines.Engine.BuiltIn.Schedulers
{
    /// <summary>
    /// Fires a pipeline when a named event is published on <see cref="PipelineEventBus"/>.
    ///
    /// Config parameters:
    /// <list type="bullet">
    ///   <item><c>EventTopic</c>  — the bus topic to subscribe to (required)</item>
    ///   <item><c>PipelineId</c>  — pipeline to trigger (injected by SchedulerHost)</item>
    ///   <item><c>Filter</c>      — optional string; payload must contain this substring</item>
    /// </list>
    /// </summary>
    [PipelinePlugin(
        "beep.schedule.eventbus",
        "Event Bus Scheduler",
        PipelinePluginType.Scheduler,
        Category = "Schedule",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class EventBusScheduler : IPipelineScheduler
    {
        public string PluginId    => "beep.schedule.eventbus";
        public string DisplayName => "Event Bus Scheduler";
        public string Description => "Fires a pipeline when an event is published on the internal event bus.";

        private string _topic      = string.Empty;
        private string _pipelineId = string.Empty;
        private string _filter     = string.Empty;

        private Action<object?>? _handler;

        public event EventHandler<PipelineTriggerArgs>? Triggered;

        // ── IPipelinePlugin ────────────────────────────────────────────────────

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef { Name = "EventTopic",  Type = ParamType.String, IsRequired = true  },
            new PipelineParameterDef { Name = "PipelineId",  Type = ParamType.String, IsRequired = true  },
            new PipelineParameterDef { Name = "Filter",      Type = ParamType.String, IsRequired = false }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("EventTopic",  out var t)) _topic      = t?.ToString() ?? "";
            if (parameters.TryGetValue("PipelineId",  out var p)) _pipelineId = p?.ToString() ?? "";
            if (parameters.TryGetValue("Filter",      out var f)) _filter     = f?.ToString() ?? "";
        }

        // ── IPipelineScheduler ─────────────────────────────────────────────────

        public Task StartAsync(CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(_topic)) return Task.CompletedTask;

            _handler = payload =>
            {
                // Apply filter if specified
                if (!string.IsNullOrEmpty(_filter))
                {
                    var payloadStr = payload?.ToString() ?? "";
                    if (!payloadStr.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                        return;
                }

                var parms = new Dictionary<string, object>
                {
                    ["__event_topic"] = _topic
                };
                if (payload != null)
                    parms["__event_payload"] = payload;

                Triggered?.Invoke(this, new PipelineTriggerArgs
                {
                    PipelineId    = _pipelineId,
                    TriggerSource = "eventbus",
                    Parameters    = parms
                });
            };

            PipelineEventBus.Subscribe(_topic, _handler);

            token.Register(() =>
            {
                if (_handler != null)
                    PipelineEventBus.Unsubscribe(_topic, _handler);
            });

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            if (_handler != null && !string.IsNullOrWhiteSpace(_topic))
            {
                PipelineEventBus.Unsubscribe(_topic, _handler);
                _handler = null;
            }
            return Task.CompletedTask;
        }
    }
}
