using System;
using System.Collections.Generic;
using System.IO;

using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.NuGetManagement
{
    /// <summary>
    /// Configuration options for NuGetPackageManager behavior.
    /// </summary>
    public class NuGetManagerConfig
    {
        /// <summary>
        /// Default installation directory for packages.
        /// </summary>
        public string InstallDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "Plugins");
        
        /// <summary>
        /// Path to the global NuGet cache directory.
        /// </summary>
        public string CachePath { get; set; }
        
        /// <summary>
        /// Maximum cache size in megabytes.
        /// </summary>
        public long MaxCacheSizeMB { get; set; } = 1024;
        
        /// <summary>
        /// Whether to enable automatic cache cleanup.
        /// </summary>
        public bool AutoCleanup { get; set; } = true;
        
        /// <summary>
        /// Number of days to keep packages in cache before cleanup.
        /// </summary>
        public int CacheRetentionDays { get; set; } = 30;
        
        /// <summary>
        /// Whether to prefer loading packages into shared context.
        /// </summary>
        public bool PreferSharedContext { get; set; } = true;
        
        /// <summary>
        /// Whether to include prerelease versions in searches and updates.
        /// </summary>
        public bool IncludePrerelease { get; set; } = false;
        
        /// <summary>
        /// Whether to check for package vulnerabilities before installation.
        /// </summary>
        public bool CheckVulnerabilities { get; set; } = true;
        
        /// <summary>
        /// Whether to require signed packages.
        /// </summary>
        public bool RequireSignedPackages { get; set; } = false;
        
        /// <summary>
        /// Whether to allow untrusted certificates during signature verification.
        /// </summary>
        public bool AllowUntrustedCertificates { get; set; } = false;
        
        /// <summary>
        /// Whether to generate lock files after package operations.
        /// </summary>
        public bool AutoGenerateLockFile { get; set; } = false;
        
        /// <summary>
        /// Default timeout for network operations in seconds.
        /// </summary>
        public int NetworkTimeoutSeconds { get; set; } = 30;
        
        /// <summary>
        /// Number of retry attempts for failed downloads.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;
        
        /// <summary>
        /// List of default NuGet sources.
        /// </summary>
        public List<NuGetSourceSetting> DefaultSources { get; set; } = new List<NuGetSourceSetting>
        {
            new NuGetSourceSetting { Name = "nuget.org", Url = "https://api.nuget.org/v3/index.json", Enabled = true }
        };
    }

    /// <summary>
    /// Represents a NuGet source configuration entry.
    /// </summary>
    public class NuGetSourceSetting
    {
        /// <summary>The source name.</summary>
        public string Name { get; set; }
        /// <summary>The source URL or path.</summary>
        public string Url { get; set; }
        /// <summary>Whether the source is enabled.</summary>
        public bool Enabled { get; set; } = true;
        /// <summary>Source priority (lower is higher priority).</summary>
        public int Priority { get; set; } = 100;
        /// <summary>Username for authenticated feeds.</summary>
        public string Username { get; set; }
        /// <summary>Password for authenticated feeds.</summary>
        public string Password { get; set; }
    }

  
}
