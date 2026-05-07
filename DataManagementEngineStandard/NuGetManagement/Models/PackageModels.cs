using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.NuGetManagement.Models
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

    /// <summary>
    /// Represents a dependency relationship between packages.
    /// </summary>
    public class PackageDependency
    {
        /// <summary>The dependent package identifier.</summary>
        public string PackageId { get; set; }
        /// <summary>The version range constraint (e.g., "[1.0.0, 2.0.0)").</summary>
        public string VersionRange { get; set; }
        /// <summary>The target framework for this dependency group.</summary>
        public string TargetFramework { get; set; }
        /// <summary>True if this is an optional dependency.</summary>
        public bool IsOptional { get; set; }
    }

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

    /// <summary>
    /// Represents the result of a package installation operation.
    /// </summary>
    public class PackageInstallResult
    {
        /// <summary>True if the operation succeeded.</summary>
        public bool Success { get; set; }
        /// <summary>The package identifier.</summary>
        public string PackageId { get; set; }
        /// <summary>The installed or downloaded version.</summary>
        public string Version { get; set; }
        /// <summary>The installation directory path.</summary>
        public string InstallPath { get; set; }
        /// <summary>List of relative paths of installed files.</summary>
        public List<string> InstalledFiles { get; set; } = new List<string>();
        /// <summary>List of non-fatal warnings during installation.</summary>
        public List<string> Warnings { get; set; } = new List<string>();
        /// <summary>Error message if the operation failed.</summary>
        public string Error { get; set; }
        /// <summary>UTC timestamp when the operation completed.</summary>
        public DateTime CompletedAt { get; set; }
        /// <summary>Operation duration in milliseconds.</summary>
        public long DurationMs { get; set; }
    }

    /// <summary>
    /// Represents the result of a package update operation.
    /// </summary>
    public class PackageUpdateResult
    {
        /// <summary>True if the update operation succeeded.</summary>
        public bool Success { get; set; }
        /// <summary>The package identifier.</summary>
        public string PackageId { get; set; }
        /// <summary>The previous installed version.</summary>
        public string OldVersion { get; set; }
        /// <summary>The new installed version.</summary>
        public string NewVersion { get; set; }
        /// <summary>True if the package was actually updated (not already latest).</summary>
        public bool WasUpdated { get; set; }
        /// <summary>Error message if the update failed.</summary>
        public string Error { get; set; }
        /// <summary>List of non-fatal warnings during update.</summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents a single search result from a NuGet query.
    /// </summary>
    public class PackageSearchResult
    {
        /// <summary>The package identifier.</summary>
        public string PackageId { get; set; }
        /// <summary>The latest version found.</summary>
        public string Version { get; set; }
        /// <summary>The package description.</summary>
        public string Description { get; set; }
        /// <summary>The package authors.</summary>
        public string Authors { get; set; }
        /// <summary>Total download count.</summary>
        public long? DownloadCount { get; set; }
        /// <summary>URL to the package icon.</summary>
        public string IconUrl { get; set; }
        /// <summary>URL to the project website.</summary>
        public string ProjectUrl { get; set; }
        /// <summary>List of tags.</summary>
        public List<string> Tags { get; set; } = new List<string>();
        /// <summary>True if this is a prerelease version.</summary>
        public bool IsPrerelease { get; set; }
        /// <summary>The source URL where this result was found.</summary>
        public string Source { get; set; }
    }

    /// <summary>
    /// Represents information about the NuGet cache state.
    /// </summary>
    public class CacheInfo
    {
        /// <summary>The root cache directory path.</summary>
        public string CachePath { get; set; }
        /// <summary>Total size of all cached packages in bytes.</summary>
        public long TotalSizeBytes { get; set; }
        /// <summary>Number of packages in cache.</summary>
        public int PackageCount { get; set; }
        /// <summary>Maximum allowed cache size in bytes.</summary>
        public long MaxSizeBytes { get; set; }
        /// <summary>True if automatic cleanup is enabled.</summary>
        public bool AutoCleanup { get; set; }
    }

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

    /// <summary>
    /// Represents a known security vulnerability in a package.
    /// </summary>
    public class VulnerabilityInfo
    {
        /// <summary>The affected package identifier.</summary>
        public string PackageId { get; set; }
        /// <summary>The affected version.</summary>
        public string Version { get; set; }
        /// <summary>URL to the security advisory.</summary>
        public string AdvisoryUrl { get; set; }
        /// <summary>Severity level (Critical, High, Moderate, Low).</summary>
        public string Severity { get; set; }
        /// <summary>Description of the vulnerability.</summary>
        public string Description { get; set; }
        /// <summary>Date the vulnerability was published.</summary>
        public DateTime? Published { get; set; }
    }

    /// <summary>
    /// Represents usage statistics for an installed package.
    /// </summary>
    public class PackageStatistics
    {
        /// <summary>The package identifier.</summary>
        public string PackageId { get; set; }
        /// <summary>Total number of downloads.</summary>
        public int TotalDownloads { get; set; }
        /// <summary>Date first installed.</summary>
        public DateTime? FirstInstalled { get; set; }
        /// <summary>Date last used.</summary>
        public DateTime? LastUsed { get; set; }
        /// <summary>Number of times the package has been loaded.</summary>
        public int LoadCount { get; set; }
        /// <summary>Total time spent loading in milliseconds.</summary>
        public long TotalLoadTimeMs { get; set; }
    }

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

    /// <summary>
    /// Represents a package installation request for bulk operations.
    /// </summary>
    public class PackageRequest
    {
        /// <summary>The package identifier.</summary>
        public string PackageId { get; set; }
        /// <summary>The desired version. If null, latest is used.</summary>
        public string Version { get; set; }
        /// <summary>Optional specific source to use.</summary>
        public string Source { get; set; }
    }

    /// <summary>
    /// Represents the result of a bulk install operation.
    /// </summary>
    public class BulkInstallResult
    {
        /// <summary>Total number of packages requested.</summary>
        public int TotalRequested { get; set; }
        /// <summary>Number of successful installations.</summary>
        public int Successful { get; set; }
        /// <summary>Number of failed installations.</summary>
        public int Failed { get; set; }
        /// <summary>Error message if the bulk operation failed.</summary>
        public string Error { get; set; }
        /// <summary>Detailed results for each package.</summary>
        public List<PackageInstallResult> Results { get; set; } = new List<PackageInstallResult>();
    }

    /// <summary>
    /// Represents the result of a bulk update operation.
    /// </summary>
    public class BulkUpdateResult
    {
        /// <summary>Total packages checked for updates.</summary>
        public int TotalChecked { get; set; }
        /// <summary>Number of packages that were updated.</summary>
        public int Updated { get; set; }
        /// <summary>Number of packages already at latest version.</summary>
        public int AlreadyLatest { get; set; }
        /// <summary>Number of failed update attempts.</summary>
        public int Failed { get; set; }
        /// <summary>Error message if the bulk operation failed.</summary>
        public string Error { get; set; }
        /// <summary>Detailed results for each package.</summary>
        public List<PackageUpdateResult> Results { get; set; } = new List<PackageUpdateResult>();
    }

    /// <summary>
    /// Represents the result of a bulk uninstall operation.
    /// </summary>
    public class BulkUninstallResult
    {
        /// <summary>Total packages requested for uninstallation.</summary>
        public int TotalRequested { get; set; }
        /// <summary>Number of successful uninstallations.</summary>
        public int Successful { get; set; }
        /// <summary>Number of failed uninstallations.</summary>
        public int Failed { get; set; }
        /// <summary>Error messages for failed operations.</summary>
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Options for controlling NuGet package search behavior.
    /// </summary>
    public class SearchOptions
    {
        /// <summary>Number of results to skip (for pagination).</summary>
        public int Skip { get; set; } = 0;
        /// <summary>Maximum number of results to return.</summary>
        public int Take { get; set; } = 20;
        /// <summary>True to include prerelease versions.</summary>
        public bool IncludePrerelease { get; set; } = false;
        /// <summary>Optional specific source to search.</summary>
        public string Source { get; set; }
        /// <summary>Optional target framework filter.</summary>
        public string Framework { get; set; }
        /// <summary>True for exact name matching instead of substring search.</summary>
        public bool ExactMatch { get; set; } = false;
    }

    /// <summary>
    /// Event arguments for NuGet package lifecycle events.
    /// </summary>
    public class PackageEventArgs : EventArgs
    {
        /// <summary>The package identifier.</summary>
        public string PackageId { get; set; }
        /// <summary>The package version.</summary>
        public string Version { get; set; }
        /// <summary>Human-readable event message.</summary>
        public string Message { get; set; }
        /// <summary>UTC timestamp when the event occurred.</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents a lock file for deterministic package restore.
    /// </summary>
    public class LockFileData
    {
        /// <summary>UTC timestamp when the lock file was generated.</summary>
        public DateTime GeneratedAt { get; set; }
        /// <summary>List of packages with exact versions.</summary>
        public List<LockFilePackage> Packages { get; set; } = new List<LockFilePackage>();
    }

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
