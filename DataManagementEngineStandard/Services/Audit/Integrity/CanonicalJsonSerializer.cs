using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TheTechIdea.Beep.Services.Audit.Models;

namespace TheTechIdea.Beep.Services.Audit.Integrity
{
    /// <summary>
    /// Deterministic JSON serializer used as the input to the audit hash
    /// chain. Properties are emitted in fixed alphabetical order, dates
    /// use round-trip ISO-8601 format, enums use their string name, and
    /// dictionaries are sorted by key. The same logical event therefore
    /// always produces the same byte sequence — a non-negotiable
    /// requirement for hash-chain reproducibility across processes,
    /// platforms, and .NET versions.
    /// </summary>
    /// <remarks>
    /// We do not delegate to <see cref="System.Text.Json"/> because its
    /// property ordering depends on reflection metadata cache state and
    /// numeric formatting can differ subtly between runtimes. A small
    /// hand-written writer is the simplest way to keep the chain stable
    /// across .NET 8 / 9 / 10 and across operating systems.
    /// </remarks>
    public static class CanonicalJsonSerializer
    {
        /// <summary>
        /// Serializes <paramref name="auditEvent"/> for chain hashing,
        /// excluding the chain fields that the signer fills in
        /// (<see cref="AuditEvent.Sequence"/>, <see cref="AuditEvent.PrevHash"/>,
        /// <see cref="AuditEvent.Hash"/>).
        /// </summary>
        public static string SerializeForHash(AuditEvent auditEvent)
        {
            if (auditEvent is null)
            {
                throw new ArgumentNullException(nameof(auditEvent));
            }

            var sb = new StringBuilder(256);
            sb.Append('{');
            bool first = true;

            WriteEnum(sb, "category", auditEvent.Category, ref first);
            WriteString(sb, "chainId", auditEvent.ChainId, ref first);
            WriteString(sb, "correlationId", auditEvent.CorrelationId, ref first);
            WriteString(sb, "entityName", auditEvent.EntityName, ref first);
            WriteGuid(sb, "eventId", auditEvent.EventId, ref first);
            WriteFieldChanges(sb, "fieldChanges", auditEvent.FieldChanges, ref first);
            WriteString(sb, "operation", auditEvent.Operation, ref first);
            WriteEnum(sb, "outcome", auditEvent.Outcome, ref first);
            WriteProperties(sb, "properties", auditEvent.Properties, ref first);
            WriteString(sb, "reason", auditEvent.Reason, ref first);
            WriteString(sb, "recordKey", auditEvent.RecordKey, ref first);
            WriteString(sb, "source", auditEvent.Source, ref first);
            WriteString(sb, "tenant", auditEvent.Tenant, ref first);
            WriteDate(sb, "timestampUtc", auditEvent.TimestampUtc, ref first);
            WriteString(sb, "traceId", auditEvent.TraceId, ref first);
            WriteString(sb, "userId", auditEvent.UserId, ref first);
            WriteString(sb, "userName", auditEvent.UserName, ref first);

            sb.Append('}');
            return sb.ToString();
        }

        private static void WriteSeparatorIfNeeded(StringBuilder sb, ref bool first)
        {
            if (first)
            {
                first = false;
                return;
            }
            sb.Append(',');
        }

        private static void WriteString(StringBuilder sb, string key, string value, ref bool first)
        {
            WriteSeparatorIfNeeded(sb, ref first);
            sb.Append('"').Append(key).Append("\":");
            if (value is null)
            {
                sb.Append("null");
                return;
            }
            EscapeAndQuote(sb, value);
        }

        private static void WriteEnum<TEnum>(StringBuilder sb, string key, TEnum value, ref bool first)
            where TEnum : struct, Enum
        {
            WriteString(sb, key, value.ToString(), ref first);
        }

        private static void WriteGuid(StringBuilder sb, string key, Guid value, ref bool first)
        {
            WriteString(sb, key, value.ToString("D", CultureInfo.InvariantCulture), ref first);
        }

        private static void WriteDate(StringBuilder sb, string key, DateTime value, ref bool first)
        {
            WriteString(sb, key, value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture), ref first);
        }

        private static void WriteFieldChanges(StringBuilder sb, string key, IList<AuditFieldChange> changes, ref bool first)
        {
            WriteSeparatorIfNeeded(sb, ref first);
            sb.Append('"').Append(key).Append("\":");
            if (changes is null || changes.Count == 0)
            {
                sb.Append("[]");
                return;
            }
            sb.Append('[');
            for (int i = 0; i < changes.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }
                AuditFieldChange ch = changes[i];
                sb.Append("{\"field\":");
                EscapeAndQuoteOrNull(sb, ch?.Field);
                sb.Append(",\"newValue\":");
                AppendScalar(sb, ch?.NewValue);
                sb.Append(",\"oldValue\":");
                AppendScalar(sb, ch?.OldValue);
                sb.Append('}');
            }
            sb.Append(']');
        }

        private static void WriteProperties(StringBuilder sb, string key, IDictionary<string, object> props, ref bool first)
        {
            WriteSeparatorIfNeeded(sb, ref first);
            sb.Append('"').Append(key).Append("\":");
            if (props is null || props.Count == 0)
            {
                sb.Append("{}");
                return;
            }

            var sortedKeys = new List<string>(props.Keys);
            sortedKeys.Sort(StringComparer.Ordinal);

            sb.Append('{');
            for (int i = 0; i < sortedKeys.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }
                string k = sortedKeys[i];
                EscapeAndQuote(sb, k ?? string.Empty);
                sb.Append(':');
                AppendScalar(sb, props[k]);
            }
            sb.Append('}');
        }

        private static void AppendScalar(StringBuilder sb, object value)
        {
            switch (value)
            {
                case null:
                    sb.Append("null");
                    return;
                case string s:
                    EscapeAndQuote(sb, s);
                    return;
                case bool b:
                    sb.Append(b ? "true" : "false");
                    return;
                case Guid g:
                    EscapeAndQuote(sb, g.ToString("D", CultureInfo.InvariantCulture));
                    return;
                case DateTime dt:
                    EscapeAndQuote(sb, dt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
                    return;
                case DateTimeOffset dto:
                    EscapeAndQuote(sb, dto.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
                    return;
                case sbyte or byte or short or ushort or int or uint or long or ulong:
                    sb.Append(((IFormattable)value).ToString(null, CultureInfo.InvariantCulture));
                    return;
                case float f:
                    sb.Append(f.ToString("R", CultureInfo.InvariantCulture));
                    return;
                case double d:
                    sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
                    return;
                case decimal m:
                    sb.Append(m.ToString(CultureInfo.InvariantCulture));
                    return;
                case Enum e:
                    EscapeAndQuote(sb, e.ToString());
                    return;
                case IDictionary nested:
                    AppendDictionary(sb, nested);
                    return;
                case IEnumerable seq:
                    AppendSequence(sb, seq);
                    return;
                default:
                    EscapeAndQuote(sb, Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
                    return;
            }
        }

        private static void AppendDictionary(StringBuilder sb, IDictionary dict)
        {
            var keys = new List<string>(dict.Count);
            foreach (object k in dict.Keys)
            {
                keys.Add(k?.ToString() ?? string.Empty);
            }
            keys.Sort(StringComparer.Ordinal);

            sb.Append('{');
            for (int i = 0; i < keys.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }
                EscapeAndQuote(sb, keys[i]);
                sb.Append(':');
                AppendScalar(sb, dict[keys[i]]);
            }
            sb.Append('}');
        }

        private static void AppendSequence(StringBuilder sb, IEnumerable seq)
        {
            sb.Append('[');
            bool first = true;
            foreach (object item in seq)
            {
                if (!first)
                {
                    sb.Append(',');
                }
                first = false;
                AppendScalar(sb, item);
            }
            sb.Append(']');
        }

        private static void EscapeAndQuoteOrNull(StringBuilder sb, string value)
        {
            if (value is null)
            {
                sb.Append("null");
                return;
            }
            EscapeAndQuote(sb, value);
        }

        private static void EscapeAndQuote(StringBuilder sb, string value)
        {
            sb.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (c < 0x20)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
