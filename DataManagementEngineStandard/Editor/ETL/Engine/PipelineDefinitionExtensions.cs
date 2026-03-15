using System.Collections.Generic;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Engine
{
    /// <summary>
    /// Fluent builder extension methods for <see cref="PipelineDefinition"/>.
    /// Enables concise, readable pipeline construction:
    /// <code>
    /// var pipeline = new PipelineDefinition()
    ///     .WithSource("beep.source.datasource", ("SourceDataSource", "northwind"), ("SourceEntity", "Orders"))
    ///     .AddTransformer("beep.transform.typecast", ("Casts", new { Amount = "System.Decimal" }))
    ///     .AddValidator("beep.validate.notnull", ("Fields", "OrderId,Amount"))
    ///     .WithSink("beep.sink.datasource", ("TargetDataSource", "warehouse"), ("TargetEntity", "FactOrders"))
    ///     .WithBatchSize(1000)
    ///     .WithRetries(3)
    ///     .Build();
    /// </code>
    /// </summary>
    public static class PipelineDefinitionExtensions
    {
        // ── Source ────────────────────────────────────────────────────────────

        /// <summary>
        /// Sets the pipeline source plugin and merges its parameters into <see cref="PipelineDefinition.Parameters"/>.
        /// </summary>
        public static PipelineDefinition WithSource(
            this PipelineDefinition pipeline,
            string pluginId,
            params (string Key, object Value)[] parameters)
        {
            pipeline.SourcePluginId = pluginId;
            foreach (var (k, v) in parameters)
                pipeline.Parameters[$"Source.{k}"] = v;
            return pipeline;
        }

        // ── Transformers ──────────────────────────────────────────────────────

        /// <summary>
        /// Appends a transformer step to the pipeline in the next available sequence position.
        /// </summary>
        public static PipelineDefinition AddTransformer(
            this PipelineDefinition pipeline,
            string pluginId,
            params (string Key, object Value)[] parameters)
            => pipeline.AddStep(StepKind.Transform, pluginId, parameters);

        // ── Validators ────────────────────────────────────────────────────────

        /// <summary>
        /// Appends a validator step to the pipeline in the next available sequence position.
        /// </summary>
        public static PipelineDefinition AddValidator(
            this PipelineDefinition pipeline,
            string pluginId,
            params (string Key, object Value)[] parameters)
            => pipeline.AddStep(StepKind.Validate, pluginId, parameters);

        // ── Sink ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Sets the pipeline primary sink plugin and merges its parameters.
        /// </summary>
        public static PipelineDefinition WithSink(
            this PipelineDefinition pipeline,
            string pluginId,
            params (string Key, object Value)[] parameters)
        {
            pipeline.SinkPluginId = pluginId;
            foreach (var (k, v) in parameters)
                pipeline.Parameters[$"Sink.{k}"] = v;
            return pipeline;
        }

        /// <summary>
        /// Sets the error sink plugin id (receives rejected records).
        /// Defaults to the built-in error-log sink when not specified.
        /// </summary>
        public static PipelineDefinition WithErrorSink(
            this PipelineDefinition pipeline,
            string pluginId = "beep.sink.errorlog")
        {
            pipeline.ErrorSinkPluginId = pluginId;
            return pipeline;
        }

        // ── Execution policy ──────────────────────────────────────────────────

        /// <summary>Sets the number of records processed per batch.</summary>
        public static PipelineDefinition WithBatchSize(this PipelineDefinition pipeline, int batchSize)
        {
            pipeline.BatchSize = batchSize;
            return pipeline;
        }

        /// <summary>Sets the maximum number of automatic retries per step on transient failure.</summary>
        public static PipelineDefinition WithRetries(this PipelineDefinition pipeline, int maxRetries)
        {
            pipeline.MaxRetries = maxRetries;
            return pipeline;
        }

        /// <summary>Enables or disables checkpoint persistence for resumable runs.</summary>
        public static PipelineDefinition WithCheckpointing(this PipelineDefinition pipeline, bool enabled = true)
        {
            pipeline.EnableCheckpointing = enabled;
            return pipeline;
        }

        /// <summary>Enables or disables column-level data lineage tracking.</summary>
        public static PipelineDefinition WithLineageTracking(this PipelineDefinition pipeline, bool enabled = true)
        {
            pipeline.EnableLineageTracking = enabled;
            return pipeline;
        }

        /// <summary>Sets the maximum consecutive error count that causes the run to abort. 0 = never abort.</summary>
        public static PipelineDefinition StopOnErrorCount(this PipelineDefinition pipeline, int count)
        {
            pipeline.StopOnErrorCount = count;
            return pipeline;
        }

        // ── Name / description ─────────────────────────────────────────────────

        /// <summary>Sets the human-readable name of this pipeline definition.</summary>
        public static PipelineDefinition Named(this PipelineDefinition pipeline, string name)
        {
            pipeline.Name = name;
            return pipeline;
        }

        /// <summary>Sets the description of this pipeline definition.</summary>
        public static PipelineDefinition WithDescription(this PipelineDefinition pipeline, string description)
        {
            pipeline.Description = description;
            return pipeline;
        }

        // ── Terminal ──────────────────────────────────────────────────────────

        /// <summary>
        /// Finalises the builder chain and returns the completed <see cref="PipelineDefinition"/>.
        /// No-op identity — present for fluent symmetry.
        /// </summary>
        public static PipelineDefinition Build(this PipelineDefinition pipeline) => pipeline;

        // ── Internal helper ──────────────────────────────────────────────────

        private static PipelineDefinition AddStep(
            this PipelineDefinition pipeline,
            StepKind kind,
            string pluginId,
            (string Key, object Value)[] parameters)
        {
            int seq = pipeline.Steps.Count + 1;
            var config = new Dictionary<string, object>();
            foreach (var (k, v) in parameters)
                config[k] = v;

            pipeline.Steps.Add(new PipelineStepDef
            {
                Sequence = seq,
                Kind     = kind,
                PluginId = pluginId,
                Config   = config
            });
            return pipeline;
        }
    }
}
