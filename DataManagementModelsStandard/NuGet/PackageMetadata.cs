using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.NuGet
{
    /// <summary>
    /// Represents detailed metadata for a NuGet package version.
    /// </summary>
    public class PackageMetadata
    {
        /// <summary>The package identifier (e.g., "Newtonsoft.Json").</summary>
        public string PackageId { get; set; }
        /// <summary>The normalized version string.</summary>
        public string Version { get; set; }
        /// <summary>The package description from the nuspec.</summary>
        public string Description { get; set; }
        /// <summary>The package authors or owners.</summary>
        public string Authors { get; set; }
        /// <summary>URL to the package icon.</summary>
        public string IconUrl { get; set; }
        /// <summary>URL to the project website.</summary>
        public string ProjectUrl { get; set; }
        /// <summary>URL to the license information.</summary>
        public string LicenseUrl { get; set; }
        /// <summary>The license type (e.g., MIT, Apache-2.0).</summary>
        public string LicenseType { get; set; }
        /// <summary>List of tags associated with the package.</summary>
        public List<string> Tags { get; set; } = new List<string>();
        /// <summary>Total download count from NuGet.org.</summary>
        public long? DownloadCount { get; set; }
        /// <summary>Date the package was published.</summary>
        public DateTime? Published { get; set; }
        /// <summary>True if this is a prerelease version.</summary>
        public bool IsPrerelease { get; set; }
        /// <summary>List of package dependencies.</summary>
        public List<PackageDependency> Dependencies { get; set; } = new List<PackageDependency>();
        /// <summary>The target framework for this package.</summary>
        public string TargetFramework { get; set; }
        /// <summary>The package file size in bytes.</summary>
        public long? PackageSize { get; set; }
    }
}
