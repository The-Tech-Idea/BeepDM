using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace TheTechIdea.Beep.Services.Persistence
{
    internal static class TypedCursorCodec
    {
        internal static TypedCursorValue Encode(object value, int depth)
        {
            if (depth > 32) throw new NotSupportedException("Watermark nesting exceeds the supported depth.");
            var culture = CultureInfo.InvariantCulture;
            return value switch
            {
                null => new TypedCursorValue { Kind = "null" },
                string text => Scalar("string", text),
                bool boolean => Scalar("bool", boolean.ToString()),
                byte number => Scalar("u8", number.ToString(culture)),
                sbyte number => Scalar("i8", number.ToString(culture)),
                short number => Scalar("i16", number.ToString(culture)),
                ushort number => Scalar("u16", number.ToString(culture)),
                int number => Scalar("i32", number.ToString(culture)),
                uint number => Scalar("u32", number.ToString(culture)),
                long number => Scalar("i64", number.ToString(culture)),
                ulong number => Scalar("u64", number.ToString(culture)),
                decimal number => Scalar("decimal", number.ToString(culture)),
                float number => Scalar("float", number.ToString("R", culture)),
                double number => Scalar("double", number.ToString("R", culture)),
                DateTime time => new TypedCursorValue { Kind = "datetime", Value = time.Ticks.ToString(culture), ClockKind = time.Kind.ToString() },
                DateTimeOffset time => Scalar("datetimeoffset", time.ToString("O", culture)),
                Guid id => Scalar("guid", id.ToString("D")),
                byte[] bytes => Scalar("bytes", Convert.ToBase64String(bytes)),
                IDictionary<string, object> fields => EncodeFields(fields, depth),
                IReadOnlyDictionary<string, object> fields => EncodeFields(fields, depth),
                object[] items => new TypedCursorValue { Kind = "array", Items = items.Select(item => Encode(item, depth + 1)).ToList() },
                _ => throw new NotSupportedException($"Watermark type '{value.GetType().FullName}' is not supported.")
            };
        }

        internal static TypedCursorValue EncodeFields(IEnumerable<KeyValuePair<string, object>> fields, int depth) =>
            new TypedCursorValue
            {
                Kind = "composite",
                Fields = fields.OrderBy(field => field.Key, StringComparer.Ordinal)
                    .ToDictionary(field => field.Key, field => Encode(field.Value, depth + 1), StringComparer.Ordinal)
            };

        internal static TypedCursorValue Scalar(string kind, string value) => new TypedCursorValue { Kind = kind, Value = value };

        internal static object Decode(TypedCursorValue value, int depth)
        {
            if (value == null || depth > 32) throw new InvalidDataException("Invalid watermark nesting.");
            var culture = CultureInfo.InvariantCulture;
            if (value.Kind != "null" && value.Kind != "composite" && value.Kind != "array" && value.Value == null)
                throw new InvalidDataException("Watermark scalar value is missing.");
            return value.Kind switch
            {
                "null" => null,
                "string" => value.Value,
                "bool" => bool.Parse(value.Value),
                "u8" => byte.Parse(value.Value, culture),
                "i8" => sbyte.Parse(value.Value, culture),
                "i16" => short.Parse(value.Value, culture),
                "u16" => ushort.Parse(value.Value, culture),
                "i32" => int.Parse(value.Value, culture),
                "u32" => uint.Parse(value.Value, culture),
                "i64" => long.Parse(value.Value, culture),
                "u64" => ulong.Parse(value.Value, culture),
                "decimal" => decimal.Parse(value.Value, culture),
                "float" => float.Parse(value.Value, NumberStyles.Float, culture),
                "double" => double.Parse(value.Value, NumberStyles.Float, culture),
                "datetime" => new DateTime(long.Parse(value.Value, culture), value.ClockKind switch
                {
                    "Utc" => DateTimeKind.Utc, "Local" => DateTimeKind.Local, "Unspecified" => DateTimeKind.Unspecified,
                    _ => throw new InvalidDataException("Invalid timestamp kind.")
                }),
                "datetimeoffset" => DateTimeOffset.ParseExact(value.Value, "O", culture),
                "guid" => Guid.ParseExact(value.Value, "D"),
                "bytes" => Convert.FromBase64String(value.Value),
                "composite" when value.Fields != null => value.Fields.ToDictionary(
                    field => field.Key, field => Decode(field.Value, depth + 1), StringComparer.Ordinal),
                "array" when value.Items != null => value.Items.Select(item => Decode(item, depth + 1)).ToArray(),
                _ => throw new InvalidDataException("Unsupported or malformed watermark value tag.")
            };
        }

    }

    internal sealed class TypedCursorValue
    {
        public string Kind { get; set; }
        public string Value { get; set; }
        public string ClockKind { get; set; }
        public Dictionary<string, TypedCursorValue> Fields { get; set; }
        public List<TypedCursorValue> Items { get; set; }
    }
}
