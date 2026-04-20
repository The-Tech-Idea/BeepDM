using System;
using System.Text;

namespace TheTechIdea.Beep.Services.Audit.Integrity
{
    /// <summary>
    /// Returns a fixed HMAC secret supplied at construction. Intended
    /// for tests and short-lived ad-hoc utilities — not for production.
    /// </summary>
    public sealed class StaticKeyMaterialProvider : IKeyMaterialProvider
    {
        private readonly byte[] _key;

        /// <summary>Creates a provider over the supplied UTF-8 secret.</summary>
        public StaticKeyMaterialProvider(string secret)
        {
            if (string.IsNullOrEmpty(secret))
            {
                throw new ArgumentException("Secret must be non-empty.", nameof(secret));
            }
            _key = Encoding.UTF8.GetBytes(secret);
        }

        /// <summary>Creates a provider over the supplied raw key bytes.</summary>
        public StaticKeyMaterialProvider(byte[] key)
        {
            if (key is null || key.Length == 0)
            {
                throw new ArgumentException("Key must be non-empty.", nameof(key));
            }
            _key = (byte[])key.Clone();
        }

        /// <inheritdoc/>
        public byte[] GetHmacKey() => (byte[])_key.Clone();
    }
}
