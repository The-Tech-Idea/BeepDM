using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Services.AppMap
{
    /// <summary>
    /// Detects shared Data projects and syncs entity definitions
    /// across multiple consumer projects to a single database.
    /// </summary>
    public interface IMultiProjectSyncService
    {
        /// <summary>Find all Data-role projects and their consumers in an AppMap.</summary>
        List<SharedDataProject> DetectSharedDataProjects();

        /// <summary>Auto-link consumer projects to the shared database.</summary>
        void AutoLinkConsumers(SharedDataProject sharedData, string environmentId);

        /// <summary>Preview what would be synced.</summary>
        SyncPreview PreviewSync(SharedDataProject sharedData, string environmentId);

        /// <summary>Apply schema to all consumer datasources.</summary>
        Task SyncAllToSharedDbAsync(SharedDataProject sharedData, string environmentId, CancellationToken token = default);

        /// <summary>Get current bindings for a shared data project.</summary>
        List<ProjectDataSourceBinding> GetConsumerBindings(SharedDataProject sharedData);
    }
}
