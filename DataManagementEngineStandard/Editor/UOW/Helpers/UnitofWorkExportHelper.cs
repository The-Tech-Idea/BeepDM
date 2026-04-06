using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor.UOW.Helpers
{
    /// <summary>
    /// Provides export (ToDataTable, ToJsonAsync, ToCsvAsync) and import
    /// (LoadFromJsonAsync, LoadFromCsvAsync) helpers for a UnitofWork collection.
    /// </summary>
    public class UnitofWorkExportHelper<T> where T : Entity, new()
    {
        private readonly ObservableBindingList<T> _units;
        private static PropertyInfo[] _props;
        private static readonly object _propLock = new object();

        public UnitofWorkExportHelper(ObservableBindingList<T> units)
        {
            _units = units ?? throw new ArgumentNullException(nameof(units));
            GetProps();
        }

        private static PropertyInfo[] GetProps()
        {
            if (_props == null)
            {
                lock (_propLock)
                {
                    if (_props == null)
                        _props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                }
            }
            return _props;
        }

        // ── Export ─────────────────────────────────────────────────────────────────

        /// <summary>Materialises the current collection into a DataTable.</summary>
        public DataTable ToDataTable()
        {
            var dt = new DataTable(typeof(T).Name);
            var props = GetProps();
            foreach (var p in props)
                dt.Columns.Add(p.Name, Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType);

            foreach (var item in _units)
            {
                var row = dt.NewRow();
                foreach (var p in props)
                    row[p.Name] = p.GetValue(item) ?? DBNull.Value;
                dt.Rows.Add(row);
            }
            return dt;
        }

        /// <summary>Writes the current collection to <paramref name="stream"/> as a JSON array.</summary>
        public async Task ToJsonAsync(Stream stream, CancellationToken ct = default)
        {
            var list = new List<T>(_units);
            await JsonSerializer.SerializeAsync(stream, list,
                new JsonSerializerOptions { WriteIndented = false }, ct);
        }

        /// <summary>Writes the current collection to <paramref name="stream"/> as CSV.</summary>
        public async Task ToCsvAsync(
            Stream stream,
            char delimiter = ',',
            CancellationToken ct = default)
        {
            var props = GetProps();
            using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);

            // Header row
            await writer.WriteLineAsync(
                string.Join(delimiter.ToString(), Array.ConvertAll(props, p => EscapeCsv(p.Name, delimiter))));

            foreach (var item in _units)
            {
                ct.ThrowIfCancellationRequested();
                var values = Array.ConvertAll(props, p => EscapeCsv(p.GetValue(item)?.ToString() ?? string.Empty, delimiter));
                await writer.WriteLineAsync(string.Join(delimiter.ToString(), values));
            }
        }

        // ── Import ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Deserializes a JSON array from <paramref name="stream"/> and loads it into the collection.
        /// Returns the number of records loaded.
        /// </summary>
        public async Task<int> LoadFromJsonAsync(
            Stream stream,
            bool clearFirst = true,
            CancellationToken ct = default)
        {
            var items = await JsonSerializer.DeserializeAsync<List<T>>(stream, cancellationToken: ct)
                        ?? new List<T>();

            if (clearFirst) _units.Clear();
            await _units.LoadBatchAsync(items, 500, null, ct);
            return items.Count;
        }

        /// <summary>
        /// Parses a CSV stream and loads it into the collection.
        /// Returns the number of records successfully loaded.
        /// </summary>
        public async Task<int> LoadFromCsvAsync(
            Stream stream,
            char delimiter = ',',
            bool clearFirst = true,
            bool hasHeaderRow = true,
            CancellationToken ct = default)
        {
            var props = GetProps();
            var items = new List<T>();
            string[] headers = null;

            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            string line;
            bool firstLine = true;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                ct.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cols = line.Split(delimiter);

                if (firstLine && hasHeaderRow)
                {
                    headers = cols;
                    firstLine = false;
                    continue;
                }
                firstLine = false;

                var item = new T();
                for (int i = 0; i < cols.Length; i++)
                {
                    string propName = headers != null && i < headers.Length ? headers[i].Trim() : null;
                    if (string.IsNullOrEmpty(propName)) continue;

                    var prop = Array.Find(props, p =>
                        string.Equals(p.Name, propName, StringComparison.OrdinalIgnoreCase));
                    if (prop == null || !prop.CanWrite) continue;

                    try
                    {
                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        var value = string.IsNullOrEmpty(cols[i])
                            ? (prop.PropertyType.IsValueType ? Activator.CreateInstance(prop.PropertyType) : null)
                            : Convert.ChangeType(cols[i].Trim(), targetType);
                        prop.SetValue(item, value);
                    }
                    catch { /* skip bad cells */ }
                }
                items.Add(item);
            }

            if (clearFirst) _units.Clear();
            await _units.LoadBatchAsync(items, 500, null, ct);
            return items.Count;
        }

        // ── Helpers ────────────────────────────────────────────────────────────────

        private static string EscapeCsv(string value, char delimiter)
        {
            if (value.Contains(delimiter) || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}
