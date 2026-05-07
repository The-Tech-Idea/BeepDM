using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.NuGet
{
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
