using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Services.Telemetry.Redaction
{
    /// <summary>
    /// Scrubs the credential keys most commonly leaked in connection strings:
    /// <c>password</c>, <c>pwd</c>, <c>account_key</c>, <c>secret</c>,
    /// <c>shared_access_key</c>, <c>shared_access_signature</c>, <c>apikey</c>,
    /// <c>api_key</c>, <c>token</c>. Compatible with both ADO.NET-style
    /// connection strings and URI query strings.
    /// </summary>
    /// <remarks>
    /// Mirrors and extends the legacy <see cref="TheTechIdea.Beep.Proxy"/>
    /// redactor pattern so callers can switch to the unified pipeline
    /// without changing observed behavior.
    /// </remarks>
    public sealed class ConnectionStringRedactor : IRedactor
    {
        private static readonly Regex Pattern = new Regex(
            @"(?<key>password|pwd|passwd|account_key|secret|shared_access_key|shared_access_signature|apikey|api_key|token)\s*=\s*(?<val>'[^']*'|""[^""]*""|[^\s;,&]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private readonly RedactionContext _context;

        /// <summary>
        /// Creates a connection-string redactor. Defaults to
        /// <see cref="RedactionMode.Mask"/>; switch to
        /// <see cref="RedactionMode.Hash"/> in audit so analysts can detect
        /// credential reuse without recovering the value.
        /// </summary>
        public ConnectionStringRedactor(RedactionContext context = null)
        {
            _context = context ?? new RedactionContext();
        }

        /// <inheritdoc/>
        public string Name => "connection-string";

        /// <inheritdoc/>
        public void Redact(TelemetryEnvelope envelope)
        {
            if (envelope is null || string.IsNullOrEmpty(envelope.Message))
            {
                return;
            }
            envelope.Message = Pattern.Replace(envelope.Message, ReplaceMatch);
        }

        private string ReplaceMatch(Match match)
        {
            string key = match.Groups["key"].Value;
            string original = match.Groups["val"].Value;
            string transformed = RedactionHelpers.Transform(original, _context, "SECRET") ?? string.Empty;
            return string.Concat(key, "=", transformed);
        }
    }
}
