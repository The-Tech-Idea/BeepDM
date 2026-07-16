using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Schema
{
    /// <summary>
    /// Immutable snapshot of a data-source entity schema captured at a point in time.
    /// Stored as JSON so schema versions can be compared across runs.
    /// </summary>
    public sealed class SchemaSnapshot
    {
        public string ContextKey    { get; init; } = string.Empty;
        public DateTime CapturedAt  { get; init; } = DateTime.UtcNow;
        public string DataSourceName { get; init; } = string.Empty;
        public string EntityName     { get; init; } = string.Empty;
        public List<SnapshotField> Fields { get; init; } = new();
    }

    /// <summary>Lightweight representation of a single field in a <see cref="SchemaSnapshot"/>.</summary>
    public sealed class SnapshotField
    {
        public string  Name         { get; init; } = string.Empty;
        public string  DataType     { get; init; } = string.Empty;
        public bool    IsNullable   { get; init; } = true;
        public int     MaxLength    { get; init; }
        public int     Precision    { get; init; }
        public int     Scale        { get; init; }
    }

    /// <summary>Summary of structural differences between two entity schemas.</summary>
    public sealed class SchemaDriftReport
    {
        public SchemaSnapshot Baseline { get; init; } = null!;
        public SchemaSnapshot Current  { get; init; } = null!;
        public List<SnapshotField> AddedFields   { get; init; } = new();
        public List<SnapshotField> RemovedFields { get; init; } = new();
        public List<FieldTypeDrift> AlteredFields { get; init; } = new();
        public bool HasDrift =>
            AddedFields.Count > 0 || RemovedFields.Count > 0 || AlteredFields.Count > 0;
    }

    /// <summary>Describes a type-level change to a single field.</summary>
    public sealed class FieldTypeDrift
    {
        public string FieldName   { get; init; } = string.Empty;
        public string BaselineType { get; init; } = string.Empty;
        public string CurrentType  { get; init; } = string.Empty;
        public string Description  { get; init; } = string.Empty;
    }
}
