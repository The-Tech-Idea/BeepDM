using System;
using System.Globalization;

namespace TheTechIdea.Beep.Distributed.Partitioning
{
    /// <summary>
    /// Best-effort typed coercion / comparison helper used by the
    /// partition functions. Allows a string <c>"42"</c> stored in a
    /// list-partition map to match an int <c>42</c> coming from a row,
    /// and vice versa for range boundaries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Coercion is intentionally conservative: failure returns the
    /// original value rather than throwing, so a partition function
    /// can fall back to <see cref="object.Equals(object,object)"/>
    /// or <see cref="StringComparer.OrdinalIgnoreCase"/> on the
    /// stringified form. This keeps the row-routing path resilient
    /// to schema drift.
    /// </para>
    /// <para>
    /// All comparisons use the invariant culture so partition results
    /// are deterministic regardless of the calling thread's locale.
    /// </para>
    /// </remarks>
    public static class PartitionKeyCoercer
    {
        /// <summary>
        /// Returns a deterministic string representation of
        /// <paramref name="value"/> suitable for hashing. <c>null</c>
        /// is rendered as empty so it hashes consistently.
        /// </summary>
        public static string Stringify(object value)
        {
            switch (value)
            {
                case null:               return string.Empty;
                case string s:           return s;
                case bool b:             return b ? "true" : "false";
                case DateTime dt:        return dt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
                case DateTimeOffset dto: return dto.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
                case Guid g:             return g.ToString("N");
                case IFormattable f:     return f.ToString(null, CultureInfo.InvariantCulture);
                default:                 return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            }
        }

        /// <summary>
        /// Compares two partition-key values. Returns 0 when the
        /// values are equivalent under coercion, a negative number
        /// when <paramref name="left"/> precedes <paramref name="right"/>,
        /// and a positive number otherwise. Never throws.
        /// </summary>
        public static int Compare(object left, object right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left  == null) return right == null ? 0 : -1;
            if (right == null) return 1;

            // Fast path: identical runtime types and IComparable.
            if (left.GetType() == right.GetType() && left is IComparable cmpSame)
                return cmpSame.CompareTo(right);

            // Try numeric coercion first (covers int vs long vs decimal vs string).
            if (TryToDecimal(left, out var ld) && TryToDecimal(right, out var rd))
                return ld.CompareTo(rd);

            // DateTime / DateTimeOffset coercion.
            if (TryToDateTime(left, out var lt) && TryToDateTime(right, out var rt))
                return lt.CompareTo(rt);

            // Guid coercion.
            if (TryToGuid(left, out var lg) && TryToGuid(right, out var rg))
                return lg.CompareTo(rg);

            // Last resort: ordinal string compare on the stringified form.
            return string.Compare(
                Stringify(left),
                Stringify(right),
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Equality wrapper around <see cref="Compare"/>.</summary>
        public static bool AreEqual(object left, object right)
            => Compare(left, right) == 0;

        // ── Coercion helpers ─────────────────────────────────────────────

        private static bool TryToDecimal(object value, out decimal result)
        {
            switch (value)
            {
                case decimal d: result = d;          return true;
                case double dd: result = (decimal)dd; return true;
                case float f:   result = (decimal)f;  return true;
                case long l:    result = l;          return true;
                case ulong ul:  result = ul;         return true;
                case int i:     result = i;          return true;
                case uint ui:   result = ui;         return true;
                case short sh:  result = sh;         return true;
                case ushort us: result = us;         return true;
                case byte by:   result = by;         return true;
                case sbyte sb:  result = sb;         return true;
                case string s when decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed):
                    result = parsed;
                    return true;
                default:
                    result = 0m;
                    return false;
            }
        }

        private static bool TryToDateTime(object value, out DateTime result)
        {
            switch (value)
            {
                case DateTime dt:        result = dt.ToUniversalTime(); return true;
                case DateTimeOffset dto: result = dto.UtcDateTime;       return true;
                case string s when DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed):
                    result = parsed;
                    return true;
                default:
                    result = default;
                    return false;
            }
        }

        private static bool TryToGuid(object value, out Guid result)
        {
            switch (value)
            {
                case Guid g: result = g; return true;
                case string s when Guid.TryParse(s, out var parsed):
                    result = parsed;
                    return true;
                default:
                    result = Guid.Empty;
                    return false;
            }
        }
    }
}
