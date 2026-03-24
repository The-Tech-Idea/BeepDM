using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TheTechIdea.Beep.Editor.Migration
{
    public partial class MigrationManager
    {
        private static long _planCount;
        private static long _executionCount;
        private static long _successCount;
        private static long _failureCount;
        private static long _retryCount;
        private static long _rollbackCount;
        private static long _policyBlockCount;
        private static long _totalStepDurationMs;
        private static long _stepDurationSamples;

        private static readonly ConcurrentDictionary<string, ConcurrentQueue<MigrationDiagnosticEntry>> DiagnosticStore = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, ConcurrentQueue<MigrationAuditEvent>> AuditStore = new(StringComparer.OrdinalIgnoreCase);

        public MigrationPlanArtifact ApproveMigrationPlan(MigrationPlanArtifact plan, string approvedBy, string notes = null)
        {
            if (plan == null)
                return null;

            plan.LifecycleState = MigrationPlanLifecycleState.Approved;
            var audit = CreateAuditEvent(
                executionToken: plan.ExecutionCheckpoint?.ExecutionToken,
                correlationId: plan.ExecutionCheckpoint?.CorrelationId,
                planId: plan.PlanId,
                planHash: plan.PlanHash,
                eventType: "PlanApproved",
                approvedBy: approvedBy,
                executedBy: string.Empty,
                result: "Approved",
                notes: notes ?? string.Empty);

            AddAuditEvent(audit);
            return plan;
        }

        public MigrationTelemetrySnapshot GetMigrationTelemetrySnapshot(string executionToken = null)
        {
            var diagnostics = GetDiagnosticsInternal(executionToken);
            var audits = GetAuditInternal(executionToken);
            var snapshot = new MigrationTelemetrySnapshot
            {
                CapturedOnUtc = DateTime.UtcNow,
                Diagnostics = diagnostics,
                AuditEvents = audits,
                Metrics = new MigrationTelemetryMetrics
                {
                    PlanCount = Interlocked.Read(ref _planCount),
                    ExecutionCount = Interlocked.Read(ref _executionCount),
                    SuccessCount = Interlocked.Read(ref _successCount),
                    FailureCount = Interlocked.Read(ref _failureCount),
                    RetryCount = Interlocked.Read(ref _retryCount),
                    RollbackCount = Interlocked.Read(ref _rollbackCount),
                    PolicyBlockCount = Interlocked.Read(ref _policyBlockCount),
                    TotalStepDurationMilliseconds = Interlocked.Read(ref _totalStepDurationMs),
                    StepDurationSamples = Interlocked.Read(ref _stepDurationSamples)
                }
            };

            snapshot.SuccessRate = snapshot.Metrics.ExecutionCount <= 0
                ? 0
                : (double)snapshot.Metrics.SuccessCount / snapshot.Metrics.ExecutionCount;
            snapshot.FailureRate = snapshot.Metrics.ExecutionCount <= 0
                ? 0
                : (double)snapshot.Metrics.FailureCount / snapshot.Metrics.ExecutionCount;

            snapshot.DiagnosticsBySeverity = diagnostics
                .GroupBy(item => item.Severity.ToString())
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

            return snapshot;
        }

        public IReadOnlyList<MigrationDiagnosticEntry> GetMigrationDiagnostics(string executionToken = null, MigrationDiagnosticSeverity? minimumSeverity = null)
        {
            var diagnostics = GetDiagnosticsInternal(executionToken);
            if (minimumSeverity == null)
                return diagnostics;

            return diagnostics
                .Where(item => item.Severity >= minimumSeverity.Value)
                .ToList();
        }

        public IReadOnlyList<MigrationAuditEvent> GetMigrationAuditEvents(string executionToken = null)
        {
            return GetAuditInternal(executionToken);
        }

        private static MigrationAuditEvent CreateAuditEvent(
            string executionToken,
            string correlationId,
            string planId,
            string planHash,
            string eventType,
            string approvedBy,
            string executedBy,
            string result,
            string notes)
        {
            return new MigrationAuditEvent
            {
                TimestampUtc = DateTime.UtcNow,
                ExecutionToken = executionToken ?? string.Empty,
                CorrelationId = correlationId ?? string.Empty,
                PlanId = planId ?? string.Empty,
                PlanHash = planHash ?? string.Empty,
                EventType = eventType ?? string.Empty,
                ApprovedBy = approvedBy ?? string.Empty,
                ExecutedBy = executedBy ?? string.Empty,
                Result = result ?? string.Empty,
                Notes = notes ?? string.Empty
            };
        }

        private static void AddAuditEvent(MigrationAuditEvent audit)
        {
            if (audit == null)
                return;

            var token = string.IsNullOrWhiteSpace(audit.ExecutionToken) ? "__global__" : audit.ExecutionToken;
            var queue = AuditStore.GetOrAdd(token, _ => new ConcurrentQueue<MigrationAuditEvent>());
            queue.Enqueue(audit);

            if (!string.IsNullOrWhiteSpace(audit.ExecutionToken) &&
                ExecutionPlans.TryGetValue(audit.ExecutionToken, out var planRef) &&
                planRef != null)
            {
                planRef.AuditTrail.Add(audit);
            }

            var global = AuditStore.GetOrAdd("__global__", _ => new ConcurrentQueue<MigrationAuditEvent>());
            global.Enqueue(audit);
        }

        private static void AddDiagnostic(MigrationDiagnosticEntry diagnostic)
        {
            if (diagnostic == null)
                return;

            var token = string.IsNullOrWhiteSpace(diagnostic.ExecutionToken) ? "__global__" : diagnostic.ExecutionToken;
            var queue = DiagnosticStore.GetOrAdd(token, _ => new ConcurrentQueue<MigrationDiagnosticEntry>());
            queue.Enqueue(diagnostic);

            if (!string.IsNullOrWhiteSpace(diagnostic.ExecutionToken) &&
                ExecutionPlans.TryGetValue(diagnostic.ExecutionToken, out var planRef) &&
                planRef != null)
            {
                planRef.Diagnostics.Add(diagnostic);
            }

            var global = DiagnosticStore.GetOrAdd("__global__", _ => new ConcurrentQueue<MigrationDiagnosticEntry>());
            global.Enqueue(diagnostic);
        }

        private static List<MigrationDiagnosticEntry> GetDiagnosticsInternal(string executionToken)
        {
            var token = string.IsNullOrWhiteSpace(executionToken) ? "__global__" : executionToken.Trim();
            if (!DiagnosticStore.TryGetValue(token, out var queue))
                return new List<MigrationDiagnosticEntry>();

            return queue.OrderByDescending(item => item.TimestampUtc).ToList();
        }

        private static List<MigrationAuditEvent> GetAuditInternal(string executionToken)
        {
            var token = string.IsNullOrWhiteSpace(executionToken) ? "__global__" : executionToken.Trim();
            if (!AuditStore.TryGetValue(token, out var queue))
                return new List<MigrationAuditEvent>();

            return queue.OrderByDescending(item => item.TimestampUtc).ToList();
        }

        private void RecordPlanCreated(MigrationPlanArtifact plan)
        {
            Interlocked.Increment(ref _planCount);
            var audit = CreateAuditEvent(
                executionToken: plan?.ExecutionCheckpoint?.ExecutionToken,
                correlationId: plan?.ExecutionCheckpoint?.CorrelationId,
                planId: plan?.PlanId,
                planHash: plan?.PlanHash,
                eventType: "PlanCreated",
                approvedBy: string.Empty,
                executedBy: Environment.UserName,
                result: "Created",
                notes: $"pending={plan?.PendingOperationCount ?? 0}");
            AddAuditEvent(audit);
        }

        private void RecordExecutionStarted(MigrationPlanArtifact plan, MigrationExecutionCheckpoint checkpoint)
        {
            Interlocked.Increment(ref _executionCount);
            var audit = CreateAuditEvent(
                executionToken: checkpoint?.ExecutionToken,
                correlationId: checkpoint?.CorrelationId,
                planId: plan?.PlanId,
                planHash: plan?.PlanHash,
                eventType: "ExecutionStarted",
                approvedBy: string.Empty,
                executedBy: Environment.UserName,
                result: "Started",
                notes: $"steps={checkpoint?.Steps?.Count ?? 0}");
            AddAuditEvent(audit);
        }

        private void RecordExecutionFinished(MigrationPlanArtifact plan, MigrationExecutionCheckpoint checkpoint, bool success, string notes)
        {
            if (success)
                Interlocked.Increment(ref _successCount);
            else
                Interlocked.Increment(ref _failureCount);

            var audit = CreateAuditEvent(
                executionToken: checkpoint?.ExecutionToken,
                correlationId: checkpoint?.CorrelationId,
                planId: plan?.PlanId,
                planHash: plan?.PlanHash,
                eventType: "ExecutionFinished",
                approvedBy: string.Empty,
                executedBy: Environment.UserName,
                result: success ? "Success" : "Failure",
                notes: notes);
            AddAuditEvent(audit);
        }

        private static void RecordRetryCount()
        {
            Interlocked.Increment(ref _retryCount);
        }

        private static void RecordRollbackCount()
        {
            Interlocked.Increment(ref _rollbackCount);
        }

        private static void RecordPolicyBlockCount()
        {
            Interlocked.Increment(ref _policyBlockCount);
        }

        private static void RecordStepDuration(long elapsedMs)
        {
            if (elapsedMs < 0)
                elapsedMs = 0;
            Interlocked.Add(ref _totalStepDurationMs, elapsedMs);
            Interlocked.Increment(ref _stepDurationSamples);
        }

        private static void RecordDiagnostic(
            string executionToken,
            string correlationId,
            string operationCode,
            MigrationDiagnosticSeverity severity,
            string entityName,
            string message,
            string recommendation)
        {
            AddDiagnostic(new MigrationDiagnosticEntry
            {
                TimestampUtc = DateTime.UtcNow,
                ExecutionToken = executionToken ?? string.Empty,
                CorrelationId = correlationId ?? string.Empty,
                OperationCode = operationCode ?? string.Empty,
                Severity = severity,
                EntityName = entityName ?? string.Empty,
                Message = message ?? string.Empty,
                Recommendation = recommendation ?? string.Empty
            });
        }
    }
}
