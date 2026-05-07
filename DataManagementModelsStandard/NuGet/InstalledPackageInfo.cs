using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.NuGet
{
    /// <summary>
    /// Represents information about an installed package on the local system.
    /// </summary>
    public class InstalledPackageInfo
    {
        /// <summary>The package identifier.</summary>
        public string PackageId { get; set; }
        /// <summary>The installed version.</summary>
        public string Version { get; set; }
        /// <summary>The local installation directory path.</summary>
        public string InstallPath { get; set; }
        /// <summary>Date the package was installed.</summary>
        public DateTime InstalledDate { get; set; }
        /// <summary>Date of last update, if updated.</summary>
        public DateTime? LastUpdated { get; set; }
        /// <summary>Paths to loaded assemblies from this package.</summary>
        public List<string> LoadedAssemblyPaths { get; set; } = new List<string>();
        /// <summary>True if currently loaded in memory.</summary>
        public bool IsLoaded { get; set; }
        /// <summary>The source URL or path where the package was obtained.</summary>
        public string Source { get; set; }
        /// <summary>The source type (NuGet, Local, etc.).</summary>
        public string SourceType { get; set; }
        /// <summary>List of dependency package IDs.</summary>
        public List<string> Dependencies { get; set; } = new List<string>();
        /// <summary>Additional metadata key-value pairs.</summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
