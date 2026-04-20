using System;

namespace TheTechIdea.Beep.Services.Audit.Integrity
{
    /// <summary>
    /// Last-known position of an audit hash chain. Persisted by an
    /// <see cref="IChainAnchorStore"/> so a process restart can continue
    /// chaining new events onto the prior <see cref="LastHash"/>.
    /// </summary>
    public sealed class ChainAnchor
    {
        /// <summary>Logical chain identifier (e.g. <c>"default"</c>, <c>"tenant-A"</c>).</summary>
        public string ChainId { get; set; }

        /// <summary>Sequence assigned to the most recent event in the chain.</summary>
        public long LastSequence { get; set; }

        /// <summary>Hex-encoded HMAC of the most recent event in the chain.</summary>
        public string LastHash { get; set; }

        /// <summary>UTC timestamp at which the anchor was last updated.</summary>
        public DateTime LastUpdatedUtc { get; set; }

        /// <summary>Default constructor for serializers and object initializers.</summary>
        public ChainAnchor()
        {
        }

        /// <summary>Convenience constructor.</summary>
        public ChainAnchor(string chainId, long lastSequence, string lastHash, DateTime lastUpdatedUtc)
        {
            ChainId = chainId;
            LastSequence = lastSequence;
            LastHash = lastHash;
            LastUpdatedUtc = lastUpdatedUtc;
        }
    }
}
