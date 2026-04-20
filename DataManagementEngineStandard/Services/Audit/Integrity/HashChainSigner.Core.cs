using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Services.Audit.Integrity
{
    /// <summary>
    /// HMAC-SHA256 hash-chain signer. Sealed and partial — split into
    /// <c>.Core</c> (lifecycle), <c>.Sign</c> (Sequence/PrevHash/Hash
    /// assignment), and <c>.Verify</c> (post-hoc replay).
    /// </summary>
    /// <remarks>
    /// Sign operations are serialized per <c>ChainId</c> via a small
    /// per-chain mutex map so concurrent producers cannot race on
    /// sequence assignment. The signer caches the active anchor for
    /// each chain to avoid hitting <see cref="IChainAnchorStore"/> on
    /// every event; the store is still updated synchronously after
    /// each successful sign so a crash never loses the chain head.
    /// </remarks>
    public sealed partial class HashChainSigner : IHashChainSigner
    {
        private readonly IKeyMaterialProvider _keyProvider;
        private readonly IChainAnchorStore _anchorStore;
        private readonly Dictionary<string, ChainState> _chains;
        private readonly object _chainsGate = new object();

        /// <summary>Creates a signer over the supplied key/anchor providers.</summary>
        public HashChainSigner(IKeyMaterialProvider keyProvider, IChainAnchorStore anchorStore)
        {
            _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
            _anchorStore = anchorStore ?? throw new ArgumentNullException(nameof(anchorStore));
            _chains = new Dictionary<string, ChainState>(StringComparer.Ordinal);
        }

        private sealed class ChainState
        {
            public readonly object Gate = new object();
            public long LastSequence;
            public string LastHash;
            public bool Initialized;
        }

        private ChainState GetChain(string chainId)
        {
            lock (_chainsGate)
            {
                if (!_chains.TryGetValue(chainId, out ChainState state))
                {
                    state = new ChainState();
                    _chains[chainId] = state;
                }
                return state;
            }
        }

        private void EnsureInitialized_NoLock(string chainId, ChainState state)
        {
            if (state.Initialized)
            {
                return;
            }
            ChainAnchor anchor = _anchorStore.TryRead(chainId);
            if (anchor is not null)
            {
                state.LastSequence = anchor.LastSequence;
                state.LastHash = anchor.LastHash ?? string.Empty;
            }
            else
            {
                state.LastSequence = 0;
                state.LastHash = string.Empty;
            }
            state.Initialized = true;
        }
    }
}
