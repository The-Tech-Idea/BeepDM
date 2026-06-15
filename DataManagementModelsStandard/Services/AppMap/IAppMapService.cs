using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Utilities;
using AppMapModel = TheTechIdea.Beep.AppMap.AppMap;
using DiscoveryOptions = TheTechIdea.Beep.AppMap.DiscoveryOptions;
using ProjectRoleAssignment = TheTechIdea.Beep.AppMap.ProjectRoleAssignment;

namespace TheTechIdea.Beep.Services.AppMap
{
    public interface IAppMapService
    {
        Task<AppMapModel?> CreateAppMapAsync(string solutionPath, DiscoveryOptions? options = null, CancellationToken token = default);
        AppMapModel? GetAppMap();
        Task<AppMapModel?> LoadAsync(CancellationToken token = default);
        void SetRole(string projectName, ProjectRole role);
        List<ProjectRoleAssignment> GetProjectsByRole(ProjectRole role);
        List<string> GetProjectDependencies(string projectName);
        Task SaveAsync(CancellationToken token = default);
        void RedetectRoles();
    }
}
