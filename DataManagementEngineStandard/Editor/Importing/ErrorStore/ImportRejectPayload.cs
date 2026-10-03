using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using TheTechIdea.Beep.Services.Persistence;

namespace TheTechIdea.Beep.Editor.Importing.ErrorStore
{
    internal static class ImportRejectPayload
    {
        private static readonly JsonSerializerOptions Options = new() { MaxDepth = 64 };

        internal static string Capture(object record)
        {
            try
            {
                IEnumerable<KeyValuePair<string, object>> fields = record switch
                {
                    IDictionary<string, object> dictionary => dictionary,
                    DataRow row => row.Table.Columns.Cast<DataColumn>().Select(column => new KeyValuePair<string, object>(column.ColumnName,
                        row[column] == DBNull.Value ? null : row[column])),
                    null => throw Invalid(),
                    _ => record.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property =>
                        property.GetMethod?.IsPublic == true && property.GetIndexParameters().Length == 0
                        ? new KeyValuePair<string, object>(property.Name, property.GetValue(record)) : throw Invalid())
                };
                var owned = fields.Take(1025).ToArray();
                if (owned.Length == 0 || owned.Length > 1024 || owned.Any(field => string.IsNullOrWhiteSpace(field.Key) || field.Key.Length > 1024) ||
                    owned.Select(field => field.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() != owned.Length) throw Invalid();
                int nodes = 0, budget = 0;
                foreach (var field in owned) { Charge(field.Key.Length * 6, ref budget); Bound(field.Value, 0, ref nodes, ref budget); }
                var encoded = JsonSerializer.Serialize(TypedCursorCodec.EncodeFields(owned, 0), Options);
                if (encoded.Length > 1048576) throw Invalid();
                return encoded;
            }
            catch (Exception) { throw Invalid(); }
        }

        private static void Charge(int size, ref int budget)
        {
            if (size < 0 || size > 1048576 - budget) throw Invalid();
            budget += size;
        }

        private static void Bound(object value, int depth, ref int nodes, ref int budget)
        {
            if (depth > 24 || ++nodes > 10000 || value is string text && text.Length > 1048576 ||
                value is byte[] bytes && bytes.Length > 524288) throw Invalid();
            Charge(128, ref budget);
            if (value is string scalar) Charge(scalar.Length * 6, ref budget);
            else if (value is byte[] binary) Charge((binary.Length + 2) / 3 * 4, ref budget);
            if (value is IDictionary<string, object> fields)
                foreach (var field in fields) BoundField(field, depth, ref nodes, ref budget);
            else if (value is IReadOnlyDictionary<string, object> readOnly)
                foreach (var field in readOnly) BoundField(field, depth, ref nodes, ref budget);
            else if (value is object[] items)
                foreach (var item in items) Bound(item, depth + 1, ref nodes, ref budget);
        }

        private static void BoundField(KeyValuePair<string, object> field, int depth, ref int nodes, ref int budget)
        {
            if (field.Key == null || field.Key.Length > 1024) throw Invalid();
            Charge(field.Key.Length * 6, ref budget);
            Bound(field.Value, depth + 1, ref nodes, ref budget);
        }

        internal static Dictionary<string, object> Decode(string payload)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(payload) || payload.Length > 1048576) throw Invalid();
                using var json = JsonDocument.Parse(payload, new JsonDocumentOptions { MaxDepth = 64 });
                CheckDuplicates(json.RootElement);
                ValidateValue(json.RootElement);
                var value = TypedCursorCodec.Decode(JsonSerializer.Deserialize<TypedCursorValue>(payload, Options), 0);
                if (value is not Dictionary<string, object> fields || fields.Count == 0 || fields.Count > 1024 ||
                    fields.Keys.Any(key => string.IsNullOrWhiteSpace(key) || key.Length > 1024) || fields.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != fields.Count) throw Invalid();
                int nodes = 0, budget = 0;
                foreach (var field in fields) { Charge(field.Key.Length * 6, ref budget); Bound(field.Value, 0, ref nodes, ref budget); }
                return fields;
            }
            catch (Exception) { throw Invalid(); }
        }

        private static void ValidateValue(JsonElement value)
        {
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("Kind", out var kind) || kind.ValueKind != JsonValueKind.String)
                throw Invalid();
            foreach (var property in value.EnumerateObject())
                if (property.Name != "Kind" && property.Name != "Value" && property.Name != "ClockKind" && property.Name != "Fields" && property.Name != "Items")
                    throw Invalid();
            var tag = kind.GetString();
            bool Present(string name) => value.TryGetProperty(name, out var member) && member.ValueKind != JsonValueKind.Null;
            if (tag == "composite")
            {
                if (!value.TryGetProperty("Fields", out var fields) || fields.ValueKind != JsonValueKind.Object ||
                    Present("Value") || Present("Items") || Present("ClockKind")) throw Invalid();
                foreach (var field in fields.EnumerateObject()) ValidateValue(field.Value);
            }
            else if (tag == "array")
            {
                if (!value.TryGetProperty("Items", out var items) || items.ValueKind != JsonValueKind.Array ||
                    Present("Value") || Present("Fields") || Present("ClockKind")) throw Invalid();
                foreach (var item in items.EnumerateArray()) ValidateValue(item);
            }
            else
            {
                if (Present("Fields") || Present("Items") || tag != "datetime" && Present("ClockKind") ||
                    tag == "null" && Present("Value") || tag != "null" &&
                    (!value.TryGetProperty("Value", out var scalar) || scalar.ValueKind != JsonValueKind.String)) throw Invalid();
                // The closed codec rejects unknown tags and validates scalar values and timestamp kind.
            }
        }

        internal static void CheckDuplicates(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Object)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in value.EnumerateObject()) { if (!names.Add(property.Name)) throw Invalid(); CheckDuplicates(property.Value); }
            }
            else if (value.ValueKind == JsonValueKind.Array)
                foreach (var item in value.EnumerateArray()) CheckDuplicates(item);
        }

        private static InvalidDataException Invalid() => new("Reject destination payload is unsupported or malformed.");
    }
}
