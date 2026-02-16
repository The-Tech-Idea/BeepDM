using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Controls the order in which entity states are committed.
    /// Default: DeletesFirst to avoid FK constraint violations.
    /// </summary>
    public enum CommitOrder
    {
        /// <summary>Deletes → Updates → Inserts (safest for FK constraints).</summary>
        DeletesFirst,
        /// <summary>Inserts → Updates → Deletes.</summary>
        InsertsFirst,
        /// <summary>Process in the order items appear in tracking.</summary>
        AsTracked
    }

    /// <summary>
    /// Result of committing a single item within a batch commit.
    /// </summary>
    public class CommitItemResult
    {
        /// <summary>The item that was committed (or attempted).</summary>
        public object Item { get; set; }

        /// <summary>The entity state at the time of commit.</summary>
        public EntityState EntityState { get; set; }

        /// <summary>True if the individual commit succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Error information if the commit failed.</summary>
        public IErrorsInfo Errors { get; set; }
    }

    /// <summary>
    /// Aggregate result returned by CommitAllAsync.
    /// </summary>
    public class CommitResult
    {
        /// <summary>Per-item commit results.</summary>
        public List<CommitItemResult> Results { get; set; } = new List<CommitItemResult>();

        /// <summary>True if ALL items committed successfully.</summary>
        public bool AllSucceeded => Results.All(r => r.Success);

        /// <summary>Number of successfully committed items.</summary>
        public int SuccessCount => Results.Count(r => r.Success);

        /// <summary>Number of failed items.</summary>
        public int FailedCount => Results.Count(r => !r.Success);

        /// <summary>Total items processed.</summary>
        public int TotalCount => Results.Count;

        /// <summary>Returns only the failed results.</summary>
        public List<CommitItemResult> FailedResults => Results.Where(r => !r.Success).ToList();
    }
}
