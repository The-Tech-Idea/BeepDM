using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
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
        public static string DetermineFieldtype(JToken token)
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
                    return fieldValue.ToString().Contains(filter.FilterValue.ToString());
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
        public static Dictionary<string, object> FlattenJson(JObject jObject, string parentKey = "")
        {
            var result = new Dictionary<string, object>();

            foreach (var property in jObject.Properties())
            {
                string key = string.IsNullOrEmpty(parentKey) ? property.Name : $"{parentKey}.{property.Name}";

                if (property.Value.Type == JTokenType.Object)
                {
                    var childObject = (JObject)property.Value;
                    foreach (var child in FlattenJson(childObject, key))
                    {
                        result.Add(child.Key, child.Value);
                    }
                }
                else if (property.Value.Type == JTokenType.Array)
                {
                    var array = property.Value as JArray;
                    for (int i = 0; i < array.Count; i++)
                    {
                        if (array[i] is JObject nestedObject)
                        {
                            foreach (var child in FlattenJson(nestedObject, $"{key}[{i}]"))
                            {
                                result.Add(child.Key, child.Value);
                            }
                        }
                        else
                        {
                            result.Add($"{key}[{i}]", array[i]);
                        }
                    }
                }
                else
                {
                    result.Add(key, property.Value);
                }
            }

            return result;
        }
        public static DataTable JsonToDataTable(string json)
        {
            var dataTable = new DataTable();
            try
            {
                var jsonArray = JArray.Parse(json);
                if (!jsonArray.Any()) return dataTable;

                foreach (JProperty column in ((JObject)jsonArray.First()).Properties())
                {
                    dataTable.Columns.Add(column.Name, typeof(string));
                }

                foreach (JObject row in jsonArray)
                {
                    var dataRow = dataTable.NewRow();
                    foreach (JProperty column in row.Properties())
                    {
                        dataRow[column.Name] = column.Value.ToString();
                    }
                    dataTable.Rows.Add(dataRow);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting JSON to DataTable: {ex.Message}");
            }
            return dataTable;
        }
        public static string DataTableToJson(DataTable table)
        {
            var jsonArray = new JArray();

            foreach (DataRow row in table.Rows)
            {
                var jsonObject = new JObject();
                foreach (DataColumn column in table.Columns)
                {
                    jsonObject[column.ColumnName] = row[column]?.ToString();
                }
                jsonArray.Add(jsonObject);
            }

            return jsonArray.ToString();
        }
        public static JObject MergeJsonObjects(JObject primary, JObject secondary)
        {
            var result = new JObject(primary);
            foreach (var property in secondary.Properties())
            {
                if (result.ContainsKey(property.Name))
                {
                    if (result[property.Name] is JObject && property.Value is JObject)
                    {
                        result[property.Name] = MergeJsonObjects((JObject)result[property.Name], (JObject)property.Value);
                    }
                }
                else
                {
                    result[property.Name] = property.Value;
                }
            }
            return result;
        }
        public static bool SearchValueInJson(JToken token, string searchValue)
        {
            if (token == null) return false;

            if (token.Type == JTokenType.Object)
            {
                return token.Children<JProperty>().Any(prop =>
                    SearchValueInJson(prop.Value, searchValue));
            }
            else if (token.Type == JTokenType.Array)
            {
                return token.Children().Any(child =>
                    SearchValueInJson(child, searchValue));
            }
            else
            {
                return token.ToString().Equals(searchValue, StringComparison.OrdinalIgnoreCase);
            }
        }
        public static string PrettyPrintJson(string json)
        {
            try
            {
                var parsedJson = JToken.Parse(json);
                return parsedJson.ToString(Newtonsoft.Json.Formatting.Indented);
            }
            catch (Exception ex)
            {
                return $"Invalid JSON: {ex.Message}";
            }
        }
        public static JObject ExtractSpecificKeys(JObject jsonObject, params string[] keys)
        {
            var result = new JObject();
            foreach (var key in keys)
            {
                if (jsonObject.ContainsKey(key))
                {
                    result[key] = jsonObject[key];
                }
            }
            return result;
        }
        public static Dictionary<string, JObject> JsonArrayToDictionary(JArray jsonArray, string keyProperty)
        {
            var result = new Dictionary<string, JObject>();

            foreach (JObject obj in jsonArray)
            {
                if (obj.ContainsKey(keyProperty))
                {
                    string key = obj[keyProperty].ToString();
                    result[key] = obj;
                }
            }

            return result;
        }

        public static string DetectJsonType(string json)
        {
            try
            {
                var token = JToken.Parse(json);
                if (token is JObject) return "Object";
                if (token is JArray) return "Array";
            }
            catch
            {
                return "Invalid";
            }
            return "Unknown";
        }

    }
}


