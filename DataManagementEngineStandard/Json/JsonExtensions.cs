using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Json
{
    public static class JsonExtensions
    {
        public static string DetermineFieldType(JToken token)
        {
            // Example implementation - adjust based on your needs
            switch (token.Type)
            {
                case JTokenType.Integer:
                    return "System.Int32";
                case JTokenType.Float:
                    return "System.Single";
                case JTokenType.String:
                    return "System.String";
                case JTokenType.Boolean:
                    return "System.Boolean";
                case JTokenType.Date:
                    return "System.DateTime";
                case JTokenType.Guid:
                    return "System.Guid";
                case JTokenType.Uri:
                    return "System.Uri";
                case JTokenType.TimeSpan:
                    return "System.TimeSpan";
                case JTokenType.Bytes:
                    return "System.Byte[]";
                case JTokenType.Null:
                case JTokenType.Undefined:
                    return "System.String";
                case JTokenType.Object:
                    if (token["$oid"] != null)
                    {
                        return "System.String"; // or the type that represents ObjectId
                    }
                    return "System.String"; // Adjust if you need different handling for objects
                case JTokenType.Array:
                    return "System.Array";  // Nested array, may need special handling
                                            // Add more mappings as needed for your application
                default:
                    return "System.String";  // Fallback type, adjusted from "unknown"
            }
        }

        public static bool MatchesCriteria(this JToken item, object criteria)
        {
            JObject criteriaObject = JObject.FromObject(criteria);

            foreach (var property in criteriaObject.Properties())
            {
                var itemProperty = item[property.Name];

                if (itemProperty == null || !itemProperty.ToString().Equals(property.Value.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
        public static string MapJsonTypeToDotNetType(string jsonType)
        {
            switch (jsonType.ToLower())
            {
                case "string":
                    return typeof(string).FullName;
                case "number":
                    return typeof(double).FullName; // or Decimal or Float based on your requirement
                case "integer":
                    return typeof(int).FullName; // or Int64 based on your requirement
                case "boolean":
                    return typeof(bool).FullName;
                case "object":
                case "array":
                    return typeof(string).FullName;
                case "null":
                    return typeof(void).FullName;
                case "datetime":
                    return typeof(DateTime).FullName;
                case "guid":
                    return typeof(Guid).FullName;
                default:
                    throw new ArgumentException($"Invalid JSON type: {jsonType}");
            }
        }
        public static string MapJsonTypeToDotNetType(JTokenType tokenType)
        {
            switch (tokenType)
            {
                case JTokenType.String:
                    return typeof(string).FullName;
                case JTokenType.Integer:
                    // Check if it fits into an int, otherwise use long
                    return typeof(int).FullName;
                case JTokenType.Float:
                    return typeof(double).FullName;
                case JTokenType.Boolean:
                    return typeof(bool).FullName;
                case JTokenType.Date:
                    return typeof(DateTime).FullName;
                case JTokenType.Guid:
                    return typeof(Guid).FullName;
                case JTokenType.Array:
                case JTokenType.Object:
                    // Since Array and Object types are being serialized to strings
                    // they are mapped to string type in .NET
                    return typeof(string).FullName;
                case JTokenType.Null:
                case JTokenType.Undefined:
                    return typeof(DBNull).FullName;
                default:
                    throw new ArgumentException($"Invalid JSON token type: {tokenType}");
            }
        }
        public static System.Data.DataView ApplyAppFilters(DataTable records, List<AppFilter> filters)
        {
            System.Data.DataView filteredRecords = new System.Data.DataView(records);

            // Apply each filter from the list
            foreach (AppFilter filter in filters)
            {
                string filterExpression = GenerateFilterExpression(filter);
                filteredRecords.RowFilter += (filteredRecords.RowFilter.Length > 0 ? " AND " : "") + filterExpression;
            }

            return filteredRecords;
        }
        public static IEnumerable<DataRow> ApplyAppFiltersNoDataView(DataTable records, List<AppFilter> filters)
        {
            IEnumerable<DataRow> filteredRows = records.AsEnumerable();

            // Apply each filter from the list
            foreach (AppFilter filter in filters)
            {
                filteredRows = filteredRows.Where(row => GenerateFilterExpression(row, filter));
            }

            return filteredRows;
        }
        public static string GenerateFilterExpression(AppFilter filter)
        {
            switch (filter.Operator)
            {
                case "equals":
                case "=":
                    return $"{filter.FieldName} = '{filter.FilterValue}'";
                case "contains":
                    return $"{filter.FieldName} LIKE '%{filter.FilterValue}%'";
                case ">":
                    return $"{filter.FieldName} > '{filter.FilterValue}'";
                case "<":
                    return $"{filter.FieldName} < '{filter.FilterValue}'";
                case ">=":
                    return $"{filter.FieldName} >= '{filter.FilterValue}'";
                case "<=":
                    return $"{filter.FieldName} <= '{filter.FilterValue}'";
                case "<>":
                case "!=":
                    return $"{filter.FieldName} <> '{filter.FilterValue}'";
                case "between":
                    return $"{filter.FieldName} >= '{filter.FilterValue}' AND {filter.FieldName} <= '{filter.FilterValue1}'";
                default:
                    throw new ArgumentException($"Invalid filter operator: {filter.Operator}");
            }
        }
        public static bool GenerateFilterExpression(DataRow record, AppFilter filter)
        {
            var fieldValue = record[filter.FieldName];

            switch (filter.Operator)
            {
                case "equals":
                case "=":
                    return fieldValue.Equals(filter.FilterValue);
                case "contains":
                    return fieldValue.ToString().Contains(filter.FilterValue);
                case ">":
                    return Comparer.Default.Compare(fieldValue, filter.FilterValue) > 0;
                case "<":
                    return Comparer.Default.Compare(fieldValue, filter.FilterValue) < 0;
                case ">=":
                    return Comparer.Default.Compare(fieldValue, filter.FilterValue) >= 0;
                case "<=":
                    return Comparer.Default.Compare(fieldValue, filter.FilterValue) <= 0;
                case "<>":
                case "!=":
                    return !fieldValue.Equals(filter.FilterValue);
                case "between":
                    var value1 = filter.FilterValue;
                    var value2 = filter.FilterValue1;

                    return Comparer.Default.Compare(fieldValue, value1) >= 0 &&
                           Comparer.Default.Compare(fieldValue, value2) <= 0;
                default:
                    throw new ArgumentException($"Invalid filter operator: {filter.Operator}");
            }
        }
        public static string ExtractEntityNameFromQuery(string query)
        {
            // Remove the '$' prefix if it's there
            var trimmedQuery = query.TrimStart('$');

            // Split the string by '.' and '[]', which are common JSONPath delimiters
            var segments = trimmedQuery.Split(new[] { '.', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);

            // Assuming the first segment after '$' is the entity name
            return segments.FirstOrDefault();
        }

    }
}


