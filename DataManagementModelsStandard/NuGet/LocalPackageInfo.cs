using System;

namespace TheTechIdea.Beep.NuGet
{
    /// <summary>
    /// Represents a local .nupkg file found in a directory scan.
    /// </summary>
    public class LocalPackageInfo
    {
        /// <summary>Full path to the .nupkg file.</summary>
        public string FilePath { get; set; }
        /// <summary>The package identifier.</summary>
        public string PackageId { get; set; }
        /// <summary>The package version.</summary>
        public string Version { get; set; }
        /// <summary>File size in bytes.</summary>
        public long FileSize { get; set; }
        /// <summary>Last modified date.</summary>
        public DateTime ModifiedDate { get; set; }
    }
}
