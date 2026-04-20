using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Services.Telemetry.Redaction
{
    /// <summary>
    /// Redacts <c>key=value</c> pairs whose key matches an allowlist of
    /// sensitive names. Operates on the message body; structured properties
    /// are handled by <see cref="StructuredFieldRedactor"/>.
    /// </summary>
    /// <remarks>
    /// Recognises both unquoted (<c>token=abc</c>) and quoted
    /// (<c>token='abc def'</c>, <c>token="abc def"</c>) values. Stops at
    /// the next whitespace, semicolon or comma when unquoted.
    /// </remarks>
    public sealed class KeyValueRedactor : IRedactor
    {
        private readonly Regex _pattern;
        private readonly RedactionContext _context;

        /// <summary>
        /// Creates a key=value redactor for the supplied sensitive keys.
        /// Empty/null keys are filtered out.
        /// </summary>
        public KeyValueRedactor(IEnumerable<string> sensitiveKeys, RedactionContext context = null, string name = "kv")
        {
            if (sensitiveKeys is null)
            {
                throw new ArgumentNullException(nameof(sensitiveKeys));
            }

            var escaped = new List<string>();
            foreach (var key in sensitiveKeys)
            {
                if (!string.IsNullOrEmpty(key))
                {
                    escaped.Add(Regex.Escape(key));
                }
            }

            Name = name;
            _context = context ?? new RedactionContext();
            _pattern = escaped.Count == 0
                ? null
                : new Regex(
                    @"(?<key>" + string.Join("|", escaped) + @")\s*=\s*(?<val>'[^']*'|""[^""]*""|[^\s;,]+)",
                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public void Redact(TelemetryEnvelope envelope)
        {
            if (_pattern is null || envelope is null || string.IsNullOrEmpty(envelope.Message))
            {
                return;
            }
            envelope.Message = _pattern.Replace(envelope.Message, ReplaceMatch);
        }

        private string ReplaceMatch(Match match)
        {
            string key = match.Groups["key"].Value;
            string original = match.Groups["val"].Value;
            string transformed = RedactionHelpers.Transform(original, _context) ?? string.Empty;
            return string.Concat(key, "=", transformed);
        }
    }
}
