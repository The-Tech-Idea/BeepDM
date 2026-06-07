using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Winform.Controls
{
    public static class ConnectionSecretProtector
    {
        private const string EncPrefix = "__enc__:";
        private static readonly string[] SecretPropertyNames =
        {
            nameof(ConnectionProperties.Password),
            nameof(ConnectionProperties.ApiKey),
            nameof(ConnectionProperties.KeyToken),
            nameof(ConnectionProperties.ClientSecret),
            nameof(ConnectionProperties.ProxyPassword),
            nameof(ConnectionProperties.ClientCertificatePassword),
            nameof(ConnectionProperties.OAuthAccessToken),
            nameof(ConnectionProperties.OAuthRefreshToken),
            nameof(ConnectionProperties.OAuthClientSecret),
            nameof(ConnectionProperties.AuthCode)
        };

        public static ConnectionProperties Encrypt(ConnectionProperties source)
        {
            var clone = Clone(source);
            foreach (var propertyName in SecretPropertyNames)
            {
                var prop = typeof(ConnectionProperties).GetProperty(propertyName);
                if (prop == null || !prop.CanRead || !prop.CanWrite) continue;
                if (prop.GetValue(clone) is not string raw || string.IsNullOrWhiteSpace(raw)) continue;
                if (raw.StartsWith(EncPrefix, StringComparison.OrdinalIgnoreCase)) continue;

                var bytes = Encoding.UTF8.GetBytes(raw);
                var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                prop.SetValue(clone, EncPrefix + Convert.ToBase64String(encrypted));
            }
            return clone;
        }

        public static ConnectionProperties Decrypt(ConnectionProperties source)
        {
            var clone = Clone(source);
            foreach (var propertyName in SecretPropertyNames)
            {
                var prop = typeof(ConnectionProperties).GetProperty(propertyName);
                if (prop == null || !prop.CanRead || !prop.CanWrite) continue;
                if (prop.GetValue(clone) is not string raw || string.IsNullOrWhiteSpace(raw)) continue;
                if (!raw.StartsWith(EncPrefix, StringComparison.OrdinalIgnoreCase)) continue;

                var payload = raw.Substring(EncPrefix.Length);
                try
                {
                    var encrypted = Convert.FromBase64String(payload);
                    var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                    prop.SetValue(clone, Encoding.UTF8.GetString(decrypted));
                }
                catch (Exception ex) when (ex is CryptographicException or FormatException or ArgumentException)
                {
                    System.Diagnostics.Debug.WriteLine($"[ConnectionSecretProtector.Decrypt] {ex.GetType().Name}: {ex.Message}");
                }
            }
            return clone;
        }

        public static ConnectionProperties StripSecrets(ConnectionProperties source)
        {
            var clone = Clone(source);
            foreach (var propertyName in SecretPropertyNames)
                if (typeof(ConnectionProperties).GetProperty(propertyName) is { CanWrite: true } prop)
                    prop.SetValue(clone, string.Empty);
            return clone;
        }

        private static ConnectionProperties Clone(ConnectionProperties source)
        {
            var clone = new ConnectionProperties();
            foreach (var property in typeof(ConnectionProperties).GetProperties()
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0))
            {
                try { property.SetValue(clone, property.GetValue(source)); }
                catch (Exception ex) when (ex is ArgumentException or NotSupportedException or MethodAccessException
                    or System.Reflection.TargetException or System.Reflection.TargetInvocationException
                    or System.Reflection.TargetParameterCountException)
                {
                    System.Diagnostics.Debug.WriteLine($"[ConnectionSecretProtector.Clone] {property.Name}: {ex.GetType().Name} - {ex.Message}");
                }
            }

            clone.ParameterList = source.ParameterList != null
                ? new Dictionary<string, string>(source.ParameterList, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return clone;
        }
    }
}
