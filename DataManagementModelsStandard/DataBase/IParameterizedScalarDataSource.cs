using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Extensions;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Optional scalar execution contract; bind every definition parameter as data.</summary>
    public interface IParameterizedScalarDataSource
    {
        Task<object> GetScalarAsync(AppFilterQueryDefinition definition, CancellationToken cancellationToken = default);
    }
}
