using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.FileManager.Governance;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// CRUD partial — Insert, Update, Delete, bulk UpdateEntities.
    /// All file write operations are delegated to <see cref="_reader"/>.
    /// </summary>
    public partial class FileDataSource
    {
        // ── Insert ────────────────────────────────────────────────────────────

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            if (_inTransaction)
            {
                _pendingOperations.Add(() => InsertEntityCore(EntityName, InsertedData));
                return ErrorObject;
            }
            return InsertEntityCore(EntityName, InsertedData);
        }

        private IErrorsInfo InsertEntityCore(string entityName, object data)
        {
            EnforceAccessPolicy(entityName, FileOperation.WriteData);

            EntityStructure entity =
                GetEntityStructure(entityName, false)
                ?? GetEntityStructure(entityName, true);

            if (entity == null) return ErrorObject;

            string filePath = ResolveEntityFilePath(entityName);
            var    map      = ToDictionary(data, entity);
            var    headers  = entity.Fields.Select(f => f.Originalfieldname ?? f.FieldName).ToArray();
            var    values   = entity.Fields
                .Select(f => map.TryGetValue(f.FieldName, out var v) ? v?.ToString() : null)
                .ToArray();

            // Validate before writing
            var validation = ValidateRow(entity, values, headers, 0, "Insert");
            if (!ShouldWriteRow(validation, "Insert", 0,
                    rawLine: string.Join(",", values), jobId: entityName))
                return ErrorObject;

            _reader.AppendRow(filePath, headers.ToList(), values.ToList());
            return ErrorObject;
        }

        // ── Update ────────────────────────────────────────────────────────────

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            if (_inTransaction)
            {
                _pendingOperations.Add(() => UpdateEntityCore(EntityName, UploadDataRow));
                return ErrorObject;
            }
            return UpdateEntityCore(EntityName, UploadDataRow);
        }

        private IErrorsInfo UpdateEntityCore(string entityName, object data)
        {
            EnforceAccessPolicy(entityName, FileOperation.WriteData);

            EntityStructure entity =
                GetEntityStructure(entityName, false)
                ?? GetEntityStructure(entityName, true);

            if (entity == null || entity.Fields.Count == 0) return ErrorObject;

            string filePath = ResolveEntityFilePath(entityName);
            var    updates  = ToDictionary(data, entity);
            string keyField = entity.Fields.First().FieldName;

            if (!updates.TryGetValue(keyField, out object keyValue)) return ErrorObject;
            string keyStr = keyValue?.ToString();

            string[] headers = _reader.ReadHeaders(filePath);
            var      hIdx    = BuildColumnIndex(headers);

            // Materialise rows, apply update to the matching row, rewrite
            var newRows = new List<IReadOnlyList<string>>();
            foreach (string[] row in _reader.ReadRows(filePath))
            {
                if (hIdx.TryGetValue(keyField, out int ki)
                    && ki < row.Length
                    && string.Equals(row[ki], keyStr, StringComparison.OrdinalIgnoreCase))
                {
                    string[] updated = (string[])row.Clone();
                    foreach (EntityField field in entity.Fields)
                    {
                        if (updates.TryGetValue(field.FieldName, out var val)
                         && hIdx.TryGetValue(field.FieldName, out int ci)
                         && ci < updated.Length)
                            updated[ci] = val?.ToString() ?? string.Empty;
                    }
                    newRows.Add(updated);
                }
                else
                {
                    newRows.Add(row);
                }
            }

            _reader.RewriteFile(filePath, headers, newRows);
            return ErrorObject;
        }

        // ── Delete ────────────────────────────────────────────────────────────

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            if (_inTransaction)
            {
                _pendingOperations.Add(() => DeleteEntityCore(EntityName, UploadDataRow));
                return ErrorObject;
            }
            return DeleteEntityCore(EntityName, UploadDataRow);
        }

        private IErrorsInfo DeleteEntityCore(string entityName, object data)
        {
            EnforceAccessPolicy(entityName, FileOperation.WriteData);

            EntityStructure entity =
                GetEntityStructure(entityName, false)
                ?? GetEntityStructure(entityName, true);

            if (entity == null || entity.Fields.Count == 0) return ErrorObject;

            string filePath = ResolveEntityFilePath(entityName);
            var    map      = ToDictionary(data, entity);
            string keyField = entity.Fields.First().FieldName;

            if (!map.TryGetValue(keyField, out object keyValue)) return ErrorObject;
            string keyStr = keyValue?.ToString();

            string[] headers = _reader.ReadHeaders(filePath);
            var      hIdx    = BuildColumnIndex(headers);

            if (!hIdx.TryGetValue(keyField, out int ki)) return ErrorObject;

            var kept = _reader.ReadRows(filePath)
                .Where(row => !(ki < row.Length &&
                    string.Equals(row[ki], keyStr, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            _reader.RewriteFile(filePath, headers, kept);
            return ErrorObject;
        }

        // ── Bulk update ───────────────────────────────────────────────────────

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData,
                                          IProgress<PassedArgs> progress)
        {
            if (UploadData is IEnumerable items)
            {
                int count = 0;
                foreach (object row in items)
                {
                    UpdateEntity(EntityName, row);
                    count++;
                    progress?.Report(new PassedArgs
                    {
                        Messege   = $"Updated {count} rows",
                        EventType = "FileDataSource"
                    });
                }
            }
            return ErrorObject;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Builds a case-insensitive column-name → index map from a header row.
        /// Both the raw name and the normalised name are keyed.
        /// </summary>
        private static Dictionary<string, int> BuildColumnIndex(string[] headers)
        {
            var idx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                idx.TryAdd(headers[i], i);
                idx.TryAdd(NormalizeFieldName(headers[i]), i);
            }
            return idx;
        }

        /// <summary>
        /// Converts an arbitrary object to a field-name → value dictionary
        /// using the entity's field definitions for normalisation.
        /// </summary>
        private static Dictionary<string, object> ToDictionary(object data, EntityStructure entity)
        {
            if (data == null)
                return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (data is Dictionary<string, object> d)
                return new Dictionary<string, object>(d, StringComparer.OrdinalIgnoreCase);

            if (data is IDictionary<string, object> id)
                return new Dictionary<string, object>(id, StringComparer.OrdinalIgnoreCase);

            if (data is DataRow dr)
                return dr.Table.Columns.Cast<DataColumn>()
                    .ToDictionary(
                        c => NormalizeFieldName(c.ColumnName),
                        c => dr[c.ColumnName],
                        StringComparer.OrdinalIgnoreCase);

            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(data))
                result[NormalizeFieldName(prop.Name)] = prop.GetValue(data);
            return result;
        }
    }
}

      