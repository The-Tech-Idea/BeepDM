using System;

namespace TheTechIdea.Beep.NuGet
{
    /// <summary>
    /// Represents a single cached package entry.
    /// </summary>
    public class CachedPackageInfo
    {
        /// <summary>The package identifier.</summary>
        public string PackageId { get; set; }
        /// <summary>The cached version.</summary>
        public string Version { get; set; }
        /// <summary>The cache directory path.</summary>
        public string Path { get; set; }
        /// <summary>Package size in bytes.</summary>
        public long SizeBytes { get; set; }
        /// <summary>Date the package was added to cache.</summary>
        public DateTime CachedDate { get; set; }
        /// <summary>Date of last access.</summary>
        public DateTime LastAccessed { get; set; }
    }
}
