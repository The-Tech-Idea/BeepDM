namespace TheTechIdea.Beep.NuGet
{
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
}
