using System.Collections.Generic;

namespace TheTechIdea.Beep.NuGet
{
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
}
