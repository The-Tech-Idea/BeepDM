using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Services.Telemetry.Redaction
{
    /// <summary>
    /// Detects standard three-segment JWTs (<c>eyJ...&#46;...&#46;...</c>) and
    /// redacts the entire token. Catches the dominant Bearer-token shape
    /// without relying on header keys, so it works regardless of whether the
    /// token was logged behind <c>Authorization:</c>, <c>token=</c>, or
    /// embedded inline.
    /// </summary>
    public sealed class JwtRedactor : IRedactor
    {
        private static readonly Regex Pattern = new Regex(
            @"eyJ[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly RedactionContext _context;

        /// <summary>Creates a JWT redactor.</summary>
        public JwtRedactor(RedactionContext context = null)
        {
            _context = context ?? new RedactionContext();
        }

        /// <inheritdoc/>
        public string Name => "jwt";

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
            string transformed = RedactionHelpers.Transform(match.Value, _context, "JWT");
            return transformed ?? string.Empty;
        }
    }
}
