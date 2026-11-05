using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Optional counted snapshot of explicitly selected stored scalar columns.
    /// Never materializes or claims a complete entity, and never evaluates calculated getters.</summary>
    public interface IBoundedStoredProjectionDataSource : IBoundedPagedDataSource
    {
        Task<BoundedPageResponse> ReadStoredProjectionPageAsync(string entityName, BoundedPageRequest request,
            IReadOnlyList<string> fields, CancellationToken cancellationToken = default);
    }
}
