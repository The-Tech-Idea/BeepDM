using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>Optional captured-record outcomes for platform-neutral editor popups.</summary>
    public interface IFormsEditorOutcomes
    {
        Task<FormEditorResult> ShowEditorWithOutcomeAsync(string blockName, string itemName,
            CancellationToken cancellationToken = default);
    }
}
