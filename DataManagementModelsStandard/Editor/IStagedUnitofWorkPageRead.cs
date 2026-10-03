using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>Optional staged bounded provider read. Never fall back to unbounded Get.</summary>
    public interface IStagedUnitofWorkPageRead
    {
        bool SupportsStagedPageRead { get; }
        Task<IUnitofWorkPageReadStage> PreparePageReadAsync(BoundedPageRequest request,
            CancellationToken cancellationToken = default);
    }

    public interface IUnitofWorkPageReadStage : IUnitofWorkReadStage, IUnitofWorkReadBufferStage
    {
        ProviderPageInfo Page { get; }
    }
}
