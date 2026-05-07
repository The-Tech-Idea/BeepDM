using System.Collections.Generic;

namespace TheTechIdea.Beep.NuGet
{
    /// <summary>
    /// Represents a single search result from a NuGet query.
    /// </summary>
    public class PackageSearchResult
    {
        /// <summary>The package identifier.</summary>
        public string PackageId { get; set; }
        /// <summary>The latest version found.</summary>
        public string Version { get; set; }
        /// <summary>The package description.</summary>
        public string Description { get; set; }
        /// <summary>The package authors.</summary>
        public string Authors { get; set; }
        /// <summary>Total download count.</summary>
        public long? DownloadCount { get; set; }
        /// <summary>URL to the package icon.</summary>
        public string IconUrl { get; set; }
        /// <summary>URL to the project website.</summary>
        public string ProjectUrl { get; set; }
        /// <summary>List of tags.</summary>
        public List<string> Tags { get; set; } = new List<string>();
        /// <summary>True if this is a prerelease version.</summary>
        public bool IsPrerelease { get; set; }
        /// <summary>The source URL where this result was found.</summary>
        public string Source { get; set; }
    }
}
