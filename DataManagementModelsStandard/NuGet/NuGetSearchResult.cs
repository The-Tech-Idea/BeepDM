using System.Collections.Generic;

namespace TheTechIdea.Beep.NuGet
{
    /// <summary>
    /// Represents a NuGet package search result
    /// </summary>
    public class NuGetSearchResult
    {
        public string PackageId { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Authors { get; set; }
        public long TotalDownloads { get; set; }
        public string IconUrl { get; set; }
        public string ProjectUrl { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }
}
