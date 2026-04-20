using System;

namespace TheTechIdea.Beep.Distributed.Query
{
    /// <summary>
    /// Immutable "order by a single column" descriptor used by
    /// <see cref="QueryIntent"/> and honoured by the
    /// <see cref="QueryAwareResultMerger"/> k-way sort-merge paths.
    /// </summary>
    /// <remarks>
    /// The column name is matched case-insensitively against the row
    /// dictionary the merger builds per shard. If the column is
    /// missing the merger falls back to a stable union by shard order
    /// — sorting on an absent column would otherwise silently flip
    /// the semantics.
    /// </remarks>
    public sealed class OrderBySpec
    {
        /// <summary>Creates a new ordering spec.</summary>
        /// <param name="column">Column name to sort by.</param>
        /// <param name="direction">Ascending / descending.</param>
        public OrderBySpec(string column, OrderDirection direction = OrderDirection.Ascending)
        {
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Column name cannot be null or whitespace.", nameof(column));

            Column    = column;
            Direction = direction;
        }

        /// <summary>Column to sort by.</summary>
        public string Column { get; }

        /// <summary>Sort direction.</summary>
        public OrderDirection Direction { get; }

        /// <summary><c>true</c> when <see cref="Direction"/> is <see cref="OrderDirection.Descending"/>.</summary>
        public bool IsDescending => Direction == OrderDirection.Descending;

        /// <summary>Convenience factory for an ascending order.</summary>
        public static OrderBySpec Asc(string column)  => new OrderBySpec(column, OrderDirection.Ascending);

        /// <summary>Convenience factory for a descending order.</summary>
        public static OrderBySpec Desc(string column) => new OrderBySpec(column, OrderDirection.Descending);

        /// <inheritdoc/>
        public override string ToString() => $"{Column} {(IsDescending ? "DESC" : "ASC")}";
    }
}
