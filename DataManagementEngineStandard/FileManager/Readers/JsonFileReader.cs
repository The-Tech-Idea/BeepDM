using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.FileManager.Attributes;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.FileManager.Readers
{
    /// <summary>
    /// Reads and writes JSON files.
    /// Supports both JSON Lines (one object per line) and a JSON array
    /// at the root.  Writes as a JSON array.
    /// </summary>
    [FileReader(DataSourceType.Json, "JSON", "json")]
    public sealed class JsonFileReader : IFileFormatReader
    {
        private Encoding _encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        private readonly List<RowDiagnostic> _diagnostics = new();

        public DataSourceType SupportedType => DataSourceType.Json;
        public string GetDefaultExtension() => "json";
        /// <summary>JSON always derives column names from the first object's keys; this flag is informational.</summary>
        public bool HasHeader { get; set; } = true;
        public ParseMode ParseMode { get; set; } = ParseMode.Lenient;
        public IReadOnlyList<RowDiagnostic> LastDiagnostics => _diagnostics;
        public void ClearDiagnostics() => _diagnostics.Clear();

        public void Configure(IConnectionProperties props) { /* no delimiter concept */ }

        // ── Schema ───────────────────────────────────────────────────────────

        public string[] ReadHeaders(string filePath)
        {
            foreach (var row in ReadRows(filePath))
                return row; // header order inferred from first object's keys
            return Array.Empty<string>();
        }

        public IEnumerable<string[]> ReadRows(string filePath)
        {
            if (!File.Exists(filePath)) yield break;

            string[] headers = null;

            foreach (JObject obj in EnumerateObjects(filePath))
            {
                if (headers == null)
                {
                    headers = obj.Properties().Select(p => p.Name).ToArray();
                    yield return headers; // first yield is the header row
                }
                yield return headers.Select(h => obj[h]?.ToString() ?? string.Empty).ToArray();
            }
        }

        public string InferFieldType(string current, string rawValue)
            => TypeInferenceHelper.Widen(current, rawValue);

        // ── Schema: GetEntityStructure ──────────────────────────────────────

        public EntityStructure GetEntityStructure(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            string entityName = Path.GetFileNameWithoutExtension(filePath);
            string[] keys = ReadHeaders(filePath);
            if (keys.Length == 0) return null;
            return FileReaderEntityHelper.BuildEntityStructure(entityName, keys);
        }

        // ── Write ────────────────────────────────────────────

        public bool CreateFile(string filePath, IReadOnlyList<string> headers)
        {
            EnsureDirectory(filePath);
            File.WriteAllText(filePath, "[]", _encoding);
            return true;
        }

        public bool AppendRow(string filePath, IReadOnlyList<string> headers, IReadOnlyList<string> values)
        {
            var existing = LoadArray(filePath);
            var obj = BuildObject(headers, values);
            existing.Add(obj);
            WriteArray(filePath, existing);
            return true;
        }

        public bool RewriteFile(string filePath, IReadOnlyList<string> headers,
                                IEnumerable<IReadOnlyList<string>> rows)
        {
            var array = new JArray();
            foreach (var row in rows)
                array.Add(BuildObject(headers, row));
            WriteArray(filePath, array);
            return true;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private IEnumerable<JObject> EnumerateObjects(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sr = new StreamReader(stream, _encoding);
            using var reader = new JsonTextReader(sr);

            // Try array style: [ {...}, {...} ]
            // Try JSONL style: one object per line
            string content = sr.ReadToEnd();

            // Reset for re-reading
            // Use string for simplicity so both formats work
            content = File.ReadAllText(filePath, _encoding);
            content = content.Trim();

            if (content.StartsWith("["))
            {
                var arr = JArray.Parse(content);
                foreach (var token in arr)
                    if (token is JObject obj) yield return obj;
            }
            else
            {
                foreach (var line in content.Split('\n'))
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;
                    JObject obj;
                    try { obj = JObject.Parse(trimmed); }
                    catch { continue; }
                    yield return obj;
                }
            }
        }

        private JArray LoadArray(string filePath)
        {
            if (!File.Exists(filePath)) return new JArray();
            try
            {
                string content = File.ReadAllText(filePath, _encoding).Trim();
                if (string.IsNullOrEmpty(content) || content == "[]") return new JArray();
                if (content.StartsWith("[")) return JArray.Parse(content);

                // JSONL — convert to array
                var arr = new JArray();
                foreach (var line in content.Split('\n'))
                {
                    string t = line.Trim();
                    if (!string.IsNullOrEmpty(t))
                        arr.Add(JObject.Parse(t));
                }
                return arr;
            }
            catch { return new JArray(); }
        }

        private void WriteArray(string filePath, JArray array)
        {
            EnsureDirectory(filePath);
            string tempPath = filePath + ".tmp";
            File.WriteAllText(tempPath,
                array.ToString(Formatting.Indented), _encoding);
            if (File.Exists(filePath))
                File.Replace(tempPath, filePath, null);
            else
                File.Move(tempPath, filePath);
        }

        private static JObject BuildObject(IReadOnlyList<string> headers, IReadOnlyList<string> values)
        {
            var obj = new JObject();
            for (int i = 0; i < headers.Count; i++)
                obj[headers[i]] = i < values.Count ? values[i] : string.Empty;
            return obj;
        }

        private static void EnsureDirectory(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
    }
}
