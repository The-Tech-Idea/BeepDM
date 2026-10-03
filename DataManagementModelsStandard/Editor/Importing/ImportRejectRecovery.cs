using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor.Importing
{
    public enum ImportRejectState { Pending, Prepared, Claimed, Acknowledged, ReconciliationRequired, ReconciledApplied, Dismissed }
    public enum ImportReplayDisposition { Acknowledged, Denied, DefinitivelyFailed, Uncertain }
    public enum ImportReplayProviderConfirmation { NotApplied, Applied }

    /// <summary>Versioned reject identity and recovery evidence. No CLR type names or inferred replay offsets.</summary>
    public sealed class ImportRejectRecovery
    {
        public int FormatVersion { get; set; } = 1;
        public string RejectId { get; set; } = string.Empty;
        public string RunId { get; set; } = string.Empty;
        public string SourceDataSourceName { get; set; } = string.Empty;
        public string SourceEntityName { get; set; } = string.Empty;
        public string DestinationDataSourceName { get; set; } = string.Empty;
        public string DestinationEntityName { get; set; } = string.Empty;
        public string? DestinationGuid { get; set; }
        public string OriginalDestinationPayload { get; set; } = string.Empty;
        public string? PreparedDestinationPayload { get; set; }
        public ImportRejectState State { get; set; }
        public int Revision { get; set; }
        public string? OperatorId { get; set; }
        public string? ClaimId { get; set; }
        public string? ClaimOwner { get; set; }
        public DateTime? ClaimedAt { get; set; }
        public string? ReconciliationReference { get; set; }
    }

    /// <summary>Optional atomic claim/triage capability. Task completion must acknowledge persisted transitions.</summary>
    public interface IImportRejectRecoveryStore
    {
        Task ValidateRecoveryAsync(string contextKey, CancellationToken token = default);
        Task<ImportErrorRecord> LoadRejectAsync(string contextKey, string rejectId, CancellationToken token = default);
        Task<ImportErrorRecord> PrepareReplayAsync(string contextKey, string rejectId, int expectedRevision,
            string operatorId, object? correctedDestinationRecord = null, CancellationToken token = default);
        Task<ImportErrorRecord> ClaimReplayAsync(string contextKey, string rejectId, int expectedRevision,
            string ownerId, CancellationToken token = default);
        Task CompleteReplayAsync(string contextKey, string rejectId, int expectedRevision, string claimId,
            ImportReplayDisposition disposition, CancellationToken token = default);
        Task ReconcileReplayAsync(string contextKey, string rejectId, int expectedRevision, string claimId,
            string operatorId, ImportReplayProviderConfirmation confirmation, string evidenceReference, CancellationToken token = default);
        Task DismissRejectAsync(string contextKey, string rejectId, int expectedRevision,
            string operatorId, string evidenceReference, CancellationToken token = default);
    }

    /// <summary>Optional host/provider-backed idempotent insert. Providers own replay-key uniqueness and commit semantics.</summary>
    public interface IImportReplayDataSource
    {
        IErrorsInfo InsertReplay(string entityName, object destinationRecord, string rejectId);
    }

    public sealed class ImportRejectReplayResult : ErrorsInfo
    {
        public int RecordsAttempted { get; set; }
        public int WriteAttempts { get; set; }
        public int RecordsAcknowledged { get; set; }
        public int RecordsDenied { get; set; }
        public int RecordsFailed { get; set; }
        public int RecordsUnsupported { get; set; }
        public int RecoveryPersistenceFailures { get; set; }
        public int DiagnosticFailures { get; set; }
        public bool HasUncertainWrites { get; set; }
        public bool RequiresReconciliation { get; set; }
        public bool Cancelled { get; set; }
    }
}
