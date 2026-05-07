using System.Collections.Generic;

namespace TheTechIdea.Beep.NuGet
{
    /// <summary>
    /// Represents the result of a bulk uninstall operation.
    /// </summary>
    public class BulkUninstallResult
    {
        /// <summary>Total packages requested for uninstallation.</summary>
        public int TotalRequested { get; set; }
        /// <summary>Number of successful uninstallations.</summary>
        public int Successful { get; set; }
        /// <summary>Number of failed uninstallations.</summary>
        public int Failed { get; set; }
        /// <summary>Error messages for failed operations.</summary>
        public List<string> Errors { get; set; } = new List<string>();
    }
}
