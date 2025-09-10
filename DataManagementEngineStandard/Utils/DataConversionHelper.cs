using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utils;
using TheTechIdea.Beep.Workflow;

namespace TheTechIdea.Beep.Utils
{
    internal static class DataConversionHelper
    {
        internal static IBindingList ConvertDataTableToObservableList(DataTable dataTable, Type type)
        {
            var listType = typeof(ObservableBindingList<>).MakeGenericType(type);
            var list = (IBindingList)Activator.CreateInstance(listType);
            foreach (DataRow row in dataTable.Rows)
            {
                var item = Activator.CreateInstance(type);
                foreach (DataColumn column in dataTable.Columns)
                {
                    var prop = type.GetProperty(column.ColumnName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (prop != null && row[column] != DBNull.Value)
                    {
                        prop.SetValue(item, Convert.ChangeType(row[column], prop.PropertyType));
                    }
                }
                listType.GetMethod("Add")?.Invoke(list, new[] { item });
            }
            return list;
        }

        internal static ObservableBindingList<T> ConvertDataTableToObservableBindingList<T>(DataTable dt) where T : Entity, new()
        {
            var list = new ObservableBindingList<T>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(GetItem<T>(row));
            }
            return list;
        }

        internal static List<T> ConvertDataTable<T>(DataTable dt)
        {
            var data = new List<T>();
            foreach (DataRow row in dt.Rows)
            {
                data.Add(GetItem<T>(row));
            }
            return data;
        }

        internal static T GetItem<T>(DataRow dr)
        {
            var type = typeof(T);
            var obj = Activator.CreateInstance<T>();
            foreach (DataColumn column in dr.Table.Columns)
            {
                var prop = type.GetProperty(column.ColumnName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop == null) continue;
                object value = dr[column];
                if (value == DBNull.Value)
                {
                    value = prop.PropertyType.IsValueType ? Activator.CreateInstance(prop.PropertyType) : null;
                }
                else if (prop.PropertyType == typeof(char) && value is string s && s.Length == 1)
                {
                    value = s[0];
                }
                else if (prop.PropertyType.IsEnum && value is string es)
                {
                    value = Enum.Parse(prop.PropertyType, es);
                }
                else if (IsNumericType(prop.PropertyType))
                {
                    try { value = Convert.ChangeType(value, prop.PropertyType); }
                    catch { value = Activator.CreateInstance(prop.PropertyType); }
                }
                prop.SetValue(obj, value);
            }
            return obj;
        }

        internal static bool IsNumericType(Type type) =>
            type == typeof(byte) || type == typeof(sbyte) ||
            type == typeof(short) || type == typeof(ushort) ||
            type == typeof(int) || type == typeof(uint) ||
            type == typeof(long) || type == typeof(ulong) ||
            type == typeof(float) || type == typeof(double) ||
            type == typeof(decimal);

        internal static ObservableCollection<T> ConvertToObservableCollection<T>(List<T> list) =>
            new ObservableCollection<T>(list);

        internal static DataTable CreateDataTableFromFile(string filepath)
        {
            var dt = new DataTable();
            using var sr = new StreamReader(filepath);
            string headerLine = sr.ReadLine();
            if (headerLine == null) return dt;
            var headers = headerLine.Split(',');
            foreach (var h in headers) dt.Columns.Add(h);
            while (!sr.EndOfStream)
            {
                var vals = sr.ReadLine()?.Split(',');
                if (vals == null) continue;
                var r = dt.NewRow();
                for (int i = 0; i < headers.Length; i++) r[i] = vals.Length > i ? vals[i] : null;
                dt.Rows.Add(r);
            }
            return dt;
        }

        internal static DataTable CreateDataTableFromListofStrings(List<string> lines)
        {
            var dt = new DataTable();
            if (lines == null || lines.Count == 0) return dt;
            var headers = lines[0].Split(',');
            foreach (var h in headers) dt.Columns.Add(h);
            for (int i = 1; i < lines.Count; i++)
            {
                var parts = lines[i].Split(',');
                var r = dt.NewRow();
                for (int c = 0; c < headers.Length; c++)
                    r[c] = c < parts.Length ? parts[c] : null;
                dt.Rows.Add(r);
            }
            return dt;
        }

        internal static DataTable ToDataTable(IList list, Type tp)
        {
            var props = TypeDescriptor.GetProperties(tp);
            var table = new DataTable();
            foreach (PropertyDescriptor prop in props)
            {
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }
            var values = new object[props.Count];
            foreach (var item in list)
            {
                for (int i = 0; i < values.Length; i++)
                    values[i] = props[i].GetValue(item) ?? DBNull.Value;
                table.Rows.Add(values);
            }
            return table;
        }

        internal static DataTable ToDataTable(Type tp)
        {
            var table = new DataTable();
            var props = TypeDescriptor.GetProperties(tp);
            foreach (PropertyDescriptor prop in props)
            {
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }
            return table;
        }

        internal static bool ToCsvFromList(IList list, string filepath)
        {
            if (list == null || list.Count == 0) return false;
            using var sw = new StreamWriter(filepath);
            var first = list[0];
            var props = TypeDescriptor.GetProperties(first);
            var header = string.Join(",", props.Cast<PropertyDescriptor>().Select(p => p.Name));
            sw.WriteLine(header);
            foreach (var item in list)
            {
                var row = string.Join(",", props.Cast<PropertyDescriptor>().Select(p => (p.GetValue(item) ?? "").ToString()));
                sw.WriteLine(row);
            }
            return true;
        }

        internal static bool ToCsvFromDataTable(DataTable dt, string filepath)
        {
            if (dt == null || dt.Rows.Count == 0) return false;
            using var sw = new StreamWriter(filepath);
            sw.WriteLine(string.Join(",", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName)));
            foreach (DataRow row in dt.Rows)
            {
                var line = string.Join(",", dt.Columns.Cast<DataColumn>().Select(c => (row[c] ?? "").ToString()));
                sw.WriteLine(line);
            }
            return true;
        }

        internal static DataTable CreateDataTableVer1(object[] array)
        {
            var props = array.GetType().GetElementType()?.GetProperties() ?? Array.Empty<PropertyInfo>();
            var dt = CreateDataTable(props);
            foreach (var o in array) FillData(props, dt, o);
            return dt;
        }

        internal static DataTable CreateDataTableVer2(object[] arr)
        {
            var xs = new System.Xml.Serialization.XmlSerializer(arr.GetType());
            using var sw = new StringWriter();
            xs.Serialize(sw, arr);
            var ds = new DataSet();
            using var reader = new StringReader(sw.ToString());
            ds.ReadXml(reader);
            return ds.Tables.Count > 0 ? ds.Tables[0] : new DataTable();
        }

        private static DataTable CreateDataTable(PropertyInfo[] props)
        {
            var dt = new DataTable();
            foreach (var p in props)
            {
                dt.Columns.Add(new DataColumn(p.Name, p.PropertyType));
            }
            return dt;
        }

        private static void FillData(PropertyInfo[] props, DataTable dt, object o)
        {
            var r = dt.NewRow();
            foreach (var p in props)
            {
                r[p.Name] = p.GetValue(o) ?? DBNull.Value;
            }
            dt.Rows.Add(r);
        }

        internal static T CreateItemFromRow<T>(DataRow row) where T : new()
        {
            var item = new T();
            SetItemFromRow(item, row);
            return item;
        }

        internal static void SetItemFromRow<T>(T item, DataRow row) where T : new()
        {
            foreach (DataColumn c in row.Table.Columns)
            {
                var prop = item!.GetType().GetProperty(c.ColumnName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null && row[c] != DBNull.Value)
                {
                    prop.SetValue(item, Convert.ChangeType(row[c], prop.PropertyType));
                }
            }
        }

        internal static List<object> ConvertTableToList(DataTable dt, EntityStructure ent, Type entType)
        {
            var list = new List<object>();
            foreach (DataRow row in dt.Rows)
            {
                var instance = Activator.CreateInstance(entType);
                foreach (var field in ent.Fields)
                {
                    var pi = entType.GetProperty(field.fieldname, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (pi == null) continue;
                    object value = row[field.fieldname];
                    if (value == DBNull.Value)
                    {
                        value = pi.PropertyType.IsValueType ? Activator.CreateInstance(pi.PropertyType) : null;
                    }
                    else
                    {
                        try { value = Convert.ChangeType(value, pi.PropertyType); }
                        catch { value = pi.PropertyType.IsValueType ? Activator.CreateInstance(pi.PropertyType) : null; }
                    }
                    pi.SetValue(instance, value);
                }
                list.Add(instance);
            }
            if (dt.Rows.Count == 0)
            {
                list.Add(Activator.CreateInstance(entType));
            }
            return list;
        }
    }
}