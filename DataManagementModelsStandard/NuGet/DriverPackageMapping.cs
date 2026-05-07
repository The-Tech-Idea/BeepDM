using System;

namespace TheTechIdea.Beep.NuGet
{
    /// <summary>
    /// Maps a driver class to the NuGet package that provides it
    /// </summary>
    public class DriverPackageMapping
    {
        public string PackageId { get; set; }
        public string Version { get; set; }
        public string DriverClassName { get; set; }
        public string DataSourceType { get; set; }
        public DateTime InstalledDate { get; set; } = DateTime.UtcNow;
        public string InstallPath { get; set; }
    }
}
