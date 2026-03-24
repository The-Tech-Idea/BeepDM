using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.FileManager.Attributes;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.FileManager.Readers
{
    /// <summary>
    /// Reads and writes delimiter-separated value files (CSV, with configurable delimiter).
    /// Uses the project's own <see cref="CsvTextFieldParser"/> for RFC 4180-compliant parsing.
    /// </summary>
    [FileReader(DataSourceType.CSV, "CSV", "csv")]
    public class CsvFileReader : IFileFormatReader
    {
        protected char Delimiter { get; private set; } = ',';
        protected Encoding FileEncoding { get; private set; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public virtual DataSourceType SupportedType => DataSourceType.CSV;
        public virtual string GetDefaultExtension() => "csv";
        public virtual bool HasHeader { get; set; } = true;

        // ── Phase 2: parse mode + diagnostics ────────────────────────────────

        public ParseMode ParseMode { get; set; } = ParseMode.Lenient;

        private readonly List<RowDiagnostic> _diagnostics = new();
        public IReadOnlyList<RowDiagnostic> LastDiagnostics => _diagnostics;
        public void ClearDiagnostics() => _diagnostics.Clear();

        // ── Configuration ────────────────────────────────────────────────────

        public virtual void Configure(IConnectionProperties props)
        {
            if (props == null) return;

            if (props.Delimiter != default && props.Delimiter != '\0')
                Delimiter = props.Delimiter;
            else if (!string.IsNullOrEmpty(props.ConnectionString))
                Delimiter = DetectDelimiter(props.ConnectionString);
        }

        // ── Schema ───────────────────────────────────────────────────────────

        public string[] ReadHeaders(string filePath)
        {
            if (!File.Exists(filePath)) return Array.Empty<string>();

            using var parser = OpenParser(filePath);
            if (parser.EndOfData) return Array.Empty<string>();
            return parser.ReadFields() ?? Array.Empty<string>();
        }

        public IEnumerable<string[]> ReadRows(string filePath)
        {
            if (!File.Exists(filePath)) yield break;

            ClearDiagnostics();
            using var parser = OpenParser(filePath);
            if (parser.EndOfData) yield break;
            if (HasHeader) parser.ReadFields(); // skip header row when HasHeader = true

            long rowIndex = 0;
            while (!parser.EndOfData)
            {
                rowIndex++;
                string[] fields = null;
                try
                {
                    fields = parser.ReadFields();
                }
                catch (Exception ex)
                {
                    var diag = new RowDiagnostic
                    {
                        RowIndex  = rowIndex,
                        Code      = "PARSE_ERROR",
                        Message   = ex.Message,
                        Severity  = DiagnosticSeverity.Error
                    };
                    _diagnostics.Add(diag);

                    if (ParseMode == ParseMode.Strict)
                        throw new InvalidOperationException(
                            $"Strict parse mode: malformed row {rowIndex} in '{filePath}'. {ex.Message}", ex);
                    continue;
                }
                if (fields != null) yield return fields;
            }
        }

        public string InferFieldType(string current, string rawValue)
        {
            return TypeInferenceHelper.Widen(current, rawValue);
        }

        // ── Schema: GetEntityStructure ───────────────────────────────────────

        public virtual EntityStructure GetEntityStructure(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            string entityName = Path.GetFileNameWithoutExtension(filePath);
            string[] names;

            if (HasHeader)
            {
                names = ReadHeaders(filePath);
                if (names.Length == 0)
                {
                    // File exists but appears empty
                    return FileReaderEntityHelper.BuildEntityStructure(entityName, Array.Empty<string>());
                }
            }
            else
            {
                // No header: peek at the first row to discover column count
                using var parser = OpenParser(filePath);
                if (parser.EndOfData) return null;
                var firstRow = parser.ReadFields();
                int colCount = firstRow?.Length ?? 0;
                if (colCount == 0) return null;
                names = FileReaderEntityHelper.GenerateColumnNames(colCount);
            }

            return FileReaderEntityHelper.BuildEntityStructure(entityName, names);
        }

        // ── Write ────────────────────────────────────────────────────────────

        public bool CreateFile(string filePath, IReadOnlyList<string> headers)
        {
            EnsureDirectory(filePath);
            using var writer = new StreamWriter(filePath, append: false, FileEncoding);
            writer.WriteLine(FormatRow(headers));
            return true;
        }

        public bool AppendRow(string filePath, IReadOnlyList<string> headers, IReadOnlyList<string> values)
        {
            if (!File.Exists(filePath))
                CreateFile(filePath, headers);

            using var writer = new StreamWriter(filePath, append: true, FileEncoding);
            writer.WriteLine(FormatRow(values));
            return true;
        }

        public bool RewriteFile(string filePath, IReadOnlyList<string> headers,
                                IEnumerable<IReadOnlyList<string>> rows)
        {
            string tempPath = filePath + ".tmp";
            EnsureDirectory(tempPath);

            using (var writer = new StreamWriter(tempPath, append: false, FileEncoding))
            {
                writer.WriteLine(FormatRow(headers));
                foreach (var row in rows)
                    writer.WriteLine(FormatRow(row));
            }

            AtomicReplace(tempPath, filePath);
            return true;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        protected CsvTextFieldParser OpenParser(string filePath)
        {
            var parser = new CsvTextFieldParser(filePath, FileEncoding);
            parser.SetDelimiter(Delimiter);
            return parser;
        }

        protected string FormatRow(IReadOnlyList<string> values)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0) sb.Append(Delimiter);
                sb.Append(EscapeField(values[i]));
            }
            return sb.ToString();
        }

        protected string EscapeField(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            bool needsQuote = value.Contains(Delimiter)
                           || value.Contains('"')
                           || value.Contains('\n')
                           || value.Contains('\r');
            if (!needsQuote) return value;
            return '"' + value.Replace("\"", "\"\"") + '"';
        }

        private static char DetectDelimiter(string filePath)
        {
            string first = File.ReadLines(filePath).FirstOrDefault() ?? string.Empty;
            char[] candidates = { ',', ';', '\t', '|' };
            char best = ',';
            int maxCount = -1;
            foreach (char c in candidates)
            {
                int count = first.Count(ch => ch == c);
                if (count > maxCount) { maxCount = count; best = c; }
            }
            return best;
        }

        private static void EnsureDirectory(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private static void AtomicReplace(string tempPath, string targetPath)
        {
            if (File.Exists(targetPath))
                File.Replace(tempPath, targetPath, null);
            else
                File.Move(tempPath, targetPath);
        }
    }
}
