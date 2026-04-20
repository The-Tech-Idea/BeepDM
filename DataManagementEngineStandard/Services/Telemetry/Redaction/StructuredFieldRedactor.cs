using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Services.Telemetry.Redaction
{
    /// <summary>
    /// Redacts entries in <see cref="TelemetryEnvelope.Properties"/> by key.
    /// Operates on the structured property bag instead of the message body,
    /// which is the typical pathway for application-supplied PII (e.g.
    /// <c>"Email"</c>, <c>"Phone"</c>, <c>"Ssn"</c>).
    /// </summary>
    /// <remarks>
    /// <see cref="RedactionMode.Drop"/> removes the key entirely. The other
    /// modes coerce the existing value via <see cref="object.ToString"/>
    /// before transforming.
    /// </remarks>
    public sealed class StructuredFieldRedactor : IRedactor
    {
        private readonly HashSet<string> _keys;
        private readonly RedactionContext _context;
        private readonly string _maskPrefix;

        /// <summary>
        /// Creates a redactor for the supplied property keys. Key match is
        /// case-insensitive (Ordinal).
        /// </summary>
        public StructuredFieldRedactor(IEnumerable<string> keys,
            RedactionContext context = null,
            string maskPrefix = null,
            string name = "structured-field")
        {
            if (keys is null)
            {
                throw new ArgumentNullException(nameof(keys));
            }
            _keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in keys)
            {
                if (!string.IsNullOrEmpty(key))
                {
                    _keys.Add(key);
                }
            }
            Name = name;
            _context = context ?? new RedactionContext();
            _maskPrefix = maskPrefix;
        }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public void Redact(TelemetryEnvelope envelope)
        {
            if (_keys.Count == 0 || envelope?.Properties is null || envelope.Properties.Count == 0)
            {
                return;
            }

            List<string> matchedKeys = null;
            foreach (var pair in envelope.Properties)
            {
                if (_keys.Contains(pair.Key))
                {
                    if (matchedKeys is null)
                    {
                        matchedKeys = new List<string>();
                    }
                    matchedKeys.Add(pair.Key);
                }
            }
            if (matchedKeys is null)
            {
                return;
            }

            for (int i = 0; i < matchedKeys.Count; i++)
            {
                string key = matchedKeys[i];
                object current = envelope.Properties[key];
                string original = current?.ToString() ?? string.Empty;
                string transformed = RedactionHelpers.Transform(original, _context, _maskPrefix);
                if (transformed is null)
                {
                    envelope.Properties.Remove(key);
                }
                else
                {
                    envelope.Properties[key] = transformed;
                }
            }
        }
    }
}
