using System;
using System.Security.Cryptography;
using System.Text;
using TheTechIdea.Beep.Services.Audit.Models;

namespace TheTechIdea.Beep.Services.Audit.Integrity
{
    /// <summary>
    /// Signing half of <see cref="HashChainSigner"/>. Owns the
    /// per-event Sequence/PrevHash/Hash assignment under a per-chain
    /// mutex so concurrent producers cannot race.
    /// </summary>
    public sealed partial class HashChainSigner
    {
        /// <inheritdoc/>
        public AuditEvent Sign(AuditEvent auditEvent)
        {
            if (auditEvent is null)
            {
                throw new ArgumentNullException(nameof(auditEvent));
            }

            string chainId = string.IsNullOrEmpty(auditEvent.ChainId)
                ? AuditEvent.DefaultChainId
                : auditEvent.ChainId;
            auditEvent.ChainId = chainId;

            ChainState state = GetChain(chainId);
            byte[] key = _keyProvider.GetHmacKey() ?? Array.Empty<byte>();

            lock (state.Gate)
            {
                EnsureInitialized_NoLock(chainId, state);

                long nextSequence = state.LastSequence + 1;
                string prevHash = state.LastHash ?? string.Empty;

                auditEvent.Sequence = nextSequence;
                auditEvent.PrevHash = prevHash;
                auditEvent.Hash = ComputeHash(key, auditEvent, prevHash);

                state.LastSequence = nextSequence;
                state.LastHash = auditEvent.Hash;

                _anchorStore.Write(new ChainAnchor
                {
                    ChainId = chainId,
                    LastSequence = nextSequence,
                    LastHash = auditEvent.Hash,
                    LastUpdatedUtc = DateTime.UtcNow
                });
            }

            return auditEvent;
        }

        /// <summary>
        /// Computes the chain hash for <paramref name="auditEvent"/> with
        /// the supplied <paramref name="prevHash"/>. Exposed for the
        /// verifier so signing and verification share one implementation.
        /// </summary>
        internal static string ComputeHash(byte[] key, AuditEvent auditEvent, string prevHash)
        {
            string canonical = CanonicalJsonSerializer.SerializeForHash(auditEvent);
            byte[] payloadBytes = Encoding.UTF8.GetBytes(canonical);
            byte[] prevBytes = string.IsNullOrEmpty(prevHash)
                ? Array.Empty<byte>()
                : Encoding.UTF8.GetBytes(prevHash);

            byte[] combined = new byte[payloadBytes.Length + prevBytes.Length];
            Buffer.BlockCopy(payloadBytes, 0, combined, 0, payloadBytes.Length);
            if (prevBytes.Length > 0)
            {
                Buffer.BlockCopy(prevBytes, 0, combined, payloadBytes.Length, prevBytes.Length);
            }

            using var hmac = new HMACSHA256(key);
            byte[] mac = hmac.ComputeHash(combined);
            return ToHex(mac);
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
