using System.Collections.Generic;

namespace TheTechIdea.Beep.Services.Telemetry.Redaction
{
    /// <summary>
    /// Curated <see cref="IRedactor"/> bundles for the two most common
    /// deployments. Operators can drop a preset directly into
    /// <c>BeepLoggingOptions.Redactors</c> or
    /// <c>BeepAuditOptions.Redactors</c> instead of hand-wiring individual
    /// redactors.
    /// </summary>
    /// <remarks>
    /// Presets are factories rather than singletons because each one binds a
    /// specific <see cref="RedactionContext"/> (mode + salt). Callers who
    /// want the same preset with a custom salt should pass it in.
    /// </remarks>
    public static class DefaultRedactionPresets
    {
        /// <summary>
        /// Empty preset. Useful as an explicit "I deliberately disabled
        /// redaction" marker.
        /// </summary>
        public static IReadOnlyList<IRedactor> Off()
        {
            return System.Array.Empty<IRedactor>();
        }

        /// <summary>
        /// Recommended preset for application logs: connection strings, JWTs,
        /// and the <c>password</c>/<c>token</c>/<c>secret</c> keyword family,
        /// all in <see cref="RedactionMode.Mask"/> mode for readability.
        /// </summary>
        public static IReadOnlyList<IRedactor> LogsBalanced(RedactionContext context = null)
        {
            var ctx = (context ?? new RedactionContext()).WithMode(RedactionMode.Mask);
            return new IRedactor[]
            {
                new ConnectionStringRedactor(ctx),
                new JwtRedactor(ctx),
                new KeywordRedactor(new[] { "password", "passwd", "secret", "apikey", "token" }, ctx, "password-keyword")
            };
        }

        /// <summary>
        /// Strict preset for audit: extends <see cref="LogsBalanced"/> with
        /// PII detectors (credit card, email) and a structured-field
        /// redactor for common PII property keys, hashed so analysts can
        /// still join records by stable digest.
        /// </summary>
        public static IReadOnlyList<IRedactor> AuditStrict(RedactionContext context = null)
        {
            var baseCtx = context ?? new RedactionContext();
            var maskCtx = baseCtx.WithMode(RedactionMode.Mask);
            var hashCtx = baseCtx.WithMode(RedactionMode.Hash);

            return new IRedactor[]
            {
                new ConnectionStringRedactor(maskCtx),
                new JwtRedactor(maskCtx),
                new KeywordRedactor(new[] { "password", "passwd", "secret", "apikey", "token" }, maskCtx, "password-keyword"),
                new CreditCardRedactor(hashCtx),
                new EmailRedactor(hashCtx),
                new StructuredFieldRedactor(new[] { "Ssn", "Phone", "Email", "CreditCard", "AccountNumber", "TaxId", "DateOfBirth" }, hashCtx, name: "audit-pii")
            };
        }
    }
}
