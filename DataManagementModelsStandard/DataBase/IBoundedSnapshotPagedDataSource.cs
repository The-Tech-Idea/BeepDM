using System;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Optional complete filtered traversal. One independent coherent native transaction
    /// owns the count and every bounded batch. It is not a sequence of independent page reads.
    /// Callbacks are synchronous, private-memory consumers: no provider I/O or partial publication.
    /// Completion includes native commit/reader/command/connection cleanup; cancellation never detaches work.</summary>
    public interface IBoundedSnapshotPagedDataSource : IBoundedPagedDataSource
    {
        /// <summary>Visits every bounded batch in one coherent native snapshot before acknowledging completion.</summary>
        Task<SnapshotReadCompletion> VisitSnapshotPagesAsync(string entityName, BoundedPageRequest firstPage,
            Action<BoundedPageResponse> consume, CancellationToken cancellationToken = default);
    }

    /// <summary>Correlated acknowledgement of a completely drained coherent traversal.</summary>
    public sealed class SnapshotReadCompletion
    {
        /// <summary>Traversal correlation copied from the admitted first request.</summary>
        public Guid RequestId { get; }
        /// <summary>Count of every matching row in the same native snapshot.</summary>
        public long TotalRecords { get; }
        /// <summary>Total rows privately consumed before completion.</summary>
        public long RowsVisited { get; }
        /// <summary>Bounded batches consumed, including one empty batch for an empty cohort.</summary>
        public long PagesVisited { get; }
        /// <summary>Copies a complete correlated traversal receipt.</summary>
        public SnapshotReadCompletion(Guid requestId, long totalRecords, long rowsVisited, long pagesVisited)
        {
            if(requestId==Guid.Empty || totalRecords<0 || rowsVisited!=totalRecords || pagesVisited<1)
                throw new ArgumentException("A complete correlated snapshot acknowledgement is required.");
            RequestId=requestId;TotalRecords=totalRecords;RowsVisited=rowsVisited;PagesVisited=pagesVisited;
        }
    }
}
