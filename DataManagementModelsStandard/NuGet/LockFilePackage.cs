using System.Collections.Generic;

namespace TheTechIdea.Beep.NuGet
{
    /// <summary>
    /// Represents a single package entry in a lock file.
    /// </summary>
    public class LockFilePackage
    {
        /// <summary>The package identifier.</summary>
        public string PackageId { get; set; }
        /// <summary>The exact version.</summary>
        public string Version { get; set; }
        /// <summary>The source URL or path.</summary>
        public string Source { get; set; }
        /// <summary>List of dependency package IDs.</summary>
        public List<string> Dependencies { get; set; } = new List<string>();
    }
}
