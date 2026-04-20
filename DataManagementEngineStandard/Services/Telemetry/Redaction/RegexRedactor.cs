using System;
using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Services.Telemetry.Redaction
{
    /// <summary>
    /// Generic operator-supplied regex redactor. The matched substring is
    /// replaced according to the configured <see cref="RedactionContext"/>:
    /// the entire <c>Match.Value</c> becomes the mask token, hash digest, or
    /// is dropped depending on <see cref="RedactionMode"/>.
    /// </summary>
    /// <remarks>
    /// Use this for environment-specific patterns (internal account ids,
    /// custom token formats). Built-in shapes (email, JWT, etc.) ship as
    /// dedicated redactors so operators do not have to maintain their own
    /// regex copies.
    /// </remarks>
    public sealed class RegexRedactor : IRedactor
    {
        private readonly Regex _pattern;
        private readonly RedactionContext _context;
        private readonly string _maskPrefix;

        /// <summary>
        /// Creates a regex redactor. <paramref name="pattern"/> is compiled
        /// once and reused for the lifetime of the redactor.
        /// </summary>
        /// <param name="name">Stable name surfaced to diagnostics.</param>
        /// <param name="pattern">Regex pattern to scrub.</param>
        /// <param name="context">Redaction mode + replacement controls. Defaults applied when null.</param>
        /// <param name="maskPrefix">Optional <c>[REDACTED:&lt;prefix&gt;]</c> prefix for masked output.</param>
        public RegexRedactor(string name, string pattern, RedactionContext context = null, string maskPrefix = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name must be non-empty.", nameof(name));
            }
            if (string.IsNullOrEmpty(pattern))
            {
                throw new ArgumentException("Pattern must be non-empty.", nameof(pattern));
            }
            Name = name;
            _pattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            _context = context ?? new RedactionContext();
            _maskPrefix = maskPrefix;
        }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public void Redact(TelemetryEnvelope envelope)
        {
            if (envelope is null || string.IsNullOrEmpty(envelope.Message))
            {
                return;
            }
            envelope.Message = _pattern.Replace(envelope.Message, ReplaceMatch);
        }

        private string ReplaceMatch(Match match)
        {
            string transformed = RedactionHelpers.Transform(match.Value, _context, _maskPrefix);
            return transformed ?? string.Empty;
        }
    }
}
