using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region "Export"
        /// <summary>
        /// Exports the current items to a DataTable. Mirror of the DataTable import constructor.
        /// </summary>
        /// <param name="tableName">Optional name for the DataTable. Defaults to the type name.</param>
        /// <returns>A DataTable containing all items and their property values.</returns>
        public DataTable ToDataTable(string tableName = null)
        {
            var dt = new DataTable(tableName ?? typeof(T).Name);
            var properties = GetCachedProperties();

            // Create columns
            foreach (var prop in properties)
            {
                var colType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                // DataTable doesn't support generic types - use string as fallback
                if (colType.IsGenericType || colType.IsArray || !colType.IsValueType && colType != typeof(string))
                    colType = typeof(string);
                dt.Columns.Add(prop.Name, colType);
            }

            // Populate rows
            foreach (var item in Items)
            {
                var row = dt.NewRow();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(item);
                    var colType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    if (colType.IsGenericType || colType.IsArray || !colType.IsValueType && colType != typeof(string))
                    {
                        // Convert complex types to string representation
                        row[prop.Name] = value?.ToString() ?? (object)DBNull.Value;
                    }
                    else
                    {
                        row[prop.Name] = value ?? DBNull.Value;
                    }
                }
                dt.Rows.Add(row);
            }

            return dt;
        }
        #endregion
    }
}
