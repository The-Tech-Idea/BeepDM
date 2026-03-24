using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.FileManager.Contracts
{
    public interface IFileIngestionDescriptor
    {
        string JobId { get; }
        string FileChecksum { get; }
        string FilePath { get; }
        DateTimeOffset ScheduledAt { get; }
        string TargetEntityName { get; }
        string SourceSystem { get; }
        string TenantId { get; }
        IReadOnlyDictionary<string, string> Tags { get; }
    }

    public enum IngestionState
    {
        Pending,
        Validating,
        Ingesting,
        Suspended,
        Complete,
        Failed,
        Quarantined
    }

    public sealed record IngestionCheckpoint(
        string JobId,
        long BytesRead,
        long RowsCommitted,
        DateTimeOffset SavedAt);

    public sealed record IngestionJobStatus(
        string JobId,
        IngestionState State,
        long RowsCommitted,
        long RowsRejected,
        DateTimeOffset LastUpdatedAt,
        string LastMessage);

    public interface IIngestionStateStore
    {
        Task CreateAsync(IFileIngestionDescriptor descriptor, CancellationToken ct = default);
        Task TransitionAsync(string jobId, IngestionState newState, string message = null, CancellationToken ct = default);
        Task SaveCheckpointAsync(string jobId, long bytesRead, long rowsCommitted, CancellationToken ct = default);
        Task<IngestionCheckpoint> GetCheckpointAsync(string jobId, CancellationToken ct = default);
        Task<IngestionJobStatus> GetStatusAsync(string jobId, CancellationToken ct = default);
        Task<string> FindByChecksumAsync(string sha256Checksum, string targetEntityName, CancellationToken ct = default);
        Task<IFileIngestionDescriptor> GetDescriptorAsync(string jobId, CancellationToken ct = default);
    }

    public interface IFileProvenanceEnvelope
    {
        string JobId { get; }
        string FileChecksum { get; }
        string SourceFilePath { get; }
        string SourceSystem { get; }
        long SourceRowIndex { get; }
        DateTimeOffset IngestedAt { get; }
    }

    public sealed record IngestionProgressArgs(
        string JobId,
        IngestionState State,
        long RowsRead,
        long RowsCommitted,
        long RowsRejected,
        double ProgressPercent);

    public interface IIdempotentFileIngester
    {
        Task<string> IsAlreadyIngestedAsync(IFileIngestionDescriptor descriptor, CancellationToken ct = default);
        Task<IngestionJobStatus> IngestAsync(
            IFileIngestionDescriptor descriptor,
            IProgress<IngestionProgressArgs> progress = null,
            CancellationToken ct = default);
        Task<IngestionJobStatus> ResumeAsync(
            string jobId,
            IProgress<IngestionProgressArgs> progress = null,
            CancellationToken ct = default);
    }

    public sealed class FileIngestionDescriptor : IFileIngestionDescriptor
    {
        public string JobId { get; init; } = Guid.NewGuid().ToString();
        public string FileChecksum { get; init; }
        public string FilePath { get; init; }
        public DateTimeOffset ScheduledAt { get; init; } = DateTimeOffset.UtcNow;
        public string TargetEntityName { get; init; }
        public string SourceSystem { get; init; } = "FILE";
        public string TenantId { get; init; } = "default";
        public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
    }

    public sealed class FileProvenanceEnvelope : IFileProvenanceEnvelope
    {
        public string JobId { get; init; }
        public string FileChecksum { get; init; }
        public string SourceFilePath { get; init; }
        public string SourceSystem { get; init; }
        public long SourceRowIndex { get; init; }
        public DateTimeOffset IngestedAt { get; init; }
    }
}
