using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.FileManager.Governance;

namespace TheTechIdea.Beep.FileManager.Observability
{
    public interface IFileIngestionTelemetry
    {
        IDisposable BeginJob(string jobId, string sourceSystem, string entityName, string tenantId);
        void RecordTransition(string jobId, string fromState, string toState, string reason = null);
        void RecordCheckpoint(string jobId, long bytesRead, long rowsCommitted);
        void RecordDeadLetter(string jobId, long rowIndex, string errorCategory, string columnName);
        void RecordCompletion(string jobId, long totalRows, long committedRows, long rejectedRows, TimeSpan elapsed, bool succeeded);
        void IncrementRows(long count);
        void IncrementBytes(long bytes);
    }

    public enum SloSeverity { Warning, Critical }

    public sealed record IngestionMetricsSnapshot(string JobId, long RowsRead, long RowsCommitted, long RowsRejected, long BytesRead, TimeSpan Elapsed, int UnresolvedDeadLetters);
    public sealed record SloViolation(string SloName, string Detail, SloSeverity Severity);

    public interface ISloEnforcer
    {
        IReadOnlyList<SloViolation> Evaluate(IngestionMetricsSnapshot snapshot);
    }

    public enum HealthStatus { Healthy, Degraded, Unhealthy }

    public sealed class HealthCheckResult
    {
        public HealthStatus Status { get; init; }
        public string Description { get; init; }
        public IReadOnlyDictionary<string, object> Data { get; init; } = new Dictionary<string, object>();
    }

    public interface IFileManagerHealthCheck
    {
        Task<HealthCheckResult> CheckAsync(CancellationToken ct = default);
    }

    public interface IFileIngestionAlerting
    {
        Task NotifyAsync(SloViolation violation, string jobId, ITenantContext context, CancellationToken ct = default);
        Task NotifyJobFailedAsync(string jobId, string reason, ITenantContext context, CancellationToken ct = default);
        Task NotifyDeadLetterBacklogAsync(int backlogSize, string tenantId, CancellationToken ct = default);
    }
}
