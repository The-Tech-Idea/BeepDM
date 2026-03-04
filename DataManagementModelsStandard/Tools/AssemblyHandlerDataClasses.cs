using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// Represents a NuGet package search result
    /// </summary>
    public class NuGetSearchResult
    {
        public string PackageId { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Authors { get; set; }
        public long TotalDownloads { get; set; }
        public string IconUrl { get; set; }
        public string ProjectUrl { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }

    /// <summary>
    /// Configuration for a NuGet package source
    /// </summary>
    public class NuGetSourceConfig
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public bool IsEnabled { get; set; } = true;
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Maps a driver class to the NuGet package that provides it
    /// </summary>
    public class DriverPackageMapping
    {
        public string PackageId { get; set; }
        public string Version { get; set; }
        public string DriverClassName { get; set; }
        public DataSourceType DataSourceType { get; set; }
        public DateTime InstalledDate { get; set; } = DateTime.UtcNow;
        public string InstallPath { get; set; }
    }

    /// <summary>
    /// Statistics about assembly loading operations
    /// </summary>
    public class AssemblyLoadStatistics
    {
        public int TotalAssembliesLoaded { get; set; }
        public int TotalAssembliesFailed { get; set; }
        public int DriversFound { get; set; }
        public int DataSourcesFound { get; set; }
        public int AddinsFound { get; set; }
        public int NuGetPackagesLoaded { get; set; }
        public int NuGetPackagesFailed { get; set; }
        public TimeSpan TotalLoadTime { get; set; }
        public DateTime LastLoadTimestamp { get; set; }
        public Dictionary<string, int> AssembliesByFolderType { get; set; } = new Dictionary<string, int>();
        public List<string> FailedAssemblyPaths { get; set; } = new List<string>();
    }
}
