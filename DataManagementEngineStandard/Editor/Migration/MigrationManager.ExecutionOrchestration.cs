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
        private readonly ConcurrentDictionary<string, string> ExecutionCheckpoints = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, MigrationPlanArtifact> ExecutionPlans = new(StringComparer.OrdinalIgnoreCase);

        private MigrationExecutionCheckpoint CreatePlanningCheckpoint(MigrationPlanArtifact plan)
        {
            // A preview token reserves intent in this process, but is not a saved execution-start checkpoint.
            var checkpoint = BuildNewCheckpoint(plan, Guid.NewGuid().ToString("N"));
            ExecutionCheckpoints[checkpoint.ExecutionToken] = SerializeCheckpointSnapshot(checkpoint);
            ExecutionPlans[checkpoint.ExecutionToken] = CopyExecutionPlan(plan);
            return checkpoint;
        }

        public MigrationExecutionCheckpoint CreateExecutionCheckpoint(MigrationPlanArtifact plan, string executionToken = null)
        {
            if (plan == null) return new MigrationExecutionCheckpoint();
            if (!BeginOperation(out var scope)) throw new InvalidOperationException("A migration operation is already active.");
            var previous = _executionScope.Value;
            var result = new MigrationExecutionResult();
            _executionScope.Value = scope;
            try
            {
                plan = CopyExecutionPlan(plan);
                if (!ValidatePlanIntent(plan, out var error)) throw new InvalidOperationException(error);
                CaptureStorage(scope);
                executionToken = string.IsNullOrWhiteSpace(executionToken) ? Guid.NewGuid().ToString("N") : executionToken.Trim();
                var admission = Acquire(scope, executionToken, plan.PlanHash, CancellationToken.None);
                if (admission.Status != MigrationAdmissionStatus.Acquired)
                    throw new InvalidOperationException("Checkpoint ownership admission was denied (" + admission.Status + ").");
                scope.FinishDisposition = MigrationClaimDisposition.SafeToRetry;
                result.Checkpoint = CreateExecutionCheckpointCore(plan, executionToken, CancellationToken.None);
                result.CheckpointPersisted = result.Success = true;
            }
            catch (MigrationCheckpointPersistenceException ex)
            {
                result.RequiresReconciliation = ex.RequiresReconciliation;
                throw;
            }
            finally
            {
                if (scope.Lease != null) CompleteExecutionOwnership(scope, result);
                _executionScope.Value = previous;
                lock (_bindingGate) _operationActive = false;
            }
            if (!result.OwnershipFinished) throw new InvalidOperationException("Checkpoint ownership completion was not acknowledged; reconcile before replay.");
            return CopySnapshot(result.Checkpoint);
        }

        private MigrationExecutionCheckpoint CreateExecutionCheckpointCore(MigrationPlanArtifact plan, string executionToken, CancellationToken cancellationToken)
        {
            if (plan == null)
                return new MigrationExecutionCheckpoint();
            if (!ValidatePlanIntent(plan, out var intentError))
                throw new InvalidOperationException(intentError);

            var token = string.IsNullOrWhiteSpace(executionToken) ? Guid.NewGuid().ToString("N") : executionToken.Trim();
            var checkpoint = TryLoadPersistedCheckpoint(token);
            if (checkpoint == null)
            {
                checkpoint = BuildNewCheckpoint(plan, token);
                checkpoint.OwnershipTargetKey = _executionScope.Value.TargetKey;
                checkpoint.OwnershipStoreIdentity = _executionScope.Value.Storage.ScopeIdentity;
            }
            if (!CheckpointScopeMatches(checkpoint)) throw new MigrationCheckpointPersistenceException(checkpoint,
                new PersistenceWriteResult(PersistenceWriteStatus.Failed), requiresReconciliation: true);
            if (checkpoint.RequiresReconciliation || checkpoint.CompensationSteps.Count > 0 ||
                checkpoint.Steps.Any(s => s.Status == MigrationExecutionStepStatus.Running))
                throw new MigrationCheckpointPersistenceException(checkpoint,
                    new PersistenceWriteResult(PersistenceWriteStatus.Failed), requiresReconciliation: true);
            _executionScope.Value.Checkpoint = checkpoint;
            if (!string.Equals(checkpoint.PlanHash, plan.PlanHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Execution token belongs to a different migration plan hash.");
            if (!ValidateCheckpointIntent(checkpoint, plan))
                throw new MigrationCheckpointPersistenceException(checkpoint,
                    new PersistenceWriteResult(PersistenceWriteStatus.Failed), requiresReconciliation: true);
            checkpoint.ApprovedPlan = CopyExecutionPlan(plan);
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

            ExecutionPlans[token] = CopyExecutionPlan(plan);
            PersistExecutionCheckpoint(checkpoint, cancellationToken);
            return checkpoint;
        }

        public MigrationExecutionResult ExecuteMigrationPlan(MigrationPlanArtifact plan, MigrationExecutionPolicy policy = null, string executionToken = null, IProgress<PassedArgs> progress = null, MigrationPolicyOptions policyOptions = null)
        {
            // Task.Run must wrap the CALL: invoking an async method runs it synchronously to its
            // first await, where it captures the caller's SynchronizationContext. Starting it on
            // the pool means there is no context to capture, so a UI caller blocked here in
            // GetResult() cannot deadlock against its own continuation.
            return Task.Run(() => ExecuteMigrationPlanAsync(plan, policy, executionToken, progress, CancellationToken.None, policyOptions))
                .GetAwaiter().GetResult();
        }

        private async Task<MigrationExecutionResult> ExecuteMigrationPlanCoreAsync(
            MigrationPlanArtifact plan,
            MigrationExecutionPolicy policy = null,
            string executionToken = null,
            IProgress<PassedArgs> progress = null,
            CancellationToken token = default,
            MigrationPolicyOptions policyOptions = null)
        {
            var result = new MigrationExecutionResult();
            var acknowledgedSteps = new HashSet<int>();
            bool providerInvoked = false;

            try
            {
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

                if (!ValidatePlanIntent(plan, out var intentError))
                {
                    result.Message = intentError;
                    return result;
                }
                if (policy != null && !JTokenEquals(policy, plan.ExecutionPolicy))
                {
                    result.Message = "Execution policy differs from the approved plan. Create a policy revision and re-approve it.";
                    return result;
                }
                if (policyOptions != null && !GovernanceMatches(policyOptions, plan.GovernancePolicy))
                {
                    result.Message = "Governance policy differs from the approved plan. Create a revision and re-approve it.";
                    return result;
                }
                if (!string.IsNullOrWhiteSpace(policyOptions?.Approver) &&
                    !string.Equals(policyOptions.ApprovedPlanHash, plan.PlanHash, StringComparison.Ordinal))
                {
                    result.Message = "Approval is not bound to the current migration plan hash.";
                    return result;
                }
                // Run from a private copy, not caller-owned lists or metadata references.
                plan = CopyExecutionPlan(plan);
                policy = CopySnapshot(plan.ExecutionPolicy);
                policyOptions = CopySnapshot(policyOptions ?? plan.GovernancePolicy);
                token.ThrowIfCancellationRequested();
                var existing = string.IsNullOrWhiteSpace(executionToken) ? null : TryLoadPersistedCheckpoint(executionToken);
                if (existing != null && existing.PlanHash != plan.PlanHash)
                { result.Message = "Execution token belongs to a different migration plan hash. Create a new token."; return result; }
                if (existing?.CompensationCompleted == true)
                { result.RequiresOperatorIntervention = true; result.Message = "Execution was compensated. Rebuild and re-approve with a new token."; return result; }
                if (existing != null && !CheckpointScopeMatches(existing))
                {
                    result.RequiresReconciliation = result.RequiresOperatorIntervention = true;
                    result.Message = "Checkpoint ownership scope is legacy or differs from the captured target/store.";
                    return result;
                }
                if (existing != null && (existing.RequiresReconciliation || existing.CompensationSteps.Count > 0 ||
                    existing.Steps?.Any(item => item.Status == MigrationExecutionStepStatus.Running) == true))
                {
                    result.ExecutionToken = existing.ExecutionToken;
                    result.Checkpoint = existing;
                    result.RequiresOperatorIntervention = true;
                    result.RequiresReconciliation = true;
                    result.Message = "Checkpoint requires provider-state reconciliation before replay.";
                    return result;
                }
                if (existing?.IsCompleted == true && ValidateCheckpointIntent(existing, plan))
                {
                    result.ExecutionToken = existing.ExecutionToken;
                    result.Checkpoint = existing;
                    result.Success = result.CheckpointPersisted = true;
                    result.CheckpointPersistenceStatus = PersistenceWriteStatus.Saved;
                    result.AppliedCount = existing.Steps.Count(s => s.Status == MigrationExecutionStepStatus.Completed);
                    result.Message = "Execution checkpoint is already completed.";
                    return result;
                }
                var checkpoint = CreateExecutionCheckpointCore(plan, executionToken, token);
                result.ExecutionToken = checkpoint.ExecutionToken;
                result.Checkpoint = checkpoint;
                result.CheckpointPersisted = true;
                result.CheckpointPersistenceStatus = PersistenceWriteStatus.Saved;
                foreach (var completed in checkpoint.Steps.Where(item => item.Status == MigrationExecutionStepStatus.Completed))
                    acknowledgedSteps.Add(completed.Sequence);
                _executionScope.Value.AcknowledgedCount = acknowledgedSteps.Count;

                if (!string.IsNullOrWhiteSpace(checkpoint.PlanHash) &&
                    !string.IsNullOrWhiteSpace(plan.PlanHash) &&
                    !string.Equals(checkpoint.PlanHash, plan.PlanHash, StringComparison.Ordinal))
                {
                    result.Success = false;
                    result.Message = "Execution token belongs to a different migration plan hash. Create a new execution token or resume the original plan.";
                    return result;
                }
                if (!ValidateCheckpointIntent(checkpoint, plan))
                {
                    result.Message = "Execution checkpoint intent is missing or differs from the approved plan. Rebuild and reconcile before resume.";
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

                // Honor caller-supplied governance options (approver/override for high-risk or
                // destructive plans) so an approved destructive plan can pass the preflight policy gate.
                plan.PreflightReport = RunPreflightChecks(plan, policyOptions);
                if (!plan.PreflightReport.CanApply)
                {
                    result.Success = false;
                    result.Message = "Preflight blocked migration plan execution.";
                    result.Checkpoint.HasFailed = true;
                    result.Checkpoint.FailureCategory = "Preflight";
                    result.Checkpoint.FailureReason = result.Message;
                    result.Checkpoint.UpdatedOnUtc = DateTime.UtcNow;
                    RecordDiagnostic(checkpoint.ExecutionToken, checkpoint.CorrelationId, "exec-preflight-block", MigrationDiagnosticSeverity.Error, string.Empty, result.Message, "Review preflight findings and regenerate plan if needed.");
                    PersistExecutionCheckpoint(result.Checkpoint, token);
                    RecordExecutionFinished(plan, checkpoint, success: false, notes: result.Message);
                    return result;
                }
                var performancePolicy = plan.PerformancePlan?.Policy ?? new MigrationPerformancePolicy();
                var runStopwatch = Stopwatch.StartNew();
                var processedInBatch = 0;

                var approvedSteps = BuildExecutionSteps(plan.Operations).ToDictionary(item => item.Sequence);
                var iterations = checkpoint.Steps.OrderBy(item => item.Sequence)
                    .Select(item => (Progress: item, Intent: approvedSteps[item.Sequence])).ToArray();
                foreach (var iteration in iterations)
                {
                    var step = iteration.Progress;
                    var intent = iteration.Intent;
                    // Plan-level cancellation: checked between steps, not within a step's
                    // retry sequence. Cancellation mid-step is the caller's responsibility
                    // (the pipeline already honors a token via its own per-step Run).
                    token.ThrowIfCancellationRequested();

                    if (step.Status == MigrationExecutionStepStatus.Completed || step.Status == MigrationExecutionStepStatus.Skipped)
                        continue;

                    if (intent.DependsOn.Count > 0)
                    {
                        var depBlocked = intent.DependsOn.Any(dep => checkpoint.Steps.All(item =>
                            item.Sequence != dep || (item.Status != MigrationExecutionStepStatus.Completed &&
                                                     item.Status != MigrationExecutionStepStatus.Skipped)));
                        if (depBlocked)
                        {
                            step.Status = MigrationExecutionStepStatus.Failed;
                            step.Message = "Dependency steps are not complete.";
                            RecordOperationKindCompleted(intent.OperationKind, success: false);
                            checkpoint.HasFailed = true;
                            checkpoint.FailureCategory = "Dependency";
                            checkpoint.FailureReason = step.Message;
                            checkpoint.UpdatedOnUtc = DateTime.UtcNow;
                            checkpoint.ElapsedMilliseconds += runStopwatch.ElapsedMilliseconds;
                            PersistExecutionCheckpoint(checkpoint, token);
                            result.Success = false;
                            var outcomes = DescribeFailureRollbackOutcomes(plan, step);
                            result.RollbackOutcome = outcomes.rollbackOutcome;
                            result.CompensationOutcome = outcomes.compensationOutcome;
                            result.Message = $"Execution blocked at step {intent.Sequence} due to dependency failure. {result.RollbackOutcome} {result.CompensationOutcome}";
                            result.AppliedCount = checkpoint.Steps.Count(item => item.Status == MigrationExecutionStepStatus.Completed);
                            RecordDiagnostic(checkpoint.ExecutionToken, checkpoint.CorrelationId, "exec-dependency-failed", MigrationDiagnosticSeverity.Error, intent.EntityName, step.Message, "Resolve upstream step failures before resume.");
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
                    MigrationCheckpointPersistenceException persistenceFailure = null;

                    var stepResult = await RetryPipeline.Instance.ExecuteAsync(new RetryPlan<IErrorsInfo>
                    {
                        MaxAttempts = Math.Max(1, policy.MaxTransientRetries + 1),
                        LoggerTag   = "Migration",

                        Backoff = _ => TimeSpan.FromMilliseconds(policy.RetryDelayMilliseconds),

                        Classify = ctx =>
                        {
                            if (persistenceFailure != null) return RetryDecision.GiveUp;
                            if (IsStepSuccess(ctx.LastResult)) return RetryDecision.Succeed;
                            // A negative/throwing DDL acknowledgement does not prove no change.
                            if (providerInvoked) return RetryDecision.GiveUp;
                            if (ctx.LastResult == null) return RetryDecision.Retry;
                            var decision = ClassifyFailure(ctx.LastResult.Message, policy);
                            // MaxTransientRetries is on the policy; MaxAttempts already
                            // encodes it (1 + MaxTransientRetries) in the plan above.
                            return decision == "transient" ? RetryDecision.Retry : RetryDecision.GiveUp;
                        },

                        BeforeAttempt = (ctx, tok) =>
                        {
                            var previousStatus = step.Status;
                            step.AttemptCount = ctx.Attempt;
                            step.Status = MigrationExecutionStepStatus.Running;
                            try { PersistExecutionCheckpoint(checkpoint, tok); }
                            catch (MigrationCheckpointPersistenceException ex)
                            {
                                persistenceFailure = ex;
                                step.Status = previousStatus;
                            }
                            return Task.CompletedTask;
                        },

                        Run = (ctx, tok) =>
                        {
                            // ExecuteStep is synchronous in the current code; preserve that
                            // by wrapping the result in Task.FromResult.
                            // RetryPipeline intentionally ignores hook exceptions; use an explicit
                            // admission sentinel so a failed before-attempt save cannot run DDL.
                            if (persistenceFailure != null)
                                return Task.FromResult<IErrorsInfo>(new ErrorsInfo { Flag = Errors.Failed, Message = "Checkpoint persistence failed." });
                            tok.ThrowIfCancellationRequested();
                            if (CaptureTargetFingerprint() != _executionScope.Value.TargetFingerprint)
                                throw new InvalidOperationException("Migration target changed during execution.");
                            providerInvoked = true;
                            _executionScope.Value.ProviderInvoked = true;
                            var acknowledgement = ExecuteStep(intent, plan);
                            if (IsStepSuccess(acknowledgement)) acknowledgedSteps.Add(intent.Sequence);
                            _executionScope.Value.AcknowledgedCount = acknowledgedSteps.Count;
                            return Task.FromResult(acknowledgement);
                        },

                        OnSuccess = (ctx, result, tok) =>
                        {
                            step.Status = MigrationExecutionStepStatus.Completed;
                            step.Message = result?.Message ?? "Completed.";
                            checkpoint.LastCompletedStep = intent.Sequence;
                            checkpoint.HasFailed = false;
                            checkpoint.FailureCategory = string.Empty;
                            checkpoint.FailureReason = string.Empty;
                            progress?.Report(new PassedArgs
                            {
                                Messege = $"Migration step {intent.Sequence} completed: {intent.EntityName} [{intent.OperationKind}]"
                            });
                            return Task.CompletedTask;
                        },

                        OnGiveUp = (ctx, result, decision, tok) =>
                        {
                            if (persistenceFailure != null) return Task.CompletedTask;
                            gaveUp = true;
                            var lastMessage = result?.Message ?? ctx.FailureMessage ?? "Failed.";
                            giveUpDecision = ClassifyFailure(lastMessage, policy);
                            giveUpMessage  = lastMessage;
                            giveUpRequiresIntervention = giveUpDecision == "hard" && policy.RequireOperatorInterventionOnHardFail;
                            step.Status = MigrationExecutionStepStatus.Failed;
                            step.Message = lastMessage;
                            RecordOperationKindCompleted(intent.OperationKind, success: false);
                            checkpoint.HasFailed = true;
                            checkpoint.FailureCategory = giveUpDecision == "hard" ? "HardFail" : "Failure";
                            checkpoint.FailureReason = step.Message;
                            checkpoint.UpdatedOnUtc = DateTime.UtcNow;
                            step.ElapsedMilliseconds += stepWatch.ElapsedMilliseconds;
                            checkpoint.ElapsedMilliseconds += runStopwatch.ElapsedMilliseconds;
                            try { PersistExecutionCheckpoint(checkpoint, tok); }
                            catch (MigrationCheckpointPersistenceException ex) { persistenceFailure = ex; }

                            RecordDiagnostic(
                                checkpoint.ExecutionToken,
                                checkpoint.CorrelationId,
                                "exec-step-failed",
                                giveUpRequiresIntervention ? MigrationDiagnosticSeverity.Critical : MigrationDiagnosticSeverity.Error,
                                intent.EntityName,
                                step.Message,
                                string.Empty /* compensation outcome filled below */);
                            gaveUp = true;
                            return Task.CompletedTask;
                        }
                    }, token /* per-step retry honors plan-level cancellation */);

                    if (persistenceFailure != null) throw persistenceFailure;

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
                                ? $"Step {intent.Sequence} failed and requires operator intervention. {policy.OperatorInterventionHint} {result.RollbackOutcome} {result.CompensationOutcome}"
                                : $"Step {intent.Sequence} failed: {giveUpMessage}. {result.RollbackOutcome} {result.CompensationOutcome}";
                            result.AppliedCount = checkpoint.Steps.Count(item => item.Status == MigrationExecutionStepStatus.Completed);
                            result.FailedSteps.Add(intent.Sequence);
                            RecordExecutionFinished(plan, checkpoint, success: false, notes: result.Message);
                            return result;
                        }

                        // policy.AbortOnStepFailure == false: continue to the next step.
                        // The failure is recorded in checkpoint.Steps[].Status = Failed
                        // and in result.FailedSteps; the final result will reflect it.
                        result.FailedSteps.Add(intent.Sequence);
                    }

                    step.ElapsedMilliseconds += stepWatch.ElapsedMilliseconds;
                    RecordStepDuration(stepWatch.ElapsedMilliseconds, intent.OperationKind);
                    RecordOperationKindCompleted(intent.OperationKind, success: true);
                    checkpoint.UpdatedOnUtc = DateTime.UtcNow;
                    PersistExecutionCheckpoint(checkpoint, token);

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
                            intent.EntityName,
                            $"Batch boundary reached after {performancePolicy.BatchSize} operation(s).",
                            "Continue with next batch to reduce lock pressure.");
                    }
                }

                if (result.FailedSteps.Count > 0)
                {
                    checkpoint.IsCompleted = false;
                    checkpoint.HasFailed = true;
                    checkpoint.FailureCategory = "PartialFailure";
                    checkpoint.FailureReason = $"{result.FailedSteps.Count} migration step(s) failed; reconcile before resume.";
                    checkpoint.ElapsedMilliseconds += runStopwatch.ElapsedMilliseconds;
                    PersistExecutionCheckpoint(checkpoint, token);
                    result.Success = false;
                    result.AppliedCount = checkpoint.Steps.Count(item => item.Status == MigrationExecutionStepStatus.Completed);
                    result.RequiresOperatorIntervention = policy.RequireOperatorInterventionOnHardFail &&
                        checkpoint.Steps.Any(item => item.Status == MigrationExecutionStepStatus.Failed &&
                            ClassifyFailure(item.Message, policy) == "hard");
                    result.Message = checkpoint.FailureReason;
                    RecordExecutionFinished(plan, checkpoint, success: false, notes: result.Message);
                    return result;
                }

                checkpoint.IsCompleted = true;
                checkpoint.HasFailed = false;
                checkpoint.FailureCategory = string.Empty;
                checkpoint.FailureReason = string.Empty;
                checkpoint.ElapsedMilliseconds += runStopwatch.ElapsedMilliseconds;
                checkpoint.UpdatedOnUtc = DateTime.UtcNow;
                PersistExecutionCheckpoint(checkpoint, token);

                result.CheckpointPersisted = true;
                result.CheckpointPersistenceStatus = PersistenceWriteStatus.Saved;
                result.Success = true;
                result.AppliedCount = checkpoint.Steps.Count(item => item.Status == MigrationExecutionStepStatus.Completed);
                result.Message = $"Migration plan executed successfully. Token: {checkpoint.ExecutionToken}";
                RecordDiagnostic(checkpoint.ExecutionToken, checkpoint.CorrelationId, "exec-complete", MigrationDiagnosticSeverity.Info, string.Empty, result.Message, "Execution completed without blocking failures.");
                RecordExecutionFinished(plan, checkpoint, success: true, notes: result.Message);
                return result;
            }
            catch (MigrationCheckpointPersistenceException ex)
            {
                var checkpoint = ex.Checkpoint;
                var primaryFailure = checkpoint.HasFailed ? checkpoint.FailureReason : string.Empty;
                checkpoint.HasFailed = true;
                checkpoint.IsCompleted = false;
                checkpoint.RequiresReconciliation = ex.RequiresReconciliation || providerInvoked || acknowledgedSteps.Count > 0;
                checkpoint.FailureCategory = "Persistence";
                checkpoint.FailureReason = string.IsNullOrWhiteSpace(primaryFailure)
                    ? "Mandatory checkpoint persistence failed." : primaryFailure;
                result.Success = false;
                result.ExecutionToken = checkpoint.ExecutionToken;
                result.Checkpoint = checkpoint;
                result.CheckpointPersisted = false;
                result.CheckpointPersistenceStatus = ex.Outcome.Status;
                result.AppliedCount = acknowledgedSteps.Count;
                result.RequiresReconciliation = checkpoint.RequiresReconciliation;
                result.RequiresOperatorIntervention = checkpoint.RequiresReconciliation;
                result.FailedSteps = checkpoint.Steps.Where(item => item.Status == MigrationExecutionStepStatus.Failed)
                    .Select(item => item.Sequence).ToList();
                result.Message = checkpoint.FailureReason + " Checkpoint was not acknowledged (" + ex.Outcome.Status +
                    "); " + (checkpoint.RequiresReconciliation ? "reconcile provider state before replay." : "no DDL was admitted.");
                return result;
            }
        }

        public MigrationExecutionResult ResumeMigrationPlan(string executionToken, MigrationExecutionPolicy policy = null, IProgress<PassedArgs> progress = null, MigrationPolicyOptions policyOptions = null)
        {
            if (string.IsNullOrWhiteSpace(executionToken))
            {
                return new MigrationExecutionResult
                {
                    Success = false,
                    Message = "Execution token is required to resume."
                };
            }

            return Task.Run(() => ExecuteMigrationPlanWithOwnershipAsync(null, policy,
                executionToken.Trim(), progress, CancellationToken.None, policyOptions, true)).GetAwaiter().GetResult();
        }

        public MigrationExecutionCheckpoint GetExecutionCheckpoint(string executionToken)
        {
            if (string.IsNullOrWhiteSpace(executionToken))
                return null;

            var scope = _executionScope.Value;
            if (scope != null) return CopySnapshot(TryLoadPersistedCheckpoint(executionToken));
            lock (_bindingGate)
                scope = new ExecutionScope { Source = _migrationSource, TargetIdentity = _executionTargetIdentity };
            CaptureStorage(scope);
            var checkpoint = LoadPersistedCheckpoint(scope, executionToken.Trim());
            if (checkpoint != null)
            {
                if (checkpoint.OwnershipStoreIdentity != scope.Storage.ScopeIdentity || checkpoint.OwnershipTargetKey != scope.TargetKey)
                    throw new InvalidOperationException("Checkpoint ownership scope differs from the captured target/store.");
                return CopySnapshot(checkpoint);
            }
            if (ExecutionCheckpoints.TryGetValue(executionToken.Trim(), out var preview))
            {
                var observation = ReadSnapshot<MigrationExecutionCheckpoint>(preview);
                if (observation.OwnershipTargetKey == null && observation.OwnershipStoreIdentity == null)
                    return observation;
            }
            return null;
        }

        private static MigrationExecutionCheckpoint BuildNewCheckpoint(MigrationPlanArtifact plan, string token)
        {
            return new MigrationExecutionCheckpoint
            {
                ExecutionToken = token,
                CorrelationId = Guid.NewGuid().ToString("N"),
                PlanId = plan.PlanId,
                PlanHash = plan.PlanHash,
                ApprovedPlan = CopyExecutionPlan(plan),
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

        private IErrorsInfo ExecuteStep(MigrationExecutionStep step, MigrationPlanArtifact plan)
        {
            if (step == null)
                return CreateErrorsInfo(Errors.Failed, "Execution step is null.");

            if (step.OperationKind == MigrationPlanOperationKind.UpToDate)
                return CreateErrorsInfo(Errors.Ok, "Step skipped: entity already up to date.");

            if (step.OperationKind == MigrationPlanOperationKind.Error)
                return CreateErrorsInfo(Errors.Failed, "Step is marked as plan error and cannot be executed.");

            var operation = plan.Operations[step.Sequence - 1];
            var desired = CopySnapshot(operation.SchemaSnapshot?.DesiredSchema);
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

                case MigrationPlanOperationKind.DropColumn:
                {
                    // Destructive: each column dropped via the per-datasource provider (no raw DDL here).
                    if (string.IsNullOrWhiteSpace(step.EntityName))
                        return CreateErrorsInfo(Errors.Failed, "DropColumn step is missing the entity name.");
                    if (step.MissingColumns == null || step.MissingColumns.Count == 0)
                        return CreateErrorsInfo(Errors.Warning, $"No columns recorded to drop for '{step.EntityName}'.");

                    var dropFailures = new List<string>();
                    foreach (var columnName in step.MissingColumns)
                    {
                        var dropResult = DropColumn(step.EntityName, columnName);
                        if (!IsStepSuccess(dropResult))
                            dropFailures.Add($"{columnName}: {dropResult?.Message}");
                    }
                    if (dropFailures.Count > 0)
                        return CreateErrorsInfo(Errors.Failed, string.Join("; ", dropFailures));
                    return CreateErrorsInfo(Errors.Ok, $"Dropped {step.MissingColumns.Count} column(s) from '{step.EntityName}'.");
                }

                case MigrationPlanOperationKind.AlterColumn:
                {
                    if (desired == null)
                        return CreateErrorsInfo(Errors.Failed, $"Cannot resolve entity metadata for '{step.EntityName}'.");
                    if (step.MissingColumns == null || step.MissingColumns.Count == 0)
                        return CreateErrorsInfo(Errors.Warning, $"No columns recorded to alter for '{step.EntityName}'.");

                    var alterFailures = new List<string>();
                    foreach (var columnName in step.MissingColumns)
                    {
                        var field = desired.Fields?.FirstOrDefault(candidate =>
                            candidate != null &&
                            string.Equals(candidate.FieldName, columnName, StringComparison.OrdinalIgnoreCase));
                        if (field == null)
                        {
                            alterFailures.Add($"Column metadata not found for '{columnName}'.");
                            continue;
                        }
                        var alterResult = AlterColumn(step.EntityName, columnName, field);
                        if (!IsStepSuccess(alterResult))
                            alterFailures.Add($"{columnName}: {alterResult?.Message}");
                    }
                    if (alterFailures.Count > 0)
                        return CreateErrorsInfo(Errors.Failed, string.Join("; ", alterFailures));
                    return CreateErrorsInfo(Errors.Ok, $"Altered {step.MissingColumns.Count} column(s) on '{step.EntityName}'.");
                }

                case MigrationPlanOperationKind.DropEntity:
                    if (string.IsNullOrWhiteSpace(step.EntityName))
                        return CreateErrorsInfo(Errors.Failed, "DropEntity step is missing the entity name.");
                    return DropEntity(step.EntityName);

                case MigrationPlanOperationKind.TruncateEntity:
                    if (string.IsNullOrWhiteSpace(step.EntityName))
                        return CreateErrorsInfo(Errors.Failed, "TruncateEntity step is missing the entity name.");
                    return TruncateEntity(step.EntityName);

                case MigrationPlanOperationKind.RenameEntity:
                    // Convention: EntityName = current name, TargetName = new name.
                    if (string.IsNullOrWhiteSpace(step.EntityName) || string.IsNullOrWhiteSpace(step.TargetName))
                        return CreateErrorsInfo(Errors.Failed, "RenameEntity step requires the current name (EntityName) and the new name (TargetName).");
                    return RenameEntity(step.EntityName, step.TargetName);

                case MigrationPlanOperationKind.RenameColumn:
                    // Convention: MissingColumns[0] = old column, TargetName = new column.
                    if (string.IsNullOrWhiteSpace(step.EntityName) || step.MissingColumns == null || step.MissingColumns.Count == 0 || string.IsNullOrWhiteSpace(step.TargetName))
                        return CreateErrorsInfo(Errors.Failed, "RenameColumn step requires EntityName, the old column (MissingColumns[0]) and the new column (TargetName).");
                    return RenameColumn(step.EntityName, step.MissingColumns[0], step.TargetName);

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


        private sealed class MigrationCheckpointPersistenceException : Exception
        {
            public MigrationExecutionCheckpoint Checkpoint { get; }
            public PersistenceWriteResult Outcome { get; }
            public bool RequiresReconciliation { get; }
            public MigrationCheckpointPersistenceException(MigrationExecutionCheckpoint checkpoint, PersistenceWriteResult outcome, bool requiresReconciliation = false)
                : base("Mandatory migration checkpoint was not acknowledged.", outcome.Error)
            { Checkpoint = checkpoint; Outcome = outcome; RequiresReconciliation = requiresReconciliation; }
        }

        private void PersistExecutionCheckpoint(MigrationExecutionCheckpoint checkpoint, CancellationToken token = default)
        {
            if (checkpoint == null) return;
            var scope = _executionScope.Value;
            if (scope?.ClaimValidated != true || !CheckpointScopeMatches(checkpoint))
                throw new InvalidOperationException("Checkpoint mutation requires captured ownership.");
            checkpoint.UpdatedOnUtc = DateTime.UtcNow;
            var outcome = MigrationRecordWriter.WriteExecutionSnapshotToStore(
                scope.Storage,
                checkpoint,
                scope.DataSourceName, scope.DataSourceType, token);
            if (!outcome.IsSaved) throw new MigrationCheckpointPersistenceException(checkpoint, outcome);
            ExecutionCheckpoints.TryRemove(checkpoint.ExecutionToken, out _);
        }

        /// <summary>
        /// Loads the most recent persisted execution checkpoint for <paramref name="executionToken"/>
        /// from migration history (the JSON snapshot <see cref="PersistExecutionCheckpoint"/> writes to
        /// <c>MigrationRecord.Notes</c>). This is what makes resume survive a process restart: when the
        /// in-memory <see cref="ExecutionCheckpoints"/> is empty, the checkpoint is re-hydrated from disk.
        /// Returns null when no persisted snapshot exists.
        /// </summary>
        private MigrationExecutionCheckpoint TryLoadPersistedCheckpoint(string executionToken)
            => LoadPersistedCheckpoint(_executionScope.Value
                ?? throw new InvalidOperationException("Checkpoint authority requires captured storage."), executionToken);

        private static MigrationExecutionCheckpoint LoadPersistedCheckpoint(ExecutionScope scope, string executionToken)
        {
            if (string.IsNullOrWhiteSpace(executionToken)) return null;
            try
            {
                var dsName = scope.DataSourceName;
                if (string.IsNullOrWhiteSpace(dsName)) return null;

                var history = scope.Storage.LoadMigrationHistory(dsName);
                var record = history?.Migrations?
                    .Where(r => r != null
                                && string.Equals(r.MigrationId, executionToken.Trim(), StringComparison.Ordinal)
                                && string.Equals(r.Name, "ExecuteMigrationPlan.Checkpoint", StringComparison.Ordinal))
                    .OrderBy(r => r.AppliedOnUtc)
                    .LastOrDefault();
                if (record == null) return null;

                var checkpoint = ReadSnapshot<MigrationExecutionCheckpoint>(record.Notes);
                if (checkpoint == null || !string.Equals(checkpoint.ExecutionToken, executionToken.Trim(), StringComparison.Ordinal) ||
                    checkpoint.Steps == null || checkpoint.Steps.Any(s => s == null || !Enum.IsDefined(s.Status) || s.AttemptCount < 0) ||
                    checkpoint.CompensationSteps == null || checkpoint.CompensationSteps.Any(s => s == null ||
                        s.Sequence < 1 || s.Sequence > checkpoint.Steps.Count || !Enum.IsDefined(s.Status)) ||
                    checkpoint.CompensationSteps.Select(s => s.Sequence).Distinct().Count() != checkpoint.CompensationSteps.Count ||
                    checkpoint.CompensationCompleted && checkpoint.CompensationSteps.Any(s => s.Status != MigrationExecutionStepStatus.Completed) ||
                    checkpoint.IsCompleted && (checkpoint.HasFailed || checkpoint.Steps.Any(s => s.Status is not
                        (MigrationExecutionStepStatus.Completed or MigrationExecutionStepStatus.Skipped))))
                    throw new System.IO.InvalidDataException("Persisted checkpoint is missing or has a different execution identity.");
                return checkpoint;
            }
            catch (Exception ex)
            {
                throw new MigrationCheckpointPersistenceException(
                    new MigrationExecutionCheckpoint { ExecutionToken = executionToken.Trim(), RequiresReconciliation = true },
                    new PersistenceWriteResult(PersistenceWriteStatus.Failed, ex), requiresReconciliation: true);
            }
        }
    }
}
