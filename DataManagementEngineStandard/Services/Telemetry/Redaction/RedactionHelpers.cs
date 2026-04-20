using System;
using System.Security.Cryptography;
using System.Text;

namespace TheTechIdea.Beep.Services.Telemetry.Redaction
{
    /// <summary>
    /// Shared transformation primitives used by every built-in redactor.
    /// Centralizing <see cref="Mask"/>, <see cref="Hash"/> and
    /// <see cref="Transform"/> guarantees consistent behavior across redactors
    /// and is the single place to change replacement formatting later.
    /// </summary>
    internal static class RedactionHelpers
    {
        /// <summary>
        /// Returns the literal mask token for the supplied context.
        /// Optional <paramref name="prefix"/> yields tokens like
        /// <c>[REDACTED:JWT]</c> when the redactor wants to advertise the
        /// kind of secret that was scrubbed.
        /// </summary>
        public static string Mask(RedactionContext ctx, string prefix = null)
        {
            string token = ctx?.ReplacementToken ?? RedactionContext.DefaultReplacementToken;
            if (string.IsNullOrEmpty(prefix))
            {
                return token;
            }
            return string.Concat("[REDACTED:", prefix, "]");
        }

        /// <summary>
        /// Returns a SHA-256 hex digest of <paramref name="value"/> mixed
        /// with <paramref name="ctx"/>'s salt. The result is lowercase and
        /// 64 hex characters long. Returns an empty string for null/empty
        /// input so downstream serializers do not write a literal hash of an
        /// empty string.
        /// </summary>
        public static string Hash(string value, RedactionContext ctx)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            string salt = ctx?.HashSalt ?? RedactionContext.DefaultHashSalt;
            byte[] payload = Encoding.UTF8.GetBytes(string.Concat(salt, "|", value));
            using (var sha = SHA256.Create())
            {
                byte[] digest = sha.ComputeHash(payload);
                var sb = new StringBuilder(digest.Length * 2);
                for (int i = 0; i < digest.Length; i++)
                {
                    sb.Append(digest[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Applies the context's <see cref="RedactionMode"/> to a captured
        /// secret, returning the appropriate replacement string. Returns
        /// <c>null</c> for <see cref="RedactionMode.Drop"/> so callers can
        /// strip the property entirely.
        /// </summary>
        public static string Transform(string original, RedactionContext ctx, string maskPrefix = null)
        {
            if (ctx == null)
            {
                return Mask(null, maskPrefix);
            }
            switch (ctx.Mode)
            {
                case RedactionMode.Hash:
                    return Hash(original, ctx);
                case RedactionMode.Drop:
                    return null;
                case RedactionMode.Mask:
                default:
                    return Mask(ctx, maskPrefix);
            }
        }
    }
}
