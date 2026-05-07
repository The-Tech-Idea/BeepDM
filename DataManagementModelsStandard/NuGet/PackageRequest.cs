namespace TheTechIdea.Beep.NuGet
{
    /// <summary>
    /// Represents a package installation request for bulk operations.
    /// </summary>
    public class PackageRequest
    {
        /// <summary>The package identifier.</summary>
        public string PackageId { get; set; }
        /// <summary>The desired version. If null, latest is used.</summary>
        public string Version { get; set; }
        /// <summary>Optional specific source to use.</summary>
        public string Source { get; set; }
    }
}
