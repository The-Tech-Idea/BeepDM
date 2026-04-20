using System.Collections.Generic;

namespace TheTechIdea.Beep.Services.Audit.Integrity
{
    /// <summary>
    /// Persists the per-chain "last sequence + last hash" anchor across
    /// process restarts. The signer reads the anchor on first use of a
    /// chain id and writes back after every successful sign so the
    /// chain stays continuous through crashes and graceful restarts.
    /// </summary>
    /// <remarks>
    /// Implementations must be thread-safe and atomic at the per-chain
    /// level (a partial write that loses a hash would silently corrupt
    /// the chain). The default <see cref="JsonChainAnchorStore"/> uses
    /// a <c>tmp + fsync + rename</c> pattern to guarantee atomicity on
    /// every supported filesystem.
    /// </remarks>
    public interface IChainAnchorStore
    {
        /// <summary>Returns the anchor for <paramref name="chainId"/>, or <c>null</c> if none.</summary>
        ChainAnchor TryRead(string chainId);

        /// <summary>Persists <paramref name="anchor"/> atomically.</summary>
        void Write(ChainAnchor anchor);

        /// <summary>Returns a snapshot of every known anchor.</summary>
        IReadOnlyList<ChainAnchor> ReadAll();
    }
}
