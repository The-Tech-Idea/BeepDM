using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    /// <summary>
    /// Serializable manifest describing a plugin package and its capabilities.
    /// </summary>
    public class PluginManifest
    {
        /// <summary>Gets or sets the logical plugin identifier.</summary>
        public string Id { get; set; }
        /// <summary>Gets or sets the display name of the plugin.</summary>
        public string Name { get; set; }
        /// <summary>Gets or sets the plugin version.</summary>
        public string Version { get; set; }
        /// <summary>Gets or sets the fully qualified entry type name used to activate the plugin.</summary>
        public string EntryType { get; set; }
        /// <summary>Gets or sets the source package or installation origin.</summary>
        public string Source { get; set; }
        /// <summary>Gets or sets whether the plugin package is signed.</summary>
        public bool Signed { get; set; }
        /// <summary>Gets or sets the declared plugin capabilities.</summary>
        public List<string> Capabilities { get; set; } = new List<string>();
    }
}
