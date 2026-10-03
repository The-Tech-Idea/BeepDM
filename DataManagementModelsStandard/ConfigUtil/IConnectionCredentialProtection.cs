using System;
using System.Threading;

namespace TheTechIdea.Beep.ConfigUtil
{
    public interface IConnectionConfigurationPersistence
    {
        PersistenceWriteResult SaveDataConnectionsAcknowledged(CancellationToken token = default);
    }

    /// <summary>Immutable per-runtime policy. Returned snapshots must not mutate or share credential containers with the input.</summary>
    public interface IConnectionSecretProtector
    {
        ConnectionProperties Protect(ConnectionProperties source);
        ConnectionProperties Unprotect(ConnectionProperties source);
        ConnectionProperties Redact(ConnectionProperties source);
    }

    public interface IConnectionProtectionContext
    {
        IConnectionSecretProtector ConnectionSecretProtector { get; }
    }

    /// <summary>Authenticated credential encryption. Purpose must be bound during protect and checked during unprotect.</summary>
    public interface IConnectionCredentialCipher
    {
        string Protect(string plaintext, string purpose);
        string Unprotect(string envelope, string purpose);
    }

    public interface ILegacyConnectionCredentialCipher
    {
        string UnprotectLegacy(string envelope);
    }

    /// <summary>Host-owned secret key ring; never persist its key material in the connection store.</summary>
    public interface IConnectionCredentialKeyProvider
    {
        string CurrentKeyId { get; }
        ReadOnlyMemory<byte> GetKey(string keyId);
    }
}
