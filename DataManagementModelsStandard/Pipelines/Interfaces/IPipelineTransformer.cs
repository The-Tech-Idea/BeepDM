using System.Collections.Generic;
using System.Threading;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Interfaces
{
    /// <summary>
    /// Transforms an async stream of records into another stream.
    /// A single transformer can map columns, split rows, enrich data, etc.
    /// Transformers are composable — chain N transformers between source and sink.
    /// </summary>
    public interface IPipelineTransformer : IPipelinePlugin
    {
        /// <summary>
        /// Declares the output schema given an input schema.
        /// Called before execution to allow downstream plugins to plan.
        /// </summary>
        PipelineSchema GetOutputSchema(PipelineSchema inputSchema);

        /// <summary>
        /// Transforms the incoming record stream into an outgoing record stream.
        /// Can yield 1:1, 1:N, or N:1 records (split / aggregate / filter are all transformers).
        /// </summary>
        IAsyncEnumerable<PipelineRecord> TransformAsync(
            IAsyncEnumerable<PipelineRecord> input,
            PipelineRunContext ctx,
            CancellationToken token);
    }
}
