using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor.Importing.Profiling
{
    /// <summary>
    /// Samples an entity from a data source and builds a <see cref="DataProfile"/>.
    /// Does not persist results — callers may store them via a history or JSON file if required.
    /// </summary>
    public static class DataProfiler
    {
        /// <summary>
        /// Reads up to <paramref name="sampleSize"/> records from <paramref name="entityName"/>
        /// on <paramref name="dataSourceName"/> and computes per-field statistics.
        /// </summary>
        public static async Task<DataProfile> ProfileAsync(
            IDMEEditor editor,
            string     dataSourceName,
            string     entityName,
            int        sampleSize = 1000,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(editor);
            if (string.IsNullOrWhiteSpace(dataSourceName)) throw new ArgumentNullException(nameof(dataSourceName));
            if (string.IsNullOrWhiteSpace(entityName))     throw new ArgumentNullException(nameof(entityName));

            var profile = new DataProfile
            {
                DataSourceName = dataSourceName,
                EntityName     = entityName,
                CapturedAt     = DateTime.UtcNow
            };

            var ds = editor.GetDataSource(dataSourceName)
                ?? throw new InvalidOperationException($"Data source '{dataSourceName}' not found.");

            ds.Openconnection();

            // Fetch sample — returns IEnumerable<object>
            var rawData = await Task.Run(() => ds.GetEntity(entityName, null), token).ConfigureAwait(false);
            if (rawData == null) return profile;

            var rows = rawData.Take(sampleSize).ToList();
            if (rows.Count == 0) return profile;

            profile.SampleSize = rows.Count;

            // Determine field names from first row or entity structure
            var fieldNames = GetFieldNames(ds, entityName, rows[0]);

            foreach (var fieldName in fieldNames)
            {
                token.ThrowIfCancellationRequested();
                var fp = BuildFieldProfile(fieldName, rows);
                profile.Fields.Add(fp);
            }

            return profile;
        }

        // ------------------------------------------------------------------
        private static IEnumerable<string> GetFieldNames(IDataSource ds, string entityName, object firstRow)
        {
            // Prefer entity structure for authoritative column list
            try
            {
                var structure = ds.GetEntityStructure(entityName, false);
                if (structure?.Fields?.Count > 0)
                    return structure.Fields.Select(f => f.FieldName).Where(n => !string.IsNullOrEmpty(n));
            }
            catch { /* fall through */ }

            // Fallback: reflect public properties of the first row object
            return firstRow.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name);
        }

        private static FieldProfile BuildFieldProfile(string fieldName, List<object> rows)
        {
            var fp = new FieldProfile { FieldName = fieldName, SampleCount = rows.Count };

            var values = rows
                .Select(r => ExtractValue(r, fieldName))
                .ToList();

            fp.NullCount     = values.Count(v => v == null || v == DBNull.Value);
            fp.DistinctCount = values.Where(v => v != null && v != DBNull.Value).Distinct().Count();

            var nonNull = values.Where(v => v != null && v != DBNull.Value).ToList();
            if (nonNull.Count == 0) return fp;

            fp.InferredType = nonNull.First()!.GetType().Name;

            // String stats
            if (fp.InferredType is "String" or "string")
            {
                var strs    = nonNull.Select(v => v!.ToString()!).ToList();
                var lengths = strs.Select(s => s.Length).ToList();
                fp.MinLength = lengths.Min();
                fp.MaxLength = lengths.Max();
                fp.MinValue  = strs.OrderBy(s => s).First();
                fp.MaxValue  = strs.OrderByDescending(s => s).First();
                return fp;
            }

            // Numeric stats
            try
            {
                var nums = nonNull.Select(v => Convert.ToDouble(v)).ToList();
                fp.MinValue = nums.Min().ToString();
                fp.MaxValue = nums.Max().ToString();
                fp.Mean     = nums.Average();
                double variance = nums.Sum(n => Math.Pow(n - fp.Mean!.Value, 2)) / nums.Count;
                fp.StdDev   = Math.Sqrt(variance);
            }
            catch { /* non-numeric — leave stats null */ }

            return fp;
        }

        private static object? ExtractValue(object record, string fieldName)
        {
            if (record == null) return null;
            var prop = record.GetType().GetProperty(fieldName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var val = prop?.GetValue(record);
            return val == DBNull.Value ? null : val;
        }
    }
}