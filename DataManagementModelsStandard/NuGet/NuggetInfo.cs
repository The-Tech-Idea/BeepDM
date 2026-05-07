using System;
using System.Collections.Generic;
using System.Reflection;

namespace TheTechIdea.Beep.NuGet
{
    /// <summary>
    /// Information about a loaded nugget in shared context
    /// </summary>
    public class NuggetInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public DateTime LoadedAt { get; set; }
        public List<Assembly> LoadedAssemblies { get; set; } = new();
        public string SourcePath { get; set; }
        public bool IsSharedContext { get; set; } = true;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public IEnumerable<PluginInfo> DiscoveredPlugins { get; set; }
    }
}
