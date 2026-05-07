using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.NuGet
{
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
}
