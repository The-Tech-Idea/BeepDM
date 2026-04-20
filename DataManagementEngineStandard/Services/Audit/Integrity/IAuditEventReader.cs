using System.Collections.Generic;
using TheTechIdea.Beep.Services.Audit.Models;

namespace TheTechIdea.Beep.Services.Audit.Integrity
{
    /// <summary>
    /// Source from which the verifier loads a previously persisted
    /// chain. File-, SQLite-, and Cosmos-backed implementations all
    /// satisfy this contract; the verifier itself is storage-agnostic.
    /// </summary>
    public interface IAuditEventReader
    {
        /// <summary>
        /// Streams every event for <paramref name="chainId"/> in
        /// strict <c>Sequence</c> order (1, 2, 3, …). Implementations
        /// should yield to keep memory bounded for large chains.
        /// </summary>
        IEnumerable<AuditEvent> ReadChain(string chainId);
    }
}
