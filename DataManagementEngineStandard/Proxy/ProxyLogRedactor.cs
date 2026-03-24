using System;
using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Proxy
{
    /// <summary>
    /// Scrubs PII patterns from strings before they reach the log.
    /// Used by <see cref="ProxyDataSource"/> via <c>LogSafe()</c> helpers.
    /// Disabled per-policy via <see cref="ProxyPolicy.EnableLogRedaction"/>.
    /// </summary>
    internal static class ProxyLogRedactor
    {
        // Compiled once at class-load time — zero allocation per call
        private static readonly Regex[] _patterns =
        {
            // Quoted string values in SQL:  = 'value'  or  = "value"
            new Regex(@"=\s*('[^']*'|""[^""]*"")", RegexOptions.Compiled),
            // Long numeric values (IDs, account numbers ≥ 6 digits)
            new Regex(@"=\s*\d{6,}", RegexOptions.Compiled),
            // Email addresses
            new Regex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled),
            // SSN pattern  ddd-dd-dddd
            new Regex(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled),
            // Credit-card-like 16-digit runs (with optional spaces/dashes)
            new Regex(@"\b(?:\d[ \-]?){16}\b", RegexOptions.Compiled),
            // Passwords in connection-string fragments  Password=xxx  or  pwd=xxx
            new Regex(@"(?i)(password|pwd|passwd|secret)\s*=\s*[^\s;,]+", RegexOptions.Compiled),
        };

        /// <summary>
        /// Returns a redacted copy of <paramref name="raw"/> safe to write to the log.
        /// All PII patterns are replaced with <c>[REDACTED]</c>.
        /// Returns the original string unchanged if it is null or empty.
        /// </summary>
        public static string Redact(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            foreach (var pattern in _patterns)
                raw = pattern.Replace(raw, "[REDACTED]");
            return raw;
        }

        /// <summary>
        /// Extracts and redacts the message from an exception chain,
        /// concatenating up to the inner-most non-null message.
        /// </summary>
        public static string RedactException(Exception ex)
        {
            if (ex == null) return string.Empty;
            var msg = ex.Message;
            if (ex.InnerException != null)
                msg += " → " + ex.InnerException.Message;
            return Redact(msg);
        }
    }
}
