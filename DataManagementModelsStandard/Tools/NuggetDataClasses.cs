using System;
using System.Collections.Generic;
using System.Reflection;

namespace TheTechIdea.Beep.Tools
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
        public List<PluginInfo> DiscoveredPlugins { get; set; } = new();
        public string SourcePath { get; set; }
        public bool IsSharedContext { get; set; } = true;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Event arguments for nugget operations
    /// </summary>
    public class NuggetEventArgs : EventArgs
    {
        public NuggetInfo NuggetInfo { get; set; }
        public string Message { get; set; }
        public Exception Error { get; set; }
    }

    /// <summary>
    /// Event arguments for plugin health status changes
    /// </summary>
    public class PluginHealthEventArgs : EventArgs
    {
        public string PluginId { get; set; }
        public PluginHealth PreviousHealth { get; set; }
        public PluginHealth CurrentHealth { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event arguments for plugin resource violations
    /// </summary>
    public class PluginResourceEventArgs : EventArgs
    {
        public string PluginId { get; set; }
        public List<string> Violations { get; set; } = new();
        public object Usage { get; set; }
        public object Limits { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

   
}