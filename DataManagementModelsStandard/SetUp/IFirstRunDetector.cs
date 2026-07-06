using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Detects whether this is a first-run install and tracks setup completion.
    /// Implementations live in the engine project (e.g. <c>FileBasedFirstRunDetector</c>).
    /// </summary>
    public interface IFirstRunDetector
    {
        Task<bool> IsFirstRunAsync();
        Task MarkSetupCompleteAsync();
        Task ClearSetupFlagAsync();
        bool WasSetupCompleted { get; }
    }
}
