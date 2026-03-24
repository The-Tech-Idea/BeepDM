using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.FileManager.Governance;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// Query / read partial — GetEntity, paging, async, GetScalar.
    /// Delegates all file parsing to the <see cref="_reader"/> set in Connection.cs.
    /// Filter evaluation is format-agnostic (works on projected string values).
    /// </summary>
    public partial class FileDataSource
    {
        // ── Public query API ──────────────────────────────────────────────────

        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            EnforceAccessPolicy(EntityName, FileOperation.ReadData);
            var securedFilters = ApplyRowSecurity(EntityName, filter);

            string filePath = ResolveEntityFilePath(EntityName);
            if (!File.Exists(filePath))
                return Enumerable.Empty<object>();

            EntityStructure structure =
                GetEntityStructure(EntityName, false)
                ?? GetEntityStructure(EntityName, true);

            if (structure == null)
                return Enumerable.Empty<object>();

            string[] headers    = _reader.ReadHeaders(filePath);
            var      headerIndex = BuildHeaderIndex(headers);

            var rows = new List<object>();
            foreach (string[] values in _reader.ReadRows(filePath))
            {
                if (!MatchesFilters(securedFilters, structure, values, headerIndex))
                    continue;

                var row = ProjectRow(structure, values, headerIndex);
                rows.Add(ApplyFieldMasking(structure, row));
            }
            return rows;
        }

        /// <summary>
        /// Paged query — single-pass streaming so the entire file is never fully materialized.
        /// Returns the actual total match count alongside the requested page window.
        /// </summary>
        public PagedResult GetEntity(string EntityName, List<AppFilter> filter,
                                     int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize  <= 0) pageSize   = 100;

            EnforceAccessPolicy(EntityName, FileOperation.ReadData);
            var securedFilters = ApplyRowSecurity(EntityName, filter);

            string filePath = ResolveEntityFilePath(EntityName);
            if (!File.Exists(filePath))
                return new PagedResult(new List<object>(), pageNumber, pageSize, 0);

            EntityStructure structure =
                GetEntityStructure(EntityName, false)
                ?? GetEntityStructure(EntityName, true);

            if (structure == null)
                return new PagedResult(new List<object>(), pageNumber, pageSize, 0);

            string[] headers    = _reader.ReadHeaders(filePath);
            var      headerIndex = BuildHeaderIndex(headers);

            int skip       = (pageNumber - 1) * pageSize;
            int matchCount = 0;
            var page       = new List<object>();

            foreach (string[] values in _reader.ReadRows(filePath))
            {
                if (!MatchesFilters(securedFilters, structure, values, headerIndex)) continue;

                if (matchCount >= skip && page.Count < pageSize)
                {
                    var row = ProjectRow(structure, values, headerIndex);
                    page.Add(ApplyFieldMasking(structure, row));
                }
                matchCount++;
            }

            return new PagedResult(page, pageNumber, pageSize, matchCount);
        }

        /// <summary>Non-cancellable async overload — satisfies interface contract.</summary>
        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> filter)
            => GetEntityAsync(EntityName, filter, CancellationToken.None);

        /// <summary>Cancellable async read — offloads file I/O to a thread-pool thread.</summary>
        public Task<IEnumerable<object>> GetEntityAsync(
            string EntityName, List<AppFilter> filter, CancellationToken cancellationToken)
            => Task.Run(() => GetEntityCancellable(EntityName, filter, cancellationToken),
                        cancellationToken);

        private IEnumerable<object> GetEntityCancellable(
            string EntityName, List<AppFilter> filter, CancellationToken ct)
        {
            EnforceAccessPolicy(EntityName, FileOperation.ReadData);
            var securedFilters = ApplyRowSecurity(EntityName, filter);

            string filePath = ResolveEntityFilePath(EntityName);
            if (!File.Exists(filePath)) return Enumerable.Empty<object>();

            EntityStructure structure =
                GetEntityStructure(EntityName, false)
                ?? GetEntityStructure(EntityName, true);

            if (structure == null) return Enumerable.Empty<object>();

            string[] headers    = _reader.ReadHeaders(filePath);
            var      headerIndex = BuildHeaderIndex(headers);

            var rows = new List<object>();
            foreach (string[] values in _reader.ReadRows(filePath))
            {
                ct.ThrowIfCancellationRequested();
                if (!MatchesFilters(securedFilters, structure, values, headerIndex)) continue;
                var row = ProjectRow(structure, values, headerIndex);
                rows.Add(ApplyFieldMasking(structure, row));
            }
            return rows;
        }

        public Task<double> GetScalarAsync(string query)
            => Task.FromResult(GetScalar(query));

        public double GetScalar(string query)
        {
            string entity = EntitiesNames.FirstOrDefault() ?? DatasourceName;
            return GetEntity(entity, new List<AppFilter>()).Count();
        }

        // ── Row projection ────────────────────────────────────────────────────

        private static Dictionary<string, object> ProjectRow(
            EntityStructure structure,
            string[] values,
            Dictionary<string, int> headerIndex)
        {
            var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (EntityField field in structure.Fields)
            {
                if (!headerIndex.TryGetValue(field.FieldName, out int idx))
                    continue;
                string raw = idx < values.Length ? values[idx] : null;
                row[field.FieldName] = ConvertToFieldType(raw, field.Fieldtype);
            }
            return row;
        }

        // ── Header index ──────────────────────────────────────────────────────

        private static Dictionary<string, int> BuildHeaderIndex(string[] headers)
        {
            var idx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                string normalized = NormalizeFieldName(headers[i]);
                idx.TryAdd(headers[i], i);
                idx.TryAdd(normalized, i);
            }
            return idx;
        }

        // ── Filter evaluation ─────────────────────────────────────────────────

        private static bool MatchesFilters(
            List<AppFilter> filters,
            EntityStructure structure,
            string[] values,
            Dictionary<string, int> headerIndex)
        {
            if (filters == null || filters.Count == 0) return true;

            foreach (AppFilter appFilter in filters)
            {
                if (appFilter == null || string.IsNullOrWhiteSpace(appFilter.FieldName))
                    continue;

                if (!headerIndex.TryGetValue(appFilter.FieldName, out int idx)
                 && !headerIndex.TryGetValue(NormalizeFieldName(appFilter.FieldName), out idx))
                    return false;

                string raw = idx < values.Length ? values[idx] : null;
                if (!EvaluateFilter(raw, appFilter))
                    return false;
            }
            return true;
        }

        private static bool EvaluateFilter(string raw, AppFilter filter)
        {
            string op     = (filter.Operator    ?? "=").Trim().ToLowerInvariant();
            string target =  filter.FilterValue ?? string.Empty;
            string left   =  raw               ?? string.Empty;

            switch (op)
            {
                case "=" or "eq":
                    return string.Equals(left, target, StringComparison.OrdinalIgnoreCase);
                case "!=" or "<>" or "neq":
                    return !string.Equals(left, target, StringComparison.OrdinalIgnoreCase);
                case "contains":
                    return left.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0;
                case "startswith":
                    return left.StartsWith(target, StringComparison.OrdinalIgnoreCase);
                case "endswith":
                    return left.EndsWith(target, StringComparison.OrdinalIgnoreCase);
                case ">" or ">=" or "<" or "<=":
                    if (decimal.TryParse(left,   NumberStyles.Any, CultureInfo.InvariantCulture, out decimal lNum)
                     && decimal.TryParse(target, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal rNum))
                        return op switch { ">" => lNum > rNum, ">=" => lNum >= rNum,
                                          "<" => lNum < rNum, "<=" => lNum <= rNum, _ => false };

                    if (DateTime.TryParse(left,   out DateTime lDate)
                     && DateTime.TryParse(target, out DateTime rDate))
                        return op switch { ">" => lDate > rDate, ">=" => lDate >= rDate,
                                          "<" => lDate < rDate, "<=" => lDate <= rDate, _ => false };
                    return false;
                default:
                    return false;
            }
        }

        // ── Type conversion ───────────────────────────────────────────────────

        private static object ConvertToFieldType(string raw, string typeName)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            Type target = Type.GetType(typeName) ?? typeof(string);
            try
            {
                if (target == typeof(string))  return raw;
                if (target == typeof(int)     && int.TryParse(raw,     out int    iv)) return iv;
                if (target == typeof(long)    && long.TryParse(raw,    out long   lv)) return lv;
                if (target == typeof(decimal) && decimal.TryParse(raw, NumberStyles.Any,
                                                 CultureInfo.InvariantCulture, out decimal dv)) return dv;
                if (target == typeof(double)  && double.TryParse(raw,  NumberStyles.Any,
                                                 CultureInfo.InvariantCulture, out double  ddv)) return ddv;
                if (target == typeof(bool)    && bool.TryParse(raw,    out bool   bv)) return bv;
                if (target == typeof(DateTime)&& DateTime.TryParse(raw,out DateTime dt)) return dt;
                return Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
            }
            catch { return raw; }
        }
    }
}

      