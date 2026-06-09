using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TheTechIdea.Beep.Editor.EntityDiscovery
{
    public class DiscoveredEntity
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AssemblyName { get; set; } = string.Empty;
        public int PropertyCount { get; set; }
        public EntityCategory Category { get; set; } = EntityCategory.Poco;
        public string Namespace { get; set; } = string.Empty;
        public bool HasParameterlessConstructor { get; set; }
    }

    public enum EntityCategory
    {
        Entity,
        Poco,
        EfCore,
        Unknown
    }
}
