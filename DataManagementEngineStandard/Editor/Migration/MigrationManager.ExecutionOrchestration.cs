using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Common.Retry;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Core;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    public partial class MigrationManager
    {
        private static readonly ConcurrentDictionary<string, MigrationExecutionCheckpoint> ExecutionCheckpoints = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, MigrationPlanArtifact> ExecutionPlans = new(StringComparer.OrdinalIgnoreCase);

        public MigrationExecutionCheckpoint CreateExecutionCheckpoint(MigrationPlanArtifact plan, string executionToken = null)
        {
            if (plan == null)
                return new MigrationExecutionCheckpoint();

            var token = string.IsNullOrWhiteSpace(executionToken) ? Guid.NewGuid().ToString("N") : executionToken.Trim();
            var checkpoint = ExecutionCheckpoints.GetOrAdd(token, _ => BuildNewCheckpoint(plan, token));
            checkpoint.UpdatedOnUtc = DateTime.UtcNow;

            if (checkpoint.Steps.Count == 0 && plan.Operations.Count > 0)
            {
                checkpoint.Steps = BuildExecutionSteps(plan.Operations);
                checkpoint.LastCompletedStep = checkpoint.Steps
                    .Where(step => step.Status == MigrationExecutionStepStatus.Completed)
                    .Select(step => step.Sequence)
                    .DefaultIfEmpty(-1)
                    .Max();
            }

            ExecutionPlans[token] = plan;
            PersistExecutionCheckpoint(checkpoint);
            return checkpoint;
        }

        public MigrationExecutionResult ExecuteMigrationPlan(MigrationPlanArtifact plan, MigrationExecutionPolicy policy = null, string executionToken = null, IProgress<PassedArgs> progress = null)
        {
            // Task.Run must wrap the CALL: invoking an async method runs it synchronously to its
            // first await, where it captures the caller's SynchronizationContext. Starting it on
            // the pool means there is no context to capture, so a UI caller blocked here in
            // GetResult() cannot deadlock against its own continuation.
            return Task.Run(() => ExecuteMigrationPlanAsync(plan, policy, executionToken, progress, CancellationToken.None))
                .GetAwaiter().GetResult();
        }

        public async Task<MigrationExecutionResult> ExecuteMigrationPlanAsync(
            MigrationPlanArtifact plan,
            MigrationExecutionPolicy policy = null,
            string executionToken = null,
            IProgress<PassedArgs> progress = null,
            CancellationToken token = default)
        {
            var result = new MigrationExecutionResult();
            policy ??= new MigrationExecutionPolicy();

            if (MigrateDataSource == null)
            {
                result.Success = false;
                result.Message = "Migration data source is not set.";
                return result;
            }

            if (plan == null)
            {
                result.Success = false;
                result.Message = "Migration plan is null.";
                return result;
            }

            var checkpoint = CreateExecutionCheckpoint(plan, executionToken);
            result.ExecutionToken = checkpoint.ExecutionToken;
            result.Checkpoint = checkpoint;

            if (!string.IsNullOrWhiteSpace(checkpoint.PlanHash) &&
                !string.IsNullOrWhiteSpace(plan.PlanHash) &&
                !string.Equals(checkpoint.PlanHash, plan.PlanHash, StringComparison.Ordinal))
            {
                result.Success = false;
                result.Message = "Execution token belongs to a different migration plan hash. Create a new execution token or resume the original plan.";
                checkpoint.HasFailed = true;
                checkpoint.FailureCategory = "PlanHashMismatch";
                checkpoint.FailureReason = result.Message;
                checkpoint.UpdatedOnUtc = DateTime.UtcNow;
                PersistExecutionCheckpoint(checkpoint);
                RecordDiagnostic(checkpoint.ExecutionToken, checkpoint.CorrelationId, "exec-plan-hash-mismatch", MigrationDiagnosticSeverity.Error, string.Empty, result.Message, "Do not reuse an execution token across different plan hashes.");
                RecordExecutionFinished(plan, checkpoint, success: false, notes: result.Message);
                return result;
            }

            RecordExecutionStarted(plan, checkpoint);
            plan.PerformancePlan ??= BuildPerformancePlan(plan);

            plan.CompensationPlan ??= BuildCompensationPlan(plan);
            var hasHighRisk = plan.Operations.Any(operation =>
                operation != null &&
                (operation.RiskLevel == MigrationPlanRiskLevel.High ||
                 operation.RiskLevel == MigrationPlanRiskLevel.Critical ||
                 operation.IsDestructive ||
                 operation.IsTypeNarrowing ||
                 operation.HasNullabilityTightening));
            if (hasHighRisk && (plan.CompensationPlan.Actions == null || plan.CompensationPlan.Actions.Count == 0))
            {
                result.Success = false;
                result.Message = "High-risk operations require compensation actions before apply.";
                RecordDiagnostic(checkpoint.ExecutionToken, checkpoint.CorrelationId, "exec-compensation-missing", MigrationDiagnosticSeverity.Error, string.Empty, result.Message, "Build compensation actions for high-risk operations before apply.");
                RecordExecutionFinished(plan, checkpoint, success: false, notes: result.Message);
                return result;
            }

            plan.RollbackReadinessReport = CheckRollbackReadiness(
                plan,
                backupConfirmed: plan.RollbackReadinessReport?.BackupConfirmed ?? false,
                restoreTestEvidenceProvided: plan.RollbackReadinessReport?.RestoreTestEvidenceProvided ?? false,
                restoreTestEvidence: plan.RollbackReadinessReport?.RestoreTestEvidence);
            if (!plan.RollbackReadinessReport.IsReady)
            {
                result.Success = false;
                result.Message = "Rollback readiness checks failed. Backup/restore evidence is required for protected execution.";
                RecordDiagnostic(checkpoint.ExecutionToken, checkpoint.CorrelationId, "exec-rollback-readiness", MigrationDiagnosticSeverity.Error, string.Empty, result.Message, "Provide backup confirmation and restore-test evidence.");
                RecordExecutionFinished(plan, checkpoint, success: false, notes: result.Message);
                return result;
            }

            plan.PreflightReport = RunPreflightChecks(plan);
            if (!plan.PreflightReport.CanApply)
            {
                result.Success = false;
                result.Message = "Preflight blocked migration plan execution.";
                result.Checkpoint.HasFailed = true;
                result.Checkpoint.FailureCategory = "Preflight";
                result.Checkpoint.FailureReason = result.Message;
                result.Checkpoint.UpdatedOnUtc = DateTime.UtcNow;
                RecordDiagnostic(checkpoint.ExecutionToken, checkpoint.CorrelationId, "exec-preflight-block", MigrationDiagnosticSeverity.Error, string.Empty, result.Message, "Review preflight findings and regenerate plan if needed.");
                PersistExecutionCheckpoint(result.Checkpoint);
                RecordExecutionFinished(plan, checkpoint, success: false, notes: result.Message);
                return result;
            }
            var performancePolicy = plan.PerformancePlan?.Policy ?? new MigrationPerformancePolicy();
            var runStopwatch = Stopwatch.StartNew();
            var processedInBatch = 0;

            foreach (var step in checkpoint.Steps.OrderBy(item => item.Sequence))
            {
                // Plan-level cancellation: checked between steps, not within a step's
                // retry sequence. Cancellation mid-step is the caller's responsibility
                // (the pipeline already honors a token via its own per-step Run).
                token.ThrowIfCancellationRequested();

                if (step.Status == MigrationExecutionStepStatus.Completed || step.Status == MigrationExecutionStepStatus.Skipped)
                    continue;

                if (step.DependsOn.Count > 0)
                {
                    var depBlocked = step.DependsOn.Any(dep => checkpoint.Steps.All(item =>
                        item.Sequence != dep || item.Status != MigrationExecutionStepStatus.Completed));
                    if (depBlocked)
                    {
                        step.Status = MigrationExecutionStepStatus.Failed;
                        step.Message = "Dependency steps are not complete.";
                        RecordOperationKindCompleted(step.OperationKind, success: false);
                        checkpoint.HasFailed = true;
                        checkpoint.FailureCategory = "Dependency";
                        checkpoint.FailureReason = step.Message;
                        checkpoint.UpdatedOnUtc = DateTime.UtcNow;
                        checkpoint.ElapsedMilliseconds += runStopwatch.ElapsedMilliseconds;
                        PersistExecutionCheckpoint(checkpoint);
                        result.Success = false;
                        var outcomes = DescribeFailureRollbackOutcomes(plan, step);
                        result.RollbackOutcome = outcomes.rollbackOutcome;
                        result.CompensationOutcome = outcomes.compensationOutcome;
                        result.Message = $"Execution blocked at step {step.Sequence} due to dependency failure. {result.RollbackOutcome} {result.CompensationOutcome}";
                        result.AppliedCount = checkpoint.Steps.Count(item => item.Status == MigrationExecutionStepStatus.Completed);
                        RecordDiagnostic(checkpoint.ExecutionToken, checkpoint.CorrelationId, "exec-dependency-failed", MigrationDiagnosticSeverity.Error, step.EntityName, step.Message, "Resolve upstream step failures before resume.");
                        RecordExecutionFinished(plan, checkpoint, success: false, notes: result.Message);
                        return result;
                    }
                }

                var stepWatch = Stopwatch.StartNew();

                // ── Per-step retry, delegated to the shared pipeline ─────────────
                //
                // The pipeline runs the inner `while (!completed)` loop for us.
                // On GiveUp, the inner-loop state is captured into closure
                // variables (giveUpDecision, giveUpMessage, giveUpRequiresIntervention)
                // so the OUTER code can decide whether to abort the whole plan
                // (policy.AbortOnStepFailure == true) or continue to the next step.
                //
                // The plan-level `token` is passed to the pipeline, so cancelling the
                // outer token mid-retry will cancel the per-step Backoff sleep (and
                // any cancellable Run that respects the token).
                string? giveUpDecision   = null;
                string? giveUpMessage    = null;
                bool giveUpRequiresIntervention = false;
                bool gaveUp = false;

                var stepResult = await RetryPipeline.Instance.ExecuteAsync(new RetryPlan<IErrorsInfo>
                {
                    MaxAttempts = Math.Max(1, policy.MaxTransientRetries + 1),
                    LoggerTag   = "Migration",

                    Backoff = _ => TimeSpan.FromMilliseconds(policy.RetryDelayMilliseconds),

                    Classify = ctx =>
                    {
                        if (ctx.LastResult == null) return RetryDecision.Retry;     // exception path — retry
                        if (IsStepSuccess(ctx.LastResult)) return RetryDecision.Succeed;
                        var decision = ClassifyFailure(ctx.LastResult.Message, policy);
                        // MaxTransientRetries is on the policy; MaxAttempts already
                        // encodes it (1 + MaxTransientRetries) in the plan above.
                        return decision == "transient" ? RetryDecision.Retry : RetryDecision.GiveUp;
                    },

                    BeforeAttempt = (ctx, tok) =>
                    {
                        step.AttemptCount = ctx.Attempt;
                        step.Status = MigrationExecutionStepStatus.Running;
                        PersistExecutionCheckpoint(checkpoint);
                        return Task.CompletedTask;
                    },

                    Run = (ctx, tok) =>
                    {
                        // ExecuteStep is synchronous in the current code; preserve that
                        // by wrapping the result in Task.FromResult.
                        return Task.FromResult(ExecuteStep(step));
                    },

                    OnSuccess = (ctx, result, tok) =>
                    {
                        step.Status = MigrationExecutionStepStatus.Completed;
                        step.Message = result?.Message ?? "Completed.";
                        checkpoint.LastCompletedStep = step.Sequence;
                        checkpoint.HasFailed = false;
                        checkpoint.FailureCategory = string.Empty;
                        checkpoint.FailureReason = string.Empty;
                        progress?.Report(new PassedArgs
                        {
                            Messege = $"Migration step {step.Sequence} completed: {step.EntityName} [{step.OperationKind}]"
                        });
                        return Task.CompletedTask;
                    },

                    OnGiveUp = (ctx, result, decision, tok) =>
                    {
                        var lastMessage = result?.Message ?? ctx.FailureMessage ?? "Failed.";
                        giveUpDecision = ClassifyFailure(lastMessage, policy);
                        giveUpMessage  = lastMessage;
                        giveUpRequiresIntervention = giveUpDecision == "hard" && policy.RequireOperatorInterventionOnHardFail;
                        step.Status = MigrationExecutionStepStatus.Failed;
                        step.Message = lastMessage;
                        RecordOperationKindCompleted(step.OperationKind, success: false);
                        checkpoint.HasFailed = true;
                        checkpoint.FailureCategory = giveUpDecision == "hard" ? "HardFail" : "Failure";
                        checkpoint.FailureReason = step.Message;
                        checkpoint.UpdatedOnUtc = DateTime.UtcNow;
                        step.ElapsedMilliseconds += stepWatch.ElapsedMilliseconds;
                        checkpoint.ElapsedMilliseconds += runStopwatch.ElapsedMilliseconds;
                        PersistExecutionCheckpoint(checkpoint);

                        RecordDiagnostic(
                            checkpoint.ExecutionToken,
                            checkpoint.CorrelationId,
                            "exec-step-failed",
                            giveUpRequiresIntervention ? MigrationDiagnosticSeverity.Critical : MigrationDiagnosticSeverity.Error,
                            step.EntityName,
                            step.Message,
                            string.Empty /* compensation outcome filled below */);
                        gaveUp = true;
                        return Task.CompletedTask;
                    }
                }, token /* per-step retry honors plan-level cancellation */);

                if (gaveUp)
                {
                    // The pipeline surfaced a GiveUp. Decide whether to abort the
                    // whole plan (default behavior, preserved exactly) or continue
                    // to the next step (new behavior, opt-in via policy).
                    if (policy.AbortOnStepFailure)
                    {
                        // Preserve original semantics: build the failure result
                        // and return. Same shape as the inlined code used to produce.
                        result.Success = false;
                        result.RequiresOperatorIntervention = giveUpRequiresIntervention;
                        var outcomes = DescribeFailureRollbackOutcomes(plan, step);
                        result.RollbackOutcome      = outcomes.rollbackOutcome;
                        result.CompensationOutcome  = outcomes.compensationOutcome;
                        result.Message = result.RequiresOperatorIntervention
                            ? $"Step {step.Sequence} failed and requires operator intervention. {policy.OperatorInterventionHint} {result.RollbackOutcome} {result.CompensationOutcome}"
                            : $"Step {step.Sequence} failed: {giveUpMessage}. {result.RollbackOutcome} {result.CompensationOutcome}";
                        result.AppliedCount = checkpoint.Steps.Count(item => item.Status == MigrationExecutionStepStatus.Completed);
                        result.FailedSteps.Add(step.Sequence);
                        RecordExecutionFinished(plan, checkpoint, success: false, notes: result.Message);
                        return result;
                    }

                    // policy.AbortOnStepFailure == false: continue to the next step.
                    // The failure is recorded in checkpoint.Steps[].Status = Failed
                    // and in result.FailedSteps; the final result will reflect it.
                    result.FailedSteps.Add(step.Sequence);
                }

                step.ElapsedMilliseconds += stepWatch.ElapsedMilliseconds;
                RecordStepDuration(stepWatch.ElapsedMilliseconds, step.OperationKind);
                RecordOperationKindCompleted(step.OperationKind, success: true);
                checkpoint.UpdatedOnUtc = DateTime.UtcNow;
                PersistExecutionCheckpoint(checkpoint);

                processedInBatch++;
                if (performancePolicy.EnableThrottledMode && performancePolicy.ThrottleDelayMilliseconds > 0)
                {
                    Thread.Sleep(performancePolicy.ThrottleDelayMilliseconds);
                }

                if (performancePolicy.BatchSize > 0 && processedInBatch >= performancePolicy.BatchSize)
                {
                    processedInBatch = 0;
                    RecordDiagnostic(
                        checkpoint.ExecutionToken,
                        checkpoint.CorrelationId,
                        "exec-batch-boundary",
                        MigrationDiagnosticSeverity.Info,
                        step.EntityName,
                        $"Batch boundary reached after {performancePolicy.BatchSize} operation(s).",
                        "Continue with next batch to reduce lock pressure.");
                }
            }

            checkpoint.IsCompleted = true;
            checkpoint.HasFailed = false;
            checkpoint.FailureCategory = string.Empty;
            checkpoint.FailureReason = string.Empty;
            checkpoint.ElapsedMilliseconds += runStopwatch.ElapsedMilliseconds;
            checkpoint.UpdatedOnUtc = DateTime.UtcNow;
            PersistExecutionCheckpoint(checkpoint);

            result.Success = true;
            result.AppliedCount = checkpoint.Steps.Count(item => item.Status == MigrationExecutionStepStatus.Completed);
            result.Message = $"Migration plan executed successfully. Token: {checkpoint.ExecutionToken}";
            RecordDiagnostic(checkpoint.ExecutionToken, checkpoint.CorrelationId, "exec-complete", MigrationDiagnosticSeverity.Info, string.Empty, result.Message, "Execution completed without blocking failures.");
            RecordExecutionFinished(plan, checkpoint, success: true, notes: result.Message);
            return result;
        }

        public MigrationExecutionResult ResumeMigrationPlan(string executionToken, MigrationExecutionPolicy policy = null, IProgress<PassedArgs> progress = null)
        {
            policy ??= new MigrationExecutionPolicy();
            if (string.IsNullOrWhiteSpace(executionToken))
            {
                return new MigrationExecutionResult
                {
                    Success = false,
                    Message = "Execution token is required to resume."
                };
            }

            if (!ExecutionCheckpoints.TryGetValue(executionToken.Trim(), out var checkpoint))
            {
                return new MigrationExecutionResult
                {
                    Success = false,
                    Message = $"No checkpoint found for execution token '{executionToken}'."
                };
            }

            if (checkpoint.IsCompleted)
            {
                return new MigrationExecutionResult
                {
                    Success = true,
                    ExecutionToken = checkpoint.ExecutionToken,
                    Checkpoint = checkpoint,
                    ResumedFromCheckpoint = true,
                    Message = "Execution checkpoint is already completed."
                };
            }

            if (!ExecutionPlans.TryGetValue(checkpoint.ExecutionToken, out var plan))
            {
                plan = BuildPlanFromCheckpoint(checkpoint);
                ExecutionPlans[checkpoint.ExecutionToken] = plan;
            }

            var result = ExecuteMigrationPlan(plan, policy, checkpoint.ExecutionToken, progress);
            result.ResumedFromCheckpoint = true;
            return result;
        }

        public MigrationExecutionCheckpoint GetExecutionCheckpoint(string executionToken)
        {
            if (string.IsNullOrWhiteSpace(executionToken))
                return null;

            ExecutionCheckpoints.TryGetValue(executionToken.Trim(), out var checkpoint);
            return checkpoint;
        }

        private static MigrationExecutionCheckpoint BuildNewCheckpoint(MigrationPlanArtifact plan, string token)
        {
            return new MigrationExecutionCheckpoint
            {
                ExecutionToken = token,
                CorrelationId = Guid.NewGuid().ToString("N"),
                PlanId = plan.PlanId,
                PlanHash = plan.PlanHash,
                StartedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                Steps = BuildExecutionSteps(plan.Operations)
            };
        }

        private static List<MigrationExecutionStep> BuildExecutionSteps(IReadOnlyList<MigrationPlanOperation> operations)
        {
            var steps = new List<MigrationExecutionStep>();
            if (operations == null || operations.Count == 0)
                return steps;

            var previousByEntity = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < operations.Count; index++)
            {
                var operation = operations[index];
                if (operation == null)
                    continue;

                var sequence = index + 1;
                var entityName = operation.EntityName ?? string.Empty;
                var step = new MigrationExecutionStep
                {
                    Sequence = sequence,
                    StepId = $"step-{sequence}",
                    EntityName = entityName,
                    EntityTypeName = operation.EntityTypeName ?? string.Empty,
                    OperationKind = operation.Kind,
                    MissingColumns = operation.MissingColumns?.ToList() ?? new List<string>(),
                    // Carry the actual constraint / index name from the plan op. The
                    // executor uses this for DropForeignKey and DropIndex steps so it
                    // does not need to re-derive the name from a (possibly missing)
                    // desired structure. Empty when the plan op is for a Create or for
                    // generic Add operations where the executor re-derives from the
                    // desired structure.
                    TargetName = operation.TargetName ?? string.Empty,
                    Status = operation.Kind == MigrationPlanOperationKind.UpToDate
                        ? MigrationExecutionStepStatus.Skipped
                        : MigrationExecutionStepStatus.Pending
                };

                if (!string.IsNullOrWhiteSpace(entityName) && previousByEntity.TryGetValue(entityName, out var previous))
                    step.DependsOn.Add(previous);
                if (!string.IsNullOrWhiteSpace(entityName))
                    previousByEntity[entityName] = sequence;

                steps.Add(step);
            }

            return steps;
        }

        private IErrorsInfo ExecuteStep(MigrationExecutionStep step)
        {
            if (step == null)
                return CreateErrorsInfo(Errors.Failed, "Execution step is null.");

            if (step.OperationKind == MigrationPlanOperationKind.UpToDate)
                return CreateErrorsInfo(Errors.Ok, "Step skipped: entity already up to date.");

            if (step.OperationKind == MigrationPlanOperationKind.Error)
                return CreateErrorsInfo(Errors.Failed, "Step is marked as plan error and cannot be executed.");

            var entityType = ResolveType(step.EntityTypeName);
            var desired = entityType != null
                ? TryGetEntityStructure(entityType)
                : ResolveCachedEntityStructure(step.EntityTypeName, step.EntityName);
            if (desired != null && !string.IsNullOrWhiteSpace(step.EntityName))
                desired.EntityName = step.EntityName;

            switch (step.OperationKind)
            {
                case MigrationPlanOperationKind.CreateEntity:
                    if (desired == null)
                        return CreateErrorsInfo(Errors.Failed, $"Cannot resolve entity metadata for '{step.EntityName}'.");
                    return CreateEntity(desired);

                case MigrationPlanOperationKind.AddMissingColumns:
                    if (desired == null)
                        return CreateErrorsInfo(Errors.Failed, $"Cannot resolve entity metadata for '{step.EntityName}'.");
                    if (step.MissingColumns == null || step.MissingColumns.Count == 0)
                        return CreateErrorsInfo(Errors.Warning, $"No missing columns recorded for '{step.EntityName}'.");

                    var failures = new List<string>();
                    foreach (var columnName in step.MissingColumns)
                    {
                        var field = desired.Fields?.FirstOrDefault(candidate =>
                            candidate != null &&
                            string.Equals(candidate.FieldName, columnName, StringComparison.OrdinalIgnoreCase));
                        if (field == null)
                        {
                            failures.Add($"Column metadata not found for '{columnName}'.");
                            continue;
                        }

                        var addResult = AddColumn(desired, field);
                        if (!IsStepSuccess(addResult))
                            failures.Add($"{columnName}: {addResult?.Message}");
                    }

                    if (failures.Count > 0)
                        return CreateErrorsInfo(Errors.Failed, string.Join("; ", failures));
                    return CreateErrorsInfo(Errors.Ok, $"Added {step.MissingColumns.Count} column(s) to '{step.EntityName}'.");

                case MigrationPlanOperationKind.AddForeignKey:
                    if (desired == null)
                        return CreateErrorsInfo(Errors.Failed, $"Cannot resolve entity metadata for '{step.EntityName}'.");
                    if (string.IsNullOrWhiteSpace(step.EntityName))
                        return CreateErrorsInfo(Errors.Failed, "AddForeignKey step is missing the entity name.");
                    // When the step carries a specific TargetName, apply only
                    // the matching FK rather than every relation on the entity.
                    // Without this, a plan with 3 FK ops on the same entity
                    // would apply all 3 on the first step (duplicating work or
                    // tripping over already-existing constraints).
                    var fkFailures = !string.IsNullOrWhiteSpace(step.TargetName)
                        ? ApplyForeignKeysForEntity(desired, step.TargetName)
                        : ApplyForeignKeysForEntity(desired);
                    if (fkFailures != null && fkFailures.Count > 0)
                        return CreateErrorsInfo(Errors.Failed, $"Foreign-key apply failed for '{step.EntityName}': {string.Join("; ", fkFailures)}");
                    return CreateErrorsInfo(Errors.Ok, $"Foreign key applied to '{step.EntityName}'.");

                case MigrationPlanOperationKind.DropForeignKey:
                    if (string.IsNullOrWhiteSpace(step.EntityName))
                        return CreateErrorsInfo(Errors.Failed, "DropForeignKey step is missing the entity name.");
                    // TargetName carries the actual constraint name from the
                    // plan op. When it's missing the plan was built without
                    // the name — usually a pre-pass-6 plan or a tool that
                    // didn't populate RalationName on the RelationShipKeys.
                    // Diagnostic is clearer than falling back to step.StepId
                    // (e.g. "step-5") which is never a valid DB constraint name.
                    if (string.IsNullOrWhiteSpace(step.TargetName))
                        return CreateErrorsInfo(Errors.Failed, $"DropForeignKey step is missing the constraint name (TargetName) for entity '{step.EntityName}'; the plan must supply the FK name.");
                    return DropForeignKey(step.EntityName, step.TargetName);

                case MigrationPlanOperationKind.CreateIndex:
                    if (desired == null)
                        return CreateErrorsInfo(Errors.Failed, $"Cannot resolve entity metadata for '{step.EntityName}'.");
                    if (string.IsNullOrWhiteSpace(step.EntityName))
                        return CreateErrorsInfo(Errors.Failed, "CreateIndex step is missing the entity name.");
                    // Same scoping as AddForeignKey: when TargetName is set,
                    // apply only the targeted index.
                    var indexFailures = !string.IsNullOrWhiteSpace(step.TargetName)
                        ? ApplyIndexesForEntity(desired, step.TargetName)
                        : ApplyIndexesForEntity(desired);
                    if (indexFailures != null && indexFailures.Count > 0)
                        return CreateErrorsInfo(Errors.Failed, $"Index apply failed for '{step.EntityName}': {string.Join("; ", indexFailures)}");
                    return CreateErrorsInfo(Errors.Ok, $"Index applied to '{step.EntityName}'.");

                case MigrationPlanOperationKind.DropIndex:
                    if (string.IsNullOrWhiteSpace(step.EntityName))
                        return CreateErrorsInfo(Errors.Failed, "DropIndex step is missing the entity name.");
                    // Same diagnostic-fail pattern as DropForeignKey.
                    if (string.IsNullOrWhiteSpace(step.TargetName))
                        return CreateErrorsInfo(Errors.Failed, $"DropIndex step is missing the index name (TargetName) for entity '{step.EntityName}'; the plan must supply the index name.");
                    return DropIndex(step.EntityName, step.TargetName);

                default:
                    return CreateErrorsInfo(Errors.Failed, $"Operation '{step.OperationKind}' is not yet supported by execution orchestration.");
            }
        }

        private static bool IsStepSuccess(IErrorsInfo stepResult)
        {
            var flag = stepResult?.Flag.ToString() ?? string.Empty;
            return flag.Equals("Ok", StringComparison.OrdinalIgnoreCase) ||
                   flag.Equals("Warning", StringComparison.OrdinalIgnoreCase);
        }

        private static string ClassifyFailure(string message, MigrationExecutionPolicy policy)
        {
            var text = message ?? string.Empty;
            if (ContainsMarker(text, policy.HardFailMarkers))
                return "hard";
            if (ContainsMarker(text, policy.TransientErrorMarkers))
                return "transient";
            return "normal";
        }

        private static bool ContainsMarker(string text, IEnumerable<string> markers)
        {
            if (string.IsNullOrWhiteSpace(text) || markers == null)
                return false;

            foreach (var marker in markers.Where(item => !string.IsNullOrWhiteSpace(item)))
            {
                if (text.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private MigrationPlanArtifact BuildPlanFromCheckpoint(MigrationExecutionCheckpoint checkpoint)
        {
            var plan = new MigrationPlanArtifact
            {
                PlanId = checkpoint.PlanId,
                PlanHash = checkpoint.PlanHash,
                DataSourceName = MigrateDataSource?.DatasourceName ?? string.Empty,
                DataSourceType = MigrateDataSource?.DatasourceType ?? DataSourceType.Unknown,
                DataSourceCategory = MigrateDataSource?.Category ?? DatasourceCategory.NONE
            };

            foreach (var step in checkpoint.Steps.OrderBy(item => item.Sequence))
            {
                plan.Operations.Add(new MigrationPlanOperation
                {
                    EntityName = step.EntityName,
                    EntityTypeName = step.EntityTypeName,
                    Kind = step.OperationKind,
                    MissingColumns = step.MissingColumns?.ToList() ?? new List<string>(),
                    // Carry the constraint/index name through so resume can
                    // re-derive the right DDL for DropForeignKey / DropIndex
                    // without re-running planning. Without this, a resumed
                    // plan loses the FK/Index name and falls back to the
                    // synthetic step-N identifier in ExecuteStep.
                    TargetName = step.TargetName ?? string.Empty,
                    Note = step.Message
                });
            }

            return plan;
        }

        private void PersistExecutionCheckpoint(MigrationExecutionCheckpoint checkpoint)
        {
            if (checkpoint == null) return;
            checkpoint.UpdatedOnUtc = DateTime.UtcNow;
            ExecutionCheckpoints[checkpoint.ExecutionToken] = checkpoint;
            MigrationRecordWriter.WriteExecutionSnapshot(
                _editor,
                checkpoint,
                MigrateDataSource?.DatasourceName ?? string.Empty,
                MigrateDataSource?.DatasourceType ?? DataSourceType.Unknown);
        }
    }
}
