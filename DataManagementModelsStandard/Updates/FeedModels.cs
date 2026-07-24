using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Updates
{
    /// <summary>
    /// The static update feed (<c>feed.json</c>) an installed app polls over HTTPS. Shape aligns
    /// with BeepDM's installer-service plan, extended with the two partial-update sections
    /// (<see cref="AppReleaseInfo.Delta"/> for file-level deltas, <see cref="Modules"/> for the
    /// NuGet module channel). No code runs server-side — this is a plain document at a stable URL.
    /// </summary>
    public sealed class UpdateFeed
    {
        public string Product { get; set; } = "";
        public string Channel { get; set; } = "stable";
        public AppReleaseInfo? Latest { get; set; }
        public List<ModuleRef> Modules { get; set; } = new();
    }

    /// <summary>The newest release on a channel, with both the full installer and the delta source.</summary>
    public sealed class AppReleaseInfo
    {
        public string Version { get; set; } = "";
        public DateTimeOffset? ReleasedAt { get; set; }

        /// <summary>
        /// Oldest installed version a delta can start from. When the installed version is older,
        /// the updater must take the <see cref="Full"/> install instead of a delta.
        /// </summary>
        public string? MinSupportedVersion { get; set; }

        public string? ReleaseNotes { get; set; }

        /// <summary>The complete Setup.exe — always present; the fallback when a delta is impossible.</summary>
        public ArtifactRef? Full { get; set; }

        /// <summary>Content-addressed delta source (payload manifest + blob base URL); optional.</summary>
        public DeltaRef? Delta { get; set; }
    }

    /// <summary>A downloadable artifact plus the SHA-256 that must match before it is used.</summary>
    public sealed class ArtifactRef
    {
        public string Url { get; set; } = "";
        public string Sha256 { get; set; } = "";
    }

    /// <summary>
    /// Delta source: the new release's <c>_payload-manifest.json</c> (path → blob sha) and the
    /// base URL under which each <c>_blobs/&lt;sha256&gt;</c> is fetched. The manifest is diffed
    /// against what is on disk so only missing blobs are downloaded.
    /// </summary>
    public sealed class DeltaRef
    {
        public string ManifestUrl { get; set; } = "";
        public string BlobBaseUrl { get; set; } = "";
    }

    /// <summary>
    /// A NuGet-packaged part of the app the feed pins to a specific version. The module channel
    /// updates these individually without reinstalling the app. <see cref="Required"/> modules
    /// block app start until updated.
    /// </summary>
    public sealed class ModuleRef
    {
        public string Id { get; set; } = "";
        public string Version { get; set; } = "";

        /// <summary>NuGet v3 index the package is pulled from (folder feed, GitHub Packages, etc.).</summary>
        public string? Feed { get; set; }

        public string? Sha256 { get; set; }
        public bool Required { get; set; }
    }
}
