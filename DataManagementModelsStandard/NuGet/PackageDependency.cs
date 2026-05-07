namespace TheTechIdea.Beep.NuGet
{
    /// <summary>
    /// Represents a dependency relationship between packages.
    /// </summary>
    public class PackageDependency
    {
        /// <summary>The dependent package identifier.</summary>
        public string PackageId { get; set; }
        /// <summary>The version range constraint (e.g., "[1.0.0, 2.0.0)").</summary>
        public string VersionRange { get; set; }
        /// <summary>The target framework for this dependency group.</summary>
        public string TargetFramework { get; set; }
        /// <summary>True if this is an optional dependency.</summary>
        public bool IsOptional { get; set; }
    }
}
