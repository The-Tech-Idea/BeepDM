using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Json.Helpers
{
    internal static class JsonFilterHelper
    {
        public static List<Func<JObject, bool>> CompileFilters(IEnumerable<AppFilter> filters, EntityStructure entity)
        {
            var list = new List<Func<JObject, bool>>();
            if (filters == null) return list;

            foreach (var f in filters.Where(v => v != null && !string.IsNullOrWhiteSpace(v.FieldName) && !string.IsNullOrWhiteSpace(v.Operator)))
            {
                var op = f.Operator.Trim().ToLowerInvariant();
                var field = f.FieldName;

                list.Add(obj =>
                {
                    if (!obj.TryGetValue(field, out var token) || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                    {
                        return op switch
                        {
                            "is null" or "isnull" => true,
                            "is not null" or "isnotnull" => false,
                            _ => false
                        };
                    }

                    string str = token.Type == JTokenType.Object && token["$oid"] != null
                        ? token["$oid"].ToString()
                        : token.Type == JTokenType.Date ? token.ToObject<DateTime>().ToString("o")
                        : token.ToString();

                    return Evaluate(op, str, f);
                });
            }
            return list;
        }

        private static bool Evaluate(string op, string value, AppFilter f)
        {
            string fv = f.FilterValue ?? "";
            switch (op)
            {
                case "=":
                case "equals": return string.Equals(value, fv, StringComparison.OrdinalIgnoreCase);
                case "!=":
                case "<>":
                case "notequals":
                case "not equals": return !string.Equals(value, fv, StringComparison.OrdinalIgnoreCase);
                case "contains": return value.IndexOf(fv, StringComparison.OrdinalIgnoreCase) >= 0;
                case "notcontains":
                case "not contains":
                case "!contains": return value.IndexOf(fv, StringComparison.OrdinalIgnoreCase) < 0;
                case "startswith":
                case "starts with": return value.StartsWith(fv, StringComparison.OrdinalIgnoreCase);
                case "endswith":
                case "ends with": return value.EndsWith(fv, StringComparison.OrdinalIgnoreCase);
                case "in":
                    return (f.FilterValue ?? "").Split(',').Select(s => s.Trim())
                        .Any(s => string.Equals(s, value, StringComparison.OrdinalIgnoreCase));
                case "notin":
                case "not in":
                    return !(f.FilterValue ?? "").Split(',').Select(s => s.Trim())
                        .Any(s => string.Equals(s, value, StringComparison.OrdinalIgnoreCase));
                case "regex":
                case "matches":
                    try { return Regex.IsMatch(value, f.FilterValue); } catch { return false; }
                case "between":
                    // Supports FilterValue + FilterValue1 OR single comma separated FilterValue
                    var v1 = f.FilterValue;
                    var v2 = f.FilterValue1;
                    if (string.IsNullOrWhiteSpace(v2) && v1?.Contains(',') == true)
                    {
                        var parts = v1.Split(',');
                        if (parts.Length == 2) { v1 = parts[0].Trim(); v2 = parts[1].Trim(); }
                    }
                    if (DateTime.TryParse(value, out var dtVal) && DateTime.TryParse(v1, out var dt1) && DateTime.TryParse(v2, out var dt2))
                    {
                        var min = dt1 < dt2 ? dt1 : dt2;
                        var max = dt1 > dt2 ? dt1 : dt2;
                        return dtVal >= min && dtVal <= max;
                    }
                    if (decimal.TryParse(value, out var numVal) && decimal.TryParse(v1, out var n1) && decimal.TryParse(v2, out var n2))
                    {
                        var min = Math.Min(n1, n2);
                        var max = Math.Max(n1, n2);
                        return numVal >= min && numVal <= max;
                    }
                    // lexical fallback
                    if (v1 != null && v2 != null)
                    {
                        var c1 = string.Compare(value, v1, StringComparison.OrdinalIgnoreCase);
                        var c2 = string.Compare(value, v2, StringComparison.OrdinalIgnoreCase);
                        return (c1 >= 0 && c2 <= 0) || (c2 >= 0 && c1 <= 0);
                    }
                    return false;
                default:
                    // numeric comparators
                    if (decimal.TryParse(value, out var vNum) && decimal.TryParse(fv, out var fNum))
                    {
                        return op switch
                        {
                            ">" => vNum > fNum,
                            ">=" or "=>" => vNum >= fNum,
                            "<" => vNum < fNum,
                            "<=" or "=<" => vNum <= fNum,
                            _ => true
                        };
                    }
                    if (DateTime.TryParse(value, out var vDt) && DateTime.TryParse(fv, out var fDt))
                    {
                        return op switch
                        {
                            ">" => vDt > fDt,
                            ">=" or "=>" => vDt >= fDt,
                            "<" => vDt < fDt,
                            "<=" or "=<" => vDt <= fDt,
                            _ => true
                        };
                    }
                    return true;
            }
        }
    }
}