using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Attributes;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Engine.BuiltIn.Schedulers
{
    /// <summary>
    /// Fires a pipeline after one or more upstream pipelines have completed.
    /// The <see cref="SchedulerHost"/> delegates completion tracking to
    /// <c>DependencyGraph</c>; this scheduler type is used primarily as a marker
    /// so that the host knows to skip active scheduling (dependency-driven pipelines
    /// are enqueued by the host itself after upstream completion).
    ///
    /// Config parameters:
    /// <list type="bullet">
    ///   <item><c>PipelineId</c>  — pipeline to trigger (injected by SchedulerHost)</item>
    ///   <item><c>DependsOn</c>   — JSON array of schedule IDs, e.g. <c>["schedA","schedB"]</c></item>
    ///   <item><c>Condition</c>   — <c>ALL_SUCCESS</c> | <c>ANY_SUCCESS</c> | <c>ALL_COMPLETE</c>, default <c>ALL_SUCCESS</c></item>
    /// </list>
    /// </summary>
    [PipelinePlugin(
        "beep.schedule.dependency",
        "Pipeline Dependency Scheduler",
        PipelinePluginType.Scheduler,
        Category = "Schedule",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class PipelineDependencyScheduler : IPipelineScheduler
    {
        public string PluginId    => "beep.schedule.dependency";
        public string DisplayName => "Pipeline Dependency Scheduler";
        public string Description => "Fires a pipeline after designated upstream pipelines complete.";

        private string       _pipelineId = string.Empty;
        private List<string> _dependsOn  = new List<string>();
        private string       _condition  = "ALL_SUCCESS";

        public event EventHandler<PipelineTriggerArgs>? Triggered;

        // ── Public properties (used by SchedulerHost to configure DependencyGraph) ─

        /// <summary>Schedule IDs that must complete before this scheduler may fire.</summary>
        public IReadOnlyList<string> DependsOn => _dependsOn;

        /// <summary>Completion condition: ALL_SUCCESS | ANY_SUCCESS | ALL_COMPLETE.</summary>
        public string Condition => _condition;

        // ── IPipelinePlugin ────────────────────────────────────────────────────

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef { Name = "PipelineId", Type = ParamType.String,  IsRequired = true  },
            new PipelineParameterDef { Name = "DependsOn",  Type = ParamType.Json,    IsRequired = true  },
            new PipelineParameterDef { Name = "Condition",  Type = ParamType.String,  IsRequired = false, DefaultValue = "ALL_SUCCESS" }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("PipelineId", out var p))
                _pipelineId = p?.ToString() ?? "";

            if (parameters.TryGetValue("Condition", out var c))
                _condition = c?.ToString() ?? "ALL_SUCCESS";

            if (parameters.TryGetValue("DependsOn", out var d) && d != null)
            {
                string raw = d.ToString()!;
                // Accept either a JSON array string or a comma-separated list
                if (raw.TrimStart().StartsWith("[", StringComparison.Ordinal))
                {
                    var list = JsonSerializer.Deserialize<List<string>>(raw);
                    _dependsOn = list ?? new List<string>();
                }
                else
                {
                    _dependsOn = new List<string>(
                        raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(s => s.Trim()));
                }
            }
        }

        // ── IPipelineScheduler ─────────────────────────────────────────────────

        /// <summary>
        /// No active polling — the SchedulerHost drives this scheduler via
        /// <c>DependencyGraph.GetUnblockedSchedules()</c> after each upstream completion.
        /// </summary>
        public Task StartAsync(CancellationToken token) => Task.CompletedTask;
        public Task StopAsync()                         => Task.CompletedTask;

        // ── Package-internal helper ────────────────────────────────────────────

        /// <summary>
        /// Called by <c>SchedulerHost</c> when the dependency graph reports this
        /// schedule is unblocked.  Fires <see cref="Triggered"/> directly.
        /// </summary>
        internal void FireDependencyMet(IReadOnlyDictionary<string, object>? context = null)
        {
            Triggered?.Invoke(this, new PipelineTriggerArgs
            {
                PipelineId    = _pipelineId,
                TriggerSource = "dependency",
                Parameters    = context ?? new Dictionary<string, object>
                {
                    ["__depends_on"] = string.Join(",", _dependsOn),
                    ["__condition"]  = _condition
                }
            });
        }
    }
}
