using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>Optional isolated mutation owner. Does not commit ordinary tracked changes or bypass tenant scope.</summary>
    public interface IConditionalUnitOfWorkMutation
    {
        /// <summary>Whether this clean owner supports guarded physical mutation.</summary>
        bool SupportsConditionalMutation { get; }
        /// <summary>Awaits the guarded mutation and validates its acknowledgement.</summary>
        Task<ConditionalMutationResponse> MutateConditionallyAsync(ConditionalMutationRequest request,
            CancellationToken cancellationToken = default);
    }
}
