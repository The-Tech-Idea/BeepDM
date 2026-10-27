using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Acknowledged row count; null means unknown, never zero work.</summary>
    public sealed class ConditionalMutationBatchResponse
    {
        /// <summary>Transaction correlation identifier.</summary>
        public Guid RequestId { get; }
        /// <summary>Physical transaction outcome.</summary>
        public ConditionalMutationBatchStatus Status { get; }
        /// <summary>Confirmed changed rows, or null for an uncertain outcome.</summary>
        public int? ChangedRows { get; }
        /// <summary>Full internal failure; never display to users.</summary>
        public Exception Failure { get; }
        /// <summary>Validates a physical acknowledgement up to the document ceiling; callers still require exact request correlation and operation count.</summary>
        public ConditionalMutationBatchResponse(Guid requestId, ConditionalMutationBatchStatus status, int? changedRows, Exception failure = null)
        {
            if (!Enum.IsDefined(typeof(ConditionalMutationBatchStatus), status)) throw new ArgumentOutOfRangeException(nameof(status));
            if (status == ConditionalMutationBatchStatus.Applied && changedRows is not (>= 1 and <= 1024) ||
                status == ConditionalMutationBatchStatus.Unconfirmed && changedRows != null ||
                (status is ConditionalMutationBatchStatus.NoMatch or ConditionalMutationBatchStatus.NotApplied or ConditionalMutationBatchStatus.RolledBack) && changedRows != 0)
                throw new ArgumentException("The acknowledgement count must match its physical outcome.");
            RequestId = requestId; Status = status; ChangedRows = changedRows; Failure = failure;
        }
    }
}
