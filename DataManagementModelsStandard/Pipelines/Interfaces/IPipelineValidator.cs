using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Interfaces
{
    /// <summary>
    /// Validates records against rules.
    /// Records that fail are routed to the error sink; valid records continue to the main sink.
    /// </summary>
    public interface IPipelineValidator : IPipelinePlugin
    {
        /// <summary>
        /// Validate one record. Return Pass, Warn, or Reject with message.
        /// </summary>
        Task<ValidationResult> ValidateAsync(
            PipelineRecord record,
            PipelineRunContext ctx,
            CancellationToken token);
    }
}
