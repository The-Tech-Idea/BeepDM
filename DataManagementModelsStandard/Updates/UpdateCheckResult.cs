using System.Collections.Generic;

namespace TheTechIdea.Beep.Updates
{
    /// <summary>
    /// The outcome of <see cref="IAppUpdateService.CheckAsync"/>: the fetched feed compared
    /// against the installed state. Carries a named <see cref="Error"/> rather than surfacing a
    /// null feed, so a failed check is never mistaken for "no update available".
    /// </summary>
    public sealed class UpdateCheckResult
    {
        /// <summary>False when the feed could not be fetched/parsed; see <see cref="Error"/>.</summary>
        public bool Succeeded { get; set; }

        /// <summary>Human-readable reason the check failed (null on success).</summary>
        public string? Error { get; set; }

        /// <summary>The parsed feed (null when the check failed).</summary>
        public UpdateFeed? Feed { get; set; }

        /// <summary>The version the check compared against.</summary>
        public string CurrentVersion { get; set; } = "";

        /// <summary>The feed's latest version for this channel (null when the check failed).</summary>
        public string? LatestVersion { get; set; }

        /// <summary>True when the feed's latest version is newer than <see cref="CurrentVersion"/>.</summary>
        public bool AppUpdateAvailable { get; set; }

        /// <summary>
        /// True when the installed version predates the release's <c>minSupportedVersion</c>, so a
        /// delta cannot be applied and the full installer must be taken instead.
        /// </summary>
        public bool RequiresFullInstall { get; set; }

        /// <summary>Feed-pinned modules whose installed version differs and should be updated.</summary>
        public List<ModuleRef> StaleModules { get; set; } = new();

        /// <summary>True when anything is available to apply — an app update or any stale module.</summary>
        public bool AnyUpdateAvailable => AppUpdateAvailable || StaleModules.Count > 0;

        public static UpdateCheckResult Failed(string error, string currentVersion) => new()
        {
            Succeeded = false,
            Error = error,
            CurrentVersion = currentVersion
        };
    }
}
