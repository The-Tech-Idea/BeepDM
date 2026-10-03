using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Editor.UOW
{
    public partial class UnitofWork<T>
    {
        private static string PageSchemaSignature(EntityStructure entity)
        {
            var text = new StringBuilder();
            foreach (var field in entity?.Fields ?? new List<EntityField>())
            {
                text.Append(field.FieldName?.Length ?? -1).Append(':').Append(field.FieldName);
                text.Append(field.Fieldtype?.Length ?? -1).Append(':').Append(field.Fieldtype).Append(':').Append(field.IsKey);
            }
            return text.ToString();
        }

        private Dictionary<string, PropertyInfo> PageProperties()
        {
            var fields = EntityStructure?.Fields;
            if (fields == null || fields.Count == 0 || !fields.Any(field => field.IsKey))
                throw new NotSupportedException("Bounded pages require declared fields and a complete unique primary key.");
            var properties = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field.FieldName) || properties.ContainsKey(field.FieldName))
                    throw new NotSupportedException("Page fields must have unique nonempty names.");
                var property = typeof(T).GetProperty(field.FieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property?.CanWrite != true || property.CanRead != true || property.GetIndexParameters().Length != 0)
                    throw new NotSupportedException("Page fields must map to readable/writable record properties.");
                var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                if (!(type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) ||
                    type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
                    type == typeof(TimeSpan) || type == typeof(byte[])))
                    throw new NotSupportedException("Bounded pages support only scalar and base64 binary record fields.");
                properties.Add(field.FieldName, property);
            }
            return properties;
        }

        private BoundedPageRequest QualifyPageRequest(BoundedPageRequest request)
        {
            var properties = PageProperties();
            var order = new List<PageOrder>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in request.Order)
            {
                if (!properties.TryGetValue(column.FieldName, out var property) || !seen.Add(column.FieldName))
                    throw new ArgumentException("Page order requires unique declared column names; SQL fragments are not supported.");
                order.Add(new PageOrder(property.Name, column.Descending));
            }
            foreach (var key in EntityStructure.Fields.Where(field => field.IsKey))
                if (seen.Add(key.FieldName)) order.Add(new PageOrder(properties[key.FieldName].Name));
            return request.WithValues(order, request.CopyFilters());
        }

        private List<T> DecodePageResponse(BoundedPageRequest request, BoundedPageResponse response,
            CancellationToken token, out ProviderPageInfo info)
        {
            if (response == null || response.RequestId != request.RequestId || response.PageNumber != request.PageNumber ||
                response.PageSize != request.PageSize || response.TotalRecords < 0 || response.PayloadBytes > request.MaxPayloadBytes)
                throw new InvalidOperationException("Provider page envelope or payload bound is invalid.");
            var properties = PageProperties();
            _ = new UTF8Encoding(false, true).GetCharCount(response.Payload.Span);
            using var document = JsonDocument.Parse(response.Payload, new JsonDocumentOptions { MaxDepth = 3 });
            var array = document.RootElement;
            if (array.ValueKind != JsonValueKind.Array) throw new InvalidOperationException("Provider page must be a JSON row array.");
            var expected = request.Offset >= response.TotalRecords ? 0 : (int)Math.Min(request.PageSize, response.TotalRecords - request.Offset);
            if (array.GetArrayLength() != expected) throw new InvalidOperationException("Provider page rows do not match the bounded snapshot count.");
            var rows = new List<T>(expected);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in array.EnumerateArray())
            {
                token.ThrowIfCancellationRequested();
                if (row.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Provider page has a non-object row.");
                var record = new T();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var value in row.EnumerateObject())
                {
                    if (!properties.TryGetValue(value.Name, out var property) || !seen.Add(value.Name) ||
                        value.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                        throw new InvalidOperationException("Provider page has an unknown, duplicate or nonscalar field.");
                    if (value.Value.ValueKind == JsonValueKind.Null && property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) == null)
                        throw new InvalidOperationException("Provider page has a null nonnullable field.");
                    property.SetValue(record, value.Value.Deserialize(property.PropertyType));
                }
                if (seen.Count != properties.Count) throw new InvalidOperationException("Provider page omitted declared record fields.");
                var keyText = new StringBuilder();
                foreach (var key in EntityStructure.Fields.Where(field => field.IsKey))
                {
                    var value = properties[key.FieldName].GetValue(record);
                    if (value == null) throw new InvalidOperationException("Provider page has a null primary key.");
                    var encoded = JsonSerializer.Serialize(value, properties[key.FieldName].PropertyType);
                    keyText.Append(encoded.Length).Append(':').Append(encoded);
                }
                if (!keys.Add(keyText.ToString())) throw new InvalidOperationException("Provider page contains duplicate primary keys.");
                rows.Add(record);
            }
            info = new ProviderPageInfo(request, response.TotalRecords, rows.Count, response.PayloadBytes);
            return rows;
        }
    }
}
