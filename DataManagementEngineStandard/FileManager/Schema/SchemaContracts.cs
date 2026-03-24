using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.FileManager.Schema
{
    public sealed class FileColumnSchema
    {
        public int OrdinalPosition { get; init; }
        public string SourceColumnName { get; init; }
        public string TargetFieldName { get; init; }
        public string InferredType { get; init; }
        public bool IsNullable { get; init; }
        public double NullRate { get; init; }
        public double UniquenessRatio { get; init; }
        public int MaxLength { get; init; }
        public string SampleValues { get; init; }
        public IReadOnlyList<string> DataClassifications { get; init; } = Array.Empty<string>();
    }

    public sealed class FileSchemaVersion
    {
        public int VersionNumber { get; init; }
        public DateTimeOffset RegisteredAt { get; init; } = DateTimeOffset.UtcNow;
        public string RegisteredBy { get; init; }
        public string FileChecksum { get; init; }
        public IReadOnlyList<FileColumnSchema> Columns { get; init; } = Array.Empty<FileColumnSchema>();
        public string Delimiter { get; init; }
        public bool HasHeader { get; init; } = true;
        public string Encoding { get; init; } = "UTF-8";
    }

    public sealed record ColumnTypeChange(string ColumnName, string OldType, string NewType, bool IsWidening);
    public sealed record ColumnRename(string OldName, string NewName, double NameSimilarity);

    public sealed class SchemaDriftReport
    {
        public string SourceSystem { get; init; }
        public string EntityName { get; init; }
        public int BaselineVersion { get; init; }
        public bool HasDrift => AddedColumns.Count > 0 || DroppedColumns.Count > 0 || TypeChanges.Count > 0 || RenamedColumns.Count > 0;
        public bool IsBreaking { get; init; }
        public IReadOnlyList<FileColumnSchema> AddedColumns { get; init; } = Array.Empty<FileColumnSchema>();
        public IReadOnlyList<FileColumnSchema> DroppedColumns { get; init; } = Array.Empty<FileColumnSchema>();
        public IReadOnlyList<ColumnTypeChange> TypeChanges { get; init; } = Array.Empty<ColumnTypeChange>();
        public IReadOnlyList<ColumnRename> RenamedColumns { get; init; } = Array.Empty<ColumnRename>();
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public string Summary { get; init; }
    }

    public interface IFileSchemaRegistry
    {
        Task<int> RegisterAsync(string sourceSystem, string entityName, FileSchemaVersion schema, CancellationToken ct = default);
        Task<FileSchemaVersion> GetLatestAsync(string sourceSystem, string entityName, CancellationToken ct = default);
        Task<FileSchemaVersion> GetVersionAsync(string sourceSystem, string entityName, int version, CancellationToken ct = default);
        Task<IReadOnlyList<FileSchemaVersion>> GetHistoryAsync(string sourceSystem, string entityName, CancellationToken ct = default);
        Task<SchemaDriftReport> DetectDriftAsync(string sourceSystem, string entityName, FileSchemaVersion candidate, CancellationToken ct = default);
    }
}
