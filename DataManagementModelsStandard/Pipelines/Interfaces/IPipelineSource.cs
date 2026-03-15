using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Interfaces
{
    /// <summary>
    /// Extracts records from a data source as an async stream.
    /// The runtime never loads all records into memory simultaneously.
    /// </summary>
    public interface IPipelineSource : IPipelinePlugin
    {
        /// <summary>Schema of the records this source produces.</summary>
        Task<PipelineSchema> GetSchemaAsync(PipelineRunContext ctx, CancellationToken token);

        /// <summary>
        /// Produces records as an async stream.
        /// Callers consume via <c>await foreach</c>.
        /// </summary>
        IAsyncEnumerable<PipelineRecord> ReadAsync(PipelineRunContext ctx, CancellationToken token);

        /// <summary>Estimated total rows, or -1 if unknown. Used for progress %.</summary>
        Task<long> GetEstimatedRowCountAsync(PipelineRunContext ctx, CancellationToken token);
    }
}
