using System.Collections.Generic;

namespace TheTechIdea.Beep.NuGet
{
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
}
