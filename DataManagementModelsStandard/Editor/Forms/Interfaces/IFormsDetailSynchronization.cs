using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    public interface IFormsDetailSynchronization
    {
        Task<DetailSynchronizationResult> SynchronizeDetailBlocksWithOutcomeAsync(string masterBlockName,
            CancellationToken cancellationToken = default);
        Task<DetailSynchronizationResult> SynchronizeDeferredDetailWithOutcomeAsync(string masterBlockName,
            string detailBlockName, CancellationToken cancellationToken = default);
    }
}
