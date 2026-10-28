using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
namespace TheTechIdea.Beep.Editor
{
    /// <summary>Optional preparation through the staged, scoped stored-column read owner.</summary>
    public interface IUnitofWorkStoredProjectionGuardRead
    {
        /// <summary>Whether the underlying owner and provider support bounded stored projections.</summary>
        bool SupportsStoredProjectionGuardRead { get; }
        /// <summary>Prepare a complete cohort without publishing partial entities or granting write authority.</summary>
        Task<ConditionalStoredProjectionGuard> PrepareStoredProjectionGuardAsync(BoundedPageRequest request, IEnumerable<string> fields, CancellationToken cancellationToken = default);
    }
    /// <summary>Optional transaction capability; no ordinary batch fallback is permitted.</summary>
    public interface IUnitofWorkProjectionGuardedBatch
    {
        /// <summary>Whether this clean canonical owner can enforce projection source guards.</summary>
        bool SupportsProjectionGuardedBatch { get; }
        /// <summary>Await the exact atomic batch acknowledgement or its explicit uncertain outcome.</summary>
        Task<ConditionalMutationBatchResponse> MutateProjectionGuardedBatchAsync(ProjectionGuardedMutationBatchRequest request, CancellationToken cancellationToken = default);
    }
}
