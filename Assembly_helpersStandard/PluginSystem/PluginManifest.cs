using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    public class PluginManifest
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string EntryType { get; set; }
        public string Source { get; set; }
        public bool Signed { get; set; }
        public List<string> Capabilities { get; set; } = new List<string>();
    }
}
