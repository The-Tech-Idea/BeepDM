using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>Optional managed query outcome/cancellation capability; not a raw SQL entry point.</summary>
    public interface IFormsQueryOutcomes
    {
        Task<FormQueryResult> ExecuteQueryWithOutcomeAsync(string blockName, List<AppFilter> filters = null,
            CancellationToken cancellationToken = default);
    }
}
