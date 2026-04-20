using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Services.Audit.Purge
{
    /// <summary>
    /// Outcome of a single purge call against an
    /// <see cref="IAuditPurgeStore"/>. Carries the count of removed
    /// rows and the affected chain identifiers so the GDPR purge
    /// service can drive the chain re-seal step.
    /// </summary>
    public sealed class PurgeImpact
    {
        /// <summary>Empty result helper.</summary>
        public static PurgeImpact Empty { get; } = new PurgeImpact();

        /// <summary>Total rows removed by the purge call.</summary>
        public int RemovedCount { get; set; }

        /// <summary>Chain ids whose contiguous sequence broke as a result of the purge.</summary>
        public ISet<string> AffectedChains { get; } =
            new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Returns <c>true</c> when the purge changed any data.</summary>
        public bool HasChanges => RemovedCount > 0 || AffectedChains.Count > 0;
    }
}
