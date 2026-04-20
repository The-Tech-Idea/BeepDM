using System;

namespace TheTechIdea.Beep.Services.Telemetry.Redaction
{
    /// <summary>
    /// Configuration shared by every <see cref="IRedactor"/>: the
    /// transformation <see cref="Mode"/>, the literal mask token and the salt
    /// used to derive deterministic hashes when <see cref="Mode"/> is
    /// <see cref="RedactionMode.Hash"/>.
    /// </summary>
    /// <remarks>
    /// Built-in redactors accept a context in their constructor. Operators may
    /// reuse a single context across multiple redactors (typical) or hand
    /// each redactor its own context to mix modes (e.g. <c>Mask</c> for
    /// connection strings, <c>Hash</c> for PII keys). The context is
    /// effectively immutable after construction; mutating it at runtime is
    /// not thread-safe.
    /// </remarks>
    public sealed class RedactionContext
    {
        /// <summary>Default literal token written by <see cref="RedactionMode.Mask"/>.</summary>
        public const string DefaultReplacementToken = "***";

        /// <summary>Default per-environment hash salt. Operators should override.</summary>
        public const string DefaultHashSalt = "beep-default-salt";

        /// <summary>Mode applied to every match.</summary>
        public RedactionMode Mode { get; set; } = RedactionMode.Mask;

        /// <summary>
        /// Literal substring written when <see cref="Mode"/> is
        /// <see cref="RedactionMode.Mask"/>. Built-in redactors may prepend a
        /// type-specific prefix, e.g. <c>[REDACTED:JWT]</c>.
        /// </summary>
        public string ReplacementToken { get; set; } = DefaultReplacementToken;

        /// <summary>
        /// Salt mixed into <see cref="RedactionMode.Hash"/> digests so two
        /// environments produce different hashes for the same input. Should
        /// be set per-environment via configuration.
        /// </summary>
        public string HashSalt { get; set; } = DefaultHashSalt;

        /// <summary>
        /// Returns a shallow copy with the requested mode override. Useful for
        /// preset builders that need a stricter mode for a single redactor.
        /// </summary>
        public RedactionContext WithMode(RedactionMode mode)
        {
            return new RedactionContext
            {
                Mode = mode,
                ReplacementToken = ReplacementToken,
                HashSalt = HashSalt
            };
        }

        /// <summary>Returns a shallow copy with the requested replacement token.</summary>
        public RedactionContext WithToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Replacement token must be non-empty.", nameof(token));
            }
            return new RedactionContext
            {
                Mode = Mode,
                ReplacementToken = token,
                HashSalt = HashSalt
            };
        }
    }
}
