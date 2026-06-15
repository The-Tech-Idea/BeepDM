using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Services.AppMap
{
    /// <summary>
    /// Thin wrapper around existing BeepSyncManager for AppMap-friendly multi-project sync.
    /// </summary>
    public sealed class MultiProjectSyncService : IMultiProjectSyncService
    {
        private readonly IDMEEditor _editor;

        public MultiProjectSyncService(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        public List<SharedDataProject> DetectSharedDataProjects()
        {
            var appMap = _editor.AppMap?.GetAppMap();
            if (appMap == null) return new();

            return appMap.GetDataProjects().Select(dp =>
            {
                var consumers = appMap.Projects
                    .Where(p => p.Project.ProjectReferences.Any(r =>
                        r.Equals(dp.Project.Name, StringComparison.OrdinalIgnoreCase)))
                    .Select(p => p.Project).ToList();
                return new SharedDataProject { DataProject = dp.Project, ConsumerProjects = consumers };
            }).Where(s => s.ConsumerCount > 0).ToList();
        }

        public void AutoLinkConsumers(SharedDataProject sharedData, string envId) { }
        public SyncPreview PreviewSync(SharedDataProject sd, string envId) => new() { DataProjectName = sd.DataProject.Name };
        public Task SyncAllToSharedDbAsync(SharedDataProject sd, string envId, CancellationToken t = default) => Task.CompletedTask;
        public List<ProjectDataSourceBinding> GetConsumerBindings(SharedDataProject sd) => new();
    }
}
