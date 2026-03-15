using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;
using TheTechIdea.Beep.Pipelines.Registry;
using TheTechIdea.Beep.Pipelines.Observability;

namespace TheTechIdea.Beep.Pipelines.Engine
{
    /// <summary>
    /// Core pipeline runner.  Given a <see cref="PipelineDefinition"/> it:
    /// <list type="number">
    ///   <item>Resolves and configures source, transformer, validator, and sink plugins via the registry.</item>
    ///   <item>Streams records through each active step.</item>
    ///   <item>Writes accepted records to the main sink in configurable batches.</item>
    ///   <item>Routes rejected records to the error sink.</item>
    ///   <item>Saves checkpoints after each committed batch when enabled.</item>
    ///   <item>Records column-level lineage when enabled.</item>
    /// </list>
    /// </summary>
    public class PipelineEngine
    {
        private readonly IDMEEditor              _editor;
        private readonly PipelinePluginRegistry  _registry;
        private readonly PipelineCheckpointManager _checkpoints;
        private readonly PipelineLineageTracker  _lineage;
        private readonly PipelineStepRunner      _stepRunner;

        // ── Optional observability hooks (set by PipelineManager) ─────
        public ObservabilityStore? ObservabilityStore { get; set; }
        public MetricsEngine?      Metrics            { get; set; }
        public AlertingEngine?     Alerting           { get; set; }

        public PipelineEngine(IDMEEditor editor)
        {
            _editor      = editor  ?? throw new ArgumentNullException(nameof(editor));
            _registry    = new PipelinePluginRegistry(editor);
            _registry.Discover();
            _checkpoints = new PipelineCheckpointManager(editor);
            _lineage     = new PipelineLineageTracker(editor);
            _stepRunner  = new PipelineStepRunner();
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>Execute a pipeline definition and return the run result.</summary>
        public Task<PipelineRunResult> RunAsync(
            PipelineDefinition def,
            IProgress<PassedArgs>? progress = null,
            CancellationToken token = default,
            Dictionary<string, object>? overrideParams = null)
            => ExecuteAsync(def, progress, token, overrideParams, resumeCheckpoint: null);

        /// <summary>
        /// Resume a previously interrupted run from its last checkpoint.
        /// </summary>
        public async Task<PipelineRunResult> ResumeAsync(
            string checkpointId,
            IProgress<PassedArgs>? progress = null,
            CancellationToken token = default)
        {
            var cp = await _checkpoints.LoadAsync(checkpointId);
            if (cp == null)
                throw new InvalidOperationException($"Checkpoint '{checkpointId}' not found.");

            // Load the definition by id via caller — we surface this as an engine concern
            throw new InvalidOperationException(
                "ResumeAsync requires the PipelineDefinition. Use PipelineManager.ResumeAsync instead.");
        }

        /// <summary>Internal resume entry-point called by PipelineManager.</summary>
        internal Task<PipelineRunResult> ResumeInternalAsync(
            PipelineDefinition def,
            PipelineCheckpoint checkpoint,
            IProgress<PassedArgs>? progress,
            CancellationToken token)
            => ExecuteAsync(def, progress, token, overrideParams: null, resumeCheckpoint: checkpoint);

        // ── Core execution ──────────────────────────────────────────────

        private async Task<PipelineRunResult> ExecuteAsync(
            PipelineDefinition def,
            IProgress<PassedArgs>? progress,
            CancellationToken token,
            Dictionary<string, object>? overrideParams,
            PipelineCheckpoint? resumeCheckpoint)
        {
            var ctx = BuildContext(def, progress, token, overrideParams);
            var result = new PipelineRunResult
            {
                RunId        = ctx.RunId,
                PipelineId   = def.Id,
                PipelineName = def.Name,
                Status       = RunStatus.Running,
                StartedAtUtc = ctx.StartedAtUtc
            };

            Metrics?.RecordStart(ctx.RunId, def.Id, def.Name);

            try
            {
                // ── Resolve source ───────────────────────────────────────
                var source = _registry.Create<IPipelineSource>(def.SourcePluginId);
                source.Configure(MergeParams(def.Parameters, overrideParams));

                // ── Resolve optional error sink ──────────────────────────
                IPipelineSink? errorSink = null;
                if (!string.IsNullOrWhiteSpace(def.ErrorSinkPluginId))
                {
                    errorSink = _registry.Create<IPipelineSink>(def.ErrorSinkPluginId);
                    errorSink.Configure(new Dictionary<string, object>());
                }
                else if (_registry.Contains("beep.sink.errorlog"))
                {
                    errorSink = _registry.Create<IPipelineSink>("beep.sink.errorlog");
                    errorSink.Configure(new Dictionary<string, object>());
                }

                // ── Resolve main sink ────────────────────────────────────
                var sink = _registry.Create<IPipelineSink>(def.SinkPluginId);
                sink.Configure(MergeParams(def.Parameters, overrideParams));

                // ── Get schema ───────────────────────────────────────────
                var schema = await source.GetSchemaAsync(ctx, token);

                // ── Begin sink batch ─────────────────────────────────────
                await sink.BeginBatchAsync(ctx, schema, token);
                if (errorSink != null)
                    await errorSink.BeginBatchAsync(ctx, schema, token);

                // ── Build the processing pipeline ────────────────────────
                var stream = source.ReadAsync(ctx, token);
                stream = ApplySteps(stream, def, ctx, errorSink, token, result, resumeCheckpoint);

                // ── Drain stream into batched sink writes ────────────────
                var retry = new PipelineRetryPolicy(def.MaxRetries);
                var batch = new List<PipelineRecord>(def.BatchSize);
                long batchOffset = 0;

                await foreach (var record in stream.WithCancellation(token))
                {
                    batch.Add(record);
                    ctx.TotalRecordsRead++;

                    if (batch.Count >= def.BatchSize)
                    {
                        var batchToWrite = new List<PipelineRecord>(batch);
                        batch.Clear();

                        await retry.ExecuteAsync(() => sink.WriteBatchAsync(batchToWrite, ctx, token));
                        batchOffset += batchToWrite.Count;

                        if (def.EnableCheckpointing)
                            await _checkpoints.SaveAsync(ctx, "sink", batchOffset);

                        ctx.ReportProgress($"Written {ctx.TotalRecordsWritten} records", -1);
                    }
                }

                // Flush remaining records
                if (batch.Count > 0)
                {
                    await retry.ExecuteAsync(() => sink.WriteBatchAsync(batch, ctx, token));
                    batchOffset += batch.Count;
                }

                // ── Commit ───────────────────────────────────────────────
                await sink.CommitAsync(ctx, token);
                if (errorSink != null)
                    await errorSink.CommitAsync(ctx, token);

                if (def.EnableCheckpointing)
                    await _checkpoints.CompleteAsync(ctx.RunId);

                // ── Lineage ──────────────────────────────────────────────
                if (def.EnableLineageTracking)
                    await _lineage.FlushAsync(ctx);

                result.Status = RunStatus.Success;
            }
            catch (OperationCanceledException)
            {
                result.Status       = RunStatus.Cancelled;
                result.ErrorMessage = "Run was cancelled.";
            }
            catch (Exception ex)
            {
                result.Status       = RunStatus.Failed;
                result.ErrorMessage = ex.Message;
                _editor.AddLogMessage(nameof(PipelineEngine),
                    $"Pipeline '{def.Name}' failed: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
            finally
            {
                result.FinishedAtUtc     = DateTime.UtcNow;
                result.RecordsRead       = ctx.TotalRecordsRead;
                result.RecordsWritten    = ctx.TotalRecordsWritten;
                result.RecordsRejected   = ctx.TotalRecordsRejected;
                result.RecordsWarned     = ctx.TotalRecordsWarned;
                result.BytesProcessed    = ctx.TotalBytesProcessed;
                Metrics?.RecordCompletion(ctx.RunId);
            }

            await PostRunObservabilityAsync(def, result).ConfigureAwait(false);
            return result;
        }

        // ── Step application ────────────────────────────────────────────

        private IAsyncEnumerable<PipelineRecord> ApplySteps(
            IAsyncEnumerable<PipelineRecord> stream,
            PipelineDefinition def,
            PipelineRunContext ctx,
            IPipelineSink? errorSink,
            CancellationToken token,
            PipelineRunResult result,
            PipelineCheckpoint? resumeCheckpoint)
        {
            var activeSteps = def.Steps
                .Where(s => s.IsActive)
                .OrderBy(s => s.Sequence)
                .ToList();

            // Skip steps already completed in a resumed run
            int skipToIndex = 0;
            if (resumeCheckpoint != null)
            {
                skipToIndex = activeSteps.FindIndex(
                    s => s.Id == resumeCheckpoint.LastCommittedStepId);
                if (skipToIndex < 0) skipToIndex = 0;
            }

            for (int i = skipToIndex; i < activeSteps.Count; i++)
            {
                var step      = activeSteps[i];
                var stepResult = _stepRunner.CreateResult(step);
                result.StepResults.Add(stepResult);

                stream = ApplySingleStep(stream, step, ctx, errorSink, token, stepResult);
            }

            return stream;
        }

        private IAsyncEnumerable<PipelineRecord> ApplySingleStep(
            IAsyncEnumerable<PipelineRecord> stream,
            PipelineStepDef step,
            PipelineRunContext ctx,
            IPipelineSink? errorSink,
            CancellationToken token,
            PipelineStepResult stepResult)
        {
            if (!_registry.Contains(step.PluginId))
            {
                _editor.AddLogMessage(nameof(PipelineEngine),
                    $"Step '{step.Name}' skipped — plugin '{step.PluginId}' not registered.",
                    DateTime.Now, -1, null, Errors.Ok);
                return stream;
            }

            switch (step.Kind)
            {
                case StepKind.Transform:
                {
                    var transformer = _registry.Create<IPipelineTransformer>(step.PluginId);
                    transformer.Configure(step.Config);
                    return WrapWithStepTracking(
                        _stepRunner.ApplyTransform(transformer, stream, ctx, token),
                        stepResult, ctx);
                }

                case StepKind.Validate:
                {
                    var validator = _registry.Create<IPipelineValidator>(step.PluginId);
                    validator.Configure(step.Config);
                    return WrapWithStepTracking(
                        _stepRunner.ApplyValidatorAsync(validator, stream, ctx, errorSink, token),
                        stepResult, ctx);
                }

                default:
                    return stream;
            }
        }

        private static async IAsyncEnumerable<PipelineRecord> WrapWithStepTracking(
            IAsyncEnumerable<PipelineRecord> inner,
            PipelineStepResult stepResult,
            PipelineRunContext ctx,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            stepResult.Status = RunStatus.Running;
            long count = 0;
            Exception? fault = null;

            var enumerator = inner.WithCancellation(token).GetAsyncEnumerator();
            try
            {
                while (true)
                {
                    bool hasNext;
                    try   { hasNext = await enumerator.MoveNextAsync(); }
                    catch (Exception ex) { fault = ex; break; }

                    if (!hasNext) break;
                    count++;
                    yield return enumerator.Current;
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
                stepResult.FinishedAt = DateTime.UtcNow;
                ctx.StepsCompleted++;
                if (fault != null)
                {
                    stepResult.Status = RunStatus.Failed;
                }
                else
                {
                    stepResult.Status     = RunStatus.Success;
                    stepResult.RecordsIn  = count;
                    stepResult.RecordsOut = count;
                }
            }

            if (fault != null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(fault);
        }

        // ── Helpers ────────────────────────────────────────────────────

        private static PipelineRunContext BuildContext(
            PipelineDefinition def,
            IProgress<PassedArgs>? progress,
            CancellationToken token,
            Dictionary<string, object>? overrides)
        {
            var merged = MergeParams(def.Parameters, overrides);
            return new PipelineRunContext
            {
                PipelineId   = def.Id,
                PipelineName = def.Name,
                Progress     = progress ?? new Progress<PassedArgs>(),
                Token        = token,
                Parameters   = merged
            };
        }

        private static IReadOnlyDictionary<string, object> MergeParams(
            Dictionary<string, object> baseParams,
            Dictionary<string, object>? overrides)
        {
            var merged = new Dictionary<string, object>(baseParams);
            if (overrides != null)
                foreach (var kv in overrides)
                    merged[kv.Key] = kv.Value;
            return merged;
        }

        // ── Observability post-run ──────────────────────────────────────

        private async Task PostRunObservabilityAsync(
            PipelineDefinition def,
            PipelineRunResult result)
        {
            if (ObservabilityStore == null && Alerting == null) return;

            var log = new PipelineRunLog
            {
                RunId           = result.RunId,
                PipelineId      = result.PipelineId,
                PipelineName    = result.PipelineName,
                PipelineVersion = def.Version.ToString(),
                TriggerSource   = "engine",
                StartedAtUtc    = result.StartedAtUtc,
                FinishedAtUtc   = result.FinishedAtUtc ?? DateTime.UtcNow,
                Status          = result.Status,
                ErrorMessage    = result.ErrorMessage,
                RecordsRead     = result.RecordsRead,
                RecordsWritten  = result.RecordsWritten,
                RecordsRejected = result.RecordsRejected,
                RecordsWarned   = result.RecordsWarned,
                BytesProcessed  = result.BytesProcessed
            };

            if (ObservabilityStore != null)
                await ObservabilityStore.SaveRunLogAsync(log).ConfigureAwait(false);

            if (Alerting != null)
                await Alerting.EvaluateAsync(log, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
