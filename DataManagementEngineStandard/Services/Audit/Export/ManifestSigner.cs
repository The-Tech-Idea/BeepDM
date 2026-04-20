using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TheTechIdea.Beep.Services.Audit.Integrity;

namespace TheTechIdea.Beep.Services.Audit.Export
{
    /// <summary>
    /// Signs and verifies <see cref="ExportManifest"/> instances using
    /// the same HMAC-SHA256 key as the audit hash chain. Keeping the
    /// key material in a single provider means rotating the audit
    /// secret invalidates every prior export's manifest as well — the
    /// caller has to re-export against the new key.
    /// </summary>
    public sealed class ManifestSigner
    {
        private readonly IKeyMaterialProvider _keyProvider;

        /// <summary>Creates a signer over <paramref name="keyProvider"/>.</summary>
        public ManifestSigner(IKeyMaterialProvider keyProvider)
        {
            _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
        }

        /// <summary>
        /// Computes the HMAC over the canonical manifest payload (every
        /// field except <see cref="ExportManifest.ManifestHmac"/>) and
        /// stores it on the manifest.
        /// </summary>
        public void Sign(ExportManifest manifest)
        {
            if (manifest is null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            byte[] payload = SerializeForHash(manifest);
            byte[] key = _keyProvider.GetHmacKey() ?? Array.Empty<byte>();
            using var hmac = new HMACSHA256(key);
            byte[] mac = hmac.ComputeHash(payload);
            manifest.ManifestHmac = ToHex(mac);
        }

        /// <summary>
        /// Returns <c>true</c> when <see cref="ExportManifest.ManifestHmac"/>
        /// matches a freshly recomputed HMAC.
        /// </summary>
        public bool Verify(ExportManifest manifest)
        {
            if (manifest is null || string.IsNullOrEmpty(manifest.ManifestHmac))
            {
                return false;
            }
            byte[] payload = SerializeForHash(manifest);
            byte[] key = _keyProvider.GetHmacKey() ?? Array.Empty<byte>();
            using var hmac = new HMACSHA256(key);
            byte[] mac = hmac.ComputeHash(payload);
            string expected = ToHex(mac);
            return string.Equals(expected, manifest.ManifestHmac, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Computes the SHA-256 (hex) of the supplied payload bytes.
        /// Exposed so the exporter can stamp the manifest before the
        /// HMAC is computed.
        /// </summary>
        public static string ComputePayloadSha256(byte[] payload)
        {
            if (payload is null)
            {
                throw new ArgumentNullException(nameof(payload));
            }
            using var sha = SHA256.Create();
            byte[] digest = sha.ComputeHash(payload);
            return ToHex(digest);
        }

        private static byte[] SerializeForHash(ExportManifest manifest)
        {
            using var ms = new MemoryStream();
            using (var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = false }))
            {
                writer.WriteStartObject();
                writer.WriteNumber("version", manifest.Version);
                writer.WriteString("createdUtc", manifest.CreatedUtc.ToString("O", CultureInfo.InvariantCulture));
                WriteOptional(writer, "operatorId", manifest.OperatorId);
                WriteOptional(writer, "format", manifest.Format);
                writer.WriteNumber("eventCount", manifest.EventCount);
                if (manifest.FromUtc.HasValue)
                {
                    writer.WriteString("fromUtc", manifest.FromUtc.Value.ToString("O", CultureInfo.InvariantCulture));
                }
                if (manifest.ToUtc.HasValue)
                {
                    writer.WriteString("toUtc", manifest.ToUtc.Value.ToString("O", CultureInfo.InvariantCulture));
                }
                writer.WritePropertyName("chainIds");
                writer.WriteStartArray();
                if (manifest.ChainIds is not null)
                {
                    for (int i = 0; i < manifest.ChainIds.Count; i++)
                    {
                        writer.WriteStringValue(manifest.ChainIds[i] ?? string.Empty);
                    }
                }
                writer.WriteEndArray();
                WriteOptional(writer, "payloadSha256", manifest.PayloadSha256);
                WriteOptional(writer, "notes", manifest.Notes);
                writer.WriteEndObject();
            }
            return ms.ToArray();
        }

        private static void WriteOptional(Utf8JsonWriter writer, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                writer.WriteString(name, value);
            }
        }

        private static string ToHex(byte[] bytes)
        {
            const string lookup = "0123456789abcdef";
            var sb = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                sb.Append(lookup[b >> 4]);
                sb.Append(lookup[b & 0x0F]);
            }
            return sb.ToString();
        }
    }
}
