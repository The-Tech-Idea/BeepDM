using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.FileManager.Readers
{
    // ── Phase 2: parse mode ──────────────────────────────────────────────────

    /// <summary>
    /// Controls how the reader handles malformed rows.
    /// Strict — throws on the first bad row.
    /// Lenient — skips/logs malformed rows and continues.
    /// </summary>
    public enum ParseMode { Lenient, Strict }

    /// <summary>Severity level for a reader-emitted diagnostic.</summary>
    public enum DiagnosticSeverity { Info, Warning, Error }

    /// <summary>A structured diagnostic emitted by a file reader during parsing.</summary>
    public sealed class RowDiagnostic
    {
        public long    RowIndex    { get; init; }
        public int?    ColumnIndex { get; init; }
        public string  ColumnName  { get; init; }
        /// <summary>Short machine-readable code, e.g. "MALFORMED_ROW", "QUOTE_NOT_CLOSED".</summary>
        public string  Code        { get; init; }
        public string  Message     { get; init; }
        public DiagnosticSeverity Severity { get; init; }
        public string  RawLine     { get; init; }
    }

    /// <summary>
    /// Contract for a pluggable file-format reader/writer.
    /// Each implementation handles exactly one <see cref="DataSourceType"/>
    /// (e.g. CSV, TSV, JSON, XML) and is resolved by <see cref="FileReaderFactory"/>.
    /// </summary>
    public interface IFileFormatReader
    {
        /// <summary>The format this reader handles.</summary>
        DataSourceType SupportedType { get; }

        /// <summary>
        /// When <c>true</c> (default), the first row of the file is treated as a header row
        /// and used for column naming. When <c>false</c>, the first row is data and columns
        /// are named column1, column2, … columnN.
        /// </summary>
        bool HasHeader { get; set; }

        /// <summary>Default file extension without the leading dot, e.g. "csv".</summary>
        string GetDefaultExtension();

        /// <summary>
        /// Called once after construction to pass connection-level settings
        /// (delimiter, encoding, quote character, etc.) sourced from
        /// <see cref="IConnectionProperties"/>.
        /// </summary>
        void Configure(IConnectionProperties props);

        // ── Phase 2: parse behaviour ─────────────────────────────────────────

        /// <summary>Strict = throw on malformed row; Lenient = skip and log.</summary>
        ParseMode ParseMode { get; set; }

        /// <summary>Diagnostics collected during the last read operation. Thread-local per-call.</summary>
        IReadOnlyList<RowDiagnostic> LastDiagnostics { get; }

        /// <summary>Clears accumulated diagnostics.</summary>
        void ClearDiagnostics();

        // ── Schema ──────────────────────────────────────────────────────────

        /// <summary>Reads the header row and returns the normalised column names.</summary>
        string[] ReadHeaders(string filePath);

        /// <summary>
        /// Builds an <see cref="EntityStructure"/> for the given file.
        /// Column names come from the header row when <see cref="HasHeader"/> is <c>true</c>;
        /// otherwise they are auto-generated as column1, column2, … columnN
        /// (column count is determined by peeking at the first data row).
        /// Returns <c>null</c> if the file is empty or does not exist.
        /// </summary>
        EntityStructure GetEntityStructure(string filePath);

        /// <summary>
        /// Streams data rows. Each element is a string-per-column array aligned
        /// with the header row. The header row itself is NOT yielded.
        /// Malformed rows are handled according to <see cref="ParseMode"/>.
        /// </summary>
        IEnumerable<string[]> ReadRows(string filePath);

        /// <summary>
        /// Heuristic type inference. Returns the "widest" type name needed to
        /// represent both the <paramref name="current"/> inferred type and the
        /// new <paramref name="rawValue"/>.  Pass <c>null</c> for
        /// <paramref name="current"/> on the first value.
        /// </summary>
        string InferFieldType(string current, string rawValue);

        // ── Write ────────────────────────────────────────────────────────────

        /// <summary>Creates a new, empty file with the given header row.</summary>
        bool CreateFile(string filePath, IReadOnlyList<string> headers);

        /// <summary>Appends a single data row to an existing file.</summary>
        bool AppendRow(string filePath, IReadOnlyList<string> headers, IReadOnlyList<string> values);

        /// <summary>
        /// Atomically rewrites the entire file with the given headers and rows.
        /// Used by update and delete operations.
        /// </summary>
        bool RewriteFile(string filePath, IReadOnlyList<string> headers,
                         IEnumerable<IReadOnlyList<string>> rows);
    }

    // ── Shared builder ───────────────────────────────────────────────────────

    /// <summary>
    /// Shared utilities for <see cref="IFileFormatReader"/> implementations.
    /// Provides a common <see cref="BuildEntityStructure"/> so that each reader
    /// produces consistently structured <see cref="EntityStructure"/> objects.
    /// </summary>
    public static class FileReaderEntityHelper
    {
        private static readonly Regex _invalidChars =
            new Regex(@"[^a-zA-Z0-9_]", RegexOptions.Compiled);

        /// <summary>
        /// Builds a bare-schema <see cref="EntityStructure"/> from an ordered list of
        /// column names. All fields are typed <c>System.String</c>; callers may apply
        /// type-inference later (<see cref="TypeInferenceHelper.InferWithStats"/>).
        /// </summary>
        public static EntityStructure BuildEntityStructure(string entityName, string[] columnNames)
        {
            if (columnNames == null || columnNames.Length == 0) return null;

            var entity = new EntityStructure
            {
                EntityName           = entityName,
                DatasourceEntityName = entityName,
                OriginalEntityName   = entityName,
                Caption              = entityName,
                Viewtype             = ViewType.File,
                Fields               = new List<EntityField>()
            };

            for (int i = 0; i < columnNames.Length; i++)
            {
                string original   = columnNames[i];
                string normalized = NormalizeColumnName(original);
                entity.Fields.Add(new EntityField
                {
                    FieldIndex        = i,
                    FieldName         = normalized,
                    Originalfieldname = original,
                    Fieldtype         = typeof(string).FullName,
                    EntityName        = entityName,
                    AllowDBNull       = true,
                    IsKey             = i == 0
                });
            }

            return entity;
        }

        /// <summary>
        /// Generates positional column names column1 … columnN.
        /// </summary>
        public static string[] GenerateColumnNames(int count)
        {
            var names = new string[count];
            for (int i = 0; i < count; i++)
                names[i] = $"column{i + 1}";
            return names;
        }

        /// <summary>Replaces characters that are invalid in field names with underscores.</summary>
        public static string NormalizeColumnName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "column_unnamed";
            string trimmed = raw.Trim();
            string normalized = _invalidChars.Replace(trimmed, "_");
            // Ensure the name doesn't start with a digit
            if (normalized.Length > 0 && char.IsDigit(normalized[0]))
                normalized = "_" + normalized;
            return normalized;
        }
    }
}
