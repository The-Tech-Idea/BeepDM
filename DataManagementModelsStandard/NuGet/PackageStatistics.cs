using System;

namespace TheTechIdea.Beep.NuGet
{
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
}
