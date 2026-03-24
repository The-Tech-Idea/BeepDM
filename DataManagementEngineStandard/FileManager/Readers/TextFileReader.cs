using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.FileManager.Attributes;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.FileManager.Readers
{
    /// <summary>
    /// Plain-text reader — one value per line, no delimiter.
    /// Each line becomes a single-column row; the implicit column name is "Line".
    /// Handles .txt, .log, .md, .ini, .yaml etc.
    /// </summary>
    [FileReader(DataSourceType.Text, "Plain text", "txt")]
    public sealed class TextFileReader : IFileFormatReader
    {
        private const string ColumnName = "Line";
        private static readonly string[] Headers = { ColumnName };
        private readonly List<RowDiagnostic> _diagnostics = new();

        private Encoding _encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public DataSourceType SupportedType => DataSourceType.Text;
        public string GetDefaultExtension() => "txt";
        /// <summary>Text files have no header row; this flag is always treated as false internally.</summary>
        public bool HasHeader { get; set; } = false;
        public ParseMode ParseMode { get; set; } = ParseMode.Lenient;
        public IReadOnlyList<RowDiagnostic> LastDiagnostics => _diagnostics;
        public void ClearDiagnostics() => _diagnostics.Clear();

        public void Configure(IConnectionProperties props) { /* no delimiter */ }

        // ── Schema ───────────────────────────────────────────────────────────

        public string[] ReadHeaders(string filePath) => Headers;

        public IEnumerable<string[]> ReadRows(string filePath)
        {
            if (!File.Exists(filePath)) yield break;
            foreach (string line in File.ReadLines(filePath, _encoding))
                yield return new[] { line };
        }

        public string InferFieldType(string current, string rawValue)
            => "System.String"; // always string for plain text

        // ── Schema: GetEntityStructure ──────────────────────────────────────

        /// <summary>
        /// Every plain-text file is treated as a single-column table named "Line".
        /// </summary>
        public EntityStructure GetEntityStructure(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            string entityName = Path.GetFileNameWithoutExtension(filePath);
            return FileReaderEntityHelper.BuildEntityStructure(entityName, Headers);
        }

        // ── Write ───────────────────────────────────────────────────────

        public bool CreateFile(string filePath, IReadOnlyList<string> headers)
        {
            EnsureDirectory(filePath);
            File.WriteAllText(filePath, string.Empty, _encoding);
            return true;
        }

        public bool AppendRow(string filePath, IReadOnlyList<string> headers,
                              IReadOnlyList<string> values)
        {
            if (!File.Exists(filePath)) CreateFile(filePath, headers);
            string line = values.Count > 0 ? values[0] : string.Empty;
            File.AppendAllText(filePath, line + Environment.NewLine, _encoding);
            return true;
        }

        public bool RewriteFile(string filePath, IReadOnlyList<string> headers,
                                IEnumerable<IReadOnlyList<string>> rows)
        {
            EnsureDirectory(filePath);
            string tempPath = filePath + ".tmp";
            using (var writer = new StreamWriter(tempPath, append: false, _encoding))
            {
                foreach (var row in rows)
                    writer.WriteLine(row.Count > 0 ? row[0] : string.Empty);
            }
            if (File.Exists(filePath))
                File.Replace(tempPath, filePath, null);
            else
                File.Move(tempPath, filePath);
            return true;
        }

        private static void EnsureDirectory(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
    }
}
