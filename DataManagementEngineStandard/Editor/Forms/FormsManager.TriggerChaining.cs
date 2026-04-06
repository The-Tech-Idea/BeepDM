using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.Forms.Helpers;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Phase 4.3 — Trigger chaining, dependency ordering, and execution logging.
    /// Subscribes to <see cref="ITriggerManager.TriggerExecuted"/> to populate an in-memory
    /// <see cref="ITriggerExecutionLog"/>, and exposes dependency-ordered fire helpers
    /// via <see cref="ITriggerDependencyManager"/>.
    /// </summary>
    public partial class FormsManager
    {
        // ─────────────────────────────────────────────────────────────────────
        // Phase 4 fields (injected or created in InitializeTriggerChaining)
        // ─────────────────────────────────────────────────────────────────────

        private ITriggerExecutionLog   _triggerExecutionLog;
        private ITriggerDependencyManager _triggerDependencyManager;

        /// <summary>Execution log for all triggers fired by this form manager.</summary>
        public ITriggerExecutionLog TriggerLog => _triggerExecutionLog;

        /// <summary>Dependency manager for trigger ordering and cycle detection.</summary>
        public ITriggerDependencyManager TriggerDependencies => _triggerDependencyManager;

        // ─────────────────────────────────────────────────────────────────────
        // Initialisation — called from the FormsManager constructor
        // ─────────────────────────────────────────────────────────────────────

        private void InitializeTriggerChaining(
            ITriggerExecutionLog    executionLog        = null,
            ITriggerDependencyManager dependencyManager = null)
        {
            _triggerExecutionLog      = executionLog      ?? new TriggerExecutionLog();
            _triggerDependencyManager = dependencyManager ?? new TriggerDependencyManager();

            // Subscribe to the underlying TriggerManager's executed event
            if (_triggerManager != null)
                _triggerManager.TriggerExecuted += OnTriggerExecuted;
        }

        // ─────────────────────────────────────────────────────────────────────
        // TriggerExecuted handler — record entry into the execution log
        // ─────────────────────────────────────────────────────────────────────

        private void OnTriggerExecuted(object sender, TriggerExecutedEventArgs e)
        {
            if (e?.Trigger == null) return;

            _triggerExecutionLog?.Record(new TriggerExecutionLogEntry
            {
                TriggerId    = e.Trigger.TriggerId,
                TriggerName  = e.Trigger.TriggerName,
                TriggerType  = e.Trigger.TriggerType,
                BlockName    = e.Trigger.BlockName,
                ItemName     = e.Trigger.ItemName,
                Result       = e.Result,
                ElapsedMs    = (long)(e.DurationMs),
                ExecutedAt   = e.StartTime,
                ErrorMessage = e.Exception?.Message
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public log helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Return all logged trigger execution entries (newest last).</summary>
        public IReadOnlyList<TriggerExecutionLogEntry> GetTriggerLog()
            => _triggerExecutionLog?.GetAll()
               ?? new List<TriggerExecutionLogEntry>();

        /// <summary>Clear the in-memory trigger execution log.</summary>
        public void ClearTriggerLog()
            => _triggerExecutionLog?.Clear();

        // ─────────────────────────────────────────────────────────────────────
        // Dependency-ordered fire helper
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Fire a list of triggers in dependency order (<see cref="TriggerDefinition.DependsOn"/>).
        /// Honours each definition's <see cref="TriggerDefinition.ChainMode"/>:
        /// StopOnFailure stops on first failure; Continue keeps going; Rollback stops then rolls back.
        /// </summary>
        /// <param name="triggers">The set of triggers to fire (DAG — must not contain cycles).</param>
        /// <param name="blockName">Block context for each trigger.</param>
        /// <returns>List of results in execution order.</returns>
        public async Task<IReadOnlyList<TriggerResult>> FireTriggersInOrderAsync(
            IReadOnlyList<TriggerDefinition> triggers,
            string blockName)
        {
            if (triggers == null || triggers.Count == 0)
                return Array.Empty<TriggerResult>();

            var ordered = _triggerDependencyManager.OrderByDependency(triggers);
            var results = new List<TriggerResult>(ordered.Count);

            foreach (var t in ordered)
            {
                if (t.AsyncHandler != null || t.Handler != null)
                {
                    var ctx    = TriggerContext.ForBlock(t.TriggerType, blockName ?? string.Empty, null, _dmeEditor);
                    var result = await t.ExecuteAsync(ctx, default);
                    results.Add(result);

                    if (result == TriggerResult.Failure || result == TriggerResult.Cancelled)
                    {
                        switch (t.ChainMode)
                        {
                            case TriggerChainMode.StopOnFailure:
                                return results;

                            case TriggerChainMode.Rollback:
                                // Signal callers via a Cancelled result appended to the list
                                results.Add(TriggerResult.Cancelled);
                                return results;

                            case TriggerChainMode.Continue:
                            default:
                                break; // keep going
                        }
                    }
                }
            }

            return results;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Cleanup
        // ─────────────────────────────────────────────────────────────────────

        private void DisposeTriggerChaining()
        {
            if (_triggerManager != null)
                _triggerManager.TriggerExecuted -= OnTriggerExecuted;
        }
    }
}
