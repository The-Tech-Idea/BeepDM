using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using TheTechIdea.Beep.Pipelines.Models;

using MaskingStrategy = TheTechIdea.Beep.Pipelines.Models.MaskingStrategy;

namespace TheTechIdea.Beep.Pipelines.Observability
{
    /// <summary>
    /// Masks sensitive field values in dictionaries and strings based on
    /// <see cref="PipelineDefinition.SensitiveFields"/> and <see cref="PipelineDefinition.MaskingStrategy"/>.
    /// Used by observability components to redact PII before writing to logs or snapshots.
    /// </summary>
    public static class FieldRedactor
    {
        private const string RedactedPlaceholder = "***REDACTED***";

        /// <summary>
        /// Returns a copy of the dictionary with sensitive field values masked.
        /// Non-sensitive fields are returned as-is.
        /// </summary>
        public static Dictionary<string, object?> RedactFields(
            IReadOnlyDictionary<string, object?> record,
            IReadOnlyList<string> sensitiveFields,
            MaskingStrategy strategy)
        {
            if (sensitiveFields.Count == 0 || strategy == MaskingStrategy.None)
                return new Dictionary<string, object?>(record);

            var sensitiveSet = new HashSet<string>(sensitiveFields, StringComparer.OrdinalIgnoreCase);
            var result = new Dictionary<string, object?>(record.Count);

            foreach (var kv in record)
            {
                if (sensitiveSet.Contains(kv.Key))
                    result[kv.Key] = MaskValue(kv.Value, strategy);
                else
                    result[kv.Key] = kv.Value;
            }
            return result;
        }

        /// <summary>
        /// Masks a single value according to the specified strategy.
        /// </summary>
        public static object? MaskValue(object? value, MaskingStrategy strategy)
        {
            if (value == null) return null;

            string text = value.ToString() ?? string.Empty;
            return strategy switch
            {
                MaskingStrategy.Redact  => RedactedPlaceholder,
                MaskingStrategy.Hash    => HashValue(text),
                MaskingStrategy.Partial => PartialMask(text),
                _                       => value
            };
        }

        private static string HashValue(string text)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
            return "sha256:" + Convert.ToHexString(hash);
        }

        private static string PartialMask(string text)
        {
            if (text.Length <= 2) return RedactedPlaceholder;
            return string.Concat(text[0], new string('*', text.Length - 2), text[^1]);
        }
    }
}
