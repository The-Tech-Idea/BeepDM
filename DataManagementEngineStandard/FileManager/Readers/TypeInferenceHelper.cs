using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.FileManager.Readers;

namespace TheTechIdea.Beep.FileManager.Readers
{
    // ── Phase 3: confidence-scored inference result ─────────────────────────

    /// <summary>
    /// Column-level inference result with quality statistics
    /// (confidence, null rate, uniqueness ratio).
    /// </summary>
    public sealed class FieldInferenceResult
    {
        /// <summary>Inferred CLR type full name (e.g. <c>System.Int64</c>).</summary>
        public string InferredType    { get; init; }
        /// <summary>0–1 confidence that <see cref="InferredType"/> is correct.</summary>
        public double Confidence      { get; init; }
        public int    SampleCount     { get; init; }
        public int    NullCount       { get; init; }
        public int    UniqueCount     { get; init; }
        /// <summary>Fraction of sampled values that were null/empty.</summary>
        public double NullRate        => SampleCount == 0 ? 0 : (double)NullCount / SampleCount;
        /// <summary>Fraction of sampled values that were distinct.</summary>
        public double UniquenessRatio => SampleCount == 0 ? 0 : (double)UniqueCount / SampleCount;
    }

    /// <summary>
    /// Shared type-inference helper used by all <see cref="IFileFormatReader"/> implementations.
    /// Widens the inferred column type as new raw values are sampled.
    ///
    /// Type hierarchy (narrowest → widest):
    ///   Boolean → Integer → Decimal → DateTime → String
    /// </summary>
    internal static class TypeInferenceHelper
    {
        private static readonly string[] _dateFormats =
        {
            "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy",
            "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ",
            "MM/dd/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm:ss"
        };

        /// <summary>
        /// Returns the widened type name that can hold both the current inferred
        /// type and the new <paramref name="rawValue"/>.
        /// </summary>
        public static string Widen(string current, string rawValue)
        {
            if (current == "System.String")
                return current; // already at max width

            string candidate = Classify(rawValue);

            if (current == null)
                return candidate;

            return Wider(current, candidate);
        }

        private static string Classify(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "System.String";

            if (bool.TryParse(raw, out _))
                return "System.Boolean";

            if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                return "System.Int64";

            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                return "System.Decimal";

            if (DateTime.TryParseExact(raw, _dateFormats, CultureInfo.InvariantCulture,
                                        DateTimeStyles.None, out _))
                return "System.DateTime";

            return "System.String";
        }

        private static string Wider(string a, string b)
        {
            if (a == b) return a;

            var order = new[]
            {
                "System.Boolean",
                "System.Int64",
                "System.Decimal",
                "System.DateTime",
                "System.String"
            };

            int ia = Array.IndexOf(order, a);
            int ib = Array.IndexOf(order, b);

            // Unknown type → fall back to string
            if (ia < 0) return b;
            if (ib < 0) return a;

            // DateTime and numeric are incompatible — widen to String
            int decimalIdx = Array.IndexOf(order, "System.Decimal");
            int dateIdx    = Array.IndexOf(order, "System.DateTime");
            if ((ia <= decimalIdx && ib == dateIdx) ||
                (ib <= decimalIdx && ia == dateIdx))
                return "System.String";

            return Math.Max(ia, ib) == ia ? a : b;
        }

        // ── Phase 3: confidence-scored column inference ──────────────────────

        /// <summary>
        /// Infers the best type for an entire column's sample values and returns
        /// quality statistics (confidence, null rate, uniqueness ratio).
        /// </summary>
        /// <param name="values">Raw string values sampled from the column (nulls/empties are allowed).</param>
        public static FieldInferenceResult InferWithStats(IEnumerable<string> values)
        {
            if (values == null)
                return new FieldInferenceResult { InferredType = "System.String", Confidence = 0 };

            string currentType  = null;
            int    sampleCount  = 0;
            int    nullCount    = 0;
            int    typeMisses   = 0;   // values that force widening to String
            var    unique       = new HashSet<string>(StringComparer.Ordinal);

            foreach (string val in values)
            {
                sampleCount++;
                if (string.IsNullOrEmpty(val))
                {
                    nullCount++;
                    continue;
                }

                unique.Add(val);
                string prev = currentType;
                currentType = Widen(currentType, val);

                // Track how many times we were forced all the way to String
                if (currentType == "System.String" && prev != "System.String" && prev != null)
                    typeMisses++;
            }

            string finalType  = currentType ?? "System.String";
            int    nonNulls   = sampleCount - nullCount;

            // Confidence: 1.0 when all non-null values are consistent with the type,
            // degraded proportionally by how many forced widening-to-String events occurred.
            double confidence = nonNulls == 0
                ? 1.0
                : Math.Max(0, 1.0 - (double)typeMisses / nonNulls);

            return new FieldInferenceResult
            {
                InferredType    = finalType,
                Confidence      = Math.Round(confidence, 4),
                SampleCount     = sampleCount,
                NullCount       = nullCount,
                UniqueCount     = unique.Count
            };
        }
    }
}
