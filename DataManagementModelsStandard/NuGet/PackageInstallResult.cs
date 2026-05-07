using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.NuGet
{
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
}
