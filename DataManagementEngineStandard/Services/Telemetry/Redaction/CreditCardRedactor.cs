using System.Text;
using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Services.Telemetry.Redaction
{
    /// <summary>
    /// Detects 13–19 digit runs (with optional spaces or dashes) that pass
    /// the Luhn check and replaces them per the configured
    /// <see cref="RedactionContext"/>. The Luhn pre-filter keeps random ID
    /// strings of similar length from being mis-flagged.
    /// </summary>
    /// <remarks>
    /// Whitespace and dashes are stripped before validation, but the
    /// surrounding non-digit characters are preserved in the replacement
    /// boundary so log readers still see where the value sat in the message.
    /// </remarks>
    public sealed class CreditCardRedactor : IRedactor
    {
        private static readonly Regex Candidate = new Regex(
            @"(?<!\d)(?:\d[ \-]?){13,19}(?!\d)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly RedactionContext _context;

        /// <summary>Creates a Luhn-checked credit card redactor.</summary>
        public CreditCardRedactor(RedactionContext context = null)
        {
            _context = context ?? new RedactionContext();
        }

        /// <inheritdoc/>
        public string Name => "credit-card";

        /// <inheritdoc/>
        public void Redact(TelemetryEnvelope envelope)
        {
            if (envelope is null || string.IsNullOrEmpty(envelope.Message))
            {
                return;
            }
            envelope.Message = Candidate.Replace(envelope.Message, ReplaceIfLuhn);
        }

        private string ReplaceIfLuhn(Match match)
        {
            string raw = match.Value;
            if (!IsLuhnValid(raw))
            {
                return raw;
            }
            string transformed = RedactionHelpers.Transform(raw, _context, "CC");
            return transformed ?? string.Empty;
        }

        private static bool IsLuhnValid(string value)
        {
            int sum = 0;
            int digitCount = 0;
            bool alternate = false;
            for (int i = value.Length - 1; i >= 0; i--)
            {
                char c = value[i];
                if (c < '0' || c > '9')
                {
                    continue;
                }
                int digit = c - '0';
                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9)
                    {
                        digit -= 9;
                    }
                }
                sum += digit;
                alternate = !alternate;
                digitCount++;
            }
            return digitCount >= 13 && digitCount <= 19 && (sum % 10) == 0;
        }

        // Reserved for diagnostic builders that need to emit "**** **** **** 1234".
        // Not used by the default Mask path which always returns the redactor token.
        internal static string LastFour(string value)
        {
            var sb = new StringBuilder(4);
            for (int i = value.Length - 1; i >= 0 && sb.Length < 4; i--)
            {
                if (value[i] >= '0' && value[i] <= '9')
                {
                    sb.Insert(0, value[i]);
                }
            }
            return sb.ToString();
        }
    }
}
