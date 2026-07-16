using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.SetUp.State
{
    /// <summary>
    /// The minimal ETag-versioned key/value transport that <c>RemoteSetupStateStore</c> builds
    /// optimistic concurrency and leases on top of.
    /// </summary>
    /// <remarks>
    /// Kept behind an interface so the concurrency logic is testable with an in-memory fake, and so
    /// a team can back it with any store (HTTP, blob, table) without reimplementing the semantics.
    /// </remarks>
    public interface ISetupStateTransport
    {
        /// <summary>Reads a resource, or null when it does not exist.</summary>
        Task<TransportEntry> GetAsync(string resourceId, CancellationToken token = default);

        /// <summary>
        /// Conditionally writes a resource and returns the new ETag.
        /// </summary>
        /// <param name="ifMatchETag">
        /// The ETag the caller last saw: null means "create only if absent". A mismatch — the
        /// resource changed, or already exists when create was expected — throws
        /// <see cref="SetupStateConflictException"/>.
        /// </param>
        Task<string> PutAsync(string resourceId, string body, string ifMatchETag,
            CancellationToken token = default);

        /// <summary>Deletes a resource if its ETag still matches. A mismatch is ignored.</summary>
        Task DeleteAsync(string resourceId, string ifMatchETag, CancellationToken token = default);
    }

    /// <summary>A resource body plus its current ETag.</summary>
    public sealed class TransportEntry
    {
        public TransportEntry(string body, string etag)
        {
            Body = body;
            ETag = etag;
        }

        public string Body { get; }
        public string ETag { get; }
    }
}
