using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Importing.Schema
{
    /// <summary>
    /// Immutable snapshot of a data-source entity schema captured at a point in time.
    /// Stored as JSON so schema versions can be compared across runs.
    /// </summary>
    public sealed class SchemaSnapshot
    {
        /// <summary>Composite key: "{dataSourceName}/{entityName}".</summary>
        public string ContextKey    { get; set; } = string.Empty;

        /// <summary>UTC timestamp when the snapshot was captured.</summary>
        public DateTime CapturedAt  { get; set; } = DateTime.UtcNow;

        /// <summary>Name of the data source connection.</summary>
        public string DataSourceName { get; set; } = string.Empty;

        /// <summary>Name of the entity / table.</summary>
        public string EntityName     { get; set; } = string.Empty;

        /// <summary>Ordered list of fields at the time of capture.</summary>
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

    // -------------------------------------------------------------------------
    // Schema drift report — produced by SchemaComparator
    // -------------------------------------------------------------------------

    /// <summary>Summary of structural differences between two entity schemas.</summary>
    public sealed class SchemaDriftReport
    {
        /// <summary>The baseline (older) snapshot used for comparison.</summary>
        public SchemaSnapshot Baseline { get; set; } = null!;

        /// <summary>The current (newer) snapshot used for comparison.</summary>
        public SchemaSnapshot Current  { get; set; } = null!;

        /// <summary>Fields present in <see cref="Current"/> but absent in <see cref="Baseline"/>.</summary>
        public List<SnapshotField> AddedFields   { get; set; } = new();

        /// <summary>Fields present in <see cref="Baseline"/> but absent in <see cref="Current"/>.</summary>
        public List<SnapshotField> RemovedFields { get; set; } = new();

        /// <summary>Fields present in both snapshots but with changed type, nullability, or length.</summary>
        public List<FieldTypeDrift> AlteredFields { get; set; } = new();

        /// <summary><c>true</c> when at least one addition, removal, or alteration exists.</summary>
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
