using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.NuGet
{
    /// <summary>
    /// Represents a lock file for deterministic package restore.
    /// </summary>
    public class LockFileData
    {
        /// <summary>UTC timestamp when the lock file was generated.</summary>
        public DateTime GeneratedAt { get; set; }
        /// <summary>List of packages with exact versions.</summary>
        public List<LockFilePackage> Packages { get; set; } = new List<LockFilePackage>();
    }
}
