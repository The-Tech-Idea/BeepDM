using System;
using System.Security.Cryptography;
using System.Text;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Security
{
    public sealed class DpapiConnectionCredentialCipher : IConnectionCredentialCipher, ILegacyConnectionCredentialCipher
    {
        private const string Prefix = "__beepenc__:1:dpapi-user:";
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);

        private static void RequireWindows()
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Default connection protection requires Windows DPAPI. Inject a host-owned portable cipher.");
        }

        public string Protect(string plaintext, string purpose)
        {
            RequireWindows();
            var bytes = Utf8.GetBytes(plaintext);
            try { return Prefix + Convert.ToBase64String(ProtectedData.Protect(bytes, Utf8.GetBytes(purpose), DataProtectionScope.CurrentUser)); }
            finally { CryptographicOperations.ZeroMemory(bytes); }
        }

        public string Unprotect(string envelope, string purpose)
        {
            RequireWindows();
            if (!envelope.StartsWith(Prefix, StringComparison.Ordinal)) throw new CryptographicException("Unsupported credential protection format.");
            var bytes = ProtectedData.Unprotect(Convert.FromBase64String(envelope.Substring(Prefix.Length)), Utf8.GetBytes(purpose), DataProtectionScope.CurrentUser);
            try { return Utf8.GetString(bytes); }
            finally { CryptographicOperations.ZeroMemory(bytes); }
        }

        public string UnprotectLegacy(string envelope)
        {
            RequireWindows();
            if (!envelope.StartsWith("__enc__:", StringComparison.OrdinalIgnoreCase)) throw new CryptographicException("Unsupported legacy credential format.");
            var bytes = ProtectedData.Unprotect(Convert.FromBase64String(envelope.Substring(8)), null, DataProtectionScope.CurrentUser);
            try { return Utf8.GetString(bytes); }
            finally { CryptographicOperations.ZeroMemory(bytes); }
        }
    }

    /// <summary>Portable AES-256-GCM envelopes; key persistence, distribution and rotation are host responsibilities.</summary>
    public sealed class AesGcmConnectionCredentialCipher : IConnectionCredentialCipher
    {
        private const string Prefix = "__beepenc__:1:aesgcm:";
        private const int MaximumPayloadBytes = 8 * 1024 * 1024;
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private readonly IConnectionCredentialKeyProvider _keys;

        public AesGcmConnectionCredentialCipher(IConnectionCredentialKeyProvider keys) => _keys = keys ?? throw new ArgumentNullException(nameof(keys));

        private byte[] GetKey(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || Utf8.GetByteCount(id) > 128)
                throw new CryptographicException("Credential key identity is invalid.");
            var material = _keys.GetKey(id);
            if (material.Length != 32) throw new CryptographicException("An available 256-bit credential key is required.");
            return material.ToArray();
        }

        public string Protect(string plaintext, string purpose)
        {
            var id = _keys.CurrentKeyId;
            var key = GetKey(id);
            byte[] bytes = null;
            try
            {
                if (Utf8.GetByteCount(plaintext) > MaximumPayloadBytes) throw new CryptographicException("Credential payload exceeds its size limit.");
                bytes = Utf8.GetBytes(plaintext);
                var nonce = RandomNumberGenerator.GetBytes(12);
                var encrypted = new byte[bytes.Length];
                var tag = new byte[16];
                var header = Prefix + Convert.ToBase64String(Utf8.GetBytes(id));
                using var cipher = new AesGcm(key, tag.Length);
                cipher.Encrypt(nonce, bytes, encrypted, tag, Utf8.GetBytes(header + "|" + purpose));
                return header + ":" + Convert.ToBase64String(nonce) + ":" + Convert.ToBase64String(tag) + ":" + Convert.ToBase64String(encrypted);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
                if (bytes != null) CryptographicOperations.ZeroMemory(bytes);
            }
        }

        public string Unprotect(string envelope, string purpose)
        {
            if (envelope.Length > MaximumPayloadBytes * 2 || !envelope.StartsWith(Prefix, StringComparison.Ordinal))
                throw new CryptographicException("Unsupported credential protection format or size.");
            var parts = envelope.Substring(Prefix.Length).Split(':');
            if (parts.Length != 4 || parts[0].Length > 172) throw new CryptographicException("Malformed credential envelope.");
            var id = Utf8.GetString(Convert.FromBase64String(parts[0]));
            var key = GetKey(id);
            byte[] plaintext = null;
            try
            {
                var nonce = Convert.FromBase64String(parts[1]);
                var tag = Convert.FromBase64String(parts[2]);
                var encrypted = Convert.FromBase64String(parts[3]);
                if (nonce.Length != 12 || tag.Length != 16 || encrypted.Length > MaximumPayloadBytes)
                    throw new CryptographicException("Malformed credential envelope.");
                plaintext = new byte[encrypted.Length];
                using var cipher = new AesGcm(key, tag.Length);
                cipher.Decrypt(nonce, encrypted, tag, plaintext, Utf8.GetBytes(Prefix + parts[0] + "|" + purpose));
                return Utf8.GetString(plaintext);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
                if (plaintext != null) CryptographicOperations.ZeroMemory(plaintext);
            }
        }
    }
}
