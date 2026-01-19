using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Utils
{
    internal static class EntityStructureHelper
    {
        internal static EntityStructure FromDataTable(DataTable tb)
        {
            var entity = new EntityStructure
            {
                EntityName = tb.TableName,
                Fields = new List<EntityField>(),
                PrimaryKeys = new List<EntityField>()
            };
            int idx = 0;
            foreach (DataColumn col in tb.Columns)
            {
                var field = BuildFieldFromColumn(col);
                field.FieldIndex = idx++;
                entity.Fields.Add(field);
            }
            return entity;
        }

        internal static EntityField BuildFieldFromColumn(DataColumn col)
        {
            var cat = DbFieldCategory.String;
            var t = col.DataType;
            if (t == typeof(string)) cat = DbFieldCategory.String;
            else if (t == typeof(int) || t == typeof(long) || t == typeof(float) || t == typeof(double) || t == typeof(decimal)) cat = DbFieldCategory.Numeric;
            else if (t == typeof(DateTime)) cat = DbFieldCategory.Date;
            else if (t == typeof(bool)) cat = DbFieldCategory.Boolean;
            else if (t == typeof(byte[])) cat = DbFieldCategory.Binary;
            else if (t == typeof(Guid)) cat = DbFieldCategory.Guid;
            else if (t == typeof(System.Text.Json.JsonDocument)) cat = DbFieldCategory.Json;
            else if (t == typeof(System.Xml.XmlDocument)) cat = DbFieldCategory.Xml;
            else if (t.IsEnum) cat = DbFieldCategory.Enum;

            return new EntityField
            {
                EntityName = col.Table.TableName,
               FieldName = col.ColumnName,
                Fieldtype = t.ToString(),
               FieldCategory = cat,
                AllowDBNull = col.AllowDBNull,
                IsAutoIncrement = col.AutoIncrement,
                IsUnique = col.Unique,
                Size1 = SafeGetMaxLen(col)
            };
        }

        private static int SafeGetMaxLen(DataColumn col)
        {
            try { return col.MaxLength; } catch { return 0; }
        }

        internal static EntityStructure FromType(Type type)
        {
            var entity = new EntityStructure
            {
                EntityName = type.Name,
                Fields = new List<EntityField>(),
                PrimaryKeys = new List<EntityField>()
            };
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var field = BuildFieldFromProperty(prop);
                entity.Fields.Add(field);
                if (field.IsKey) entity.PrimaryKeys.Add(field);
            }
            return entity;
        }

        internal static EntityField BuildFieldFromProperty(PropertyInfo prop)
        {
            var t = prop.PropertyType;
            var cat = DbFieldCategory.String;
            if (t == typeof(string)) cat = DbFieldCategory.String;
            else if (t == typeof(int) || t == typeof(long) || t == typeof(float) || t == typeof(double) || t == typeof(decimal)) cat = DbFieldCategory.Numeric;
            else if (t == typeof(DateTime)) cat = DbFieldCategory.Date;
            else if (t == typeof(bool)) cat = DbFieldCategory.Boolean;
            else if (t == typeof(byte[])) cat = DbFieldCategory.Binary;
            else if (t == typeof(Guid)) cat = DbFieldCategory.Guid;
            else if (t == typeof(System.Text.Json.JsonDocument)) cat = DbFieldCategory.Json;
            else if (t == typeof(System.Xml.XmlDocument)) cat = DbFieldCategory.Xml;
            else if (t.IsEnum) cat = DbFieldCategory.Enum;

            return new EntityField
            {
               FieldName = prop.Name,
                Fieldtype = t.FullName,
               FieldCategory = cat
            };
        }

        internal static List<object> ConvertTableToList(DataTable dt, EntityStructure ent, Type entType) =>
            DataConversionHelper.ConvertTableToList(dt, ent, entType);
    }
}