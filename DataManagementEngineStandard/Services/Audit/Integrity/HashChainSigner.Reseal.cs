using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Services.Audit.Models;

namespace TheTechIdea.Beep.Services.Audit.Integrity
{
    /// <summary>
    /// Re-seal half of <see cref="HashChainSigner"/>. Re-assigns
    /// Sequence, PrevHash, and Hash to the supplied chain after a
    /// GDPR-style purge has removed events out of the middle. Keeps the
    /// in-memory chain state and anchor store consistent with the new
    /// tail so subsequent <see cref="Sign(AuditEvent)"/> calls continue
    /// without producing a sequence gap.
    /// </summary>
    public sealed partial class HashChainSigner
    {
        /// <summary>
        /// Re-numbers and re-hashes every surviving event in
        /// <paramref name="survivors"/> (in the order supplied) for the
        /// chain identified by <paramref name="chainId"/>.
        /// </summary>
        public IReadOnlyList<AuditEvent> Reseal(string chainId, IReadOnlyList<AuditEvent> survivors)
        {
            if (string.IsNullOrEmpty(chainId))
            {
                throw new ArgumentNullException(nameof(chainId));
            }
            if (survivors is null)
            {
                throw new ArgumentNullException(nameof(survivors));
            }

            ChainState state = GetChain(chainId);
            byte[] key = _keyProvider.GetHmacKey() ?? Array.Empty<byte>();
            var resealed = new List<AuditEvent>(survivors.Count);

            lock (state.Gate)
            {
                long sequence = 0;
                string prevHash = string.Empty;

                for (int i = 0; i < survivors.Count; i++)
                {
                    AuditEvent ev = survivors[i];
                    if (ev is null)
                    {
                        continue;
                    }

                    sequence++;
                    ev.ChainId = chainId;
                    ev.Sequence = sequence;
                    ev.PrevHash = prevHash;
                    ev.Hash = ComputeHash(key, ev, prevHash);
                    prevHash = ev.Hash;
                    resealed.Add(ev);
                }

                state.LastSequence = sequence;
                state.LastHash = prevHash;
                state.Initialized = true;

                _anchorStore.Write(new ChainAnchor
                {
                    ChainId = chainId,
                    LastSequence = sequence,
                    LastHash = prevHash,
                    LastUpdatedUtc = DateTime.UtcNow
                });
            }

            return resealed;
        }
    }
}
