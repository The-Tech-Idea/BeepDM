using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Security.Cryptography;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Optional native capability for the same ordinal case-insensitive text semantics in reads and transaction cohorts.</summary>
    public interface IOrdinalIgnoreCaseTextDataSource
    {
        /// <summary>Whether this provider enforces the declared text semantics rather than its default database collation.</summary>
        bool SupportsOrdinalIgnoreCaseText { get; }
    }

    /// <summary>One immutable, bounded, declared string-column comparison under the current runtime's OrdinalIgnoreCase policy.</summary>
    public sealed class OrdinalIgnoreCaseTextEquality
    {
        /// <summary>Declared column, resolved and quoted by the native metadata owner.</summary>
        public string FieldName { get; }
        /// <summary>Exact text, including whitespace; no trimming, linguistic or compatibility normalization is performed.</summary>
        public string Value { get; }
        /// <summary>Expanded comparison cost included in the physical batch budget.</summary>
        [JsonIgnore] public int ScalarCost { get; }
        internal IReadOnlyList<IReadOnlyList<string>> Groups { get; }

        private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<string>>> CaseGroups = new(BuildGroups);
        private static readonly Lazy<string> PolicyHash = new(() => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            "ordinal-scalar-v1|" + string.Join("|", CaseGroups.Value.Values.Select(group => string.Join(",", group.Select(text =>
                Convert.ToHexString(Encoding.Unicode.GetBytes(text))))).OrderBy(value => value, StringComparer.Ordinal))))));
        /// <summary>Fingerprint of the actual runtime case-equivalence table; persisted reviewed commands can fence a changed policy.</summary>
        public static string RuntimePolicyFingerprint => PolicyHash.Value;
        private static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildGroups()
        {
            var groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            for (var value = 0; value <= 0x10ffff; value++)
            {
                if (!Rune.IsValid(value)) continue;
                var rune = new Rune(value);
                if (Rune.ToUpperInvariant(rune) == rune && Rune.ToLowerInvariant(rune) == rune) continue;
                var text = rune.ToString();
                if (!groups.TryGetValue(text, out var members)) groups.Add(text, members = new());
                members.Add(text);
            }
            return groups.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<string>)Array.AsReadOnly(pair.Value.ToArray()), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Captures up to256 valid UTF-16 code units; null, embedded NUL and malformed surrogate sequences refuse.</summary>
        public OrdinalIgnoreCaseTextEquality(string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(fieldName) || fieldName.Length > 128 || fieldName != fieldName.Trim() || fieldName.Any(char.IsControl))
                throw new ArgumentException("A bounded declared text column is required.", nameof(fieldName));
            if (value == null || value.Length > 256 || value.Contains('\0'))
                throw new ArgumentException("A bounded non-null text comparison without NUL is required.", nameof(value));
            var remaining = value.AsSpan(); var groups = new List<IReadOnlyList<string>>();
            while (!remaining.IsEmpty)
            {
                if (Rune.DecodeFromUtf16(remaining, out var rune, out var consumed) != OperationStatus.Done)
                    throw new ArgumentException("Text comparisons require valid Unicode scalar sequences.", nameof(value));
                var text = rune.ToString();
                var members = CaseGroups.Value.TryGetValue(text, out var found) ? found : Array.AsReadOnly(new[] { text });
                if (members.Any(member => member.Length != consumed || !StringComparer.OrdinalIgnoreCase.Equals(member, text)))
                    throw new NotSupportedException("The runtime returned an incompatible ordinal casing group.");
                groups.Add(members); remaining = remaining[consumed..];
            }
            FieldName = fieldName; Value = value; Groups = Array.AsReadOnly(groups.ToArray());
            ScalarCost = 2 + groups.Sum(group => group.Sum(member => member.Length));
        }
    }
}
