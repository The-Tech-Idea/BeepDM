using System.Collections.Generic;

namespace TheTechIdea.Beep.Pipelines.Observability
{
    /// <summary>
    /// Controls how sensitive column values are masked when captured in <see cref="RowRunLog.FieldSnapshot"/>.
    /// </summary>
    public class MaskingConfig
    {
        /// <summary>
        /// Column names that are always masked regardless of which pipeline produced the row.
        /// Comparison is case-insensitive.
        /// </summary>
        public List<string> GlobalMaskedFields { get; set; } = new()
        {
            "Password", "CreditCard", "SSN", "NationalId", "CVV", "PIN", "SecretKey",
            "Token", "ApiKey", "Secret", "PrivateKey"
        };

        public MaskingStrategy Strategy    { get; set; } = MaskingStrategy.Redact;
        /// <summary>Number of leading characters to preserve when <see cref="MaskingStrategy.Partial"/>.</summary>
        public int ShowFirstChars          { get; set; } = 0;
        /// <summary>Number of trailing characters to preserve — e.g. 4 yields "****1234".</summary>
        public int ShowLastChars           { get; set; } = 4;
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>How sensitive field values are represented in row-level logs.</summary>
    public enum MaskingStrategy
    {
        /// <summary>No masking — value logged as-is.</summary>
        None,
        /// <summary>Replace the value with <c>"***REDACTED***"</c>.</summary>
        Redact,
        /// <summary>Show first/last N characters; mask the middle with asterisks.</summary>
        Partial,
        /// <summary>Replace the value with its SHA-256 hex digest.</summary>
        Hash
    }
}
