using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    public interface IFormsCommitOutcomes
    {
        Task<FormCommitResult> CommitFormWithOutcomeAsync();
    }
}
