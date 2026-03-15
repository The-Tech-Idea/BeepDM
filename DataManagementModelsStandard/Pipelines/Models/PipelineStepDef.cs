using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Defines a single step inside a <see cref="PipelineDefinition"/>.
    /// Replaces <see cref="ETLScriptDet"/> — a superset that adds plugin identity,
    /// field mappings, filter expressions, and retry policy.
    /// </summary>
    public class PipelineStepDef
    {
        public string   Id         { get; set; } = Guid.NewGuid().ToString();
        public string   Name       { get; set; } = string.Empty;
        public int      Sequence   { get; set; }
        public StepKind Kind       { get; set; }
        public string   PluginId   { get; set; } = string.Empty;
        public bool     IsActive   { get; set; } = true;
        public bool     IsParallel { get; set; } = false;

        /// <summary>Plugin-specific configuration key-value pairs.</summary>
        public Dictionary<string, object> Config { get; set; } = new();

        /// <summary>
        /// Field mapping: key = destination field name, value = source expression.
        /// Supports simple column names and basic computed expressions.
        /// </summary>
        public Dictionary<string, string> FieldMappings { get; set; } = new();

        /// <summary>
        /// Optional filter predicate, e.g. <c>"Age &gt; 18 AND Country = 'US'"</c>.
        /// Evaluated by the engine before the step runs on each record.
        /// </summary>
        public string? FilterExpression { get; set; }

        /// <summary>
        /// Per-step maximum retry count.
        /// 0 means inherit from <see cref="PipelineDefinition.MaxRetries"/>.
        /// </summary>
        public int MaxRetries { get; set; } = 0;

        /// <summary>
        /// Per-step timeout in seconds.
        /// 0 means inherit from the run context.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 0;

        // ── Backward compat ───────────────────────────────────────────────
        /// <summary>
        /// Creates a <see cref="PipelineStepDef"/> from a legacy <see cref="ETLScriptDet"/>.
        /// </summary>
        public static PipelineStepDef FromLegacyScriptDet(ETLScriptDet det)
        {
            var kind = det.ScriptType switch
            {
                DDLScriptType.CreateEntity => StepKind.Extract,
                DDLScriptType.CopyData     => StepKind.Load,
                _                          => StepKind.Transform
            };

            return new PipelineStepDef
            {
                Name     = det.SourceEntityName ?? string.Empty,
                Kind     = kind,
                IsActive = det.Active,
                Config   = new Dictionary<string, object>
                {
                    ["SourceDataSource"]      = det.SourceDataSourceName ?? string.Empty,
                    ["SourceEntity"]          = det.SourceEntityName     ?? string.Empty,
                    ["DestinationDataSource"] = det.DestinationDataSourceName ?? string.Empty,
                    ["DestinationEntity"]     = det.DestinationEntityName     ?? string.Empty,
                    ["CopyData"]              = det.CopyData
                }
            };
        }
    }
}
