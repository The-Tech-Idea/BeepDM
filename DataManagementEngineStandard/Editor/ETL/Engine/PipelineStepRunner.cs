using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Engine
{
    /// <summary>
    /// Applies a single transformer or validator step to a streaming record sequence.
    /// Called by <see cref="PipelineEngine"/> for each active step in the pipeline definition.
    /// </summary>
    public class PipelineStepRunner
    {
        /// <summary>
        /// Wraps <paramref name="input"/> through a transformer plugin and returns the output stream.
        /// </summary>
        public IAsyncEnumerable<PipelineRecord> ApplyTransform(
            IPipelineTransformer transformer,
            IAsyncEnumerable<PipelineRecord> input,
            PipelineRunContext ctx,
            CancellationToken token)
            => transformer.TransformAsync(input, ctx, token);

        /// <summary>
        /// Validates each record in <paramref name="input"/> via <paramref name="validator"/>.
        /// Records that pass (Pass / Warn) continue downstream.
        /// Records that are rejected are written to <paramref name="errorSink"/> if supplied,
        /// and the <see cref="PipelineRunContext.TotalRecordsRejected"/> counter is incremented.
        /// </summary>
        public async IAsyncEnumerable<PipelineRecord> ApplyValidatorAsync(
            IPipelineValidator validator,
            IAsyncEnumerable<PipelineRecord> input,
            PipelineRunContext ctx,
            IPipelineSink? errorSink,
            [EnumeratorCancellation] CancellationToken token)
        {
            var rejectBatch = errorSink != null ? new List<PipelineRecord>() : null;

            await foreach (var record in input.WithCancellation(token))
            {
                var result = await validator.ValidateAsync(record, ctx, token);

                if (result.Outcome == ValidationOutcome.Reject)
                {
                    record.Meta[PipelineRecordMeta.ValidationRule]    = result.RuleName  ?? string.Empty;
                    record.Meta[PipelineRecordMeta.ValidationMessage] = result.Message   ?? string.Empty;
                    ctx.TotalRecordsRejected++;

                    rejectBatch?.Add(record);
                }
                else
                {
                    if (result.Outcome == ValidationOutcome.Warn)
                        ctx.TotalRecordsWarned++;

                    yield return record;
                }
            }

            // Flush rejected records to the error sink
            if (rejectBatch != null && rejectBatch.Count > 0 && errorSink != null)
                await errorSink.WriteBatchAsync(rejectBatch, ctx, token);
        }

        /// <summary>Creates a fresh <see cref="PipelineStepResult"/> for the given step definition.</summary>
        public PipelineStepResult CreateResult(PipelineStepDef step) =>
            new PipelineStepResult
            {
                StepId    = step.Id,
                StepName  = step.Name,
                Kind      = step.Kind,
                Status    = RunStatus.Running,
                StartedAt = DateTime.UtcNow
            };
    }
}
