using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>An owned, drained read whose required column names and raw values are acknowledged by the provider.</summary>
    public interface IOwnedEntityProjectionDataSource
    {
        bool SupportsOwnedEntityProjections { get; }
        Task<EntityReadResult> ReadEntityProjectionAsync(string entityName, List<AppFilter> filters,
            IReadOnlyList<string> requiredColumns, CancellationToken cancellationToken = default);
    }
}
