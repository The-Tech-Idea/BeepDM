using System.Collections.Generic;
using TheTechIdea.Beep.Distributed.Plan;

namespace TheTechIdea.Beep.Distributed.Partitioning
{
    /// <summary>
    /// Contract for a row-level partition function. Implementations
    /// take a <see cref="PartitionInput"/> (entity name + key values)
    /// and return the shard ids a row should be placed on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All implementations MUST be pure: no I/O, no state mutation,
    /// no time- or thread-dependent behaviour. The same input must
    /// always produce the same output for a given function instance.
    /// This is what makes the resharding logic in Phase 11 cheap —
    /// only inputs whose hash slot moves need to be rewritten.
    /// </para>
    /// <para>
    /// Most functions return exactly one shard id. The contract uses
    /// <see cref="IReadOnlyList{T}"/> to allow
    /// <see cref="CompositePartitionFunction"/> to return the union
    /// of multiple inner-function outputs without boxing.
    /// </para>
    /// </remarks>
    public interface IPartitionFunction
    {
        /// <summary>Kind tag matching the originating <see cref="PartitionFunctionRef.Kind"/>.</summary>
        PartitionKind Kind { get; }

        /// <summary>Ordered list of key columns this function reads from <see cref="PartitionInput.KeyValues"/>.</summary>
        IReadOnlyList<string> KeyColumns { get; }

        /// <summary>
        /// Returns the shard ids the input row should land on. The
        /// result is never <c>null</c>; an empty list indicates "no
        /// candidate shard found" and callers must handle it (raise a
        /// placement violation, fall back to broadcast, etc.).
        /// </summary>
        IReadOnlyList<string> Resolve(PartitionInput input);
    }
}
