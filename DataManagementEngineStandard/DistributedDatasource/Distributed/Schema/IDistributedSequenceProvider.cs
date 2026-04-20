namespace TheTechIdea.Beep.Distributed.Schema
{
    /// <summary>
    /// Client-side distributed sequence provider consulted by Phase 07
    /// writes when the active
    /// <see cref="Plan.DistributionMode.Sharded"/> placement has an
    /// identity column that cannot rely on per-shard auto-increment.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations MUST be thread-safe and collision-free across
    /// every node that calls them, typically by combining a unique
    /// node id with a monotonic counter (see
    /// <c>SnowflakeSequenceProvider</c>) or by reserving a batch of
    /// ids via a centralized allocator (<c>HiLoSequenceProvider</c>).
    /// </para>
    /// <para>
    /// The provider is opt-in; the default options wiring leaves it
    /// <c>null</c>, which means Phase 07 writes inherit the shard's
    /// native identity behaviour.
    /// </para>
    /// </remarks>
    public interface IDistributedSequenceProvider
    {
        /// <summary>
        /// Returns a new globally-unique <see cref="long"/> id for the
        /// given entity / column pair. Implementations may ignore the
        /// arguments when a single id space is sufficient.
        /// </summary>
        /// <param name="entityName">Entity the id is being generated for. Never <c>null</c>.</param>
        /// <param name="columnName">Column the id is being generated for. Never <c>null</c>.</param>
        long NextId(string entityName, string columnName);
    }
}
