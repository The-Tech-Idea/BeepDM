using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>Optional policy-gated bounded provider paging, distinct from local cursor navigation.</summary>
    public interface IFormsProviderPaging
    {
        Task<FormQueryResult> FetchPageWithOutcomeAsync(string blockName, BoundedPageRequest request,
            CancellationToken cancellationToken = default);
    }
}
