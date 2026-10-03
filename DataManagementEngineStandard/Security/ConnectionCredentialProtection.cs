using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.JsonLoaderService;

namespace TheTechIdea.Beep.Security
{
    /// <summary>Conservative whole-container protection; metadata labels are not a credential vault.</summary>
    public sealed class ConnectionCredentialProtection : IConnectionSecretProtector
    {
        public static IConnectionSecretProtector Default { get; } = new ConnectionCredentialProtection(new DpapiConnectionCredentialCipher());
        private const int MaximumPayloadCharacters = 16 * 1024 * 1024;
        private readonly IConnectionCredentialCipher _cipher;
        private static readonly string[] Names = (
            "Password ApiKey KeyToken ClientSecret ProxyPassword ClientCertificatePassword OAuthAccessToken OAuthRefreshToken OAuthClientSecret AuthCode " +
            "ConnectionString Parameters ParameterList AdditionalAuthInfo OAuthCodeVerifier OAuthState Url ProxyUrl AuthUrl TokenUrl OAuthTokenEndpoint " +
            "Authority RedirectUriAuth Resource Audience RedirectUri Headers QueryParameters BodyParameters FormParameters FileParameters").Split(' ');
        private static readonly PropertyInfo[] Properties = Names.Select(name => typeof(ConnectionProperties).GetProperty(name)
            ?? throw new InvalidOperationException("Credential property contract is missing.")).ToArray();

        public ConnectionCredentialProtection(IConnectionCredentialCipher cipher) => _cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
        private static JsonSerializer Serializer() => JsonSerializer.Create(new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None, ObjectCreationHandling = ObjectCreationHandling.Replace,
            DateParseHandling = DateParseHandling.None, ReferenceLoopHandling = ReferenceLoopHandling.Error, MaxDepth = 64
        });

        private static ConnectionProperties Clone(ConnectionProperties source)
        {
            ArgumentNullException.ThrowIfNull(source);
            var codec = new JsonLoader();
            return codec.DeserializeSnapshot<ConnectionProperties>(codec.SerializeSnapshot(source));
        }

        private static bool HasValue(object value) => value switch
        {
            null => false, string text => text.Length > 0, ICollection collection => collection.Count > 0, _ => true
        };

        private static string Purpose(ConnectionProperties source)
        {
            if (string.IsNullOrWhiteSpace(source.GuidID)) throw new CryptographicException("A connection identity is required for credential protection.");
            return "BeepDM.ConnectionCredentials.v1|" + source.GuidID;
        }

        public ConnectionProperties Protect(ConnectionProperties source) => Guard(() =>
        {
            var plaintext = Unprotect(source);
            var values = new JObject();
            foreach (var property in Properties)
            {
                var value = property.GetValue(plaintext);
                if (HasValue(value)) values[property.Name] = JToken.FromObject(value, Serializer());
            }
            if (values.Count == 0) return plaintext;
            var payload = new JObject { ["Version"] = 1, ["Values"] = values }.ToString(Formatting.None);
            if (payload.Length > MaximumPayloadCharacters) throw new CryptographicException("Credential payload exceeds its size limit.");
            var envelope = _cipher.Protect(payload, Purpose(plaintext));
            if (string.IsNullOrEmpty(envelope) || envelope.Length > MaximumPayloadCharacters)
                throw new CryptographicException("Credential cipher returned an invalid protected payload size.");
            Clear(plaintext);
            plaintext.ProtectedCredentialPayload = envelope;
            return plaintext;
        });

        public ConnectionProperties Unprotect(ConnectionProperties source) => Guard(() =>
        {
            var clone = Clone(source);
            if (!string.IsNullOrEmpty(clone.ProtectedCredentialPayload))
            {
                if (clone.ProtectedCredentialPayload.Length > MaximumPayloadCharacters || Properties.Any(property => HasValue(property.GetValue(clone))))
                    throw new CryptographicException("Protected connection contains conflicting plaintext credential containers or exceeds its size limit.");
                var payload = _cipher.Unprotect(clone.ProtectedCredentialPayload, Purpose(clone));
                if (payload == null || payload.Length > MaximumPayloadCharacters) throw new CryptographicException("Invalid credential payload size.");
                using var reader = new JsonTextReader(new StringReader(payload)) { MaxDepth = 64, DateParseHandling = DateParseHandling.None };
                var document = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (reader.Read() || document["Version"]?.Type != JTokenType.Integer || (int)document["Version"] != 1 || document["Values"] is not JObject values)
                    throw new CryptographicException("Unsupported credential payload format.");
                foreach (var value in values.Properties())
                {
                    var property = Properties.SingleOrDefault(item => item.Name == value.Name)
                        ?? throw new CryptographicException("Credential payload contains an unsupported property.");
                    property.SetValue(clone, value.Value.ToObject(property.PropertyType, Serializer()));
                }
                clone.ProtectedCredentialPayload = null;
            }
            else
            {
                foreach (var property in Properties.Where(item => item.PropertyType == typeof(string)))
                    if (property.GetValue(clone) is string text && text.StartsWith("__enc__:", StringComparison.OrdinalIgnoreCase))
                        property.SetValue(clone, (_cipher as ILegacyConnectionCredentialCipher
                            ?? throw new NotSupportedException("The configured cipher cannot read legacy DPAPI credentials.")).UnprotectLegacy(text));
            }
            return clone;
        });

        public ConnectionProperties Redact(ConnectionProperties source) => Guard(() =>
        {
            var clone = Clone(source);
            Clear(clone);
            return clone;
        });

        private static void Clear(ConnectionProperties source)
        {
            foreach (var property in Properties) property.SetValue(source, property.PropertyType == typeof(string) ? string.Empty : null);
            source.ProtectedCredentialPayload = null;
        }

        private static ConnectionProperties Guard(Func<ConnectionProperties> operation)
        {
            try { return operation(); }
            catch (PlatformNotSupportedException)
            {
                throw new PlatformNotSupportedException("Credential protection is unavailable on this platform; inject a supported host-owned cipher.");
            }
            catch (NotSupportedException)
            {
                throw new NotSupportedException("Credential protection provider/format is unsupported; no plaintext fallback is permitted.");
            }
            catch (Exception ex)
            {
                // Provider exception messages can contain plaintext credentials; expose only the failure type.
                throw new CryptographicException($"Connection credential processing failed ({ex.GetType().Name}); no plaintext fallback is permitted.");
            }
        }
    }
}
