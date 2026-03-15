using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Interfaces
{
    /// <summary>
    /// Writes records into a target data store.
    /// Receives records in batches for efficient I/O.
    /// </summary>
    public interface IPipelineSink : IPipelinePlugin
    {
        /// <summary>
        /// Called once before streaming begins. Use to open connections,
        /// create missing tables, begin transactions, etc.
        /// </summary>
        Task BeginBatchAsync(PipelineRunContext ctx, PipelineSchema schema, CancellationToken token);

        /// <summary>Write one batch of records.</summary>
        Task WriteBatchAsync(IReadOnlyList<PipelineRecord> batch, PipelineRunContext ctx, CancellationToken token);

        /// <summary>
        /// Called once after all batches. Commit transactions, close files, update metadata.
        /// </summary>
        Task CommitAsync(PipelineRunContext ctx, CancellationToken token);

        /// <summary>Called on cancellation or error. Roll back / clean up.</summary>
        Task RollbackAsync(PipelineRunContext ctx, CancellationToken token);
    }
}
