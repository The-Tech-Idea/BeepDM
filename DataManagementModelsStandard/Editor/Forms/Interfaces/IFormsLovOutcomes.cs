using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    public interface IFormsLovOutcomes
    {
        Task<FormLovResult> ShowLOVWithOutcomeAsync(string blockName, string fieldName, string searchText = null,
            object selectedRecord = null, CancellationToken cancellationToken = default);
    }
}
