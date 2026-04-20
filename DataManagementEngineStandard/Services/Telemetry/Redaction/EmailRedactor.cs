using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Services.Telemetry.Redaction
{
    /// <summary>
    /// Redacts RFC-5322-ish email addresses. Designed to favour false
    /// negatives over false positives so legitimate technical strings (file
    /// paths, urls without user-info) are left intact.
    /// </summary>
    public sealed class EmailRedactor : IRedactor
    {
        private static readonly Regex Pattern = new Regex(
            @"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,24}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly RedactionContext _context;

        /// <summary>Creates an email redactor.</summary>
        public EmailRedactor(RedactionContext context = null)
        {
            _context = context ?? new RedactionContext();
        }

        /// <inheritdoc/>
        public string Name => "email";

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
            string transformed = RedactionHelpers.Transform(match.Value, _context, "EMAIL");
            return transformed ?? string.Empty;
        }
    }
}
