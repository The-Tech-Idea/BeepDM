using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TheTechIdea.Beep.Editor.Schema
{
    /// <summary>
    /// Stable, comparable fingerprints for BeepDM schema artifacts.
    /// Used for change detection, plan-hash stability, mapping drift, and version promotion gates.
    ///
    /// <para>
    /// All fingerprints are SHA-256 over a deterministic, sorted, pipe-separated
    /// representation of the relevant fields. Two artifacts with the same
    /// fingerprint are considered identical for change-detection purposes.
    /// </para>
    /// </summary>
    public static class SchemaFingerprinter
    {
        /// <summary>
        /// Compute a stable SHA-256 hex fingerprint of a <see cref="DataSyncSchema"/>.
        /// Fingerprint = (SourceDs|DestDs|SourceEntity|DestEntity|Direction|Type)
        ///                + ordered MappedFields.
        /// </summary>
        /// <remarks>
        /// On any error the method returns a fresh <see cref="Guid"/> string,
        /// so callers always treat the result as "changed" rather than crashing.
        /// </remarks>
        public static string ComputeSchemaHash(DataSyncSchema schema)
        {
            if (schema == null) return Guid.NewGuid().ToString("N");

            try
            {
                var fp = new StringBuilder(256)
                    .Append(schema.SourceDataSourceName      ?? string.Empty).Append('|')
                    .Append(schema.DestinationDataSourceName ?? string.Empty).Append('|')
                    .Append(schema.SourceEntityName          ?? string.Empty).Append('|')
                    .Append(schema.DestinationEntityName     ?? string.Empty).Append('|')
                    .Append(schema.SyncDirection             ?? string.Empty).Append('|')
                    .Append(schema.SyncType                   ?? string.Empty);

                if (schema.MappedFields != null && schema.MappedFields.Count > 0)
                {
                    var fields = schema.MappedFields
                        .Where(f => f != null)
                        .Select(f => $"{(f.SourceField ?? string.Empty)}:{(f.DestinationField ?? string.Empty)}")
                        .OrderBy(s => s, StringComparer.Ordinal);

                    fp.Append('|').Append(string.Join(",", fields));
                }

                var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(fp.ToString()));
                return Convert.ToHexString(bytes).ToLowerInvariant();
            }
            catch
            {
                return Guid.NewGuid().ToString("N");
            }
        }

        /// <summary>
        /// Build a <see cref="TheTechIdea.Beep.Rules.RuleExecutionPolicy"/> from a
        /// policy-shape with sensible defaults. Shared by BeepSync's rule-evaluation
        /// sites and the schema manager's preflight, so they all share the same depth
        /// and timeout interpretation.
        /// </summary>
        /// <param name="maxDepth">Requested MaxDepth; falls back to <paramref name="defaultMaxDepth"/> when &lt;= 0.</param>
        /// <param name="maxExecutionMs">Requested MaxExecutionMs; falls back to <paramref name="defaultMaxMs"/> when &lt;= 0.</param>
        /// <param name="defaultMaxDepth">Default MaxDepth when caller did not specify one.</param>
        /// <param name="defaultMaxMs">Default MaxExecutionMs when caller did not specify one.</param>
        public static TheTechIdea.Beep.Rules.RuleExecutionPolicy BuildRulePolicy(
            int maxDepth, int maxExecutionMs,
            int defaultMaxDepth = 10, int defaultMaxMs = 5000) =>
            new TheTechIdea.Beep.Rules.RuleExecutionPolicy
            {
                MaxDepth       = maxDepth       > 0 ? maxDepth       : defaultMaxDepth,
                MaxExecutionMs = maxExecutionMs > 0 ? maxExecutionMs : defaultMaxMs
            };

        // ── Rule output parsing ────────────────────────────────────────────
        //
        // The rule engine returns (IDictionary<string, object>, object). Callers
        // typically pull a named key out of the dictionary and treat the result
        // as a string, bool, or a specific value. These helpers consolidate the
        // parsing pattern that otherwise shows up as:
        //
        //   var s = outputs?.TryGetValue("winner", out var w) == true ? w?.ToString() : fallback;
        //   var b = result is bool bb ? bb : result?.ToString() != "false";
        //
        // which is error-prone (different fallback defaults in every callsite).

        /// <summary>
        /// Read a string value from a rule output dictionary. Returns
        /// <paramref name="fallback"/> when the key is missing or the value is null.
        /// </summary>
        public static string ReadString(IReadOnlyDictionary<string, object>? outputs, string key, string fallback)
        {
            if (outputs == null || string.IsNullOrEmpty(key)) return fallback;
            return outputs.TryGetValue(key, out var v) && v != null ? v.ToString() ?? fallback : fallback;
        }

        /// <summary>
        /// Read a boolean value from a rule output dictionary. The value is
        /// considered <c>true</c> if it's a bool <c>true</c>, or if its string
        /// form is anything other than the literal <c>"false"</c>.
        /// </summary>
        public static bool ReadBoolean(IReadOnlyDictionary<string, object>? outputs, string key, bool fallback = false)
        {
            if (outputs == null || string.IsNullOrEmpty(key)) return fallback;
            if (!outputs.TryGetValue(key, out var v) || v == null) return fallback;
            return v is bool b ? b : v.ToString() != "false";
        }

        /// <summary>
        /// Read a boolean value from a rule return value (the second tuple
        /// element from <c>IRuleEngine.SolveRule</c>). Treats <c>null</c> as
        /// <paramref name="fallback"/>.
        /// </summary>
        public static bool ReadBoolean(object? result, bool fallback = false)
        {
            if (result == null) return fallback;
            return result is bool b ? b : result.ToString() != "false";
        }
    }
}
