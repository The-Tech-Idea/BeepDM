using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Exact cardinality of a declared filtered cohort, checked inside the transaction.</summary>
    public sealed class ConditionalRowSetGuard
    {
        /// <summary>Declared entity.</summary>
        public string EntityName { get; }
        /// <summary>Copied equality predicates.</summary>
        public IReadOnlyDictionary<string, object> Match { get; }
        /// <summary>Expected bounded cohort count.</summary>
        public int ExpectedCount { get; }
        /// <summary>Copied optional physical date/time intervals, combined with the equality predicates.</summary>
        public IReadOnlyList<ConditionalDateRange> DateRanges { get; }
        /// <summary>Optional exact ordinal case-insensitive string comparisons, enforced inside the transaction.</summary>
        public IReadOnlyList<OrdinalIgnoreCaseTextEquality> TextEqualities { get; }
        /// <summary>Copies a bounded cohort observation.</summary>
        public ConditionalRowSetGuard(string entityName, IReadOnlyDictionary<string, object> match, int expectedCount)
            : this(entityName, match, expectedCount, Array.Empty<ConditionalDateRange>()) { }
        /// <summary>Copies a cohort with at most four distinct declared date/time intervals.</summary>
        public ConditionalRowSetGuard(string entityName, IReadOnlyDictionary<string, object> match, int expectedCount,
            IEnumerable<ConditionalDateRange> dateRanges)
            : this(entityName, match, expectedCount, dateRanges, Array.Empty<OrdinalIgnoreCaseTextEquality>()) { }
        /// <summary>Copies up to four nonoverlapping declared string comparisons alongside the existing equality/date cohorts.</summary>
        public ConditionalRowSetGuard(string entityName, IReadOnlyDictionary<string, object> match, int expectedCount,
            IEnumerable<ConditionalDateRange> dateRanges, IEnumerable<OrdinalIgnoreCaseTextEquality> textEqualities)
        {
            if (string.IsNullOrWhiteSpace(entityName)) throw new ArgumentException("A declared cohort entity is required.");
            if (expectedCount < 0 || expectedCount > 128) throw new ArgumentOutOfRangeException(nameof(expectedCount));
            EntityName = entityName; Match = ConditionalMutationRequest.CopyValues(match); ExpectedCount = expectedCount;
            if (Match.Count == 0) throw new ArgumentException("Cohorts require explicit equality predicates.");
            var ranges = dateRanges?.ToArray() ?? throw new ArgumentNullException(nameof(dateRanges));
            if (ranges.Length > 4 || ranges.Any(range => range == null) ||
                ranges.Select(range => range.FieldName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != ranges.Length ||
                ranges.Any(range => Match.Keys.Contains(range.FieldName, StringComparer.OrdinalIgnoreCase)))
                throw new ArgumentException("Cohorts require at most four distinct date ranges without overlapping equality fields.");
            DateRanges = Array.AsReadOnly(ranges);
            var text = textEqualities?.ToArray() ?? throw new ArgumentNullException(nameof(textEqualities));
            if (text.Length > 4 || text.Any(item => item == null) || text.Select(item => item.FieldName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != text.Length ||
                text.Any(item => Match.Keys.Contains(item.FieldName, StringComparer.OrdinalIgnoreCase) || ranges.Any(range => range.FieldName.Equals(item.FieldName, StringComparison.OrdinalIgnoreCase))))
                throw new ArgumentException("Cohorts require at most four distinct nonoverlapping text columns.");
            TextEqualities = Array.AsReadOnly(text);
        }
    }
}
