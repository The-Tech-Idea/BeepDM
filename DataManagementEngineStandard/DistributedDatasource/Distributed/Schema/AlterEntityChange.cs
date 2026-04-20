using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Distributed.Schema
{
    /// <summary>
    /// Discriminated-record description of a single DDL change to an
    /// entity that the Phase 12 schema service broadcasts to every
    /// owning shard. Each change kind carries only the payload it
    /// needs; unused fields are <c>null</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The record is intentionally closed (no subclassing) because
    /// providers must pattern-match on <see cref="Kind"/>. Use the
    /// static factory methods (<c>AddColumn</c>, <c>DropColumn</c>,
    /// etc.) instead of the raw constructor to guarantee the payload
    /// for each kind is fully populated.
    /// </para>
    /// <para>
    /// For <see cref="AlterEntityChangeKind.AddColumn"/> and
    /// <see cref="AlterEntityChangeKind.AlterColumn"/> the column's
    /// name, type, length, and nullability come from
    /// <see cref="Column"/>; column-name-only kinds
    /// (<see cref="AlterEntityChangeKind.DropColumn"/>) use
    /// <see cref="ColumnName"/>. Index kinds use
    /// <see cref="IndexName"/> + <see cref="IndexColumns"/>.
    /// </para>
    /// </remarks>
    public sealed class AlterEntityChange
    {
        private AlterEntityChange(
            AlterEntityChangeKind kind,
            string                entityName,
            EntityField           column,
            string                columnName,
            string                indexName,
            IReadOnlyList<string> indexColumns,
            bool                  indexIsUnique)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be null or whitespace.", nameof(entityName));

            Kind          = kind;
            EntityName    = entityName;
            Column        = column;
            ColumnName    = columnName;
            IndexName     = indexName;
            IndexColumns  = indexColumns ?? Array.Empty<string>();
            IndexIsUnique = indexIsUnique;
        }

        /// <summary>Classification of this change.</summary>
        public AlterEntityChangeKind Kind { get; }

        /// <summary>Target entity (table). Always set.</summary>
        public string EntityName { get; }

        /// <summary>Column payload for <c>AddColumn</c> / <c>AlterColumn</c>; <c>null</c> otherwise.</summary>
        public EntityField Column { get; }

        /// <summary>Column identifier for <c>DropColumn</c>; <c>null</c> otherwise.</summary>
        public string ColumnName { get; }

        /// <summary>Index identifier for index-scoped kinds; <c>null</c> otherwise.</summary>
        public string IndexName { get; }

        /// <summary>Ordered column list for <c>AddIndex</c>; empty for others.</summary>
        public IReadOnlyList<string> IndexColumns { get; }

        /// <summary>Whether an <c>AddIndex</c> change creates a unique index.</summary>
        public bool IndexIsUnique { get; }

        // ── Factories ─────────────────────────────────────────────────────

        /// <summary>Create an <see cref="AlterEntityChangeKind.AddColumn"/> change.</summary>
        public static AlterEntityChange AddColumn(string entityName, EntityField column)
        {
            if (column == null) throw new ArgumentNullException(nameof(column));
            if (string.IsNullOrWhiteSpace(column.FieldName))
                throw new ArgumentException("Column.FieldName must be set.", nameof(column));
            return new AlterEntityChange(
                AlterEntityChangeKind.AddColumn,
                entityName, column, column.FieldName, indexName: null,
                indexColumns: null, indexIsUnique: false);
        }

        /// <summary>Create an <see cref="AlterEntityChangeKind.DropColumn"/> change.</summary>
        public static AlterEntityChange DropColumn(string entityName, string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name cannot be null or whitespace.", nameof(columnName));
            return new AlterEntityChange(
                AlterEntityChangeKind.DropColumn,
                entityName, column: null, columnName, indexName: null,
                indexColumns: null, indexIsUnique: false);
        }

        /// <summary>Create an <see cref="AlterEntityChangeKind.AlterColumn"/> change.</summary>
        public static AlterEntityChange AlterColumn(string entityName, EntityField column)
        {
            if (column == null) throw new ArgumentNullException(nameof(column));
            if (string.IsNullOrWhiteSpace(column.FieldName))
                throw new ArgumentException("Column.FieldName must be set.", nameof(column));
            return new AlterEntityChange(
                AlterEntityChangeKind.AlterColumn,
                entityName, column, column.FieldName, indexName: null,
                indexColumns: null, indexIsUnique: false);
        }

        /// <summary>Create an <see cref="AlterEntityChangeKind.AddIndex"/> change.</summary>
        public static AlterEntityChange AddIndex(
            string                entityName,
            string                indexName,
            IReadOnlyList<string> columns,
            bool                  unique = false)
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("Index name cannot be null or whitespace.", nameof(indexName));
            if (columns == null || columns.Count == 0)
                throw new ArgumentException("At least one column is required.", nameof(columns));
            return new AlterEntityChange(
                AlterEntityChangeKind.AddIndex,
                entityName, column: null, columnName: null,
                indexName, columns, unique);
        }

        /// <summary>Create an <see cref="AlterEntityChangeKind.DropIndex"/> change.</summary>
        public static AlterEntityChange DropIndex(string entityName, string indexName)
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("Index name cannot be null or whitespace.", nameof(indexName));
            return new AlterEntityChange(
                AlterEntityChangeKind.DropIndex,
                entityName, column: null, columnName: null,
                indexName, indexColumns: null, indexIsUnique: false);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            switch (Kind)
            {
                case AlterEntityChangeKind.AddColumn:
                case AlterEntityChangeKind.AlterColumn:
                case AlterEntityChangeKind.DropColumn:
                    return $"{Kind} {EntityName}.{ColumnName ?? "(?)"}";
                case AlterEntityChangeKind.AddIndex:
                case AlterEntityChangeKind.DropIndex:
                    return $"{Kind} {EntityName}.{IndexName}";
                default:
                    return $"{Kind} {EntityName}";
            }
        }
    }
}
