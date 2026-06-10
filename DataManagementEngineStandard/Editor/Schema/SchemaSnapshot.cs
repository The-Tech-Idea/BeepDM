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
        public string ContextKey    { get; set; } = string.Empty;
        public DateTime CapturedAt  { get; set; } = DateTime.UtcNow;
        public string DataSourceName { get; set; } = string.Empty;
        public string EntityName     { get; set; } = string.Empty;
        public List<SnapshotField> Fields { get; set; } = new();
    }

    /// <summary>Lightweight representation of a single field in a <see cref="SchemaSnapshot"/>.</summary>
    public sealed class SnapshotField
    {
        public string  Name         { get; set; } = string.Empty;
        public string  DataType     { get; set; } = string.Empty;
        public bool    IsNullable   { get; set; } = true;
        public int     MaxLength    { get; set; }
        public int     Precision    { get; set; }
        public int     Scale        { get; set; }
    }

    /// <summary>Summary of structural differences between two entity schemas.</summary>
    public sealed class SchemaDriftReport
    {
        public SchemaSnapshot Baseline { get; set; } = null!;
        public SchemaSnapshot Current  { get; set; } = null!;
        public List<SnapshotField> AddedFields   { get; set; } = new();
        public List<SnapshotField> RemovedFields { get; set; } = new();
        public List<FieldTypeDrift> AlteredFields { get; set; } = new();
        public bool HasDrift =>
            AddedFields.Count > 0 || RemovedFields.Count > 0 || AlteredFields.Count > 0;
    }

    /// <summary>Describes a type-level change to a single field.</summary>
    public sealed class FieldTypeDrift
    {
        public string FieldName   { get; set; } = string.Empty;
        public string BaselineType { get; set; } = string.Empty;
        public string CurrentType  { get; set; } = string.Empty;
        public string Description  { get; set; } = string.Empty;
    }
}
