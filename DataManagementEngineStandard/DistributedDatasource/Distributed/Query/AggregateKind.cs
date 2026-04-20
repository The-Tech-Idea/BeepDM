namespace TheTechIdea.Beep.Distributed.Query
{
    /// <summary>
    /// Supported aggregate functions for the Phase 08 planner. All
    /// aggregates are commutative / associative so they can be pushed
    /// to every shard and re-folded by
    /// <see cref="QueryAwareResultMerger"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Avg"/> is special: the planner rewrites it into a
    /// <see cref="Sum"/> and <see cref="Count"/> pair on the per-shard
    /// sub-intent, and the merger divides after combining the pair so
    /// the global average is always <c>total_sum / total_count</c>.
    /// </remarks>
    public enum AggregateKind
    {
        /// <summary><c>COUNT(*)</c> / <c>COUNT(col)</c>.</summary>
        Count = 0,

        /// <summary><c>SUM(col)</c>.</summary>
        Sum = 1,

        /// <summary><c>MIN(col)</c>.</summary>
        Min = 2,

        /// <summary><c>MAX(col)</c>.</summary>
        Max = 3,

        /// <summary><c>AVG(col)</c>. Rewritten to Sum/Count pair.</summary>
        Avg = 4,
    }
}
