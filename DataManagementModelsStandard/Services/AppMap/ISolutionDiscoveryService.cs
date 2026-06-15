using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.AppMap;

namespace TheTechIdea.Beep.Services.AppMap
{
    public interface ISolutionDiscoveryService
    {
        Task<SolutionInfo?> DiscoverAsync(string path, DiscoveryOptions? options = null, CancellationToken token = default);
        Task<ProjectInfo?> DiscoverProjectAsync(string csprojPath, CancellationToken token = default);
        ProjectDependencyGraph BuildDependencyGraph(IReadOnlyList<ProjectInfo> projects);
        List<string> FindSolutionFiles(string directory, int maxDepth = 4);
    }
}
