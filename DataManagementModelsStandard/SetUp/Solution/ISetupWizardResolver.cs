using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.SetUp.State;

namespace TheTechIdea.Beep.SetUp.Solution
{
    /// <summary>
    /// Builds the setup wizard for one app in a solution, from that app's
    /// <see cref="AppDefinition.SetupDefinitionPath"/>.
    /// </summary>
    /// <remarks>
    /// The wizard is keyed by <c>SetupStateKey(wizardId, environment, appId)</c>, so several apps
    /// sharing one state store don't collide. Returns null when the app has no setup definition.
    /// </remarks>
    public interface ISetupWizardResolver
    {
        Task<ISetupWizard> ResolveAsync(AppDefinition app, string environmentId,
            CancellationToken token = default);

        /// <summary>
        /// The state key an app's wizard would use, without building the wizard — so a status view
        /// can read persisted state. Null when the app has no setup definition.
        /// </summary>
        Task<SetupStateKey> GetStateKeyAsync(AppDefinition app, string environmentId,
            CancellationToken token = default);
    }
}
