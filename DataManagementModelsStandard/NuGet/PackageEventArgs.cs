using System;

namespace TheTechIdea.Beep.NuGet
{
    /// <summary>
    /// Event arguments for NuGet package lifecycle events.
    /// </summary>
    public class PackageEventArgs : EventArgs
    {
        /// <summary>The package identifier.</summary>
        public string PackageId { get; set; }
        /// <summary>The package version.</summary>
        public string Version { get; set; }
        /// <summary>Human-readable event message.</summary>
        public string Message { get; set; }
        /// <summary>UTC timestamp when the event occurred.</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
