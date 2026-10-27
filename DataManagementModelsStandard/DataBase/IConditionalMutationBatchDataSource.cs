using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Optional bounded multi-entity physical transaction; no ordinary-write fallback.</summary>
    public interface IConditionalMutationBatchDataSource
    {
        /// <summary>Awaits all guarded changes and one transaction acknowledgement.</summary>
        Task<ConditionalMutationBatchResponse> MutateBatchConditionallyAsync(ConditionalMutationBatchRequest request, CancellationToken cancellationToken = default);
    }
}
