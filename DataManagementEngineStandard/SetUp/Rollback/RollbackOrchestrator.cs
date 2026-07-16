using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.SetUp.Rollback;

namespace TheTechIdea.Beep.SetUp.Rollback
{
    /// <summary>
    /// Default <see cref="IRollbackOrchestrator"/>: walks completed steps in reverse and asks each
    /// to undo itself.
    /// </summary>
    public sealed class RollbackOrchestrator : IRollbackOrchestrator
    {
        private readonly ILogger _logger;

        public RollbackOrchestrator(ILogger logger = null) => _logger = logger;

        public async Task<RollbackReport> RollbackAsync(
            IReadOnlyList<ISetupStep> steps,
            SetupContext context,
            IProgress<PassedArgs> progress = null,
            CancellationToken token = default)
        {
            var report = new RollbackReport
            {
                RunId = context?.State?.RunId,
                StartedAt = DateTimeOffset.UtcNow,
                Succeeded = true
            };

            if (steps == null || context?.State == null)
            {
                report.FinishedAt = DateTimeOffset.UtcNow;
                return report;
            }

            var completed = context.State.CompletedStepIds ?? new HashSet<string>();
            var byId = steps.Where(s => s != null)
                            .ToDictionary(s => s.StepId, s => s, StringComparer.Ordinal);

            // Reverse registration order, restricted to steps that actually completed. Skipped steps
            // applied nothing, so there is nothing to undo for them.
            var toRollback = steps
                .Where(s => s != null && completed.Contains(s.StepId))
                .Reverse()
                .ToList();

            _logger?.LogInformation("Rolling back {Count} completed step(s) for run {RunId}.",
                toRollback.Count, report.RunId);

            foreach (var step in toRollback)
            {
                token.ThrowIfCancellationRequested();
                var sw = Stopwatch.StartNew();

                if (!step.SupportsRollback)
                {
                    sw.Stop();
                    report.StepResults.Add(new RollbackStepResult
                    {
                        StepId = step.StepId,
                        Succeeded = true,
                        Skipped = true,
                        Message = "Step does not support rollback; nothing undone.",
                        Elapsed = sw.Elapsed
                    });
                    continue;
                }

                IErrorsInfo result;
                try
                {
                    result = await step.RollbackAsync(context, progress, token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // A throwing rollback must not abort the remaining rollbacks — continue and
                    // record it, so as much state as possible is cleaned up.
                    _logger?.LogError(ex, "Rollback of step '{StepId}' threw.", step.StepId);
                    result = new ErrorsInfo { Flag = Errors.Failed, Message = $"Rollback threw: {ex.Message}", Ex = ex };
                }

                sw.Stop();
                var ok = result != null && result.Flag != Errors.Failed;
                if (!ok) report.Succeeded = false;

                report.StepResults.Add(new RollbackStepResult
                {
                    StepId = step.StepId,
                    Succeeded = ok,
                    Message = result?.Message,
                    Elapsed = sw.Elapsed
                });

                _logger?.LogInformation("Rollback of step '{StepId}': {Outcome}.",
                    step.StepId, ok ? "ok" : "FAILED");
            }

            report.FinishedAt = DateTimeOffset.UtcNow;
            return report;
        }
    }
}
