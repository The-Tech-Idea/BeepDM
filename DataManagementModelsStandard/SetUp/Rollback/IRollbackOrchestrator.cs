using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.SetUp.Rollback
{
    /// <summary>
    /// Undoes a failed setup run by rolling its completed steps back in reverse order.
    /// </summary>
    /// <remarks>
    /// Without this, a failure at step N left steps 1..N-1 applied with no automated undo — schema
    /// created, reference data half-inserted. The orchestrator is best-effort: one step's failed
    /// rollback does not abort the rest, because stopping halfway strands <em>more</em> state.
    /// </remarks>
    public interface IRollbackOrchestrator
    {
        Task<RollbackReport> RollbackAsync(
            IReadOnlyList<ISetupStep> steps,
            SetupContext context,
            IProgress<PassedArgs> progress = null,
            CancellationToken token = default);
    }

    /// <summary>Outcome of a rollback pass.</summary>
    public sealed class RollbackReport
    {
        public string RunId { get; set; }
        public bool Succeeded { get; set; }
        public List<RollbackStepResult> StepResults { get; set; } = new();
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset FinishedAt { get; set; }
    }

    public sealed class RollbackStepResult
    {
        public string StepId { get; set; }
        public bool Succeeded { get; set; }

        /// <summary>The step did not support rollback; nothing was undone (not a failure).</summary>
        public bool Skipped { get; set; }

        public string Message { get; set; }
        public TimeSpan Elapsed { get; set; }
    }
}
