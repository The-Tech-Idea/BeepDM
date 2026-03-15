using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Pipelines.Engine;
using TheTechIdea.Beep.Pipelines.Models;
using TheTechIdea.Beep.Workflow;
using RunStatus = TheTechIdea.Beep.Pipelines.Models.RunStatus;

namespace TheTechIdea.Beep.Workflows.Engine
{
    /// <summary>
    /// Core workflow runner.  Executes a <see cref="WorkFlowDefinition"/> by:
    /// <list type="number">
    ///   <item>Topologically sorting steps via the <see cref="StepConnection"/> graph.</item>
    ///   <item>Executing each step, dispatching to the appropriate handler by <see cref="StepActionKind"/>.</item>
    ///   <item>Capturing a <see cref="StepExecutionRecord"/> per step.</item>
    ///   <item>Applying per-step <see cref="OnFailureBehavior"/> and <see cref="WorkFlowRetryPolicy"/> rules.</item>
    ///   <item>Pausing on Approval steps and waiting for <see cref="ApproveAsync"/> / <see cref="RejectAsync"/>.</item>
    /// </list>
    /// </summary>
    public class WorkFlowEngine
    {
        private readonly IDMEEditor       _editor;
        private readonly PipelineEngine   _pipelineEngine;
        private readonly PipelineManager  _pipelineManager;
        private readonly WorkFlowStorage  _storage;

        public WorkFlowEngine(IDMEEditor editor)
        {
            _editor          = editor  ?? throw new ArgumentNullException(nameof(editor));
            _pipelineEngine  = new PipelineEngine(editor);
            _pipelineManager = new PipelineManager(editor);
            _storage         = new WorkFlowStorage(editor);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Execute a workflow definition and return the structured result.</summary>
        public async Task<WorkFlowRunResult> RunAsync(
            WorkFlowDefinition definition,
            IProgress<PassedArgs>? progress    = null,
            CancellationToken token            = default,
            IReadOnlyDictionary<string, object>? overrideParams = null)
        {
            ArgumentNullException.ThrowIfNull(definition);

            var ctx = new WorkFlowRunContext
            {
                WorkFlowId   = definition.Id,
                WorkFlowName = definition.Name,
                DMEEditor    = _editor,
                Progress     = progress,
                Token        = token,
                Parameters   = MergeParameters(definition, overrideParams)
            };

            return await ExecuteAsync(definition, ctx);
        }

        /// <summary>Resume a paused or failed workflow run from the last committed step.</summary>
        public async Task<WorkFlowRunResult> ResumeAsync(
            string workflowRunId,
            IProgress<PassedArgs>? progress = null,
            CancellationToken token         = default)
        {
            var saved = await _storage.LoadRunResultAsync(workflowRunId)
                ?? throw new InvalidOperationException($"Run '{workflowRunId}' not found.");

            var def = await _storage.LoadDefinitionAsync(saved.WorkFlowId)
                ?? throw new InvalidOperationException($"Definition '{saved.WorkFlowId}' not found.");

            var ctx = new WorkFlowRunContext
            {
                WorkFlowId   = def.Id,
                WorkFlowName = def.Name,
                DMEEditor    = _editor,
                Progress     = progress,
                Token        = token,
                Parameters   = new Dictionary<string, object>()
            };

            // Replay already-completed step records into context so we can skip them
            foreach (var rec in saved.StepRecords.Where(r => r.Success))
                ctx.StepResults[rec.StepId] = rec;

            return await ExecuteAsync(def, ctx, resumeFromRunId: workflowRunId);
        }

        /// <summary>Approve a paused Approval step, resuming the workflow.</summary>
        public async Task ApproveAsync(string workflowRunId, string stepId, string approverNote)
        {
            var state = new ApprovalState
            {
                RunId        = workflowRunId,
                StepId       = stepId,
                Decision     = ApprovalDecision.Approved,
                ApproverNote = approverNote,
                DecidedAtUtc = DateTime.UtcNow
            };
            await _storage.SaveApprovalStateAsync(workflowRunId, stepId, state);
        }

        /// <summary>Reject a paused Approval step.</summary>
        public async Task RejectAsync(string workflowRunId, string stepId, string rejectionNote)
        {
            var state = new ApprovalState
            {
                RunId        = workflowRunId,
                StepId       = stepId,
                Decision     = ApprovalDecision.Rejected,
                ApproverNote = rejectionNote,
                DecidedAtUtc = DateTime.UtcNow
            };
            await _storage.SaveApprovalStateAsync(workflowRunId, stepId, state);
        }

        // ── Internal execution ────────────────────────────────────────────────

        private async Task<WorkFlowRunResult> ExecuteAsync(
            WorkFlowDefinition def,
            WorkFlowRunContext  ctx,
            string?             resumeFromRunId = null)
        {
            var result = new WorkFlowRunResult
            {
                RunId        = ctx.RunId,
                WorkFlowId   = def.Id,
                WorkFlowName = def.Name,
                StartedAtUtc = ctx.StartedAtUtc
            };

            try
            {
                var ordered = TopologicalSort(def);
                ctx.StepsTotal = ordered.Count;
                result.StepsTotal = ordered.Count;

                foreach (var step in ordered)
                {
                    ctx.Token.ThrowIfCancellationRequested();

                    // Skip already-completed steps when resuming
                    if (resumeFromRunId != null && ctx.StepResults.ContainsKey(step.ID))
                    {
                        result.StepRecords.Add(ctx.StepResults[step.ID]);
                        result.StepsCompleted++;
                        ctx.StepsCompleted++;
                        continue;
                    }

                    // Evaluate incoming edge condition (if any)
                    if (!ShouldExecuteStep(step, ctx, def.Connections))
                    {
                        var skippedRec = SkippedRecord(step);
                        result.StepRecords.Add(skippedRec);
                        ctx.StepResults[step.ID] = skippedRec;
                        result.StepsSkipped++;
                        ctx.StepsSkipped++;
                        continue;
                    }

                    ctx.CurrentStepId = step.ID;
                    ctx.ReportProgress($"Running step '{step.Name}' ({step.Kind})");

                    var rec = await ExecuteStepWithPolicyAsync(step, ctx, def.RetryPolicy);
                    result.StepRecords.Add(rec);
                    ctx.StepResults[step.ID] = rec;
                    result.TotalRecordsProcessed += rec.RecordsWritten;
                    ctx.TotalRecordsProcessed    += rec.RecordsWritten;

                    if (rec.Success)
                    {
                        result.StepsCompleted++;
                        ctx.StepsCompleted++;
                    }
                    else
                    {
                        result.StepsFailed++;
                        ctx.StepsFailed++;

                        if (step.OnFailure == OnFailureBehavior.Fail)
                        {
                            result.ErrorMessage = $"Step '{step.Name}' failed: {rec.ErrorMessage}";
                            break;
                        }
                        // Skip and Route are handled naturally by the edge graph in the
                        // next iteration; Retry was already applied in ExecuteStepWithPolicyAsync.
                    }
                }

                result.Success = result.StepsFailed == 0;
            }
            catch (OperationCanceledException)
            {
                result.Success      = false;
                result.ErrorMessage = "Workflow was cancelled.";
            }
            catch (Exception ex)
            {
                result.Success      = false;
                result.ErrorMessage = ex.Message;
                _editor.AddLogMessage(nameof(WorkFlowEngine),
                    $"Unexpected error in workflow '{def.Name}': {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
            finally
            {
                result.FinishedAtUtc = DateTime.UtcNow;
                await _storage.SaveRunResultAsync(result);
            }

            ctx.ReportProgress(result.Success ? "Workflow completed." : $"Workflow failed: {result.ErrorMessage}", 100);
            return result;
        }

        // ── Step dispatch ─────────────────────────────────────────────────────

        private async Task<StepExecutionRecord> ExecuteStepWithPolicyAsync(
            WorkFlowStepDef    step,
            WorkFlowRunContext  ctx,
            WorkFlowRetryPolicy defaultPolicy)
        {
            var policy   = step.RetryPolicy.MaxRetries > 0 ? step.RetryPolicy : defaultPolicy;
            int attempts = 0;

            while (true)
            {
                var rec = await ExecuteStepAsync(step, ctx);
                if (rec.Success || attempts >= policy.MaxRetries)
                {
                    rec.RetryCount = attempts;
                    return rec;
                }

                attempts++;
                _editor.AddLogMessage(nameof(WorkFlowEngine),
                    $"Step '{step.Name}' attempt {attempts} failed — retrying in {policy.GetDelay(attempts).TotalSeconds:0}s",
                    DateTime.Now, -1, null, Errors.Ok);

                await Task.Delay(policy.GetDelay(attempts), ctx.Token);
            }
        }

        private async Task<StepExecutionRecord> ExecuteStepAsync(
            WorkFlowStepDef   step,
            WorkFlowRunContext ctx)
        {
            return step.Kind switch
            {
                StepActionKind.ETLPipeline  => await RunEtlStepAsync(step, ctx),
                StepActionKind.Script       => await RunScriptStepAsync(step, ctx),
                StepActionKind.Notification => await RunNotificationStepAsync(step, ctx),
                StepActionKind.Approval     => await RunApprovalStepAsync(step, ctx),
                StepActionKind.Wait         => await RunWaitStepAsync(step, ctx),
                StepActionKind.SubWorkflow  => await RunSubWorkflowStepAsync(step, ctx),
                StepActionKind.SchemaSync   => await RunSchemaSyncStepAsync(step, ctx),
                StepActionKind.DataQuality  => await RunDQStepAsync(step, ctx),
                StepActionKind.Merge        => await RunMergeStepAsync(step, ctx),
                StepActionKind.Split        => await RunSplitStepAsync(step, ctx),
                _                           => throw new NotSupportedException($"Unknown StepActionKind: {step.Kind}")
            };
        }

        // ── Step handlers ─────────────────────────────────────────────────────

        private async Task<StepExecutionRecord> RunEtlStepAsync(
            WorkFlowStepDef   step,
            WorkFlowRunContext ctx)
        {
            var rec = StartRecord(step);
            try
            {
                if (string.IsNullOrWhiteSpace(step.PipelineId))
                    throw new InvalidOperationException("ETLPipeline step requires a PipelineId.");

                var def = await _pipelineManager.LoadAsync(step.PipelineId)
                    ?? throw new InvalidOperationException($"Pipeline '{step.PipelineId}' not found.");

                // Merge workflow-level params with step-level pipeline overrides
                var paramOverrides = new Dictionary<string, object>(step.PipelineParams);
                foreach (var kv in ctx.Parameters)
                    paramOverrides.TryAdd(kv.Key, kv.Value);

                var pipelineResult = await _pipelineEngine.RunAsync(
                    def, ctx.Progress, ctx.Token, paramOverrides);

                rec.RecordsRead     = pipelineResult.RecordsRead;
                rec.RecordsWritten  = pipelineResult.RecordsWritten;
                rec.RecordsRejected = pipelineResult.RecordsRejected;
                rec.Success         = pipelineResult.Status == RunStatus.Success;
                if (pipelineResult.Status != RunStatus.Success)
                    rec.ErrorMessage = pipelineResult.ErrorMessage;

                rec.Output["pipelineRunId"] = pipelineResult.RunId;
            }
            catch (Exception ex)
            {
                rec.Success      = false;
                rec.ErrorMessage = ex.Message;
            }
            return FinishRecord(rec);
        }

        private Task<StepExecutionRecord> RunScriptStepAsync(
            WorkFlowStepDef step, WorkFlowRunContext ctx)
        {
            // Roslyn-based execution planned for a later phase.
            // Log and succeed without executing so workflows do not break.
            var rec = StartRecord(step);
            _editor.AddLogMessage(nameof(WorkFlowEngine),
                $"Script step '{step.Name}' — script execution is not yet implemented.",
                DateTime.Now, -1, null, Errors.Ok);
            rec.Success = true;
            rec.Output["warning"] = "Script execution not yet implemented.";
            return Task.FromResult(FinishRecord(rec));
        }

        private Task<StepExecutionRecord> RunNotificationStepAsync(
            WorkFlowStepDef step, WorkFlowRunContext ctx)
        {
            var rec = StartRecord(step);
            _editor.AddLogMessage(nameof(WorkFlowEngine),
                $"Notification step '{step.Name}' — no notifier plugin registered.",
                DateTime.Now, -1, null, Errors.Ok);
            rec.Success = true;
            return Task.FromResult(FinishRecord(rec));
        }

        private async Task<StepExecutionRecord> RunApprovalStepAsync(
            WorkFlowStepDef step, WorkFlowRunContext ctx)
        {
            var rec     = StartRecord(step);
            var timeout = TimeSpan.FromHours(step.ApprovalTimeoutHours > 0 ? step.ApprovalTimeoutHours : 24);
            var deadline = rec.StartedAtUtc + timeout;

            _editor.AddLogMessage(nameof(WorkFlowEngine),
                $"Approval step '{step.Name}' waiting for approval (timeout {step.ApprovalTimeoutHours}h).",
                DateTime.Now, -1, null, Errors.Ok);

            while (DateTime.UtcNow < deadline)
            {
                ctx.Token.ThrowIfCancellationRequested();
                var state = await _storage.LoadApprovalStateAsync(ctx.RunId, step.ID);
                if (state?.Decision == ApprovalDecision.Approved)
                {
                    rec.Success = true;
                    rec.Output["approvedBy"]   = state.ApprovedBy;
                    rec.Output["approverNote"] = state.ApproverNote;
                    return FinishRecord(rec);
                }
                if (state?.Decision == ApprovalDecision.Rejected)
                {
                    rec.Success      = false;
                    rec.ErrorMessage = $"Rejected: {state.ApproverNote}";
                    return FinishRecord(rec);
                }
                await Task.Delay(TimeSpan.FromSeconds(10), ctx.Token);
            }

            rec.Success      = false;
            rec.ErrorMessage = "Approval timed out.";
            return FinishRecord(rec);
        }

        private async Task<StepExecutionRecord> RunWaitStepAsync(
            WorkFlowStepDef step, WorkFlowRunContext ctx)
        {
            var rec     = StartRecord(step);
            int seconds = step.WaitSeconds > 0 ? step.WaitSeconds : 1;
            await Task.Delay(TimeSpan.FromSeconds(seconds), ctx.Token);
            rec.Success = true;
            return FinishRecord(rec);
        }

        private async Task<StepExecutionRecord> RunSubWorkflowStepAsync(
            WorkFlowStepDef step, WorkFlowRunContext ctx)
        {
            var rec = StartRecord(step);
            try
            {
                if (string.IsNullOrWhiteSpace(step.SubWorkflowId))
                    throw new InvalidOperationException("SubWorkflow step requires a SubWorkflowId.");

                var subDef = await _storage.LoadDefinitionAsync(step.SubWorkflowId)
                    ?? throw new InvalidOperationException($"Sub-workflow '{step.SubWorkflowId}' not found.");

                var subResult = await RunAsync(subDef, ctx.Progress, ctx.Token, ctx.Parameters);
                rec.RecordsWritten = subResult.TotalRecordsProcessed;
                rec.Success        = subResult.Success;
                if (!subResult.Success) rec.ErrorMessage = subResult.ErrorMessage;
                rec.Output["subRunId"] = subResult.RunId;
            }
            catch (Exception ex)
            {
                rec.Success      = false;
                rec.ErrorMessage = ex.Message;
            }
            return FinishRecord(rec);
        }

        private Task<StepExecutionRecord> RunSchemaSyncStepAsync(
            WorkFlowStepDef step, WorkFlowRunContext ctx)
        {
            var rec = StartRecord(step);
            _editor.AddLogMessage(nameof(WorkFlowEngine),
                $"SchemaSync step '{step.Name}' — not yet implemented.", DateTime.Now, -1, null, Errors.Ok);
            rec.Success = true;
            rec.Output["warning"] = "SchemaSync step not yet implemented.";
            return Task.FromResult(FinishRecord(rec));
        }

        private Task<StepExecutionRecord> RunDQStepAsync(
            WorkFlowStepDef step, WorkFlowRunContext ctx)
        {
            var rec = StartRecord(step);
            _editor.AddLogMessage(nameof(WorkFlowEngine),
                $"DataQuality step '{step.Name}' — not yet implemented.", DateTime.Now, -1, null, Errors.Ok);
            rec.Success = true;
            rec.Output["warning"] = "DataQuality step not yet implemented.";
            return Task.FromResult(FinishRecord(rec));
        }

        private Task<StepExecutionRecord> RunMergeStepAsync(
            WorkFlowStepDef step, WorkFlowRunContext ctx)
        {
            var rec = StartRecord(step);
            rec.Success = true;
            return Task.FromResult(FinishRecord(rec));
        }

        private Task<StepExecutionRecord> RunSplitStepAsync(
            WorkFlowStepDef step, WorkFlowRunContext ctx)
        {
            var rec = StartRecord(step);
            rec.Success = true;
            return Task.FromResult(FinishRecord(rec));
        }

        // ── Graph helpers ─────────────────────────────────────────────────────

        /// <summary>
        /// Kahn's algorithm topological sort of <see cref="WorkFlowStepDef"/> nodes
        /// connected by <see cref="StepConnection"/> edges.
        /// Returns steps in valid execution order.
        /// </summary>
        private static List<WorkFlowStepDef> TopologicalSort(WorkFlowDefinition def)
        {
            var inDegree = def.Steps.ToDictionary(s => s.ID, _ => 0);
            var adjacency = def.Steps.ToDictionary(s => s.ID, _ => new List<string>());

            foreach (var conn in def.Connections)
            {
                if (!inDegree.ContainsKey(conn.ToStepId))   continue;
                if (!adjacency.ContainsKey(conn.FromStepId)) continue;
                inDegree[conn.ToStepId]++;
                adjacency[conn.FromStepId].Add(conn.ToStepId);
            }

            var queue   = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            var ordered = new List<WorkFlowStepDef>();
            var stepMap = def.Steps.ToDictionary(s => s.ID);

            while (queue.Count > 0)
            {
                string id = queue.Dequeue();
                if (stepMap.TryGetValue(id, out var s)) ordered.Add(s);
                foreach (string next in adjacency[id])
                {
                    if (--inDegree[next] == 0) queue.Enqueue(next);
                }
            }

            // Append any steps not reachable from roots (isolated nodes)
            foreach (var step in def.Steps)
                if (!ordered.Contains(step)) ordered.Add(step);

            return ordered;
        }

        /// <summary>
        /// Returns true when at least one incoming <see cref="StepConnection"/>
        /// with a null or passing condition exists for the step.
        /// Steps with no incoming edges are always executed.
        /// </summary>
        private static bool ShouldExecuteStep(
            WorkFlowStepDef           step,
            WorkFlowRunContext         ctx,
            IEnumerable<StepConnection> connections)
        {
            var incoming = connections
                .Where(c => c.ToStepId == step.ID)
                .OrderBy(c => c.Priority)
                .ToList();

            if (incoming.Count == 0) return true;   // root node

            foreach (var conn in incoming)
            {
                if (conn.Condition == null) return true;    // unconditional edge
                if (EvaluateCondition(conn.Condition, conn.FromStepId, ctx)) return true;
            }
            return false;
        }

        /// <summary>
        /// Evaluates a simple string condition against the predecessor step result.
        /// Supports: "result.Success == true/false" and "result.RecordsWritten > N".
        /// Returns true on any evaluation error (fail-safe = proceed).
        /// </summary>
        private static bool EvaluateCondition(
            string condition, string fromStepId, WorkFlowRunContext ctx)
        {
            try
            {
                if (!ctx.StepResults.TryGetValue(fromStepId, out var rec)) return true;

                var expr = condition.Trim();

                if (expr.StartsWith("result.Success", StringComparison.OrdinalIgnoreCase))
                {
                    bool expected = expr.Contains("true", StringComparison.OrdinalIgnoreCase);
                    return rec.Success == expected;
                }
                if (expr.StartsWith("result.RecordsWritten", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = expr.Split(new[] { '>', '<', '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2 && long.TryParse(parts[1].Trim(), out long threshold))
                    {
                        if (expr.Contains('>')) return rec.RecordsWritten > threshold;
                        if (expr.Contains('<')) return rec.RecordsWritten < threshold;
                        return rec.RecordsWritten == threshold;
                    }
                }
                if (expr.StartsWith("result.RecordsRead", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = expr.Split(new[] { '>', '<', '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2 && long.TryParse(parts[1].Trim(), out long threshold))
                    {
                        if (expr.Contains('>')) return rec.RecordsRead > threshold;
                        if (expr.Contains('<')) return rec.RecordsRead < threshold;
                        return rec.RecordsRead == threshold;
                    }
                }
            }
            catch { /* fall through */ }

            return true;    // unknown condition → proceed
        }

        // ── Record helpers ────────────────────────────────────────────────────

        private static StepExecutionRecord StartRecord(WorkFlowStepDef step) =>
            new()
            {
                StepId       = step.ID,
                StepName     = step.Name,
                Kind         = step.Kind,
                StartedAtUtc = DateTime.UtcNow
            };

        private static StepExecutionRecord FinishRecord(StepExecutionRecord rec)
        {
            rec.FinishedAtUtc = DateTime.UtcNow;
            return rec;
        }

        private static StepExecutionRecord SkippedRecord(WorkFlowStepDef step) =>
            FinishRecord(new StepExecutionRecord
            {
                StepId       = step.ID,
                StepName     = step.Name,
                Kind         = step.Kind,
                StartedAtUtc = DateTime.UtcNow,
                Success      = true,
                ErrorMessage = "Skipped — condition not met."
            });

        // ── Parameter helpers ─────────────────────────────────────────────────

        private static IReadOnlyDictionary<string, object> MergeParameters(
            WorkFlowDefinition def,
            IReadOnlyDictionary<string, object>? overrides)
        {
            var merged = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            // Defaults from definition
            foreach (var p in def.Parameters)
                if (p.DefaultValue != null)
                    merged[p.Name] = p.DefaultValue;

            // Caller overrides
            if (overrides != null)
                foreach (var kv in overrides)
                    merged[kv.Key] = kv.Value;

            return merged;
        }
    }
}
