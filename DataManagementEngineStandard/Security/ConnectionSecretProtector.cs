using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Security;

namespace TheTechIdea.Beep.Winform.Controls
{
    /// <summary>Legacy stateless entrypoints. Runtime hosts can inject IConnectionSecretProtector instead.</summary>
    public static class ConnectionSecretProtector
    {
        public static ConnectionProperties Encrypt(ConnectionProperties source) => ConnectionCredentialProtection.Default.Protect(source);
        public static ConnectionProperties Decrypt(ConnectionProperties source) => ConnectionCredentialProtection.Default.Unprotect(source);
        public static ConnectionProperties StripSecrets(ConnectionProperties source) => ConnectionCredentialProtection.Default.Redact(source);
    }
}
