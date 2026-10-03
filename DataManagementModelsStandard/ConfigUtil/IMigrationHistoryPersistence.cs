using System;
using System.Threading;

namespace TheTechIdea.Beep.ConfigUtil
{
    public enum PersistenceWriteStatus { Saved, Failed, Cancelled, Unsupported }

    /// <summary>Acknowledgement of a completed storage operation, not a power-loss guarantee.</summary>
    public sealed class PersistenceWriteResult
    {
        public PersistenceWriteStatus Status { get; }
        public Exception Error { get; }
        public bool IsSaved => Status == PersistenceWriteStatus.Saved;

        public PersistenceWriteResult(PersistenceWriteStatus status, Exception error = null)
        {
            Status = status;
            Error = error;
        }

        public void ThrowIfNotSaved()
        {
            if (!IsSaved)
                throw Error ?? new InvalidOperationException($"Persistence was not acknowledged: {Status}.");
        }
    }

    /// <summary>
    /// Optional additive configuration capability. A legacy void append is not an acknowledgement.
    /// Reads must distinguish missing history from corrupt/unreadable history.
    /// </summary>
    public interface IMigrationHistoryPersistence
    {
        PersistenceWriteResult SaveMigrationHistoryAcknowledged(MigrationHistory history, CancellationToken token = default);
        PersistenceWriteResult AppendMigrationRecordAcknowledged(string dataSourceName, Utilities.DataSourceType dataSourceType,
            MigrationRecord record, CancellationToken token = default);
    }

    /// <summary>Optional loader capability for complete, stable JSON snapshots before file replacement.</summary>
    public interface IJsonSnapshotCodec
    {
        string SerializeSnapshot(object value);
        T DeserializeSnapshot<T>(string json);
    }
}
