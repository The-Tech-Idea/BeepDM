using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Environments;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Services.AppMap
{
    public interface IEnvironmentManagementService
    {
        List<AppEnvironment> GetStandardTiers();
        List<ProjectEnvironmentProfile> GetAllProfilesForProject(string projectName);
        ProjectEnvironmentProfile? GetProjectProfile(string projectName, string environmentId);
        void SetProjectProfile(ProjectEnvironmentProfile profile);
        void ApplyStandardProfile(string projectName);
        ProjectEnvironmentProfile PromoteConfig(string projectName, EnvironmentTier fromTier, EnvironmentTier toTier);
        List<ProjectEnvironmentProfile> SwitchEnvironment(List<string> projectNames, string fromEnvId, string toEnvId);
        bool DeleteProfile(string projectName, string environmentId);
        Task SaveAsync(CancellationToken token = default);
        Task LoadAsync(CancellationToken token = default);
    }
}
