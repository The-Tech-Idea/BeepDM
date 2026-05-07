namespace TheTechIdea.Beep.NuGet
{
    /// <summary>
    /// Options for controlling NuGet package search behavior.
    /// </summary>
    public class SearchOptions
    {
        /// <summary>Number of results to skip (for pagination).</summary>
        public int Skip { get; set; } = 0;
        /// <summary>Maximum number of results to return.</summary>
        public int Take { get; set; } = 20;
        /// <summary>True to include prerelease versions.</summary>
        public bool IncludePrerelease { get; set; } = false;
        /// <summary>Optional specific source to search.</summary>
        public string Source { get; set; }
        /// <summary>Optional target framework filter.</summary>
        public string Framework { get; set; }
        /// <summary>True for exact name matching instead of substring search.</summary>
        public bool ExactMatch { get; set; } = false;
    }
}
