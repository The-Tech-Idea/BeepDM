using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
namespace TheTechIdea.Beep.Editor
{
    /// <summary>Clean tenant-scoped owner of an optional atomic multi-entity mutation.</summary>
    public interface IConditionalUnitOfWorkBatchMutation
    {
        /// <summary>Whether the provider has the physical batch capability.</summary>
        bool SupportsConditionalMutationBatch { get; }
        /// <summary>Awaits and validates one atomic batch acknowledgement.</summary>
        Task<ConditionalMutationBatchResponse> MutateBatchConditionallyAsync(ConditionalMutationBatchRequest request, CancellationToken cancellationToken = default);
    }
}
