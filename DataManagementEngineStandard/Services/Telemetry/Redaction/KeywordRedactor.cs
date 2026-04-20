using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Services.Telemetry.Redaction
{
    /// <summary>
    /// Redacts whole-word matches against a fixed keyword list. Useful for
    /// fixed secret tokens, internal product code names, or known passwords
    /// that should never appear in logs.
    /// </summary>
    /// <remarks>
    /// Matching is case-insensitive and word-boundary anchored to avoid
    /// over-matching (e.g. <c>"sun"</c> will not redact <c>"sunday"</c>).
    /// Patterns are compiled once and combined into a single alternation so
    /// runtime cost is roughly one regex per envelope regardless of keyword
    /// count.
    /// </remarks>
    public sealed class KeywordRedactor : IRedactor
    {
        private readonly Regex _pattern;
        private readonly RedactionContext _context;

        /// <summary>
        /// Creates a keyword redactor. Empty/null keywords are ignored. If
        /// every keyword filters out, the redactor becomes a no-op.
        /// </summary>
        public KeywordRedactor(IEnumerable<string> keywords, RedactionContext context = null, string name = "keyword")
        {
            if (keywords is null)
            {
                throw new ArgumentNullException(nameof(keywords));
            }

            var escaped = new List<string>();
            foreach (var keyword in keywords)
            {
                if (!string.IsNullOrEmpty(keyword))
                {
                    escaped.Add(Regex.Escape(keyword));
                }
            }

            Name = name;
            _context = context ?? new RedactionContext();
            _pattern = escaped.Count == 0
                ? null
                : new Regex(@"\b(?:" + string.Join("|", escaped) + @")\b",
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
            string transformed = RedactionHelpers.Transform(match.Value, _context);
            return transformed ?? string.Empty;
        }
    }
}
