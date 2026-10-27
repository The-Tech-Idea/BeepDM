using System;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>A bounded physical date/time interval, without time-zone conversion or clock semantics.</summary>
    public sealed class ConditionalDateRange
    {
        /// <summary>Declared DateTime metadata field.</summary>
        public string FieldName { get; }
        /// <summary>Physical lower endpoint (Unspecified kind).</summary>
        public DateTime From { get; }
        /// <summary>Physical upper endpoint (Unspecified kind).</summary>
        public DateTime Through { get; }
        /// <summary>Whether the lower endpoint belongs to the interval.</summary>
        public bool IncludeFrom { get; }
        /// <summary>Whether the upper endpoint belongs to the interval.</summary>
        public bool IncludeThrough { get; }

        /// <summary>Copies a nonempty physical interval. Callers must explicitly resolve instant/calendar meaning before capture.</summary>
        public ConditionalDateRange(string fieldName, DateTime from, DateTime through, bool includeFrom = true, bool includeThrough = true)
        {
            if (string.IsNullOrWhiteSpace(fieldName) || fieldName.Length > 128 || Array.Exists(fieldName.ToCharArray(), char.IsControl))
                throw new ArgumentException("A bounded declared date/time field is required.", nameof(fieldName));
            if (from.Kind != DateTimeKind.Unspecified || through.Kind != DateTimeKind.Unspecified)
                throw new ArgumentException("Date ranges require physical DateTime values with Unspecified kind.");
            if (from > through || from == through && (!includeFrom || !includeThrough))
                throw new ArgumentException("A nonempty ordered date/time interval is required.");
            FieldName = fieldName; From = from; Through = through; IncludeFrom = includeFrom; IncludeThrough = includeThrough;
        }
    }
}
