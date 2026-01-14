using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    public static partial class EntitiesExtensions
    {
        #region Diff & Merge

        public static EntityDiff DiffAgainst(this EntityStructure entity, EntityStructure other)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(other);

            var added = new List<EntityField>();
            var removed = new List<EntityField>();
            var changed = new List<FieldChange>();

            var otherMap = other.Fields.ToDictionary(f => f.fieldname, StringComparer.OrdinalIgnoreCase);
            var thisMap = entity.Fields.ToDictionary(f => f.fieldname, StringComparer.OrdinalIgnoreCase);

            foreach (var field in entity.Fields)
            {
                if (!otherMap.TryGetValue(field.fieldname, out var otherField))
                {
                    removed.Add(field);
                }
                else if (!FieldsEqual(field, otherField))
                {
                    changed.Add(new FieldChange(field.fieldname, field, otherField));
                }
            }

            foreach (var field in other.Fields)
            {
                if (!thisMap.ContainsKey(field.fieldname))
                {
                    added.Add(field);
                }
            }

            return new EntityDiff(added, removed, changed);
        }

        public static EntityStructure MergeFields(this EntityStructure entity, EntityStructure sourceEntity, FieldMergeStrategy strategy)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(sourceEntity);

            foreach (var sourceField in sourceEntity.Fields)
            {
                var existing = entity.GetField(sourceField.fieldname);
                if (existing == null)
                {
                    entity.AddField((EntityField)sourceField.Clone());
                    continue;
                }

                if (strategy == FieldMergeStrategy.KeepExisting)
                {
                    continue;
                }

                if (strategy == FieldMergeStrategy.Overwrite)
                {
                    entity.Fields.Remove(existing);
                    entity.PrimaryKeys.Remove(existing);
                    entity.AddField((EntityField)sourceField.Clone());
                    continue;
                }

                if (strategy == FieldMergeStrategy.PreferNonNull)
                {
                    var merged = (EntityField)existing.Clone();
                    merged.fieldtype = string.IsNullOrWhiteSpace(existing.fieldtype) ? sourceField.fieldtype : existing.fieldtype;
                    merged.Description = string.IsNullOrWhiteSpace(existing.Description) ? sourceField.Description : existing.Description;
                    merged.AllowDBNull = existing.AllowDBNull && sourceField.AllowDBNull;
                    merged.IsKey = existing.IsKey || sourceField.IsKey;
                    entity.Fields.Remove(existing);
                    entity.PrimaryKeys.Remove(existing);
                    entity.AddField(merged);
                }
            }

            return entity;
        }

        #endregion

        #region Naming & Ordering

        public static EntityStructure RenameField(this EntityStructure entity, string oldName, string newName)
        {
            ArgumentNullException.ThrowIfNull(entity);
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return entity;

            var field = entity.GetField(oldName);
            if (field == null) return entity;

            field.fieldname = newName;
            if (entity.PrimaryKeys.Contains(field))
            {
                entity.PrimaryKeys.Remove(field);
                entity.PrimaryKeys.Add(field);
            }
            return entity;
        }

        public static EntityStructure ReorderFields(this EntityStructure entity, params string[] orderedNames)
        {
            ArgumentNullException.ThrowIfNull(entity);
            if (orderedNames == null || orderedNames.Length == 0) return entity;

            var orderMap = orderedNames.Select((n, i) => new { n, i }).ToDictionary(x => x.n, x => x.i, StringComparer.OrdinalIgnoreCase);
            entity.Fields = entity.Fields.OrderBy(f => orderMap.TryGetValue(f.fieldname, out var idx) ? idx : int.MaxValue).ToList();
            return entity;
        }

        public static EntityStructure ApplyNaming(this EntityStructure entity, Func<string, string> namer)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(namer);
            foreach (var f in entity.Fields)
            {
                f.fieldname = namer(f.fieldname);
            }
            return entity;
        }

        #endregion

        #region Validation & Keys

        public static (bool IsValid, List<string> Errors) ValidateDetailed(this EntityStructure entity, bool requirePrimaryKey = false)
        {
            var result = entity.Validate();
            if (!result.IsValid && requirePrimaryKey == false)
            {
                return result;
            }

            if (requirePrimaryKey && (entity.PrimaryKeys == null || entity.PrimaryKeys.Count == 0))
            {
                result.Errors.Add("Entity must have a primary key.");
            }

            foreach (var field in entity.Fields)
            {
                if (!string.IsNullOrWhiteSpace(field.fieldtype) && Type.GetType(field.fieldtype) == null)
                {
                    result.Errors.Add($"Field '{field.fieldname}' has an unknown type '{field.fieldtype}'.");
                }
            }

            return (!result.Errors.Any(), result.Errors);
        }

        public static EntityStructure EnsurePrimaryKey(this EntityStructure entity, string keyName = "Id", Type keyType = null)
        {
            ArgumentNullException.ThrowIfNull(entity);
            if (entity.PrimaryKeys.Any()) return entity;

            keyType ??= typeof(int);
            var field = new EntityField
            {
                fieldname = keyName,
                fieldtype = keyType.FullName,
                fieldCategory = DbFieldCategory.Integer,
                AllowDBNull = false,
                IsKey = true,
                EntityName = entity.EntityName,
                IsAutoIncrement = keyType == typeof(int) || keyType == typeof(long)
            };

            entity.Fields.Insert(0, field);
            entity.PrimaryKeys.Add(field);
            return entity;
        }

        #endregion

        #region Conversion

        public static DataTable ToDataTableWithData(this EntityStructure entity, IEnumerable<object> data)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(data);

            var list = data.ToList();
            if (!entity.Fields.Any() && list.Count > 0)
            {
                entity.FromType(list.First().GetType());
            }

            var table = entity.ToDataTableSchema();
            foreach (var item in list)
            {
                var row = table.NewRow();
                foreach (var field in entity.Fields)
                {
                    var prop = item.GetType().GetProperty(field.fieldname, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (prop != null)
                    {
                        row[field.fieldname] = prop.GetValue(item) ?? DBNull.Value;
                    }
                }
                table.Rows.Add(row);
            }

            return table;
        }

        public static EntityStructure FromDataReader(this EntityStructure entity, IDataReader reader)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(reader);

            entity.Fields.Clear();
            entity.PrimaryKeys.Clear();
            entity.EntityName = string.IsNullOrWhiteSpace(entity.EntityName) ? reader.GetType().Name : entity.EntityName;

            var schema = reader.GetSchemaTable();
            if (schema == null)
            {
                return entity;
            }

            foreach (DataRow row in schema.Rows)
            {
                var name = row["ColumnName"]?.ToString();
                var type = row["DataType"] as Type ?? typeof(string);
                var isKey = row.Table.Columns.Contains("IsKey") && row["IsKey"] is bool b && b;

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var field = new EntityField
                {
                    fieldname = name,
                    fieldtype = type.FullName,
                    fieldCategory = GetFieldCategory(type),
                    AllowDBNull = row.Table.Columns.Contains("AllowDBNull") && row["AllowDBNull"] is bool canNull && canNull,
                    IsKey = isKey,
                    EntityName = entity.EntityName
                };

                entity.Fields.Add(field);
                if (isKey)
                {
                    entity.PrimaryKeys.Add(field);
                }
            }

            return entity;
        }

        #endregion

        #region Lookups

        public static bool TryGetField(this EntityStructure entity, string fieldName, out EntityField field)
        {
            field = entity.GetField(fieldName);
            return field != null;
        }

        public static List<EntityField> GetKeyFields(this EntityStructure entity)
        {
            return entity?.PrimaryKeys?.ToList() ?? new List<EntityField>();
        }

        public static List<EntityField> GetRequiredFields(this EntityStructure entity)
        {
            if (entity == null) return new List<EntityField>();
            return entity.Fields.Where(f => f.AllowDBNull == false).ToList();
        }

        public static List<EntityField> GetNavigationFields(this EntityStructure entity)
        {
            if (entity == null) return new List<EntityField>();
            // Without a dedicated ForeignKey category, fall back to non-primary key fields marked as keys if available.
            return entity.Fields.Where(f => f.IsKey && !entity.PrimaryKeys.Contains(f)).ToList();
        }

        #endregion

        #region Sampling

        public static List<Dictionary<string, object>> GenerateSampleRows(this EntityStructure entity, int count)
        {
            ArgumentNullException.ThrowIfNull(entity);
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

            var rng = new Random();
            var rows = new List<Dictionary<string, object>>(count);

            for (int i = 0; i < count; i++)
            {
                var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var field in entity.Fields)
                {
                    row[field.fieldname] = GenerateSampleValue(field, rng, i);
                }
                rows.Add(row);
            }

            return rows;
        }

        private static object GenerateSampleValue(EntityField field, Random rng, int index)
        {
            var type = field.fieldtype?.ToLowerInvariant() ?? string.Empty;
            if (type.Contains("int")) return index;
            if (type.Contains("decimal") || type.Contains("double") || type.Contains("float")) return rng.NextDouble() * 100;
            if (type.Contains("bool")) return rng.Next(0, 2) == 0;
            if (type.Contains("date")) return DateTime.UtcNow.AddDays(-rng.Next(0, 30));
            if (type.Contains("guid")) return Guid.NewGuid();
            return $"Sample_{field.fieldname}_{index}";
        }

        #endregion

        #region Helpers

        private static bool FieldsEqual(EntityField a, EntityField b)
        {
            return string.Equals(a.fieldname, b.fieldname, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.fieldtype, b.fieldtype, StringComparison.OrdinalIgnoreCase)
                && a.AllowDBNull == b.AllowDBNull
                && a.IsKey == b.IsKey
                && a.IsUnique == b.IsUnique;
        }

        public enum FieldMergeStrategy
        {
            KeepExisting,
            Overwrite,
            PreferNonNull
        }

        public sealed class EntityDiff
        {
            public EntityDiff(List<EntityField> added, List<EntityField> removed, List<FieldChange> changed)
            {
                AddedFields = added ?? new List<EntityField>();
                RemovedFields = removed ?? new List<EntityField>();
                ChangedFields = changed ?? new List<FieldChange>();
            }

            public List<EntityField> AddedFields { get; }
            public List<EntityField> RemovedFields { get; }
            public List<FieldChange> ChangedFields { get; }
        }

        public sealed class FieldChange
        {
            public FieldChange(string name, EntityField current, EntityField updated)
            {
                Name = name;
                Current = current;
                Updated = updated;
            }

            public string Name { get; }
            public EntityField Current { get; }
            public EntityField Updated { get; }
        }

        #endregion
    }
}
